// SPDX-FileCopyrightText: 2026 Space Veil Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Veil.Genitals;
using Robust.Client.GameObjects;

namespace Content.Client._Veil.Genitals;

public sealed partial class CondomVisualizerSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private AppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CondomComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<CondomComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnInit(Entity<CondomComponent> ent, ref ComponentInit args)
    {
        UpdateAppearance(ent);
    }

    private void OnAppearanceChange(Entity<CondomComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        UpdateAppearance((ent.Owner, ent.Comp, args.Sprite, args.Component));
    }

    private void UpdateAppearance(Entity<CondomComponent, SpriteComponent?, AppearanceComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp2, false) || !Resolve(ent, ref ent.Comp3, false))
            return;

        if (!_appearance.TryGetData<CondomFill>(ent.Owner, CondomVisuals.Fill, out var stage, ent.Comp3))
            stage = ent.Comp1.Unwrapped ? CondomFill.Empty : CondomFill.Wrapped;

        _sprite.LayerSetRsiState((ent.Owner, ent.Comp2), 0, stage switch
        {
            CondomFill.Inflated => "b_condom_inflated",
            CondomFill.Medium => "b_condom_inflated_med",
            CondomFill.Large => "b_condom_inflated_large",
            CondomFill.Huge => "b_condom_inflated_huge",
            CondomFill.Empty => "b_condom",
            _ => "b_condom_wrapped",
        });
    }
}
