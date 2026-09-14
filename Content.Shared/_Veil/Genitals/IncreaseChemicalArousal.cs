// Space Veil
// Copyright (C) 2026 Space Veil contributors
//
// This file is licensed under AGPL-3.0-or-later.
// See LICENSES for the full license text.

using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Veil.Genitals;

public sealed partial class IncreaseChemicalArousal : EntityEffectBase<IncreaseChemicalArousal>
{
    [DataField]
    public float Amount = 1f;

    [DataField]
    public float Maximum = 60f;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("entity-effect-guidebook-increase-chemical-arousal",
            ("amount", Amount),
            ("maximum", Maximum));
    }
}
