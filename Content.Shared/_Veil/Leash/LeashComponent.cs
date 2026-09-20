// SPDX-FileCopyrightText: 2026 Space Veil Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared._Veil.Leash;

[RegisterComponent]
public sealed partial class LeashComponent : Component
{
    [DataField]
    public float MaxDistance = 3f;

    public EntityUid? AttachedEntity;

    public EntityUid? Anchor;

    public string? JointId;
}
