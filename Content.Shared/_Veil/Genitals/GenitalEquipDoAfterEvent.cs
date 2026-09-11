// SPDX-FileCopyrightText: 2026 Space Veil Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Veil.Genitals;

[Serializable, NetSerializable]
public sealed partial class GenitalEquipDoAfterEvent : SimpleDoAfterEvent
{
    public NetEntity? TargetOrgan;
}
