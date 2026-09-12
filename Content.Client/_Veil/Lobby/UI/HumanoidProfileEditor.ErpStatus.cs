// Content adapted from Wega (https://github.com/wega-team/ss14-wega), licensed under GNU GPL v3.

using Content.Shared.Humanoid;

#pragma warning disable IDE0130
namespace Content.Client.Lobby.UI;
#pragma warning restore IDE0130

public sealed partial class HumanoidProfileEditor
{
    private void InitializeErpStatus()
    {
        StatusButton.AddItem(Loc.GetString("humanoid-profile-editor-status-no-text"), (int)ErpStatus.No);
        StatusButton.AddItem(Loc.GetString("humanoid-profile-editor-status-ask-text"), (int)ErpStatus.Ask);
        StatusButton.AddItem(Loc.GetString("humanoid-profile-editor-status-semi-text"), (int)ErpStatus.Semi);
        StatusButton.AddItem(Loc.GetString("humanoid-profile-editor-status-full-text"), (int)ErpStatus.Full);
        StatusButton.AddItem(Loc.GetString("humanoid-profile-editor-status-absolute-text"), (int)ErpStatus.Absolute);
        StatusButton.OnItemSelected += args => SetErpStatus((ErpStatus)args.Id);
    }

    private void SetErpStatus(ErpStatus status)
    {
        if (Profile == null || !_prototypeManager.TryIndex(Profile.Species, out var species) || !species.ErpStatuses.Contains(status))
            status = ErpStatus.No;

        Profile = Profile?.WithErpStatus(status);
        UpdateErpStatusControls();
        ReloadPreview();
    }

    private void UpdateErpStatusControls()
    {
        if (Profile == null)
            return;

        StatusButton.SelectId((int)Profile.ErpStatus);
    }
}
