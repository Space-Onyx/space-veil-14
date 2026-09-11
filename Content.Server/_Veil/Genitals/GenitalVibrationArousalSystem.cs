// SPDX-FileCopyrightText: 2026 Space Veil Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Veil.Genitals;

namespace Content.Server._Veil.Genitals;

public sealed partial class GenitalVibrationArousalSystem : EntitySystem
{
    [Dependency] private GenitalArousalSystem _arousalSys = default!;

    private const float ArousalIncreasePerSecond = 0.001f;
    private const float ArousalThreshold = 0.5f;

    private readonly Dictionary<EntityUid, float> _progress = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<GenitalEquipmentComponent>();
        while (query.MoveNext(out var uid, out var equipment))
        {
            if (equipment.Vibration <= 0)
                continue;

            var organ = Transform(uid).ParentUid;
            if (!TryComp(organ, out GenitalArousalComponent? arousal) || arousal.Aroused)
                continue;

            var progress = _progress.GetValueOrDefault(organ) + ArousalIncreasePerSecond * equipment.Vibration * frameTime;
            if (progress >= ArousalThreshold)
            {
                _progress.Remove(organ);
                _arousalSys.TrySetAroused(organ, true);
                continue;
            }

            _progress[organ] = progress;
        }
    }
}
