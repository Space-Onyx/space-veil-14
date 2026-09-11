// SPDX-FileCopyrightText: 2026 Space Veil Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Veil.Genitals;
using Content.Shared.EntityEffects;
using Content.Shared.Mobs.Components;

namespace Content.Server._Veil.Genitals;

public sealed partial class ModifyGenitalSizeSystem : EntityEffectSystem<MobStateComponent, ModifyGenitalSize>
{
    [Dependency] private GenitalSystem _genitals = default!;
    [Dependency] private GenitalVisualSystem _visuals = default!;

    protected override void Effect(Entity<MobStateComponent> entity, ref EntityEffectEvent<ModifyGenitalSize> args)
    {
        if (!_genitals.TryGetGenital(entity, args.Effect.Category, out var organ) ||
            !TryComp(organ, out GenitalComponent? genital))
            return;

        var size = Math.Clamp(genital.Size + args.Effect.Amount * args.Scale, genital.MinSize, genital.MaxSize);
        if (Math.Abs(size - genital.Size) < 0.001f)
            return;

        genital.Size = size;
        _visuals.RefreshBody(entity);
    }
}
