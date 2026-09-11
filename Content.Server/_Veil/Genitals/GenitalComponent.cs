// SPDX-FileCopyrightText: 2026 Space Veil Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Veil.Genitals;
using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Server._Veil.Genitals;

[RegisterComponent]
[Access(typeof(GenitalSystem), typeof(GenitalProfileSystem), typeof(GenitalManagerSystem), typeof(ModifyGenitalSizeSystem),
    typeof(GenitalFluidSystem), typeof(GenitalDetailExamineSystem), typeof(GenitalEquipmentSystem), typeof(GenitalVisualSystem),
    typeof(GenitalArousalSystem))]
public sealed partial class GenitalComponent : Component
{
    public ProtoId<GenitalCategoryPrototype> Category;

    public EntityUid Body;

    public string Shape = "Human";

    public bool UseSkinColor = true;

    public Color Color = Color.White;

    public float Size = 1f;

    public float MinSize;

    public float MaxSize = 20f;

    public GenitalVisibility Visibility = GenitalVisibility.HiddenByUnderwear;
}
