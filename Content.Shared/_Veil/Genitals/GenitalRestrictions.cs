// SPDX-FileCopyrightText: 2026 Space Veil Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;

namespace Content.Shared._Veil.Genitals;

public static class GenitalRestrictions
{
    public const string DefaultConfig = "Default";

    public static GenitalConfigPrototype Config(IPrototypeManager prototypes, string speciesId)
    {
        return prototypes.TryIndex<GenitalConfigPrototype>(speciesId, out var config)
            ? config
            : prototypes.Index<GenitalConfigPrototype>(DefaultConfig);
    }

    public static bool IsCategoryAllowed(
        IPrototypeManager prototypes,
        string speciesId,
        Sex sex,
        ProtoId<GenitalCategoryPrototype> category)
    {
        var config = Config(prototypes, speciesId);
        return config.Categories.TryGetValue(sex, out var categories) && categories.Contains(category);
    }

    public static bool IsAlwaysPresent(
        IPrototypeManager prototypes,
        string speciesId,
        Sex sex,
        ProtoId<GenitalCategoryPrototype> category)
    {
        var config = Config(prototypes, speciesId);
        return config.AlwaysPresent.TryGetValue(sex, out var categories) && categories.Contains(category);
    }

    public static string DefaultShape(IPrototypeManager prototypes, string speciesId, ProtoId<GenitalCategoryPrototype> category)
    {
        var config = Config(prototypes, speciesId);
        return config.Shapes.TryGetValue(category, out var shape) ? shape : GenitalProfileData.Default(category).Shape;
    }

    public static List<string> AllowedFluids(IPrototypeManager prototypes, string speciesId, ProtoId<GenitalCategoryPrototype> category)
    {
        var config = Config(prototypes, speciesId);
        if (config.Fluids.TryGetValue(category, out var fluids) && fluids.Count > 0)
            return fluids;
        return config.DefaultFluids;
    }

    public static string DefaultFluid(IPrototypeManager prototypes, string speciesId, ProtoId<GenitalCategoryPrototype> category)
    {
        var fluids = AllowedFluids(prototypes, speciesId, category);
        return fluids.Count > 0 ? fluids[0] : "Milk";
    }

    public static bool IsShapeAllowed(
        IPrototypeManager prototypes,
        string speciesId,
        Sex sex,
        ProtoId<GenitalCategoryPrototype> category,
        string shape)
    {
        var config = Config(prototypes, speciesId);
        if (config.AvailableShapes.TryGetValue(category, out var available) && available.Count > 0)
            return available.Any(name => string.Equals(name, shape, StringComparison.OrdinalIgnoreCase));

        foreach (var visual in prototypes.EnumeratePrototypes<GenitalVisualPrototype>())
        {
            if (visual.Internal || visual.Category != category ||
                !string.Equals(visual.Shape, shape, StringComparison.OrdinalIgnoreCase))
                continue;

            if (visual.Species.Count > 0 && !visual.Species.Any(species => species.Id == speciesId))
                continue;

            if (visual.Sexes.Count > 0 && !visual.Sexes.Contains(sex))
                continue;

            return true;
        }

        return false;
    }

    public static IEnumerable<ProtoId<GenitalCategoryPrototype>> Categories()
    {
        yield return new ProtoId<GenitalCategoryPrototype>("Penis");
        yield return new ProtoId<GenitalCategoryPrototype>("Testicles");
        yield return new ProtoId<GenitalCategoryPrototype>("Vagina");
        yield return new ProtoId<GenitalCategoryPrototype>("Breasts");
        yield return new ProtoId<GenitalCategoryPrototype>("Butt");
        yield return new ProtoId<GenitalCategoryPrototype>("Anus");
    }
}
