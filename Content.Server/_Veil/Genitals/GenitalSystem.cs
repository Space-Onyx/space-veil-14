// SPDX-FileCopyrightText: 2026 Space Veil Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Veil.Genitals;
using Content.Shared.Body;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.Server._Veil.Genitals;

public sealed partial class GenitalSystem : EntitySystem
{
    [Dependency] private SharedBodySystem _body = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private GenitalVisualSystem _visuals = default!;
    [Dependency] private MetaDataSystem _meta = default!;

    private const string GenitalsContainer = "veil-genitals";

    private static readonly EntProtoId GenitalPrototype = "Genital";
    private static readonly ProtoId<GenitalCategoryPrototype> Penis = "Penis";
    private static readonly ProtoId<GenitalCategoryPrototype> Testicles = "Testicles";
    private static readonly ProtoId<GenitalCategoryPrototype> Vagina = "Vagina";
    private static readonly ProtoId<GenitalCategoryPrototype> Breasts = "Breasts";
    private static readonly ProtoId<GenitalCategoryPrototype> Butt = "Butt";
    private static readonly ProtoId<GenitalCategoryPrototype> Anus = "Anus";

    public override void Initialize()
    {
        base.Initialize();
    }

    public IEnumerable<(EntityUid Id, GenitalComponent Component)> GetGenitals(EntityUid body)
    {
        foreach (var (partId, _) in _body.GetBodyChildren(body))
        {
            if (!_containers.TryGetContainer(partId, GenitalsContainer, out var container))
                continue;

            foreach (var uid in container.ContainedEntities)
            {
                if (TryComp(uid, out GenitalComponent? genital))
                    yield return (uid, genital);
            }
        }
    }

    private void Refresh(Entity<GenitalComponent> ent, EntityUid body)
    {
        ApplyDetachedPresentation(ent);
        _visuals.RefreshBody(body);
        RaiseLocalEvent(new GenitalsChangedEvent(body));
    }

    public bool TryGetGenital(EntityUid body, ProtoId<GenitalCategoryPrototype> category, out EntityUid organ)
    {
        organ = default;
        foreach (var (uid, genital) in GetGenitals(body))
        {
            if (genital.Category != category)
                continue;

            organ = uid;
            return true;
        }

        return false;
    }

    public bool CanResize(EntityUid body)
    {
        return TryComp(body, out HumanoidProfileComponent? profile) &&
            GenitalRestrictions.Config(_prototypes, profile.Species.Id).AllowRuntimeSize;
    }

    public bool TryCreateGenital(
        EntityUid body,
        ProtoId<GenitalCategoryPrototype> category,
        out EntityUid genital)
    {
        genital = default;
        if (TryGetGenital(body, category, out _))
            return false;

        var partType = category == Breasts ? BodyPartType.Chest : BodyPartType.Groin;
        EntityUid? target = null;
        foreach (var (candidate, _) in _body.GetBodyChildrenOfType(body, partType))
        {
            if (target != null)
                return false;

            target = candidate;
        }

        if (target == null)
            return false;

        var part = target.Value;
        var spawned = Spawn(GenitalPrototype, Transform(body).Coordinates);
        if (!TryConfigureGenital(spawned, category))
        {
            QueueDel(spawned);
            return false;
        }

        var container = _containers.EnsureContainer<Container>(part, GenitalsContainer);
        if (!_containers.Insert(spawned, container))
        {
            QueueDel(spawned);
            return false;
        }

        var comp = Comp<GenitalComponent>(spawned);
        comp.Body = body;

        genital = spawned;
        Refresh((spawned, comp), body);
        return true;
    }

    public bool TryRemoveGenital(EntityUid body, ProtoId<GenitalCategoryPrototype> category, out EntityUid organ)
    {
        organ = default;
        if (!TryGetGenital(body, category, out var found))
            return false;

        foreach (var (partId, _) in _body.GetBodyChildren(body))
        {
            if (!_containers.TryGetContainer(partId, GenitalsContainer, out var container) ||
                !container.Contains(found))
                continue;

            _containers.Remove(found, container);
            break;
        }

        organ = found;
        _visuals.RefreshBody(body);
        RaiseLocalEvent(new GenitalsChangedEvent(body));
        return true;
    }

    public bool TryConfigureGenital(
        EntityUid organ,
        ProtoId<GenitalCategoryPrototype> category)
    {
        if (!_prototypes.HasIndex(category))
            return false;

        var genital = EnsureComp<GenitalComponent>(organ);
        genital.Category = category;
        genital.Shape = GenitalProfileData.Default(category).Shape;
        ApplyArousalCapability(organ, category);
        if (category == Breasts || category == Testicles || category == Vagina)
            EnsureComp<GenitalFluidComponent>(organ);
        else
            RemCompDeferred<GenitalFluidComponent>(organ);
        return true;
    }

    private void ApplyArousalCapability(EntityUid organ, ProtoId<GenitalCategoryPrototype> category)
    {
        if (category == Penis || category == Testicles || category == Vagina || category == Breasts || category == Butt || category == Anus)
        {
            EnsureComp<GenitalArousalComponent>(organ);
            return;
        }

        RemCompDeferred<GenitalArousalComponent>(organ);
    }

    private void ApplyDetachedPresentation(Entity<GenitalComponent> ent)
    {
        var (rsi, state) = ResolveDetachedSprite(ent.Comp);
        var visual = EnsureComp<DetachedGenitalVisualComponent>(ent);
        visual.Rsi = rsi;
        visual.State = state;
        visual.Color = ent.Comp.Color;
        Dirty(ent, visual);

        _meta.SetEntityName(ent, Loc.GetString($"ent-Genital{ent.Comp.Category.Id}"));
        _meta.SetEntityDescription(ent, Loc.GetString($"ent-Genital{ent.Comp.Category.Id}-desc"));
    }

    private (string Rsi, string State) ResolveDetachedSprite(GenitalComponent genital)
    {
        if (GenitalVisualBuilder.TryFindBest(_prototypes, genital.Category, genital.Shape, genital.Size, out var visual) &&
            !string.IsNullOrEmpty(visual.DetachedRsi) && visual.DetachedStates.Count > 0)
        {
            var state = GenitalVisualBuilder.BuildDetachedState(visual, genital.Size, genital.UseSkinColor);
            if (string.IsNullOrEmpty(state))
                state = visual.DetachedStates[0];

            return (visual.DetachedRsi, state);
        }

        return ("Mobs/Species/Human/organs.rsi", "appendix");
    }
}

public readonly record struct GenitalsChangedEvent(EntityUid Body);
