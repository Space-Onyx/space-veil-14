// SPDX-FileCopyrightText: 2026 Space Veil Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Veil.Genitals;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;

namespace Content.Client._Veil.Genitals;

public sealed partial class GenitalPreviewSystem : SharedGenitalCoverageSystem
{
    [Dependency] private IPrototypeManager _prototypes = default!;

    public readonly HashSet<string> PreviewAroused = new();

    public void ApplyPreview(EntityUid dummy, HumanoidCharacterProfile profile)
    {
        if (TerminatingOrDeleted(dummy))
            return;

        var layers = new List<GenitalLayerData>();
        var skinColor = profile.Appearance.SkinColor;

        foreach (var (category, data) in profile.Genitals)
        {
            if (!data.Present ||
                !GenitalRestrictions.IsCategoryAllowed(_prototypes, profile.Species.Id, profile.Sex, category) ||
                !GenitalRestrictions.IsShapeAllowed(_prototypes, profile.Species.Id, profile.Sex, category, data.Shape))
                continue;

            if (!GenitalVisualBuilder.TryFindBest(_prototypes, category, data.Shape, data.Size, out var visual) ||
                visual.Internal)
                continue;

            if (!IsVisibleTo(dummy, visual.Part, data.Visibility))
                continue;

            var aroused = PreviewAroused.Contains(category.Id);
            layers.Add(new GenitalLayerData
            {
                Layer = visual.Layer,
                State = GenitalVisualBuilder.BuildState(visual, data.Size, data.UseSkinColor, aroused),
                Rsi = visual.Rsi,
                Color = data.UseSkinColor ? skinColor : data.Color,
                Aroused = aroused,
                Part = visual.Part,
                Visibility = data.Visibility,
            });
        }

        layers.Sort((a, b) => a.Layer.CompareTo(b.Layer));

        if (layers.Count == 0 && !HasComp<GenitalVisualStateComponent>(dummy))
            return;

        var state = EnsureComp<GenitalVisualStateComponent>(dummy);
        state.Layers = layers;
    }
}
