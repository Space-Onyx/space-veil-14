// Space Veil
// Copyright (C) 2026 Space Veil contributors
//
// This file is licensed under AGPL-3.0-or-later.
// See LICENSES for the full license text.

using Content.Shared._Veil.Genitals;
using Content.Shared.EntityEffects;
using Content.Shared.Humanoid;
using Robust.Shared.Timing;

namespace Content.Server._Veil.Genitals;

public sealed partial class IncreaseChemicalArousalSystem
    : EntityEffectSystem<SexualArousalComponent, IncreaseChemicalArousal>
{
    [Dependency] private GenitalArousalSystem _arousal = default!;
    [Dependency] private GenitalSystem _genitals = default!;
    [Dependency] private IGameTiming _timing = default!;

    private const float ArousalThreshold = 10f;
    private const float DecayPerSecond = 0.3f;
    private static readonly TimeSpan DecayDelay = TimeSpan.FromSeconds(2);

    protected override void Effect(Entity<SexualArousalComponent> entity,
        ref EntityEffectEvent<IncreaseChemicalArousal> args)
    {
        if (TryComp(entity, out HumanoidProfileComponent? profile) && profile.ErpStatus == ErpStatus.No)
            return;

        entity.Comp.ChemicalArousal = Math.Clamp(
            entity.Comp.ChemicalArousal + args.Effect.Amount * args.Scale,
            0f,
            args.Effect.Maximum);
        entity.Comp.LastChemicalArousalAt = _timing.CurTime;
        EnsureComp<ChemicalArousalVisualComponent>(entity);
        UpdateArousal(entity);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SexualArousalComponent>();
        while (query.MoveNext(out var uid, out var arousal))
        {
            if (TryComp(uid, out HumanoidProfileComponent? profile) && profile.ErpStatus == ErpStatus.No)
            {
                arousal.ChemicalArousal = 0f;
                UpdateArousal((uid, arousal));
                continue;
            }

            if (arousal.ChemicalArousal <= 0f)
                continue;

            if (_timing.CurTime - arousal.LastChemicalArousalAt <= DecayDelay)
                continue;

            arousal.ChemicalArousal = Math.Max(0f, arousal.ChemicalArousal - DecayPerSecond * frameTime);
            UpdateArousal((uid, arousal));
        }
    }

    private void UpdateArousal(Entity<SexualArousalComponent> entity)
    {
        if (entity.Comp.ChemicalArousal <= 0f)
            RemComp<ChemicalArousalVisualComponent>(entity);

        var aroused = entity.Comp.ChemicalArousal >= ArousalThreshold;
        if (entity.Comp.ChemicallyAroused == aroused)
            return;

        entity.Comp.ChemicallyAroused = aroused;
        if (aroused)
        {
            EnsureComp<ChemicalArousalVisualComponent>(entity);
            foreach (var (organ, _) in _genitals.GetGenitals(entity))
            {
                if (_arousal.TrySetAroused(organ, true))
                    entity.Comp.ChemicallyArousedOrgans.Add(organ);
            }
            return;
        }

        foreach (var organ in entity.Comp.ChemicallyArousedOrgans)
            _arousal.TrySetAroused(organ, false);
        entity.Comp.ChemicallyArousedOrgans.Clear();
    }
}
