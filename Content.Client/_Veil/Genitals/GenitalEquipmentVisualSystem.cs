// SPDX-FileCopyrightText: 2026 Space Veil Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Veil.Genitals;
using Robust.Client.GameObjects;

namespace Content.Client._Veil.Genitals;

public sealed partial class GenitalEquipmentVisualSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GenitalEquipmentComponent, AfterAutoHandleStateEvent>(OnState);
        SubscribeLocalEvent<GenitalEquipmentComponent, ComponentStartup>(OnStartup);
    }

    private void OnState(Entity<GenitalEquipmentComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        Apply(ent);
    }

    private void OnStartup(Entity<GenitalEquipmentComponent> ent, ref ComponentStartup args)
    {
        Apply(ent);
    }

    private void Apply(Entity<GenitalEquipmentComponent> ent)
    {
        if (!TryComp(ent, out SpriteComponent? sprite))
            return;

        Entity<SpriteComponent?> target = (ent, sprite);

        if (ent.Comp.Customizable)
        {
            _sprite.LayerSetRsiState(target, 0, $"dildo_{ent.Comp.Shape}_{ent.Comp.SizeStage}");
            _sprite.LayerSetColor(target, 0, ent.Comp.Color);
        }

        if (ent.Comp.WrappedState != null && ent.Comp.UnwrappedState != null)
            _sprite.LayerSetRsiState(target, 0, ent.Comp.Wrapped ? ent.Comp.WrappedState : ent.Comp.UnwrappedState);

        var vibrationState = ent.Comp.Vibration >= GenitalCustomizationData.MaxVibration
            ? ent.Comp.HighVibrationState
            : ent.Comp.Vibration <= GenitalCustomizationData.MinVibration
                ? ent.Comp.LowVibrationState
                : ent.Comp.MediumVibrationState;

        if (!string.IsNullOrEmpty(vibrationState))
            _sprite.LayerSetRsiState(target, 0, vibrationState);
    }
}
