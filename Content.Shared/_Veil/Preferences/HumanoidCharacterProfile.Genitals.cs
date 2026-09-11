// SPDX-FileCopyrightText: 2026 Space Veil Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared._Veil.Genitals;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Preferences;

public sealed partial class HumanoidCharacterProfile
{
    private static readonly HashSet<ProtoId<GenitalCategoryPrototype>> CoreGenitalCategories =
    [
        "Penis",
        "Testicles",
        "Vagina",
        "Breasts",
        "Butt",
        "Anus",
    ];

    [DataField]
    private Dictionary<ProtoId<GenitalCategoryPrototype>, GenitalProfileData> _genitals = [];

    public IReadOnlyDictionary<ProtoId<GenitalCategoryPrototype>, GenitalProfileData> Genitals => _genitals;

    public HumanoidCharacterProfile WithGenitals(
        IReadOnlyDictionary<ProtoId<GenitalCategoryPrototype>, GenitalProfileData> genitals)
    {
        return new(this)
        {
            _genitals = genitals.ToDictionary(entry => entry.Key, entry => new GenitalProfileData(entry.Value)),
        };
    }

    public HumanoidCharacterProfile WithSanitizedGenitals(IPrototypeManager prototypes)
    {
        var profile = new HumanoidCharacterProfile(this);
        profile.SanitizeGenitals(prototypes);
        return profile;
    }

    public static Dictionary<ProtoId<GenitalCategoryPrototype>, GenitalProfileData> DefaultGenitals(
        IPrototypeManager prototypes,
        string speciesId,
        Sex sex)
    {
        var result = new Dictionary<ProtoId<GenitalCategoryPrototype>, GenitalProfileData>();
        foreach (var category in CoreGenitalCategories)
        {
            if (!GenitalRestrictions.IsCategoryAllowed(prototypes, speciesId, sex, category))
                continue;

            var data = GenitalProfileData.Default(category);
            data.Shape = GenitalRestrictions.DefaultShape(prototypes, speciesId, category);
            data.FluidId = GenitalRestrictions.DefaultFluid(prototypes, speciesId, category);
            data.Present = GenitalRestrictions.IsAlwaysPresent(prototypes, speciesId, sex, category);
            result[category] = data;
        }

        return result;
    }

    public static Dictionary<ProtoId<GenitalCategoryPrototype>, GenitalProfileData> DefaultGenitals(string speciesId, Sex sex)
    {
        return DefaultGenitals(IoCManager.Resolve<IPrototypeManager>(), speciesId, sex);
    }

    private void CopyGenitalFields(HumanoidCharacterProfile other)
    {
        _genitals = other._genitals.ToDictionary(entry => entry.Key, entry => new GenitalProfileData(entry.Value));
    }

    private void CopyRandomizedGenitalFields(HumanoidCharacterProfile other)
    {
        var prototypes = IoCManager.Resolve<IPrototypeManager>();
        _genitals = other._genitals.Count == 0 || other.HasDefaultGenitals(prototypes, other.Species.Id, other.Sex)
            ? DefaultGenitals(prototypes, Species.Id, Sex)
            : other._genitals.ToDictionary(entry => entry.Key, entry => new GenitalProfileData(entry.Value));
    }

    private void UpdateDefaultGenitals(Sex previousSex)
    {
        var prototypes = IoCManager.Resolve<IPrototypeManager>();
        if (_genitals.Count == 0 || HasDefaultGenitals(prototypes, Species.Id, previousSex))
            _genitals = DefaultGenitals(prototypes, Species.Id, Sex);

        SanitizeGenitals(prototypes);
    }

    private bool HasDefaultGenitals(IPrototypeManager prototypes, string speciesId, Sex sex)
    {
        var defaults = DefaultGenitals(prototypes, speciesId, sex);
        return _genitals.Count == defaults.Count &&
            _genitals.All(entry => defaults.TryGetValue(entry.Key, out var data) && entry.Value.Equals(data));
    }

    private bool GenitalFieldsEqual(HumanoidCharacterProfile other)
    {
        if (_genitals.Count != other._genitals.Count)
            return false;

        foreach (var (category, data) in _genitals)
        {
            if (!other._genitals.TryGetValue(category, out var otherData) || !data.Equals(otherData))
                return false;
        }

        return true;
    }

    private void SanitizeGenitals(IPrototypeManager prototypes)
    {
        var speciesId = Species.Id;

        foreach (var category in _genitals.Keys.ToArray())
        {
            if (!CoreGenitalCategories.Contains(category) ||
                !GenitalRestrictions.IsCategoryAllowed(prototypes, speciesId, Sex, category))
            {
                _genitals.Remove(category);
                continue;
            }

            var data = _genitals[category];
            if (!GenitalRestrictions.IsShapeAllowed(prototypes, speciesId, Sex, category, data.Shape))
                data.Shape = GenitalRestrictions.DefaultShape(prototypes, speciesId, category);
            if (!GenitalRestrictions.AllowedFluids(prototypes, speciesId, category).Contains(data.FluidId))
                data.FluidId = GenitalRestrictions.DefaultFluid(prototypes, speciesId, category);
            _genitals[category] = data;
        }

        foreach (var category in CoreGenitalCategories)
        {
            if (!GenitalRestrictions.IsCategoryAllowed(prototypes, speciesId, Sex, category) || _genitals.ContainsKey(category))
                continue;

            var data = GenitalProfileData.Default(category);
            data.Shape = GenitalRestrictions.DefaultShape(prototypes, speciesId, category);
            data.FluidId = GenitalRestrictions.DefaultFluid(prototypes, speciesId, category);
            data.Present = GenitalRestrictions.IsAlwaysPresent(prototypes, speciesId, Sex, category);
            _genitals[category] = data;
        }
    }

