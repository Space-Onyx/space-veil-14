// SPDX-FileCopyrightText: 2026 Space Veil Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Administration.Logs;
using System.Linq;
using Content.Server.Fluids.EntitySystems;
using Content.Shared._Veil.Genitals;
using Content.Shared.Body;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Jittering;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server._Veil.Genitals;

public sealed partial class GenitalEquipmentSystem : SharedGenitalCoverageSystem
{
    [Dependency] private GenitalSystem _genitals = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private GenitalArousalSystem _genitalArousal = default!;
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedJitteringSystem _jitter = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private PuddleSystem _puddle = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;

    private const string EquipmentContainer = "genital-equipment";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GenitalEquipmentComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<GenitalEquipmentComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<GenitalEquipmentComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<GenitalEquipmentComponent, GenitalEquipDoAfterEvent>(OnEquipDoAfter);
        SubscribeLocalEvent<CondomComponent, LandEvent>(OnCondomLand);
        SubscribeLocalEvent<SignalVibratorToggleComponent, SignalReceivedEvent>(OnSignalVibratorToggle);

        Subs.BuiEvents<GenitalEquipmentComponent>(GenitalCustomizeUiKey.Key, subs =>
        {
            subs.Event<GenitalCustomizeShapeMessage>(OnCustomizeShape);
            subs.Event<GenitalCustomizeSizeMessage>(OnCustomizeSize);
            subs.Event<GenitalCustomizeColorMessage>(OnCustomizeColor);
            subs.Event<GenitalCustomizeVibrationMessage>(OnCustomizeVibration);
        });
    }

    private void OnGetVerbs(Entity<GenitalEquipmentComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanInteract || (!ent.Comp.Customizable && !ent.Comp.HasVibration))
            return;

        var user = args.User;
        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("genital-equipment-customize"),
            Act = () => OpenCustomize(ent, user),
        });
    }

    private void OpenCustomize(Entity<GenitalEquipmentComponent> ent, EntityUid user)
    {
        var userInterface = EnsureComp<UserInterfaceComponent>(ent);
        _ui.SetUi((ent.Owner, userInterface), GenitalCustomizeUiKey.Key,
            new InterfaceData("GenitalCustomizeBoundUserInterface"));
        _ui.TryOpenUi(ent.Owner, GenitalCustomizeUiKey.Key, user);
    }

    private void OnCustomizeShape(Entity<GenitalEquipmentComponent> ent, ref GenitalCustomizeShapeMessage args)
    {
        if (!ent.Comp.Customizable || !GenitalCustomizationData.Shapes.Contains(args.Shape))
            return;

        ent.Comp.Shape = args.Shape;
        Dirty(ent);
        _popup.PopupEntity(Loc.GetString("genital-equipment-shape-changed",
            ("shape", Loc.GetString($"genital-shape-{ent.Comp.Shape}"))), ent, args.Actor);
    }

    private void OnCustomizeSize(Entity<GenitalEquipmentComponent> ent, ref GenitalCustomizeSizeMessage args)
    {
        if (!ent.Comp.CanInsert ||
            args.Size is < GenitalCustomizationData.MinSize or > GenitalCustomizationData.MaxSize ||
            args.Size == ent.Comp.SizeStage)
            return;

        ent.Comp.SizeStage = args.Size;
        Dirty(ent);
        _popup.PopupEntity(Loc.GetString("genital-equipment-resized", ("size", args.Size)), ent, args.Actor);
    }

    private void OnCustomizeColor(Entity<GenitalEquipmentComponent> ent, ref GenitalCustomizeColorMessage args)
    {
        if (!ent.Comp.Customizable || (uint) args.Color >= (uint) GenitalCustomizationData.Colors.Length)
            return;

        ent.Comp.Color = GenitalCustomizationData.Colors[args.Color];
        Dirty(ent);
        _popup.PopupEntity(Loc.GetString("genital-equipment-color-changed"), ent, args.Actor);
    }

    private void OnCustomizeVibration(Entity<GenitalEquipmentComponent> ent, ref GenitalCustomizeVibrationMessage args)
    {
        if (!ent.Comp.HasVibration ||
            args.Level is < GenitalCustomizationData.MinVibration or > GenitalCustomizationData.MaxVibration)
            return;

        ent.Comp.Vibration = args.Level;
        Dirty(ent);
        _popup.PopupEntity(Loc.GetString("genital-equipment-vibration-set",
            ("mode", Loc.GetString($"genital-vibration-{ent.Comp.Vibration}"))), ent, args.Actor);
    }

    private void OnAfterInteract(Entity<GenitalEquipmentComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target is not { } target || !args.CanReach)
            return;

        if (!ent.Comp.CanUse)
        {
            _popup.PopupEntity(Loc.GetString("genital-equipment-use-insert-menu"), ent, args.User);
            args.Handled = true;
            return;
        }

        args.Handled = TryUse(ent, target, args.User);
    }

    private void OnUseInHand(Entity<GenitalEquipmentComponent> ent, ref UseInHandEvent args)
    {
        if (TryUnwrap(ent, args.User))
        {
            args.Handled = true;
            return;
        }

        if (TryCycleVibration(ent, args.User))
            args.Handled = true;
    }

    public bool TryCycleVibration(Entity<GenitalEquipmentComponent> ent, EntityUid user)
    {
        if (!ent.Comp.HasVibration)
            return false;

        ent.Comp.Vibration = ent.Comp.Vibration >= GenitalCustomizationData.MaxVibration
            ? GenitalCustomizationData.MinVibration
            : ent.Comp.Vibration + 1;
        Dirty(ent);
        _popup.PopupEntity(Loc.GetString("genital-equipment-vibration-set",
            ("mode", Loc.GetString($"genital-vibration-{ent.Comp.Vibration}"))), ent, user);
        return true;
    }

    private void OnSignalVibratorToggle(Entity<SignalVibratorToggleComponent> ent, ref SignalReceivedEvent args)
    {
        if (!TryComp(ent, out GenitalEquipmentComponent? equipment))
            return;

        equipment.Vibration = equipment.Vibration == 0 ? 1 : 0;
        Dirty(ent.Owner, equipment);
    }

    private void OnCondomLand(Entity<CondomComponent> ent, ref LandEvent args)
    {
        if (!ent.Comp.Unwrapped)
            return;

        if (!TryComp(ent, out GenitalFluidComponent? fluid) || fluid.Amount < 0.1f)
            return;

        var release = fluid.Amount;
        fluid.Amount = 0f;
        _puddle.TrySpillAt(ent.Owner, new Solution(fluid.ReagentId, FixedPoint2.New(release)), out _, sound: false);
        SyncCondom(ent.Owner);
    }

    private bool TryUnwrap(Entity<GenitalEquipmentComponent> ent, EntityUid user)
    {
        if (!TryComp(ent, out CondomComponent? condom) || condom.Unwrapped)
        {
            if (ent.Comp.WrappedState == null || !ent.Comp.Wrapped)
                return false;

            ent.Comp.Wrapped = false;
            Dirty(ent);
            _audio.PlayPvs("/Audio/_Veil/Interactions/poster_ripped.ogg", ent.Owner);
            _popup.PopupEntity(Loc.GetString("genital-equipment-unwrapped"), ent, user);
            return true;
        }

        condom.Unwrapped = true;
        Dirty(ent);
        SyncCondom(ent.Owner);
        _audio.PlayPvs("/Audio/_Veil/Interactions/poster_ripped.ogg", ent.Owner);
        _popup.PopupEntity(Loc.GetString("genital-condom-unwrapped"), ent, user);
        return true;
    }

    public void SyncCondom(EntityUid item)
    {
        if (!TryComp(item, out CondomComponent? condom))
            return;

        var stage = CondomFill.Wrapped;
        if (condom.Unwrapped)
        {
            stage = CondomFill.Empty;
            if (TryComp(item, out GenitalFluidComponent? fluid))
            {
                stage = fluid.Amount switch
                {
                    >= 24f => CondomFill.Huge,
                    >= 12f => CondomFill.Large,
                    >= 6f => CondomFill.Medium,
                    > 0f => CondomFill.Inflated,
                    _ => CondomFill.Empty,
                };
            }
        }

        _appearance.SetData(item, CondomVisuals.Fill, stage);
    }

    public bool TryEquip(Entity<GenitalEquipmentComponent> equipment, EntityUid body, EntityUid user, EntityUid? targetOrgan = null)
    {
        if (!equipment.Comp.CanInsert)
            return false;

        if (equipment.Comp.Wrapped)
        {
            _popup.PopupEntity(Loc.GetString("genital-equipment-wrapped"), equipment, user);
            return false;
        }

        if (TryComp(equipment, out CondomComponent? condom) && !condom.Unwrapped)
        {
            _popup.PopupEntity(Loc.GetString("genital-condom-wrapped"), equipment, user);
            return false;
        }

        EntityUid organ;
        if (targetOrgan is { } chosen)
        {
            if (!TryComp(chosen, out GenitalComponent? chosenGenital) ||
                !equipment.Comp.Slots.Contains(chosenGenital.Category) ||
                !IsGenitalAccessible(body, chosenGenital.Category.Id, chosenGenital.Shape, chosenGenital.Size, chosenGenital.Visibility) ||
                (_containers.TryGetContainer(chosen, EquipmentContainer, out var chosenContainer) &&
                    chosenContainer is ContainerSlot { ContainedEntity: not null }))
            {
                _popup.PopupEntity(Loc.GetString("genital-equipment-cannot-equip"), body, user);
                return false;
            }

            organ = chosen;
        }
        else if (!CanEquip(equipment, body, user, out organ))
        {
            _popup.PopupEntity(Loc.GetString("genital-equipment-cannot-equip"), body, user);
            return false;
        }

        var container = _containers.EnsureContainer<ContainerSlot>(organ, EquipmentContainer);
        if (!_containers.Insert(equipment.Owner, container))
            return false;

        if (equipment.Comp.PreventsArousal)
            _genitalArousal.TrySetAroused(organ, false);

        if (HasComp<CondomComponent>(equipment))
            _audio.PlayPvs("/Audio/_Veil/Interactions/latex.ogg", body);
        else
            _audio.PlayPredicted(new SoundCollectionSpecifier("LewdSquelch"), body, user);

        if (user == body)
        {
            _popup.PopupEntity(Loc.GetString("genital-equipment-equipped-felt", ("item", equipment.Owner)), body, body);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("genital-equipment-equipped", ("item", equipment.Owner)), body, user);
            _popup.PopupEntity(Loc.GetString("genital-equipment-equipped-felt", ("item", equipment.Owner)), body, body);
        }

        RaiseLocalEvent(body, new GenitalPopupShownEvent(body, 3f));
        if (user != body)
            RaiseLocalEvent(user, new GenitalPopupShownEvent(user, 3f));

        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(user):player} equipped {ToPrettyString(equipment)} on {ToPrettyString(body):player}");
        RaiseLocalEvent(new GenitalsChangedEvent(body));
        return true;
    }

    public bool IsOrganFree(EntityUid organ)
    {
        return !_containers.TryGetContainer(organ, EquipmentContainer, out var found) ||
            found is not ContainerSlot { ContainedEntity: not null };
    }

    public bool TryStartEquip(Entity<GenitalEquipmentComponent> equipment, EntityUid body, EntityUid user, EntityUid? targetOrgan = null)
    {
        if (!equipment.Comp.CanInsert ||
            equipment.Comp.Wrapped ||
            (TryComp(equipment, out CondomComponent? condom) && !condom.Unwrapped) ||
            (targetOrgan is null && !CanEquip(equipment, body, user, out _)))
        {
            return TryEquip(equipment, body, user, targetOrgan);
        }

        var wearable = HasComp<CondomComponent>(equipment);
        if (user == body)
        {
            _popup.PopupEntity(Loc.GetString(
                wearable ? "genital-condom-equip-attempt-self" : "genital-equipment-insert-attempt-self",
                ("item", equipment.Owner)), body, user);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString(
                wearable ? "genital-condom-equip-attempt" : "genital-equipment-insert-attempt",
                ("user", user), ("item", equipment.Owner), ("target", body)), body, PopupType.Large);
        }

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(equipment.Comp.InsertDelay),
            new GenitalEquipDoAfterEvent { TargetOrgan = targetOrgan is { } organ ? GetNetEntity(organ) : null },
            equipment.Owner, target: body, used: equipment.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 0.01f,
            NeedHand = true
        });

        return true;
    }

    private void OnEquipDoAfter(Entity<GenitalEquipmentComponent> ent, ref GenitalEquipDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is not { } target)
            return;

        EntityUid? organ = null;
        if (args.TargetOrgan is { } net && TryGetEntity(net, out var resolved))
            organ = resolved;

        TryEquip(ent, target, args.User, organ);
        args.Handled = true;
    }

    public bool TryUse(Entity<GenitalEquipmentComponent> equipment, EntityUid body, EntityUid user)
    {
        if (_timing.CurTime < equipment.Comp.NextUse ||
            !CanUse(equipment, body, user, out var organ))
        {
            _popup.PopupEntity(Loc.GetString("genital-equipment-cannot-use"), body, user);
            return false;
        }

        var alreadyAroused = TryComp(organ, out GenitalArousalComponent? arousal) && arousal.Aroused;
        if (!alreadyAroused && !_genitalArousal.TrySetAroused(organ, true))
        {
            _popup.PopupEntity(Loc.GetString("genital-equipment-cannot-use"), body, user);
            return false;
        }

        equipment.Comp.NextUse = _timing.CurTime + TimeSpan.FromSeconds(1);

        var intense = equipment.Comp.SizeStage >= 3 || equipment.Comp.Vibration >= 3;

        if (user == body)
        {
            _popup.PopupEntity(Loc.GetString(
                intense ? "genital-equipment-used-self-intense" : "genital-equipment-used-self",
                ("user", user), ("item", equipment.Owner)), body, PopupType.Small);
        }
        else
        {
            _popup.PopupEntity(
                Loc.GetString(intense ? "genital-equipment-felt-intense" : "genital-equipment-felt"),
                Loc.GetString(intense ? "genital-equipment-used-intense" : "genital-equipment-used",
                    ("user", user), ("item", equipment.Owner), ("target", body)),
                body,
                body,
                PopupType.Small);
        }

        RaiseLocalEvent(body, new GenitalPopupShownEvent(body, 3f));

        if (user != body)
            RaiseLocalEvent(user, new GenitalPopupShownEvent(user, 3f));

        if (intense)
        {
            _jitter.DoJitter(body, TimeSpan.FromSeconds(2), true);
            if (equipment.Comp.Vibration >= 3)
                _stun.TryAddStunDuration(body, TimeSpan.FromSeconds(2));
        }

        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(user):player} used {ToPrettyString(equipment)} on {ToPrettyString(body):player}");
        return true;
    }

    public bool CanEquip(Entity<GenitalEquipmentComponent> equipment, EntityUid body, EntityUid user, out EntityUid organ)
    {
        organ = default;

        foreach (var (candidate, genital) in _genitals.GetGenitals(body))
        {
            if (!IsGenitalAccessible(body, genital.Category.Id, genital.Shape, genital.Size, genital.Visibility) ||
                !equipment.Comp.Slots.Contains(genital.Category) ||
                !IsOrganFree(candidate))
                continue;

            organ = candidate;
            return true;
        }

        return false;
    }

    public bool CanUse(Entity<GenitalEquipmentComponent> equipment, EntityUid body, EntityUid user, out EntityUid organ)
    {
        organ = default;

        if (!equipment.Comp.CanUse)
            return false;

        foreach (var (candidate, genital) in _genitals.GetGenitals(body))
        {
            if (!IsGenitalAccessible(body, genital.Category.Id, genital.Shape, genital.Size, genital.Visibility) ||
                !equipment.Comp.Slots.Contains(genital.Category) ||
                !HasComp<GenitalArousalComponent>(candidate))
                continue;

            organ = candidate;
            return true;
        }

        return false;
    }

    public bool TryRemove(EntityUid organ, EntityUid user)
    {
        if (!_containers.TryGetContainer(organ, EquipmentContainer, out var found) ||
            found is not ContainerSlot { ContainedEntity: { } item } container ||
            !_containers.Remove(item, container))
            return false;

        if (!_hands.TryPickupAnyHand(user, item))
            _transform.SetCoordinates(item, Transform(user).Coordinates);

        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(user):player} removed {ToPrettyString(item)} from {ToPrettyString(organ)}");
        return true;
    }

    public EntityUid? GetEquipment(EntityUid organ)
    {
        return _containers.TryGetContainer(organ, EquipmentContainer, out var found) && found is ContainerSlot slot
            ? slot.ContainedEntity
            : null;
    }

    public bool PreventsArousal(EntityUid organ)
    {
        return GetEquipment(organ) is { } item &&
            TryComp(item, out GenitalEquipmentComponent? equipment) &&
            equipment.PreventsArousal;
    }
}
