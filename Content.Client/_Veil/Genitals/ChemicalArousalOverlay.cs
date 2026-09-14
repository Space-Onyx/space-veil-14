// Space Veil
// Copyright (C) 2026 Space Veil contributors
//
// This file is licensed under AGPL-3.0-or-later.
// See LICENSES for the full license text.

using System.Numerics;
using Content.Shared._Veil.Genitals;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._Veil.Genitals;

public sealed partial class ChemicalArousalOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> Shader = "ChemicalArousalScreenEffect";

    [Dependency] private IEntityManager _entity = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IResourceCache _resources = default!;

    private readonly ShaderInstance _shader;
    private readonly Texture _heart;
    private float _time;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public ChemicalArousalOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototype.Index(Shader).InstanceUnique();
        _heart = _resources.GetResource<TextureResource>("/Textures/_Veil/Interface/heart.png").Texture;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        _time += args.DeltaSeconds;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return _player.LocalEntity is { Valid: true } player &&
            _entity.HasComponent<ChemicalArousalVisualComponent>(player) &&
            _entity.TryGetComponent(player, out EyeComponent? eye) &&
            args.Viewport.Eye == eye.Eye;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        handle.UseShader(_shader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);

        var bounds = args.WorldBounds.Box;
        for (var index = 0; index < 6; index++)
        {
            var progress = Fract(index / 6f + _time * (0.025f + index % 3 * 0.004f));
            var size = bounds.Height * (0.02f + index % 2 * 0.0056f);
            var x = index % 2 == 0 ? bounds.Left + size : bounds.Right - size;
            var y = MathHelper.Lerp(bounds.Bottom, bounds.Top, progress);
            var alpha = MathF.Sin(progress * MathF.PI) * 0.128f;
            var box = Box2.CenteredAround(new Vector2(x, y), new Vector2(size));
            handle.DrawTextureRect(
                _heart,
                new Box2Rotated(box, args.WorldBounds.Rotation, args.WorldBounds.Origin),
                Color.White.WithAlpha(alpha));
        }
    }

    private static float Fract(float value)
    {
        return value - MathF.Floor(value);
    }
}
