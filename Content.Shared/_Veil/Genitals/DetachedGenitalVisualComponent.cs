// SPDX-FileCopyrightText: 2026 Space Veil Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Shared._Veil.Genitals;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class DetachedGenitalVisualComponent : Component
{
    [AutoNetworkedField]
    public string Rsi = "Mobs/Species/Human/organs.rsi";

    [AutoNetworkedField]
    public string State = string.Empty;

    [AutoNetworkedField]
    public Color Color = Color.White;
}
