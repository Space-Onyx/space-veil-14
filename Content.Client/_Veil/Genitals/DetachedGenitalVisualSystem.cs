// SPDX-FileCopyrightText: 2026 Space Veil Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Veil.Genitals;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Utility;

namespace Content.Client._Veil.Genitals;

public sealed partial class DetachedGenitalVisualSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DetachedGenitalVisualComponent, AfterAutoHandleStateEvent>(OnState);
        SubscribeLocalEvent<DetachedGenitalVisualComponent, ComponentStartup>(OnStartup);
    }

    private void OnState(Entity<DetachedGenitalVisualComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        Apply(ent);
    }

    private void OnStartup(Entity<DetachedGenitalVisualComponent> ent, ref ComponentStartup args)
    {
        Apply(ent);
    }

    private void Apply(Entity<DetachedGenitalVisualComponent> ent)
    {
        if (!TryComp(ent, out SpriteComponent? sprite))
            return;

        Entity<SpriteComponent?> target = (ent, sprite);
        if (!string.IsNullOrEmpty(ent.Comp.State))
        {
            RSI.StateId stateId = ent.Comp.State;
            _sprite.LayerSetRsi(target, 0, new ResPath(ent.Comp.Rsi), stateId);
        }
        else
        {
            _sprite.LayerSetRsi(target, 0, new ResPath(ent.Comp.Rsi));
        }
        _sprite.LayerSetColor(target, 0, ent.Comp.Color);
    }
}
