// Content adapted from Wega (https://github.com/wega-team/ss14-wega), licensed under GNU GPL v3.

using Content.Shared.Humanoid;

namespace Content.Shared.Preferences;

public sealed partial class HumanoidCharacterProfile
{
    [DataField("status")]
    public ErpStatus ErpStatus { get; private set; } = ErpStatus.No;

    public HumanoidCharacterProfile WithErpStatus(ErpStatus status)
    {
        return new(this) { ErpStatus = status };
    }

    private void CopyErpStatus(HumanoidCharacterProfile other)
    {
        ErpStatus = other.ErpStatus;
    }

    private void EnsureErpStatusValid()
    {
        if (!Enum.IsDefined(ErpStatus))
            ErpStatus = ErpStatus.No;
    }
}
