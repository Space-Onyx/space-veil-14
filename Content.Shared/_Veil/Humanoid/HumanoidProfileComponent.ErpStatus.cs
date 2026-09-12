// Content adapted from Wega (https://github.com/wega-team/ss14-wega), licensed under GNU GPL v3.

namespace Content.Shared.Humanoid;

public sealed partial class HumanoidProfileComponent
{
    [DataField, AutoNetworkedField]
    public ErpStatus ErpStatus = ErpStatus.No;
}
