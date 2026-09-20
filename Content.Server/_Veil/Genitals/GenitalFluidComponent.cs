// SPDX-FileCopyrightText: 2026 Space Veil Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Server._Veil.Genitals;

[RegisterComponent]
public sealed partial class GenitalFluidComponent : Component
{
    public float Amount;
    public string ReagentId = "Milk";
    public TimeSpan NextExpress;
}
