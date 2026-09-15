// Space Veil
// Copyright (C) 2026 Space Veil contributors
//
// This file is licensed under AGPL-3.0-or-later.
// See LICENSES for the full license text.

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
