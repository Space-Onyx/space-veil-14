// SPDX-FileCopyrightText: 2026 Space Veil Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Veil.Genitals;
using Content.Shared.Humanoid;
using Content.Shared.Inventory.Events;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Utility;

namespace Content.Client._Veil.Genitals;

public sealed partial class GenitalAppearanceSystem : SharedGenitalCoverageSystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IEyeManager _eye = default!;
    [Dependency] private IResourceCache _resourceCache = default!;

    private static readonly ResPath TextureRoot = new("/Textures/");

    private const string FrontSuffix = "_FRONT";
    private const string BehindSuffix = "_BEHIND";

    private readonly Dictionary<EntityUid, int> _appliedHashes = new();
    private readonly HashSet<EntityUid> _dirty = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GenitalVisualStateComponent, ComponentShutdown>(OnStateShutdown);
        SubscribeLocalEvent<DidEquipEvent>(OnClothingChanged);
        SubscribeLocalEvent<DidUnequipEvent>(OnClothingChanged);
    }

    private void OnStateShutdown(Entity<GenitalVisualStateComponent> ent, ref ComponentShutdown args)
    {
        _appliedHashes.Remove(ent);
        _dirty.Remove(ent);
    }

    private void OnClothingChanged(DidEquipEvent args)
    {
        _dirty.Add(args.EquipTarget);
    }

    private void OnClothingChanged(DidUnequipEvent args)
    {
        _dirty.Add(args.EquipTarget);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<GenitalVisualStateComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var state, out var sprite))
        {
            var direction = GetDirection(uid);
            var hash = ComputeHash(state, direction);
            if (!_dirty.Contains(uid) && _appliedHashes.TryGetValue(uid, out var applied) && applied == hash)
                continue;

            ApplyLayers(uid, sprite, state, direction);
            _appliedHashes[uid] = hash;
            _dirty.Remove(uid);
        }
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _appliedHashes.Clear();
        _dirty.Clear();
    }

    private Direction GetDirection(EntityUid body)
    {
        var angle = (_transform.GetWorldRotation(body) + _eye.CurrentEye.Rotation).Reduced().FlipPositive();
        return angle.GetCardinalDir();
    }

    private static int ComputeHash(GenitalVisualStateComponent state, Direction direction)
    {
        var hash = new HashCode();
        hash.Add(direction);
        foreach (var layer in state.Layers)
            hash.Add(layer);
        return hash.ToHashCode();
    }

    private void ApplyLayers(EntityUid uid, SpriteComponent sprite, GenitalVisualStateComponent state, Direction direction)
    {
        Entity<SpriteComponent?> ent = new(uid, sprite);
        foreach (GenitalVisualLayer layer in Enum.GetValues<GenitalVisualLayer>())
            _sprite.RemoveLayer(ent, layer, logMissing: false);

        var behind = direction == Direction.North;

        foreach (var layer in state.Layers)
        {
            if (string.IsNullOrWhiteSpace(layer.Rsi) || string.IsNullOrWhiteSpace(layer.State))
                continue;

            var stateName = ResolveState(layer, behind);
            if (stateName == null)
                continue;

            var index = ResolveInsertIndex(ent, layer.Layer, direction);
            var newIndex = _sprite.AddRsiLayer(ent, stateName, new ResPath(layer.Rsi), index);
            _sprite.LayerMapSet(ent, layer.Layer, newIndex);
            _sprite.LayerSetColor(ent, newIndex, layer.Color);
            _sprite.LayerSetVisible(ent, newIndex, IsVisibleTo(uid, layer.Part, layer.Visibility));
        }
    }

    private string? ResolveState(GenitalLayerData layer, bool behind)
    {
        if (!behind)
            return layer.State;

        if (layer.State.EndsWith(FrontSuffix, StringComparison.Ordinal))
        {
            var behindState = string.Concat(layer.State.AsSpan(0, layer.State.Length - FrontSuffix.Length), BehindSuffix);
            if (StateExists(layer.Rsi, behindState))
                return behindState;
        }

        return null;
    }

    private bool StateExists(string rsi, string state)
    {
        if (!_resourceCache.TryGetResource<RSIResource>(TextureRoot / new ResPath(rsi), out var resource))
            return false;

        return resource.RSI.TryGetState(state, out _);
    }

    private int? ResolveInsertIndex(Entity<SpriteComponent?> ent, GenitalVisualLayer layer, Direction direction)
    {
        switch (layer)
        {
            case GenitalVisualLayer.Butt:
                return LayerAfter(ent, HumanoidVisualLayers.LLeg);
            case GenitalVisualLayer.Breasts when direction == Direction.South:
                return LayerAfterTop(ent);
            default:
                return LayerAfter(ent, HumanoidVisualLayers.Groin);
        }
    }

    private int? LayerAfter(Entity<SpriteComponent?> ent, HumanoidVisualLayers anchor)
    {
        return _sprite.LayerMapTryGet(ent, anchor, out var index, false) ? index + 1 : null;
    }

    private int? LayerAfterTop(Entity<SpriteComponent?> ent)
    {
        var best = -1;
        foreach (var key in new[] { HumanoidVisualLayers.RArm, HumanoidVisualLayers.LArm, HumanoidVisualLayers.RHand, HumanoidVisualLayers.LHand })
        {
            if (_sprite.LayerMapTryGet(ent, key, out var index, false) && index > best)
                best = index;
        }

        return best >= 0 ? best + 1 : null;
    }
}
