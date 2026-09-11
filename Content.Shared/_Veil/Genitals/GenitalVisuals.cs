// SPDX-FileCopyrightText: 2026 Space Veil Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Shared._Veil.Genitals;

[Serializable, NetSerializable]
public enum GenitalVisualLayer : byte
{
    Butt,
    Vagina,
    Testicles,
    Penis,
    Breasts,
    Anus,
}
