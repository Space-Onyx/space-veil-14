// SPDX-FileCopyrightText: 2026 Space Veil Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Veil.Genitals;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._Veil.Genitals;

public sealed partial class ChemicalArousalOverlaySystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private IPlayerManager _player = default!;

    private ChemicalArousalOverlay _arousalOverlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _arousalOverlay = new ChemicalArousalOverlay();
        SubscribeLocalEvent<ChemicalArousalVisualComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ChemicalArousalVisualComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ChemicalArousalVisualComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<ChemicalArousalVisualComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
    }

    public override void Shutdown()
    {
        _overlay.RemoveOverlay(_arousalOverlay);
        base.Shutdown();
    }

    private void OnStartup(Entity<ChemicalArousalVisualComponent> entity, ref ComponentStartup args)
    {
        if (_player.LocalEntity == entity && !_overlay.HasOverlay<ChemicalArousalOverlay>())
            _overlay.AddOverlay(_arousalOverlay);
    }

    private void OnShutdown(Entity<ChemicalArousalVisualComponent> entity, ref ComponentShutdown args)
    {
        if (_player.LocalEntity == entity)
            _overlay.RemoveOverlay(_arousalOverlay);
    }

    private void OnPlayerAttached(Entity<ChemicalArousalVisualComponent> entity, ref LocalPlayerAttachedEvent args)
    {
        if (!_overlay.HasOverlay<ChemicalArousalOverlay>())
            _overlay.AddOverlay(_arousalOverlay);
    }

    private void OnPlayerDetached(Entity<ChemicalArousalVisualComponent> entity, ref LocalPlayerDetachedEvent args)
    {
        _overlay.RemoveOverlay(_arousalOverlay);
    }
}
