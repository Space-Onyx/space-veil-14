// SPDX-FileCopyrightText: 2026 Space Veil Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Shared._Veil.Genitals;

[RegisterComponent]
public sealed partial class CondomComponent : Component
{
    [DataField]
    public bool Unwrapped;
}

[Serializable, NetSerializable]
public enum CondomVisuals : byte
{
    Fill,
}

[Serializable, NetSerializable]
public enum CondomFill : byte
{
    Wrapped,
    Empty,
    Inflated,
    Medium,
    Large,
    Huge,
}
