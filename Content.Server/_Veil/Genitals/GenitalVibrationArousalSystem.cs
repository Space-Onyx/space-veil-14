// SPDX-FileCopyrightText: 2026 Space Veil Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Veil.Genitals;
using Content.Shared.Humanoid;
using Content.Shared.Jittering;
using Content.Shared.Popups;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Veil.Genitals;

public sealed partial class GenitalVibrationArousalSystem : EntitySystem
{
    [Dependency] private GenitalArousalSystem _arousalSys = default!;
    [Dependency] private SharedJitteringSystem _jitter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;

    private const float ArousalIncreasePerSecond = 0.001f;
    private const float ArousalThreshold = 0.5f;

    private static readonly string[] WeakPulses =
    [
        "genital-vibration-pulse-weak-1",
        "genital-vibration-pulse-weak-2",
        "genital-vibration-pulse-weak-3",
    ];

    private static readonly string[] MediumPulses =
    [
        "genital-vibration-pulse-medium-1",
        "genital-vibration-pulse-medium-2",
        "genital-vibration-pulse-medium-3",
    ];

    private static readonly string[] StrongPulses =
    [
        "genital-vibration-pulse-strong-1",
        "genital-vibration-pulse-strong-2",
        "genital-vibration-pulse-strong-3",
    ];

    private readonly Dictionary<EntityUid, float> _progress = new();
    private readonly Dictionary<EntityUid, TimeSpan> _nextPulse = new();
    private readonly Dictionary<EntityUid, TimeSpan> _nextMessage = new();
    private readonly Dictionary<EntityUid, int> _pulses = new();
    private readonly Dictionary<EntityUid, TimeSpan> _suppressedUntil = new();
    private readonly HashSet<EntityUid> _activeOrgans = [];
    private readonly List<EntityUid> _stale = [];

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GenitalPopupShownEvent>(OnPopupShown);
    }

    private void OnPopupShown(GenitalPopupShownEvent args)
    {
        if (args.SuppressSeconds <= 0f)
            return;

        _suppressedUntil[args.Body] = _timing.CurTime + TimeSpan.FromSeconds(args.SuppressSeconds);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _pulses.Clear();
        _activeOrgans.Clear();
        var query = EntityQueryEnumerator<GenitalEquipmentComponent>();
        while (query.MoveNext(out var uid, out var equipment))
        {
            if (equipment.Vibration <= 0)
                continue;

            var organ = Transform(uid).ParentUid;
            if (!TryComp(organ, out GenitalComponent? genital) || !genital.Body.IsValid())
                continue;

            var body = genital.Body;

            if (IsErpDisabled(body))
                continue;

            _activeOrgans.Add(organ);

            if (TryComp(organ, out GenitalArousalComponent? arousal) && !arousal.Aroused)
            {
                var progress = _progress.GetValueOrDefault(organ) + ArousalIncreasePerSecond * equipment.Vibration * frameTime;
                if (progress >= ArousalThreshold)
                {
                    _progress.Remove(organ);
                    _arousalSys.TrySetAroused(organ, true);
                }
                else
                {
                    _progress[organ] = progress;
                }
            }

            _pulses[body] = Math.Max(_pulses.GetValueOrDefault(body), equipment.Vibration);
        }

        foreach (var (body, vibration) in _pulses)
        {
            if (IsErpDisabled(body))
                continue;

            if (!_nextPulse.TryGetValue(body, out var nextPulse) || _timing.CurTime >= nextPulse)
            {
                _nextPulse[body] = _timing.CurTime + PulseInterval(vibration);
                Jitter(body, vibration);
                _arousalSys.TryMoan(body, vibration);
            }

            if (_suppressedUntil.TryGetValue(body, out var until) && _timing.CurTime < until)
                continue;

            if (_nextMessage.TryGetValue(body, out var nextMessage) && _timing.CurTime < nextMessage)
                continue;

            _nextMessage[body] = _timing.CurTime + MessageInterval(vibration);
            Message(body, vibration);
        }

        RemoveInactive(_progress, _activeOrgans.Contains);
        RemoveInactive(_nextPulse, _pulses.ContainsKey);
        RemoveInactive(_nextMessage, _pulses.ContainsKey);
        RemoveInactive(_suppressedUntil, _pulses.ContainsKey);
    }

    private void Jitter(EntityUid body, int vibration)
    {
        var (duration, amplitude, frequency) = vibration switch
        {
            1 => (TimeSpan.FromSeconds(1.2), 3f, 3f),
            2 => (TimeSpan.FromSeconds(1.8), 6f, 5f),
            _ => (TimeSpan.FromSeconds(2.5), 10f, 8f),
        };

        _jitter.DoJitter(body, duration, true, amplitude, frequency);
    }

    private void Message(EntityUid body, int vibration)
    {
        var messages = vibration switch
        {
            1 => WeakPulses,
            2 => MediumPulses,
            _ => StrongPulses,
        };
        _popup.PopupEntity(Loc.GetString(_random.Pick(messages)), body, body);
    }

    private static TimeSpan PulseInterval(int vibration)
    {
        return vibration switch
        {
            1 => TimeSpan.FromSeconds(4),
            2 => TimeSpan.FromSeconds(3),
            _ => TimeSpan.FromSeconds(2),
        };
    }

    private static TimeSpan MessageInterval(int vibration)
    {
        return vibration switch
        {
            1 => TimeSpan.FromSeconds(6),
            2 => TimeSpan.FromSeconds(5),
            _ => TimeSpan.FromSeconds(4),
        };
    }

    private void RemoveInactive<T>(Dictionary<EntityUid, T> entries, Func<EntityUid, bool> isActive)
    {
        _stale.Clear();
        foreach (var uid in entries.Keys)
        {
            if (!isActive(uid))
                _stale.Add(uid);
        }

        foreach (var uid in _stale)
            entries.Remove(uid);
    }

    private bool IsErpDisabled(EntityUid uid)
    {
        return TryComp(uid, out HumanoidProfileComponent? profile) && profile.ErpStatus == ErpStatus.No;
    }
}
