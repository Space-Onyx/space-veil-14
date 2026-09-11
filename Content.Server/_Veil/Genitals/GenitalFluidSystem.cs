// SPDX-FileCopyrightText: 2026 Space Veil Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Veil.Genitals;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Veil.Genitals;

public sealed partial class GenitalFluidSystem : EntitySystem
{
    [Dependency] private GenitalSystem _genitals = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private PuddleSystem _puddle = default!;
    [Dependency] private GenitalEquipmentSystem _equipment = default!;

    private const float CondomCapacity = 30f;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<GenitalFluidComponent, GenitalComponent>();
        while (query.MoveNext(out _, out var fluid, out var genital))
        {
            var capacity = GetCapacity(genital.Size);
            fluid.Amount = Math.Min(capacity, fluid.Amount + fluid.ProductionPerSecond * frameTime);
        }
    }

    public static float GetCapacity(float size)
    {
        return Math.Max(5f, size * 2.5f);
    }

    public bool TryGetAmount(EntityUid organ, float size, out float amount, out float capacity)
    {
        capacity = GetCapacity(size);
        if (!TryComp(organ, out GenitalFluidComponent? fluid))
        {
            amount = 0f;
            return false;
        }

        amount = fluid.Amount;
        return true;
    }

    public bool TryExpress(EntityUid user, EntityUid organ, out float amount, out string fluidName, out bool intoCondom)
    {
        amount = 0f;
        fluidName = string.Empty;
        intoCondom = false;
        if (!TryComp(organ, out GenitalFluidComponent? fluid) ||
            fluid.Amount < 0.1f ||
            _timing.CurTime < fluid.NextExpress)
            return false;

        var release = Math.Min(2f, fluid.Amount);
        if (TryGetPenisCondom(user, out var condom, out var stored))
        {
            if (stored.Amount >= CondomCapacity)
                return false;

            release = Math.Min(release, CondomCapacity - stored.Amount);
            stored.ReagentId = fluid.ReagentId;
            stored.Amount += release;
            fluid.Amount -= release;
            amount = release;
            intoCondom = true;
            fluid.NextExpress = _timing.CurTime + TimeSpan.FromSeconds(2);
            fluidName = _prototypes.TryIndex<ReagentPrototype>(fluid.ReagentId, out var storedReagent)
                ? storedReagent.LocalizedName
                : fluid.ReagentId;
            _equipment.SyncCondom(condom);
            _popup.PopupEntity(Loc.GetString("genital-fluid-condom-filled"), user, user);
            return true;
        }
        if (_hands.TryGetActiveItem(user, out var container) &&
            _solutions.TryGetFitsInDispenser(container.Value, out var solution, out _))
        {
            _solutions.TryAddReagent(solution.Value, fluid.ReagentId, FixedPoint2.New(release), out var accepted);
            if (accepted <= FixedPoint2.Zero)
                return false;

            amount = accepted.Float();
        }
        else
        {
            _puddle.TrySpillAt(user, new Solution(fluid.ReagentId, FixedPoint2.New(release)), out _, sound: false);
            amount = release;
        }

        fluid.NextExpress = _timing.CurTime + TimeSpan.FromSeconds(2);

        fluid.Amount -= amount;
        fluidName = _prototypes.TryIndex<ReagentPrototype>(fluid.ReagentId, out var reagent)
            ? reagent.LocalizedName
            : fluid.ReagentId;
        return true;
    }

    private bool TryGetPenisCondom(EntityUid user, out EntityUid condom, out GenitalFluidComponent stored)
    {
        condom = default;
        stored = default!;
        foreach (var (candidate, genital) in _genitals.GetGenitals(user))
        {
            if (genital.Category.Id != "Penis")
                continue;

            if (_equipment.GetEquipment(candidate) is not { } item ||
                Prototype(item)?.ID != "SexToyCondom" ||
                !TryComp(item, out GenitalFluidComponent? fluid))
                continue;

            condom = item;
            stored = fluid;
            return true;
        }

        return false;
    }
}
