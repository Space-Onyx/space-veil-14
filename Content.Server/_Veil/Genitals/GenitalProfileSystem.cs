// SPDX-FileCopyrightText: 2026 Space Veil Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared._Onyx.Body.Systems;
using Content.Shared._Veil.Genitals;
using Content.Shared.Body;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;

namespace Content.Server._Veil.Genitals;

public sealed partial class GenitalProfileSystem : EntitySystem
{
    [Dependency] private GenitalSystem _genitals = default!;
    [Dependency] private GenitalManagerSystem _manager = default!;
    [Dependency] private GenitalVisualSystem _visuals = default!;
    [Dependency] private GenitalEquipmentSystem _equipment = default!;
    [Dependency] private SharedBodySystem _body = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HumanoidProfileComponent, HumanoidProfileAppliedEvent>(OnProfileApplied);
        SubscribeLocalEvent<PendingGenitalProfileComponent, BodyGraphInitializedEvent>(OnBodyGraphInitialized);
    }

    private void OnProfileApplied(Entity<HumanoidProfileComponent> ent, ref HumanoidProfileAppliedEvent args)
    {
        var pending = EnsureComp<PendingGenitalProfileComponent>(ent);
        pending.Genitals = args.Profile.ErpStatus == ErpStatus.No
            ? []
            : args.Profile.Genitals.ToDictionary(entry => entry.Key, entry => new GenitalProfileData(entry.Value));
        pending.Enabled = args.Profile.ErpStatus != ErpStatus.No;
        pending.SkinColor = args.Profile.Appearance.SkinColor;
        pending.Species = args.Profile.Species;
        pending.Sex = args.Profile.Sex;
        TryApply(ent, pending);
    }

    private void OnBodyGraphInitialized(Entity<PendingGenitalProfileComponent> ent, ref BodyGraphInitializedEvent args)
    {
        TryApply(ent, ent.Comp);
    }

    private void TryApply(EntityUid body, PendingGenitalProfileComponent pending)
    {
        if (!pending.Enabled)
        {
            _manager.CloseFor(body);
            RemCompDeferred<PendingGenitalProfileComponent>(body);
            _visuals.RefreshBody(body);
            return;
        }

        var groins = _body.GetBodyChildrenOfType(body, BodyPartType.Groin).ToList();
        if (groins.Count != 1)
            return;

        foreach (var (category, data) in pending.Genitals)
        {
            if (!GenitalRestrictions.IsCategoryAllowed(_prototypes, pending.Species.Id, pending.Sex, category))
                continue;

            if (data.Present)
            {
                if (_genitals.TryGetGenital(body, category, out var existing))
                {
                    if (HasComp<ProfileGeneratedGenitalComponent>(existing))
                        ApplyProfileData(existing, data, pending.SkinColor, pending.Species.Id);
                    continue;
                }

                if (_genitals.TryCreateGenital(body, category, out var created))
                {
                    EnsureComp<ProfileGeneratedGenitalComponent>(created);
                    ApplyProfileData(created, data, pending.SkinColor, pending.Species.Id);
                }
                continue;
            }

            if (!_genitals.TryGetGenital(body, category, out var absent) ||
                !HasComp<ProfileGeneratedGenitalComponent>(absent))
                continue;

            _equipment.Eject(absent, body);
            if (!_genitals.TryRemoveGenital(body, category, out var removed))
                continue;

            QueueDel(removed);
        }

        if (_genitals.GetGenitals(body).Any())
        {
            EnsureComp<SexualArousalComponent>(body);
            _manager.EnsureUi(body);
        }

        RemCompDeferred<PendingGenitalProfileComponent>(body);
        _visuals.RefreshBody(body);
    }

    private void ApplyProfileData(EntityUid organ, GenitalProfileData data, Color skinColor, string speciesId)
    {
        if (!TryComp(organ, out GenitalComponent? genital))
            return;

        genital.Shape = data.Shape;
        genital.UseSkinColor = data.UseSkinColor;
        genital.Color = data.UseSkinColor ? skinColor : data.Color;
        genital.Size = data.Size;
        genital.MinSize = data.MinSize;
        genital.MaxSize = data.MaxSize;
        genital.Visibility = data.Visibility;

        var category = _prototypes.Index(genital.Category);
        if (!category.ProducesFluid)
        {
            RemCompDeferred<GenitalFluidComponent>(organ);
            return;
        }

        if (category.FluidOptional && !data.Lactating)
        {
            RemCompDeferred<GenitalFluidComponent>(organ);
            return;
        }

        var fluid = EnsureComp<GenitalFluidComponent>(organ);
        var allowed = GenitalRestrictions.AllowedFluids(_prototypes, speciesId, genital.Category);
        fluid.ReagentId = allowed.Contains(data.FluidId)
            ? data.FluidId
            : GenitalRestrictions.DefaultFluid(_prototypes, speciesId, genital.Category);
    }
}

[RegisterComponent]
public sealed partial class PendingGenitalProfileComponent : Component
{
    public bool Enabled;

    public Dictionary<ProtoId<GenitalCategoryPrototype>, GenitalProfileData> Genitals = [];

    public Color SkinColor = Color.White;

    public ProtoId<SpeciesPrototype> Species = "Human";

    public Sex Sex;
}

[RegisterComponent]
public sealed partial class ProfileGeneratedGenitalComponent : Component;
