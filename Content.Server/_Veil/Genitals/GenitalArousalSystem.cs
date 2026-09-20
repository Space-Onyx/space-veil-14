// SPDX-FileCopyrightText: 2026 Space Veil Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Administration.Logs;
using Content.Server.Chat.Systems;
using Content.Shared._Veil.Genitals;
using Content.Shared.Humanoid;
using Content.Shared.Database;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Veil.Genitals;

public sealed partial class GenitalArousalSystem : EntitySystem
{
    [Dependency] private GenitalSystem _genitals = default!;
    [Dependency] private GenitalEquipmentSystem _equipment = default!;
    [Dependency] private GenitalVisualSystem _visuals = default!;
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;

    private static readonly TimeSpan MoanCooldown = TimeSpan.FromSeconds(6);

    public bool CanSetAroused(EntityUid organ, bool aroused)
    {
        if (!TryComp(organ, out GenitalArousalComponent? state))
            return false;

        if (aroused && IsErpDisabled(organ))
            return false;

        return state.Aroused != aroused && (!aroused || !_equipment.PreventsArousal(organ));
    }

    public bool TrySetAroused(EntityUid organ, bool aroused)
    {
        if (!CanSetAroused(organ, aroused))
            return false;

        SetAroused(organ, aroused);
        return true;
    }

    public void SetAroused(EntityUid organ, bool aroused)
    {
        var state = EnsureComp<GenitalArousalComponent>(organ);
        if (state.Aroused == aroused)
            return;

        state.Aroused = aroused;
        Dirty(organ, state);

        var ev = new GenitalArousalChangedEvent(organ, aroused);
        RaiseLocalEvent(organ, ref ev);

        if (TryComp(organ, out GenitalComponent? genital) && genital.Body.IsValid())
            _visuals.RefreshBody(genital.Body);
    }

    public void SetAllAroused(EntityUid body, bool aroused, bool log = true)
    {
        if (aroused && TryComp(body, out HumanoidProfileComponent? profile) && profile.ErpStatus == ErpStatus.No)
            return;

        var changed = false;
        foreach (var (organ, _) in _genitals.GetGenitals(body))
        {
            if (TrySetAroused(organ, aroused))
                changed = true;
        }

        if (changed && log)
            _adminLog.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(body)} set genital arousal to {aroused}");
    }

    public bool TryMoan(EntityUid body, int intensity)
    {
        if (intensity <= 0 || IsErpDisabledBody(body) ||
            !TryComp(body, out SexualArousalComponent? arousal) || _timing.CurTime < arousal.NextMoan)
            return false;

        var chance = Math.Clamp(intensity, 1, 3) switch
        {
            1 => 0.2f,
            2 => 0.4f,
            _ => 0.65f,
        };
        if (!_random.Prob(chance) || !_chat.TryEmoteWithChat(body, "Moan"))
            return false;

        arousal.NextMoan = _timing.CurTime + MoanCooldown;
        return true;
    }

    private bool IsErpDisabled(EntityUid organ)
    {
        if (TryComp(organ, out GenitalComponent? genital) && genital.Body.IsValid())
            return TryComp(genital.Body, out HumanoidProfileComponent? profile) && profile.ErpStatus == ErpStatus.No;

        return false;
    }

    private bool IsErpDisabledBody(EntityUid body)
    {
        return TryComp(body, out HumanoidProfileComponent? profile) && profile.ErpStatus == ErpStatus.No;
    }
}
