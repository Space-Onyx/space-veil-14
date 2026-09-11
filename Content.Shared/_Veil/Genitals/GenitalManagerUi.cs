// SPDX-FileCopyrightText: 2026 Space Veil Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Shared._Veil.Genitals;

[Serializable, NetSerializable]
public enum GenitalManagerUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class GenitalManagerUiState(List<GenitalManagerEntry> organs)
    : BoundUserInterfaceState
{
    public readonly List<GenitalManagerEntry> Organs = organs;
}

[Serializable, NetSerializable]
public sealed class GenitalManagerEntry(
    string category,
    float size,
    GenitalVisibility visibility,
    bool canArouse,
    bool aroused,
    string? equipmentName,
    bool canResize,
    float minSize,
    float maxSize,
    string previewRsi,
    string previewState,
    Color previewColor,
    float? milkAmount,
    float? milkCapacity,
    string? fluidName)
{
    public readonly string Category = category;
    public readonly float Size = size;
    public readonly GenitalVisibility Visibility = visibility;
    public readonly bool CanArouse = canArouse;
    public readonly bool Aroused = aroused;
    public readonly string? EquipmentName = equipmentName;
    public readonly bool CanResize = canResize;
    public readonly float MinSize = minSize;
    public readonly float MaxSize = maxSize;
    public readonly string PreviewRsi = previewRsi;
    public readonly string PreviewState = previewState;
    public readonly Color PreviewColor = previewColor;
    public readonly float? MilkAmount = milkAmount;
    public readonly float? MilkCapacity = milkCapacity;
    public readonly string? FluidName = fluidName;
}

[Serializable, NetSerializable]
public sealed class GenitalSetVisibilityMessage(string category, GenitalVisibility visibility)
    : BoundUserInterfaceMessage
{
    public readonly string Category = category;
    public readonly GenitalVisibility Visibility = visibility;
}

[Serializable, NetSerializable]
public sealed class GenitalSetArousedMessage(string category, bool aroused)
    : BoundUserInterfaceMessage
{
    public readonly string Category = category;
    public readonly bool Aroused = aroused;
}

[Serializable, NetSerializable]
public sealed class GenitalEquipmentRemoveMessage(string category)
    : BoundUserInterfaceMessage
{
    public readonly string Category = category;
}

[Serializable, NetSerializable]
public sealed class GenitalSetSizeMessage(string category, float size) : BoundUserInterfaceMessage
{
    public readonly string Category = category;
    public readonly float Size = size;
}

[Serializable, NetSerializable]
public enum GenitalCustomizeUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class GenitalCustomizeShapeMessage(string shape) : BoundUserInterfaceMessage
{
    public readonly string Shape = shape;
}

[Serializable, NetSerializable]
public sealed class GenitalCustomizeSizeMessage(int size) : BoundUserInterfaceMessage
{
    public readonly int Size = size;
}

[Serializable, NetSerializable]
public sealed class GenitalCustomizeColorMessage(int color) : BoundUserInterfaceMessage
{
    public readonly int Color = color;
}

[Serializable, NetSerializable]
public sealed class GenitalCustomizeVibrationMessage(int level) : BoundUserInterfaceMessage
{
    public readonly int Level = level;
}
