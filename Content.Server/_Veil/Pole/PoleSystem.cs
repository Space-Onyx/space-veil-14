// SPDX-FileCopyrightText: 2026 Space Veil Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Veil.Pole;
using Content.Shared.Buckle.Components;
using Content.Shared.Foldable;
using Content.Shared.Jittering;
using Content.Shared.Popups;
using Content.Shared.Tools.Components;
using Content.Shared.Verbs;

namespace Content.Server._Veil.Pole;

public sealed partial class PoleSystem : SharedPoleSystem
{
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedJitteringSystem _jitter = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private FoldableSystem _foldable = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PoleComponent, GetVerbsEvent<Verb>>(OnGetDanceVerbs);
        SubscribeLocalEvent<PoleComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<PoleComponent, UnstrappedEvent>(OnUnstrapped);
        SubscribeLocalEvent<FoldableComponent, FoldedEvent>(OnFoldableFolded);
        SubscribeLocalEvent<PoleComponent, AttemptSimpleToolUseEvent>(OnToolAttempt);
        SubscribeLocalEvent<PoleComponent, SimpleToolDoAfterEvent>(OnToolFinished);
    }

    private void OnGetDanceVerbs(Entity<PoleComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || ent.Comp.InUse)
            return;

        if (TryComp(ent, out FoldableComponent? foldable) && foldable.IsFolded)
            return;

        var user = args.User;
        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("pole-dance-verb"),
            Act = () => TryStartDance(ent, user, user),
        });
    }

    public bool TryStartDance(Entity<PoleComponent> ent, EntityUid dancer, EntityUid? initiator = null)
    {
        if (TryComp(ent, out FoldableComponent? foldable) && foldable.IsFolded)
            return false;

        if (ent.Comp.InUse)
        {
            if (initiator is { } source)
                _popup.PopupEntity(Loc.GetString("pole-dance-busy"), ent, source);
            return false;
        }

        return Buckle.TryBuckle(dancer, initiator ?? dancer, ent);
    }

    private void OnStrapped(Entity<PoleComponent> ent, ref StrappedEvent args)
    {
        ent.Comp.InUse = true;
        _jitter.AddJitter(args.Buckle.Owner, 10f, 4f);
        _popup.PopupEntity(Loc.GetString("pole-dance-start", ("user", args.Buckle.Owner), ("pole", ent.Owner)), ent, PopupType.Large);
    }

    private void OnUnstrapped(Entity<PoleComponent> ent, ref UnstrappedEvent args)
    {
        ent.Comp.InUse = false;
        RemComp<JitteringComponent>(args.Buckle.Owner);
    }

    private void OnToolAttempt(Entity<PoleComponent> ent, ref AttemptSimpleToolUseEvent args)
    {
        if (ent.Comp.InUse)
            args.Cancelled = true;
    }

    private void OnToolFinished(Entity<PoleComponent> ent, ref SimpleToolDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || ent.Comp.InUse || !TryComp(ent, out FoldableComponent? foldable))
            return;

        ent.Comp.ToolFold = true;
        var changed = _foldable.TryToggleFold(ent, foldable, args.User);
        ent.Comp.ToolFold = false;

        if (changed)
            args.Handled = true;
    }

    private void OnFoldableFolded(Entity<FoldableComponent> ent, ref FoldedEvent args)
    {
        if (!HasComp<PoleComponent>(ent))
            return;

        if (args.IsFolded)
            _transform.Unanchor(ent.Owner);
        else
            _transform.AnchorEntity(ent.Owner);
    }
}
