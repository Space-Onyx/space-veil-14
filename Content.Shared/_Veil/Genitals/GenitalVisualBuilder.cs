// SPDX-FileCopyrightText: 2026 Space Veil Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared._Veil.Genitals;

public static class GenitalVisualBuilder
{
    public static bool TryFindBest(
        IPrototypeManager prototypes,
        ProtoId<GenitalCategoryPrototype> category,
        string shape,
        float size,
        out GenitalVisualPrototype visual)
    {
        GenitalVisualPrototype? best = null;
        var bestDistance = float.MaxValue;

        foreach (var candidate in prototypes.EnumeratePrototypes<GenitalVisualPrototype>())
        {
            if (candidate.Category != category ||
                !string.Equals(candidate.Shape, shape, StringComparison.OrdinalIgnoreCase))
                continue;

            if (size >= candidate.MinSize && size <= candidate.MaxSize)
            {
                visual = candidate;
                return true;
            }

            var edge = size < candidate.MinSize ? candidate.MinSize : candidate.MaxSize;
            var distance = Math.Abs(size - edge);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = candidate;
            }
        }

        visual = best!;
        return best != null;
    }

    public static bool SupportsArousal(IPrototypeManager prototypes, ProtoId<GenitalCategoryPrototype> category)
    {
        foreach (var visual in prototypes.EnumeratePrototypes<GenitalVisualPrototype>())
        {
            if (visual.Internal || visual.Category != category)
                continue;

            if (visual.SizeStates.Count == 0)
                return visual.NoArousedStates.Count == 0;

            foreach (var state in visual.SizeStates)
            {
                if (!visual.NoArousedStates.Contains(state))
                    return true;
            }
        }

        return false;
    }

    public static int ResolveSizeIndex(GenitalVisualPrototype visual, float size)
    {
        var index = visual.SizeStates.Count == 0 ? 0 : visual.SizeStates.Count - 1;
        for (var i = 0; i < visual.SizeThresholds.Count && i < visual.SizeStates.Count; i++)
        {
            if (size > visual.SizeThresholds[i])
                continue;

            index = i;
            break;
        }

        return index;
    }

    public static string BuildDetachedState(GenitalVisualPrototype visual, float size, bool useSkinColor)
    {
        if (string.IsNullOrEmpty(visual.DetachedRsi) || visual.DetachedStates.Count == 0)
            return string.Empty;

        var index = Math.Clamp(ResolveSizeIndex(visual, size), 0, visual.DetachedStates.Count - 1);
        var skinSuffix = useSkinColor ? visual.DetachedSkintoneSuffix : string.Empty;
        return $"{visual.DetachedStates[index]}{skinSuffix}";
    }

    public static string BuildState(GenitalVisualPrototype visual, float size, bool useSkinColor, bool aroused = false)
    {
        var sizeState = visual.SizeStates.Count == 0
            ? string.Empty
            : visual.SizeStates[Math.Clamp(ResolveSizeIndex(visual, size), 0, visual.SizeStates.Count - 1)];

        var arousedState = aroused && !visual.NoArousedStates.Contains(sizeState);
        var skin = useSkinColor && !visual.NoSkintoneStates.Contains(sizeState);
        if (arousedState && visual.NoArousedSkintoneStates.Contains(sizeState))
            skin = false;

        var skinSuffix = skin ? "_s" : string.Empty;
        var arousedStage = arousedState ? 1 : 0;
        return $"{visual.StatePrefix}{sizeState}{skinSuffix}_{arousedStage}_FRONT";
    }

}
