// SPDX-FileCopyrightText: 2026 Space Veil Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Administration.Logs;
using System.Linq;
using Content.Shared._Veil.Genitals;
using Content.Shared.Body;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server._Veil.Genitals;

public sealed partial class GenitalManagerSystem : SharedGenitalCoverageSystem
{
    [Dependency] private GenitalSystem _genitals = default!;
    [Dependency] private GenitalArousalSystem _arousal = default!;
    [Dependency] private GenitalVisualSystem _visuals = default!;
    [Dependency] private GenitalEquipmentSystem _equipment = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private GenitalFluidSystem _fluids = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SexualArousalComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<GenitalComponent, GenitalArousalChangedEvent>(OnArousalChanged);
        SubscribeLocalEvent<GenitalsChangedEvent>(OnGenitalsChanged);
        SubscribeLocalEvent<SexualArousalComponent, GetVerbsEvent<Verb>>(OnGetVerbs);

        Subs.BuiEvents<SexualArousalComponent>(GenitalManagerUiKey.Key, subs =>
        {
            subs.Event<GenitalSetVisibilityMessage>(OnSetVisibility);
            subs.Event<GenitalSetArousedMessage>(OnSetAroused);
            subs.Event<GenitalEquipmentRemoveMessage>(OnRemoveEquipment);
            subs.Event<GenitalSetSizeMessage>(OnSetSize);
        });
    }

    private float _uiRefreshTimer;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _uiRefreshTimer += frameTime;
        if (_uiRefreshTimer < 5f)
            return;

        _uiRefreshTimer = 0f;
        var query = EntityQueryEnumerator<SexualArousalComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_ui.IsUiOpen(uid, GenitalManagerUiKey.Key, uid))
                UpdateUi((uid, comp));
        }
    }

    private static readonly VerbCategory InsertCategory = new("genital-equipment-insert-category", null);
    private static readonly VerbCategory WearCategory = new("genital-equipment-wear-category", null);
    private static readonly VerbCategory ExpressCategory = new("genital-fluid-express-category", null);
    private static readonly VerbCategory RemoveCategory = new("genital-equipment-remove-category", null);

    private void OnUiOpened(Entity<SexualArousalComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (args.Actor != ent.Owner || !IsAlive(ent))
        {
            _ui.CloseUi(ent.Owner, GenitalManagerUiKey.Key, args.Actor);
            return;
        }

        UpdateUi(ent);
    }

    private void OnGetVerbs(Entity<SexualArousalComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        AddInsertVerbs(ent, ref args);
        AddRemoveVerbs(ent, ref args);

        if (args.User != ent.Owner || !IsAlive(ent))
            return;

        var user = args.User;
        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("genital-manager-title"),
            Act = () => OpenFor(user),
        });

        if (IsCoveredByClothes(ent.Owner, BodyPartType.Chest) ||
            IsCoveredByClothes(ent.Owner, BodyPartType.Groin) ||
            IsCoveredByUnderwear(ent.Owner, BodyPartType.Chest) ||
            IsCoveredByUnderwear(ent.Owner, BodyPartType.Groin))
            return;

        if (TryGetOwned(ent.Owner, "Breasts", out var breasts, out _) &&
            TryComp(breasts, out GenitalFluidComponent? milk) && milk.Amount >= 0.1f)
        {
            args.Verbs.Add(new Verb
            {
                Category = ExpressCategory,
                Text = Loc.GetString("genital-lactation-button"),
                Act = () => TryMilk((ent.Owner, ent.Comp), user),
            });
        }

        foreach (var category in new[] { "Testicles", "Vagina" })
        {
            if (!TryGetOwned(ent.Owner, category, out var organ, out _) ||
                !TryComp(organ, out GenitalFluidComponent? fluid) || fluid.Amount < 0.1f)
                continue;

            var captured = category;
            var capturedOrgan = organ;
            args.Verbs.Add(new Verb
            {
                Category = ExpressCategory,
                Text = Loc.GetString("genital-fluid-express-button",
                    ("organ", Loc.GetString($"ent-Genital{captured}"))),
                Act = () => TryExpress((ent.Owner, ent.Comp), user, captured, capturedOrgan),
            });
        }
    }

    private void AddInsertVerbs(Entity<SexualArousalComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanInteract || args.Using is not { } item)
            return;

        if (!TryComp(item, out GenitalEquipmentComponent? equipment) || !equipment.CanInsert)
            return;

        var user = args.User;
        var itemEnt = (item, equipment);
        var category = HasComp<CondomComponent>(item) ? WearCategory : InsertCategory;
        foreach (var (candidate, genital) in _genitals.GetGenitals(ent))
        {
            if (!IsGenitalAccessible(ent.Owner, genital.Category.Id, genital.Shape, genital.Size, genital.Visibility) ||
                !equipment.Slots.Contains(genital.Category) ||
                !_equipment.IsOrganFree(candidate))
                continue;

            var organ = candidate;
            args.Verbs.Add(new Verb
            {
                Category = category,
                Text = Loc.GetString($"ent-Genital{genital.Category.Id}"),
                Act = () => _equipment.TryStartEquip(itemEnt, ent.Owner, user, organ),
            });
        }
    }

    private void AddRemoveVerbs(Entity<SexualArousalComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanInteract)
            return;

        var user = args.User;
        foreach (var (organ, genital) in _genitals.GetGenitals(ent))
        {
            if (_equipment.GetEquipment(organ) is not { } item ||
                !IsGenitalAccessible(ent.Owner, genital.Category.Id, genital.Shape, genital.Size, genital.Visibility))
                continue;

            var itemName = Name(item);
            var organName = Loc.GetString($"ent-Genital{genital.Category.Id}");
            args.Verbs.Add(new Verb
            {
                Category = RemoveCategory,
                Text = Loc.GetString("genital-equipment-remove-verb", ("item", itemName), ("organ", organName)),
                Act = () => RemoveEquipment(ent, organ, itemName, user),
            });
        }
    }

    private void RemoveEquipment(Entity<SexualArousalComponent> ent, EntityUid organ, string itemName, EntityUid user)
    {
        if (!_equipment.TryRemove(organ, user))
            return;

        if (user == ent.Owner)
        {
            _popup.PopupEntity(Loc.GetString("genital-equipment-removed-felt", ("item", itemName)), ent, ent.Owner);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("genital-equipment-removed", ("item", itemName)), ent, user);
            _popup.PopupEntity(Loc.GetString("genital-equipment-removed-felt", ("item", itemName)), ent, ent.Owner);
        }

        SuppressVibrationPopup(ent.Owner);
        if (user != ent.Owner)
            SuppressVibrationPopup(user);
        UpdateUi(ent);
    }

    private void SuppressVibrationPopup(EntityUid body)
    {
        RaiseLocalEvent(body, new GenitalPopupShownEvent(body, 3f));
    }

    private void OnArousalChanged(Entity<GenitalComponent> ent, ref GenitalArousalChangedEvent args)
    {
        if (ent.Comp.Body.IsValid() && TryComp(ent.Comp.Body, out SexualArousalComponent? state))
            UpdateUi((ent.Comp.Body, state));
    }

    private void OnGenitalsChanged(GenitalsChangedEvent args)
    {
        var hasGenitals = _genitals.GetGenitals(args.Body).Any();
        if (hasGenitals)
        {
            var state = EnsureComp<SexualArousalComponent>(args.Body);
            EnsureUi(args.Body);
            UpdateUi((args.Body, state));
            return;
        }

        _ui.CloseUi(args.Body, GenitalManagerUiKey.Key);
        RemCompDeferred<SexualArousalComponent>(args.Body);
    }

    private void OnSetVisibility(Entity<SexualArousalComponent> ent, ref GenitalSetVisibilityMessage args)
    {
        if (!ValidateOwner(ent, args.Actor) || !Enum.IsDefined(args.Visibility) ||
            !TryGetOwned(ent, args.Category, out var organ, out var genital) ||
            genital.Visibility == args.Visibility)
            return;

        genital.Visibility = args.Visibility;
        FinishChange(ent, organ, args.Category, $"visibility to {args.Visibility}", refreshVisuals: true);
    }

    private void OnSetAroused(Entity<SexualArousalComponent> ent, ref GenitalSetArousedMessage args)
    {
        if (!ValidateOwner(ent, args.Actor) ||
            !TryGetOwned(ent, args.Category, out var organ, out _) ||
            !_arousal.TrySetAroused(organ, args.Aroused))
            return;

        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(ent):player} set {args.Category} arousal to {args.Aroused}");
        UpdateUi(ent);
    }

    private void OnRemoveEquipment(Entity<SexualArousalComponent> ent, ref GenitalEquipmentRemoveMessage args)
    {
        if (!ValidateOwner(ent, args.Actor) ||
            !TryGetOwned(ent, args.Category, out var organ, out _) ||
            _equipment.GetEquipment(organ) is not { } item)
            return;

        var itemName = Name(item);
        if (!_equipment.TryRemove(organ, ent.Owner))
            return;

        _popup.PopupEntity(Loc.GetString("genital-equipment-removed-felt", ("item", itemName)), ent, ent.Owner);
        SuppressVibrationPopup(ent.Owner);
        UpdateUi(ent);
    }

    private void OnSetSize(Entity<SexualArousalComponent> ent, ref GenitalSetSizeMessage args)
    {
        if (!ValidateOwner(ent, args.Actor) ||
            !_genitals.CanResize(ent.Owner) ||
            !float.IsFinite(args.Size) ||
            !TryGetOwned(ent, args.Category, out var organ, out var genital))
            return;

        var size = Math.Clamp(args.Size, genital.MinSize, genital.MaxSize);
        if (Math.Abs(size - genital.Size) < 0.001f)
            return;

        genital.Size = size;
        _visuals.RefreshBody(ent);
        FinishChange(ent, organ, args.Category, $"size to {size:F1}");
    }

    private void TryMilk(Entity<SexualArousalComponent> ent, EntityUid actor)
    {
        if (actor != ent.Owner || !IsAlive(ent) ||
            !TryGetOwned(ent, "Breasts", out var breasts, out _) ||
            !_fluids.TryExpress(ent.Owner, breasts, out var amount, out _, out var intoCondom))
        {
            _popup.PopupEntity(Loc.GetString("genital-lactation-failed"), ent, actor);
            SuppressVibrationPopup(ent.Owner);
            return;
        }

        if (!intoCondom)
            _popup.PopupEntity(Loc.GetString("genital-lactation-success", ("amount", amount.ToString("F1"))), ent, actor);
        SuppressVibrationPopup(ent.Owner);
        _adminLog.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(ent):player} milked {amount:F1}u from breasts");
        UpdateUi(ent);
    }

    private void TryExpress(Entity<SexualArousalComponent> ent, EntityUid actor, string category, EntityUid organ)
    {
        if (actor != ent.Owner || !IsAlive(ent) ||
            !_fluids.TryExpress(ent.Owner, organ, out var amount, out var fluid, out var intoCondom))
        {
            _popup.PopupEntity(Loc.GetString("genital-fluid-express-failed"), ent, actor);
            SuppressVibrationPopup(ent.Owner);
            return;
        }

        if (!intoCondom)
            _popup.PopupEntity(Loc.GetString("genital-fluid-express-success",
                ("amount", amount.ToString("F1")), ("fluid", fluid)), ent, actor);
        SuppressVibrationPopup(ent.Owner);
        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(ent):player} expressed {amount:F1}u of {fluid} from {category}");
        UpdateUi(ent);
    }

    public void EnsureUi(EntityUid body)
    {
        if (!_ui.HasUi(body, GenitalManagerUiKey.Key))
        {
            var userInterface = EnsureComp<UserInterfaceComponent>(body);
            _ui.SetUi((body, userInterface), GenitalManagerUiKey.Key,
                new InterfaceData("GenitalManagerBoundUserInterface"));
        }
    }

    private void OpenFor(EntityUid user)
    {
        EnsureUi(user);
        _ui.TryOpenUi(user, GenitalManagerUiKey.Key, user);
        if (TryComp(user, out SexualArousalComponent? state))
            UpdateUi((user, state));
    }

    private bool ValidateOwner(Entity<SexualArousalComponent> ent, EntityUid actor)
    {
        return actor == ent.Owner && IsAlive(ent) && _ui.IsUiOpen(ent.Owner, GenitalManagerUiKey.Key, actor);
    }

    private bool TryGetOwned(
        EntityUid body,
        string categoryId,
        out EntityUid organ,
        out GenitalComponent genital)
    {
        organ = default;
        genital = default!;
        var category = new ProtoId<GenitalCategoryPrototype>(categoryId);
        if (!_genitals.TryGetGenital(body, category, out organ) ||
            !TryComp(organ, out GenitalComponent? found))
            return false;

        genital = found;
        return true;
    }

    private void FinishChange(
        Entity<SexualArousalComponent> body,
        EntityUid organ,
        string category,
        string change,
        bool refreshVisuals = false)
    {
        if (refreshVisuals)
            _visuals.RefreshBody(body);

        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(body):player} set {category} {change} on {ToPrettyString(organ)}");
        UpdateUi(body);
    }

    private void UpdateUi(Entity<SexualArousalComponent> ent)
    {
        if (!_ui.IsUiOpen(ent.Owner, GenitalManagerUiKey.Key, ent.Owner))
            return;

        var entries = new List<GenitalManagerEntry>();
        var canResize = _genitals.CanResize(ent.Owner);
        foreach (var (organ, genital) in _genitals.GetGenitals(ent))
        {
            var canArouse = TryComp(organ, out GenitalArousalComponent? arousal) &&
                GenitalVisualBuilder.SupportsArousal(_prototypeManager, genital.Category);
            var previewRsi = string.Empty;
            var previewState = string.Empty;
            if (GenitalVisualBuilder.TryFindBest(_prototypeManager, genital.Category, genital.Shape, genital.Size, out var visual) &&
                !visual.Internal)
            {
                previewRsi = visual.Rsi;
                previewState = GenitalVisualBuilder.BuildState(visual, genital.Size, genital.UseSkinColor, arousal?.Aroused ?? false);
            }
            float? fluidAmount = null;
            float? fluidCapacity = null;
            if (_fluids.TryGetAmount(organ, genital.Size, out var genitalFluid, out var genitalCapacity))
            {
                fluidAmount = genitalFluid;
                fluidCapacity = genitalCapacity;
            }

            entries.Add(new GenitalManagerEntry(
                genital.Category.Id,
                genital.Size,
                genital.Visibility,
                canArouse,
                arousal?.Aroused ?? false,
                _equipment.GetEquipment(organ) is { } item ? Name(item) : null,
                canResize && genital.MaxSize > genital.MinSize,
                genital.MinSize,
                genital.MaxSize,
                previewRsi,
                previewState,
                genital.Color,
                fluidAmount,
                fluidCapacity,
                GetFluidName(organ)));
        }

        entries.Sort((a, b) => string.Compare(a.Category, b.Category, StringComparison.Ordinal));
        _ui.SetUiState(ent.Owner, GenitalManagerUiKey.Key,
            new GenitalManagerUiState(entries));
    }

    private bool IsAlive(EntityUid body)
    {
        return TryComp(body, out MobStateComponent? mob) && mob.CurrentState == MobState.Alive;
    }

    private string? GetFluidName(EntityUid organ)
    {
        string? reagent = null;
        if (TryComp(organ, out GenitalFluidComponent? genitalFluid))
            reagent = genitalFluid.ReagentId;

        if (reagent == null)
            return null;

        return _prototypeManager.TryIndex<ReagentPrototype>(reagent, out var prototype)
            ? prototype.LocalizedName
            : reagent;
    }
}
