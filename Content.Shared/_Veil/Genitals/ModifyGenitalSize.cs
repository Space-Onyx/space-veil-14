// SPDX-FileCopyrightText: 2026 Space Veil Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Veil.Genitals;

public sealed partial class ModifyGenitalSize : EntityEffectBase<ModifyGenitalSize>
{
    [DataField(required: true)]
    public ProtoId<GenitalCategoryPrototype> Category;

    [DataField]
    public float Amount;
}
