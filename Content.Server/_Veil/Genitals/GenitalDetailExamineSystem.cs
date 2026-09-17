// SPDX-FileCopyrightText: 2026 Space Veil Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Veil.Genitals;
using Content.Shared.Examine;
using Content.Shared.Humanoid;

namespace Content.Server._Veil.Genitals;

public sealed partial class GenitalDetailExamineSystem : SharedGenitalCoverageSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GenitalVisualStateComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<GenitalVisualStateComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (TryComp(ent.Owner, out HumanoidProfileComponent? profile) && profile.ErpStatus == ErpStatus.No)
            return;

        foreach (var layer in ent.Comp.Layers)
        {
            if (!IsVisibleTo(ent.Owner, layer.Part, layer.Visibility))
                continue;

            var key = $"genital-examine-{layer.Layer.ToString().ToLowerInvariant()}";
            args.PushMarkup(Loc.GetString(key));
        }
    }
}
