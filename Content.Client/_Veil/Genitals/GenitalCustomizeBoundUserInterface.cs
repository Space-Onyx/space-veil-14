// SPDX-FileCopyrightText: 2026 Space Veil Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.UserInterface.Controls;
using Content.Shared._Veil.Genitals;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._Veil.Genitals;

[UsedImplicitly]
public sealed class GenitalCustomizeBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private SimpleRadialMenu? _menu;

    protected override void Open()
    {
        base.Open();

        if (!EntMan.TryGetComponent(Owner, out GenitalEquipmentComponent? equipment))
            return;

        var layers = new List<RadialMenuOptionBase>();
        if (equipment.Customizable)
        {
            layers.Add(ShapeLayer(equipment));
            layers.Add(ColorLayer(equipment));
        }

        if (equipment.CanInsert)
            layers.Add(SizeLayer(equipment));

        if (equipment.HasVibration)
            layers.Add(VibrationLayer());

        if (layers.Count == 0)
            return;

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.SetButtons(layers);
        _menu.OpenCentered();
    }

    private RadialMenuNestedLayerOption ShapeLayer(GenitalEquipmentComponent equipment)
    {
        var options = new List<RadialMenuOptionBase>();
        foreach (var shape in GenitalCustomizationData.Shapes)
        {
            var captured = shape;
            options.Add(new RadialMenuActionOption<string>(
                s => SendPredictedMessage(new GenitalCustomizeShapeMessage(s)), captured)
            {
                IconSpecifier = PreviewIcon(captured, equipment.SizeStage),
                ToolTip = Loc.GetString($"genital-shape-{captured}"),
            });
        }

        return new RadialMenuNestedLayerOption(options)
        {
            IconSpecifier = PreviewIcon(equipment.Shape, equipment.SizeStage),
            ToolTip = Loc.GetString("genital-equipment-change-shape"),
        };
    }

    private RadialMenuNestedLayerOption ColorLayer(GenitalEquipmentComponent equipment)
    {
        var options = new List<RadialMenuOptionBase>();
        for (var i = 0; i < GenitalCustomizationData.Colors.Length; i++)
        {
            var captured = i;
            options.Add(new RadialMenuActionOption<int>(
                c => SendPredictedMessage(new GenitalCustomizeColorMessage(c)), captured)
            {
                IconSpecifier = PreviewIcon(equipment.Shape, equipment.SizeStage),
                BackgroundColor = GenitalCustomizationData.Colors[captured],
                ToolTip = Loc.GetString($"genital-color-{GenitalCustomizationData.ColorLocKeys[captured]}"),
            });
        }

        return new RadialMenuNestedLayerOption(options)
        {
            IconSpecifier = PreviewIcon(equipment.Shape, equipment.SizeStage),
            ToolTip = Loc.GetString("genital-equipment-change-color"),
        };
    }

    private RadialMenuNestedLayerOption SizeLayer(GenitalEquipmentComponent equipment)
    {
        var options = new List<RadialMenuOptionBase>();
        for (var size = GenitalCustomizationData.MinSize; size <= GenitalCustomizationData.MaxSize; size++)
        {
            var captured = size;
            options.Add(new RadialMenuActionOption<int>(
                s => SendPredictedMessage(new GenitalCustomizeSizeMessage(s)), captured)
            {
                IconSpecifier = equipment.Customizable ? PreviewIcon(equipment.Shape, captured) : null,
                ToolTip = Loc.GetString("genital-equipment-resized", ("size", captured)),
            });
        }

        return new RadialMenuNestedLayerOption(options)
        {
            IconSpecifier = equipment.Customizable
                ? PreviewIcon(equipment.Shape, equipment.SizeStage)
                : null,
            ToolTip = Loc.GetString("genital-customize-size"),
        };
    }

    private RadialMenuNestedLayerOption VibrationLayer()
    {
        var options = new List<RadialMenuOptionBase>();
        for (var level = GenitalCustomizationData.MinVibration;
             level <= GenitalCustomizationData.MaxVibration;
             level++)
        {
            var captured = level;
            options.Add(new RadialMenuActionOption<int>(
                l => SendPredictedMessage(new GenitalCustomizeVibrationMessage(l)), captured)
            {
                ToolTip = Loc.GetString("genital-equipment-vibration-set",
                    ("mode", Loc.GetString($"genital-vibration-{captured}"))),
            });
        }

        return new RadialMenuNestedLayerOption(options)
        {
            ToolTip = Loc.GetString("genital-equipment-vibration-mode"),
        };
    }

    private static RadialMenuIconSpecifier? PreviewIcon(string shape, int size)
    {
        return RadialMenuIconSpecifier.With(new SpriteSpecifier.Rsi(
            new ResPath(GenitalCustomizationData.PreviewRsi),
            GenitalCustomizationData.PreviewState(shape, size)));
    }
}
