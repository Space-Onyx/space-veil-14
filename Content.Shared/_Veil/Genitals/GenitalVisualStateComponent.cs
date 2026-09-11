// SPDX-FileCopyrightText: 2026 Space Veil Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body.Part;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Veil.Genitals;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GenitalVisualStateComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<GenitalLayerData> Layers = new();
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class GenitalLayerData : IEquatable<GenitalLayerData>
{
    [DataField]
    public GenitalVisualLayer Layer;

    [DataField]
    public string State = string.Empty;

    [DataField]
    public string Rsi = string.Empty;

    [DataField]
    public Color Color = Color.White;

    [DataField]
    public bool Aroused;

    [DataField]
    public BodyPartType Part;

    [DataField]
    public GenitalVisibility Visibility;

    public bool Equals(GenitalLayerData? other)
    {
        return other != null && Layer == other.Layer && State == other.State && Rsi == other.Rsi && Color.Equals(other.Color) &&
               Aroused == other.Aroused && Part == other.Part && Visibility == other.Visibility;
    }

    public override bool Equals(object? obj) => obj is GenitalLayerData other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Layer, State, Rsi, Color, Aroused, Part, Visibility);
}
