// SPDX-FileCopyrightText: 2026 Space Veil Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Veil.Genitals;
using Content.Shared.Humanoid;
using Content.Server.Fluids.EntitySystems;
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
    private const float UpdateInterval = 1f;

    private float _updateAccumulator;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _updateAccumulator += frameTime;
        if (_updateAccumulator < UpdateInterval)
            return;

        var elapsed = _updateAccumulator;
        _updateAccumulator = 0f;
        var query = EntityQueryEnumerator<GenitalFluidComponent, GenitalComponent>();
        while (query.MoveNext(out _, out var fluid, out var genital))
        {
            if (genital.Body.IsValid() && IsErpDisabled(genital.Body))
                continue;

            var category = _prototypes.Index(genital.Category);
            var capacity = GetCapacity(category, genital.Size);
            fluid.Amount = Math.Min(capacity, fluid.Amount + category.FluidProductionPerSecond * elapsed);
        }
    }

    public static float GetCapacity(GenitalCategoryPrototype category, float size)
    {
        return Math.Max(0f, category.FluidCapacityBase + size * category.FluidCapacityPerSize);
    }

    public bool TryGetAmount(EntityUid organ, float size, out float amount, out float capacity)
    {
        if (!TryComp(organ, out GenitalComponent? genital))
        {
            amount = 0f;
            capacity = 0f;
            return false;
        }

        capacity = GetCapacity(_prototypes.Index(genital.Category), size);
        if (!TryComp(organ, out GenitalFluidComponent? fluid))
        {
            amount = 0f;
            return false;
        }

        amount = fluid.Amount;
        return true;
    }

    public void ClampToCapacity(EntityUid organ, float size)
    {
        if (!TryComp(organ, out GenitalComponent? genital) ||
            !TryComp(organ, out GenitalFluidComponent? fluid))
            return;

        fluid.Amount = Math.Min(fluid.Amount, GetCapacity(_prototypes.Index(genital.Category), size));
    }

    public bool TryExpress(EntityUid user, EntityUid organ, out float amount, out string fluidName, out bool intoCondom)
    {
        amount = 0f;
        fluidName = string.Empty;
        intoCondom = false;
        if (IsErpDisabled(user))
            return false;

        if (!TryComp(organ, out GenitalComponent? genital) || genital.Body != user || IsErpDisabled(genital.Body))
            return false;

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
                !HasComp<CondomComponent>(item) ||
                !TryComp(item, out GenitalFluidComponent? fluid))
                continue;

            condom = item;
            stored = fluid;
            return true;
        }

        return false;
    }

    private bool IsErpDisabled(EntityUid uid)
    {
        return TryComp(uid, out HumanoidProfileComponent? profile) && profile.ErpStatus == ErpStatus.No;
    }
}
