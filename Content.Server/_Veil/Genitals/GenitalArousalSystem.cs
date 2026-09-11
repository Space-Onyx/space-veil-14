// Space Veil
// Copyright (C) 2026 Space Veil contributors
//
// This file is licensed under AGPL-3.0-or-later.
// See LICENSES for the full license text.

using Content.Server.Administration.Logs;
using Content.Shared._Veil.Genitals;
using Content.Shared.Body;
using Content.Shared.Body.Systems;
using Content.Shared.Database;

namespace Content.Server._Veil.Genitals;

public sealed partial class GenitalArousalSystem : EntitySystem
{
    [Dependency] private GenitalSystem _genitals = default!;
    [Dependency] private GenitalEquipmentSystem _equipment = default!;
    [Dependency] private GenitalVisualSystem _visuals = default!;
    [Dependency] private IAdminLogManager _adminLog = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public bool CanSetAroused(EntityUid organ, bool aroused)
    {
        if (!TryComp(organ, out GenitalArousalComponent? state))
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
        var changed = false;
        foreach (var (organ, _) in _genitals.GetGenitals(body))
        {
            if (TrySetAroused(organ, aroused))
                changed = true;
        }

        if (changed && log)
            _adminLog.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(body)} set genital arousal to {aroused}");
    }
}