    private void EnsureGenitalFieldsValid(IPrototypeManager prototypes)
    {
        SanitizeGenitals(prototypes);

        var allowTransparent = GenitalRestrictions.Config(prototypes, Species.Id).AllowTransparentColor;

        foreach (var category in _genitals.Keys.ToArray())
        {
            if (!prototypes.HasIndex(category))
            {
                _genitals.Remove(category);
                continue;
            }

            var data = _genitals[category].Validated(prototypes, Species.Id, category);
            if (!prototypes.EnumeratePrototypes<GenitalVisualPrototype>().Any(visual =>
                    visual.Category == category &&
                    string.Equals(visual.Shape, data.Shape, StringComparison.OrdinalIgnoreCase)))
            {
                data.Shape = GenitalRestrictions.DefaultShape(prototypes, Species.Id, category);
            }
            if (!allowTransparent)
                data.Color = data.Color.WithAlpha(1f);
            _genitals[category] = data;
        }
    }

    private void AddGenitalFieldsHash(ref HashCode hashCode)
    {
        foreach (var (category, data) in _genitals.OrderBy(entry => entry.Key.Id))
        {
            hashCode.Add(category);
            hashCode.Add(data);
        }
    }
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class GenitalProfileData : IEquatable<GenitalProfileData>
{
    public const float MinimumAlpha = 0.01f;

    [DataField]
    public bool Present;

    [DataField]
    public string Shape = "Human";

    [DataField]
    public bool UseSkinColor = true;

    [DataField]
    public Color Color = Color.White;

    [DataField]
    public float Size = 1f;

    [DataField]
    public float MinSize;

    [DataField]
    public float MaxSize = 20f;

    [DataField]
    public GenitalVisibility Visibility = GenitalVisibility.HiddenByUnderwear;

    [DataField]
    public bool Lactating = true;

    [DataField]
    public string FluidId = "Milk";

    public GenitalProfileData()
    {
    }

    public GenitalProfileData(GenitalProfileData other)
    {
        Present = other.Present;
        Shape = other.Shape;
        UseSkinColor = other.UseSkinColor;
        Color = other.Color;
        Size = other.Size;
        MinSize = other.MinSize;
        MaxSize = other.MaxSize;
        Visibility = other.Visibility;
        Lactating = other.Lactating;
        FluidId = other.FluidId;
    }

    public static GenitalProfileData Default(ProtoId<GenitalCategoryPrototype> category, bool present = false)
    {
        var (shape, size, fluid) = category.Id switch
        {
            "Penis" => ("Human", 6f, "Milk"),
            "Testicles" => ("Single", 2f, "Semen"),
            "Breasts" => ("Pair", 3f, "Milk"),
            "Butt" => ("Pair", 0f, "Milk"),
            "Anus" => ("Donut", 1f, "Milk"),
            _ => ("Human", 1f, "Femcum"),
        };

        return new GenitalProfileData
        {
            Present = present,
            Shape = shape,
            Size = size,
            MinSize = size,
            MaxSize = GetSizeRange(category).Max,
            FluidId = fluid,
        };
    }

    public static (float Min, float Max) GetSizeRange(ProtoId<GenitalCategoryPrototype> category)
    {
        return category.Id switch
        {
            "Penis" => (1f, 20f),
            "Testicles" => (1f, 3f),
            "Breasts" => (1f, 7f),
            "Butt" => (0f, 3f),
            _ => (1f, 1f),
        };
    }

    public GenitalProfileData Validated(IPrototypeManager prototypes, string speciesId, ProtoId<GenitalCategoryPrototype> category)
    {
        var defaults = Default(category);
        var (minSize, maxSize) = GetSizeRange(category);
        var initialSize = Math.Clamp(float.IsFinite(Size) ? Size : defaults.Size, minSize, maxSize);
        var runtimeMin = Math.Clamp(float.IsFinite(MinSize) ? MinSize : minSize, minSize, maxSize);
        var runtimeMax = Math.Clamp(float.IsFinite(MaxSize) ? MaxSize : maxSize, runtimeMin, maxSize);
        var result = new GenitalProfileData(this)
        {
            Shape = Shape,
            Color = new Color(Color.RByte, Color.GByte, Color.BByte)
                .WithAlpha(Math.Clamp(float.IsFinite(Color.A) ? Color.A : 1f, MinimumAlpha, 1f)),
            MinSize = runtimeMin,
            MaxSize = runtimeMax,
            Size = initialSize,
        };

        result.Visibility = !Enum.IsDefined(Visibility)
            ? GenitalVisibility.HiddenByClothes
            : Visibility;

        var fluids = GenitalRestrictions.AllowedFluids(prototypes, speciesId, category);
        result.FluidId = fluids.Contains(FluidId) ? FluidId : GenitalRestrictions.DefaultFluid(prototypes, speciesId, category);
        return result;
    }

    public bool Equals(GenitalProfileData? other)
    {
        return other != null &&
            Present == other.Present &&
            Shape == other.Shape &&
            UseSkinColor == other.UseSkinColor &&
            Color == other.Color &&
            Size.Equals(other.Size) &&
            MinSize.Equals(other.MinSize) &&
            MaxSize.Equals(other.MaxSize) &&
            Visibility == other.Visibility &&
            Lactating == other.Lactating &&
            FluidId == other.FluidId;
    }

    public override bool Equals(object? obj) => obj is GenitalProfileData other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Present);
        hash.Add(Shape);
        hash.Add(UseSkinColor);
        hash.Add(Color);
        hash.Add(Size);
        hash.Add(MinSize);
        hash.Add(MaxSize);
        hash.Add(Visibility);
        hash.Add(Lactating);
        hash.Add(FluidId);
        return hash.ToHashCode();
    }
}
