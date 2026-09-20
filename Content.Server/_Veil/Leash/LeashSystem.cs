// SPDX-FileCopyrightText: 2026 Space Veil Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Veil.Leash;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Utility;

namespace Content.Server._Veil.Leash;

public sealed partial class LeashSystem : EntitySystem
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedJointSystem _joints = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private static readonly SpriteSpecifier RopeSprite = new SpriteSpecifier.Rsi(
        new ResPath("/Textures/Objects/Weapons/Guns/Launchers/grappling_gun.rsi"),
        "rope");

    private float _distanceCheckAccumulator;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LeashComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<LeashComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<LeashComponent, DroppedEvent>(OnDropped);
        SubscribeLocalEvent<LeashComponent, EntGotInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<LeashComponent, ComponentShutdown>(OnLeashShutdown);
        SubscribeLocalEvent<LeashedComponent, ComponentShutdown>(OnLeashedShutdown);
        SubscribeLocalEvent<LeashedComponent, EntGotInsertedIntoContainerMessage>(OnLeashedInserted);
        SubscribeLocalEvent<CollarComponent, GotUnequippedEvent>(OnCollarUnequipped);
    }

    private void OnAfterInteract(Entity<LeashComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        args.Handled = TryAttach(ent.Owner, args.User, target, ent.Comp);
    }

    private void OnUseInHand(Entity<LeashComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled || ent.Comp.AttachedEntity == null)
            return;

        args.Handled = TryDetach(ent.Owner, args.User, ent.Comp);
    }

    private void OnDropped(Entity<LeashComponent> ent, ref DroppedEvent args)
    {
        if (ent.Comp.AttachedEntity != null)
            TryReanchor(ent, ent.Owner);
    }

    private void OnInserted(Entity<LeashComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (ent.Comp.AttachedEntity == null)
            return;

        if (_hands.IsHolding(args.Container.Owner, ent.Owner, out _))
        {
            TryReanchor(ent, args.Container.Owner);
            return;
        }

        var target = ent.Comp.AttachedEntity;
        TryDetach(ent.Owner, leash: ent.Comp);
        if (target is { } leashed)
            _popup.PopupEntity(Loc.GetString("leash-detached-storage"), leashed, leashed);
    }

    private void OnLeashedInserted(Entity<LeashedComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        TryDetach(ent.Comp.Leash);
    }

    private void OnCollarUnequipped(Entity<CollarComponent> ent, ref GotUnequippedEvent args)
    {
        if (TryComp(args.EquipTarget, out LeashedComponent? leashed))
            TryDetach(leashed.Leash);
    }

    private void OnLeashShutdown(Entity<LeashComponent> ent, ref ComponentShutdown args)
    {
        TryDetach(ent.Owner, leash: ent.Comp);
    }

    private void OnLeashedShutdown(Entity<LeashedComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp(ent.Comp.Leash, out LeashComponent? leash) && leash.AttachedEntity == ent.Owner)
        {
            RemoveJoint(leash);
            leash.AttachedEntity = null;
            leash.Anchor = null;
            RemComp<JointVisualsComponent>(ent.Comp.Leash);
        }
    }

    public bool TryAttach(EntityUid leashUid, EntityUid user, EntityUid target, LeashComponent? leash = null)
    {
        if (!Resolve(leashUid, ref leash) || leash.AttachedEntity != null)
            return false;

        if (TryComp(target, out CollarComponent? _) &&
            _containers.TryGetContainingContainer(target, out var collarContainer))
        {
            target = collarContainer.Owner;
        }

        if (target == user)
        {
            _popup.PopupEntity(Loc.GetString("leash-cannot-attach-self"), user, user);
            return false;
        }

        if (!TryGetCollar(target, out _) ||
            HasComp<LeashedComponent>(target) ||
            !HasComp<PhysicsComponent>(user) ||
            !HasComp<PhysicsComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("leash-cannot-attach"), target, user);
            return false;
        }

        leash.AttachedEntity = target;
        var leashed = EnsureComp<LeashedComponent>(target);
        leashed.Leash = leashUid;

        if (!TryReanchor((leashUid, leash), user))
        {
            RemComp<LeashedComponent>(target);
            leash.AttachedEntity = null;
            return false;
        }

        _popup.PopupEntity(Loc.GetString("leash-attached", ("target", target)), target, user);
        return true;
    }

    public bool TryDetach(EntityUid leashUid, EntityUid? user = null, LeashComponent? leash = null)
    {
        if (!Resolve(leashUid, ref leash) || leash.AttachedEntity is not { } target)
            return false;

        ClearConnection((leashUid, leash), target);
        if (user is { } actor)
            _popup.PopupEntity(Loc.GetString("leash-detached"), actor, actor);
        return true;
    }

    private bool TryReanchor(Entity<LeashComponent> leash, EntityUid anchor)
    {
        if (leash.Comp.AttachedEntity is not { } target ||
            anchor == target ||
            !HasComp<PhysicsComponent>(anchor) ||
            !HasComp<PhysicsComponent>(target))
        {
            RemoveJoint(leash.Comp);
            TryDetach(leash.Owner, leash: leash.Comp);
            return false;
        }

        RemoveJoint(leash.Comp);
        leash.Comp.Anchor = anchor;
        leash.Comp.JointId = $"leash-{GetNetEntity(leash.Owner)}";

        var joint = _joints.CreateDistanceJoint(anchor, target, id: leash.Comp.JointId!);
        joint.CollideConnected = false;
        joint.MaxLength = leash.Comp.MaxDistance;
        joint.MinLength = 0f;
        joint.Stiffness = 0f;

        var visuals = EnsureComp<JointVisualsComponent>(leash.Owner);
        visuals.Sprite = RopeSprite;
        visuals.Target = target;
        Dirty(leash.Owner, visuals);
        return true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _distanceCheckAccumulator += frameTime;
        if (_distanceCheckAccumulator < 0.25f)
            return;

        _distanceCheckAccumulator = 0f;
        var query = EntityQueryEnumerator<LeashComponent>();
        while (query.MoveNext(out var uid, out var leash))
        {
            if (leash.AttachedEntity is not { } target || leash.Anchor is not { } anchor)
                continue;

            if (!TryComp(anchor, out TransformComponent? anchorXform) ||
                !TryComp(target, out TransformComponent? targetXform) ||
                !HasComp<PhysicsComponent>(anchor) ||
                !HasComp<PhysicsComponent>(target) ||
                anchorXform.MapID != targetXform.MapID)
            {
                Snap((uid, leash), target);
                continue;
            }

            var difference = _transform.GetWorldPosition(anchorXform) - _transform.GetWorldPosition(targetXform);
            var snapDistance = leash.MaxDistance + 1.5f;
            if (difference.LengthSquared() > snapDistance * snapDistance)
                Snap((uid, leash), target);
        }
    }

    private bool TryGetCollar(EntityUid target, out EntityUid collar)
    {
        if (HasComp<CollarComponent>(target))
        {
            collar = target;
            return true;
        }

        if (_inventory.TryGetSlotEntity(target, "neck", out var equipped) &&
            HasComp<CollarComponent>(equipped.Value))
        {
            collar = equipped.Value;
            return true;
        }

        collar = default;
        return false;
    }

    private void ClearConnection(Entity<LeashComponent> leash, EntityUid target)
    {
        RemoveJoint(leash.Comp);
        leash.Comp.AttachedEntity = null;
        leash.Comp.Anchor = null;
        RemComp<JointVisualsComponent>(leash.Owner);

        if (LifeStage(target) < EntityLifeStage.Terminating)
            RemCompDeferred<LeashedComponent>(target);
    }

    private void Snap(Entity<LeashComponent> leash, EntityUid target)
    {
        ClearConnection(leash, target);
        if (LifeStage(target) < EntityLifeStage.Terminating)
            _popup.PopupEntity(Loc.GetString("leash-snapped"), target, target);
    }

    private void RemoveJoint(LeashComponent leash)
    {
        if (leash.JointId == null || leash.Anchor is not { } anchor)
            return;

        if (LifeStage(anchor) < EntityLifeStage.Terminating)
            _joints.RemoveJoint(anchor, leash.JointId);

        leash.JointId = null;
    }
}
