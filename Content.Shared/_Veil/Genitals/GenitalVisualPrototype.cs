// SPDX-FileCopyrightText: 2026 Space Veil Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Content.Shared.Body.Part;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._Veil.Genitals;

[Prototype]
public sealed partial class GenitalVisualPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    [DataField(required: true)]
    public ProtoId<GenitalCategoryPrototype> Category;

    [DataField(required: true)]
    public string Shape = string.Empty;

    [DataField(required: true)]
    public GenitalVisualLayer Layer;

    [DataField(required: true)]
    public string StatePrefix = string.Empty;

    [DataField(required: true)]
    public string Rsi = string.Empty;

    [DataField(required: true)]
    public BodyPartType Part;

    [DataField]
    public List<ProtoId<SpeciesPrototype>> Species = [];

    [DataField]
    public List<Sex> Sexes = [];

    [DataField]
    public float MinSize;

    [DataField]
    public float MaxSize = float.MaxValue;

    [DataField]
    public List<float> SizeThresholds = [];

    [DataField]
    public List<string> SizeStates = [];

    [DataField]
    public HashSet<string> NoSkintoneStates = [];

    [DataField]
    public HashSet<string> NoArousedStates = [];

    [DataField]
    public HashSet<string> NoArousedSkintoneStates = [];

    [DataField]
    public bool Internal;

    [DataField]
    public string DetachedRsi = string.Empty;

    [DataField]
    public List<string> DetachedStates = [];

    [DataField]
    public string DetachedSkintoneSuffix = "_s";
}
