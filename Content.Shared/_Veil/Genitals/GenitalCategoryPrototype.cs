// SPDX-FileCopyrightText: 2026 Space Veil Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;
using Content.Shared.Body.Part;

namespace Content.Shared._Veil.Genitals;

[Prototype]
public sealed partial class GenitalCategoryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public int Order;

    [DataField]
    public BodyPartType Part = BodyPartType.Groin;

    [DataField]
    public string DefaultShape = "Human";

    [DataField]
    public float DefaultSize = 1f;

    [DataField]
    public float MinSize = 1f;

    [DataField]
    public float MaxSize = 1f;

    [DataField]
    public bool ProducesFluid;

    [DataField]
    public bool FluidOptional;

    [DataField]
    public bool UsesMilkLabel;

    [DataField]
    public float FluidCapacityBase = 5f;

    [DataField]
    public float FluidCapacityPerSize = 2.5f;

    [DataField]
    public float FluidProductionPerSecond = 0.02f;
}
