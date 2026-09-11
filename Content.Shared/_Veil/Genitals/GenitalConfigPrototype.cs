// SPDX-FileCopyrightText: 2026 Space Veil Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared._Veil.Genitals;

[Prototype]
public sealed partial class GenitalConfigPrototype : IPrototype, IInheritingPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<GenitalConfigPrototype>))]
    public string[]? Parents { get; private set; }

    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }

    [DataField]
    public Dictionary<Sex, List<ProtoId<GenitalCategoryPrototype>>> Categories = new();

    [DataField]
    public Dictionary<Sex, List<ProtoId<GenitalCategoryPrototype>>> AlwaysPresent = new();

    [DataField]
    public Dictionary<ProtoId<GenitalCategoryPrototype>, string> Shapes = new();

    [DataField]
    public Dictionary<ProtoId<GenitalCategoryPrototype>, List<string>> AvailableShapes = new();

    [DataField]
    public Dictionary<ProtoId<GenitalCategoryPrototype>, List<string>> Fluids = new();

    [DataField]
    public List<string> DefaultFluids = new() { "Milk" };

    [DataField]
    public bool AllowTransparentColor;

    [DataField]
    public bool AllowRuntimeSize;
}
