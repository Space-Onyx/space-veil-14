// SPDX-FileCopyrightText: 2026 Space Veil Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Veil.Genitals;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class GenitalEquipmentComponent : Component
{
    [DataField(required: true)]
    public HashSet<ProtoId<GenitalCategoryPrototype>> Slots = [];

    [DataField]
    public bool PreventsArousal;

    [DataField]
    public bool CanInsert = true;

    [DataField]
    public bool CanUse = true;

    [DataField]
    public float InsertDelay = 5f;

    [DataField]
    public bool HasVibration;

    [DataField, AutoNetworkedField]
    public int Vibration = 1;

    [DataField, AutoNetworkedField]
    public string? LowVibrationState;

    [DataField, AutoNetworkedField]
    public string? MediumVibrationState;

    [DataField, AutoNetworkedField]
    public string? HighVibrationState;

    [DataField, AutoNetworkedField]
    public bool Wrapped;

    [DataField]
    public string? WrappedState;

    [DataField]
    public string? UnwrappedState;

    [DataField]
    public bool Customizable;

    public TimeSpan NextUse;

    [DataField, AutoNetworkedField]
    public int SizeStage = 2;

    [DataField, AutoNetworkedField]
    public string Shape = "human";

    [DataField, AutoNetworkedField]
    public Color Color = Color.White;
}
