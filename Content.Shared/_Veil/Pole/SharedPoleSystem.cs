// SPDX-FileCopyrightText: 2026 Space Veil Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Foldable;
using Content.Shared.Verbs;

namespace Content.Shared._Veil.Pole;

public abstract partial class SharedPoleSystem : EntitySystem
{
    [Dependency] protected SharedBuckleSystem Buckle = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PoleComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAlternativeVerbs, after: new[] { typeof(FoldableSystem) });
        SubscribeLocalEvent<PoleComponent, FoldedEvent>(OnFolded);
        SubscribeLocalEvent<PoleComponent, StrapAttemptEvent>(OnStrapAttempt);
    }

    private void OnGetAlternativeVerbs(Entity<PoleComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!TryComp(ent, out FoldableComponent? foldable))
            return;

        var foldText = Loc.GetString(foldable.IsFolded ? foldable.UnfoldVerbText : foldable.FoldVerbText);
        args.Verbs.RemoveWhere(verb => verb.Text == foldText);
    }

    private void OnFolded(Entity<PoleComponent> ent, ref FoldedEvent args)
    {
        if (HasComp<StrapComponent>(ent))
            Buckle.StrapSetEnabled(ent.Owner, false);
    }

    private void OnStrapAttempt(Entity<PoleComponent> ent, ref StrapAttemptEvent args)
    {
        if (TryComp(ent, out FoldableComponent? foldable) && foldable.IsFolded)
            args.Cancelled = true;
    }
}
