// Content adapted from Wega (https://github.com/wega-team/ss14-wega), licensed under GNU GPL v3.

using Content.Shared.Humanoid;

namespace Content.Shared.Humanoid.Prototypes;

public sealed partial class SpeciesPrototype
{
    [DataField]
    public List<ErpStatus> ErpStatuses { get; private set; } = new()
    {
        ErpStatus.No,
        ErpStatus.Ask,
        ErpStatus.Semi,
        ErpStatus.Full,
        ErpStatus.Absolute,
    };
}
