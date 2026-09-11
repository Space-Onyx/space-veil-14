// SPDX-FileCopyrightText: 2026 Space Veil Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Numerics;
using Content.Client._Veil.Genitals;
using Content.Shared._Veil.Genitals;
using Content.Shared.Body;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

#pragma warning disable IDE0130
namespace Content.Client.Lobby.UI;
#pragma warning restore IDE0130

public sealed partial class HumanoidProfileEditor
{
    private static readonly ProtoId<GenitalCategoryPrototype>[] GenitalOrder =
    [
        "Penis",
        "Testicles",
        "Vagina",
        "Breasts",
        "Butt",
        "Anus",
    ];

    private static readonly ProtoId<GenitalCategoryPrototype> BreastsCategory = "Breasts";
    private static readonly ProtoId<GenitalCategoryPrototype> ButtCategory = "Butt";

    private bool CanPreviewArousal(ProtoId<GenitalCategoryPrototype> category)
    {
        return GenitalVisualBuilder.SupportsArousal(_prototypeManager, category);
    }

    private readonly Dictionary<ProtoId<GenitalCategoryPrototype>, CheckBox> _genitalToggles = [];
    private readonly Dictionary<ProtoId<GenitalCategoryPrototype>, OptionButton> _genitalVisibility = [];
    private readonly Dictionary<ProtoId<GenitalCategoryPrototype>, OptionButton> _genitalShapes = [];
    private readonly Dictionary<ProtoId<GenitalCategoryPrototype>, List<string>> _genitalShapeNames = [];
    private readonly Dictionary<ProtoId<GenitalCategoryPrototype>, Control> _genitalShapeRows = [];
    private readonly Dictionary<ProtoId<GenitalCategoryPrototype>, FloatSpinBox> _genitalSizes = [];
    private readonly Dictionary<ProtoId<GenitalCategoryPrototype>, FloatSpinBox> _genitalMinSizes = [];
    private readonly Dictionary<ProtoId<GenitalCategoryPrototype>, FloatSpinBox> _genitalMaxSizes = [];
    private readonly Dictionary<ProtoId<GenitalCategoryPrototype>, List<Control>> _genitalRuntimeSizeControls = [];
    private readonly Dictionary<ProtoId<GenitalCategoryPrototype>, CheckBox> _genitalSkinToggles = [];
    private readonly Dictionary<ProtoId<GenitalCategoryPrototype>, CheckBox> _genitalLactationToggles = [];
    private readonly Dictionary<ProtoId<GenitalCategoryPrototype>, OptionButton> _genitalFluids = [];
    private readonly Dictionary<ProtoId<GenitalCategoryPrototype>, BoxContainer> _genitalFluidRows = [];
    private readonly Dictionary<ProtoId<GenitalCategoryPrototype>, List<string>> _genitalFluidIds = [];
    private readonly Dictionary<ProtoId<GenitalCategoryPrototype>, CheckBox> _genitalPreviewToggles = [];
    private readonly Dictionary<ProtoId<GenitalCategoryPrototype>, Button> _genitalColorButtons = [];
    private readonly Dictionary<ProtoId<GenitalCategoryPrototype>, BoxContainer> _genitalColorRows = [];
    private readonly Dictionary<ProtoId<GenitalCategoryPrototype>, Label> _genitalStatus = [];
    private readonly Dictionary<ProtoId<GenitalCategoryPrototype>, BoxContainer> _genitalDetails = [];
    private readonly Dictionary<ProtoId<GenitalCategoryPrototype>, PanelContainer> _genitalCards = [];
    private BoxContainer? _genitalEditor;
    private PanelContainer? _genitalColorCard;
    private ColorSelectorSliders? _genitalColorPicker;
    private string? _genitalColorTarget;
    private bool _updatingGenitals;

    private void InitializeGenitalEditor()
    {
        _genitalEditor = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            SeparationOverride = 8,
            Margin = new Thickness(10),
        };

        var header = new PanelContainer
        {
            HorizontalExpand = true,
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = Color.FromHex("#20232A"),
                BorderColor = Color.FromHex("#454B57"),
                BorderThickness = new Thickness(0, 0, 0, 2),
                ContentMarginLeftOverride = 8,
                ContentMarginTopOverride = 5,
                ContentMarginRightOverride = 8,
                ContentMarginBottomOverride = 5,
            },
        };
        var headerBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 2,
        };
        headerBox.AddChild(new Label
        {
            Text = Loc.GetString("genital-header"),
            StyleClasses = { "LabelKeyText" },
            FontColorOverride = Color.FromHex("#C7D5E0"),
        });
        headerBox.AddChild(new Label
        {
            Text = Loc.GetString("genital-subheader"),
            StyleClasses = { "font-small" },
            FontColorOverride = Color.FromHex("#8F9BAA"),
        });
        header.AddChild(headerBox);
        _genitalEditor.AddChild(header);

        foreach (var category in GenitalOrder)
            AddGenitalCard(category);

        _genitalColorCard = new PanelContainer
        {
            HorizontalExpand = true,
            Visible = false,
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = Color.FromHex("#1B1E24"),
                BorderColor = Color.FromHex("#3F4651"),
                BorderThickness = new Thickness(1),
                ContentMarginLeftOverride = 8,
                ContentMarginTopOverride = 6,
                ContentMarginRightOverride = 8,
                ContentMarginBottomOverride = 6,
            },
        };
        var colorBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 4,
        };
        var colorHeader = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 6,
        };
        colorHeader.AddChild(new Label
        {
            Text = Loc.GetString("genital-setting-color"),
            StyleClasses = { "font-bold" },
            FontColorOverride = Color.FromHex("#C7D5E0"),
            HorizontalExpand = true,
        });
        var colorClose = new Button { Text = "×", MinWidth = 34 };
        colorClose.OnPressed += _ => HideGenitalColorPicker();
        colorHeader.AddChild(colorClose);
        colorBox.AddChild(colorHeader);
        _genitalColorPicker = new ColorSelectorSliders
        {
            SelectorType = ColorSelectorSliders.ColorSelectorType.Hsv,
        };
        _genitalColorPicker.OnColorChanged += OnGenitalColorPicked;
        colorBox.AddChild(_genitalColorPicker);
        _genitalColorCard.AddChild(colorBox);
        _genitalEditor.AddChild(_genitalColorCard);

        var scroll = new ScrollContainer
        {
            VerticalExpand = true,
            HScrollEnabled = false,
        };
        scroll.AddChild(_genitalEditor);
        TabContainer.AddChild(scroll);
        TabContainer.SetTabTitle(TabContainer.ChildCount - 1, Loc.GetString("humanoid-profile-editor-genitals-tab"));
    }

    private void AddGenitalCard(ProtoId<GenitalCategoryPrototype> category)
    {
        var card = new PanelContainer
        {
            HorizontalExpand = true,
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = Color.FromHex("#191C22"),
                BorderColor = Color.FromHex("#454B57"),
                BorderThickness = new Thickness(1),
                ContentMarginLeftOverride = 10,
                ContentMarginTopOverride = 8,
                ContentMarginRightOverride = 10,
                ContentMarginBottomOverride = 8,
            },
        };
        var body = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 4,
        };

        var header = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 6,
        };
        var toggle = new CheckBox
        {
            Text = Loc.GetString($"humanoid-profile-editor-genitals-{category.Id.ToLowerInvariant()}"),
            StyleClasses = { "font-bold" },
        };
        toggle.OnToggled += _ =>
        {
            if (_updatingGenitals)
                return;
            SetGenitalPresence(category, toggle.Pressed);
        };
        _genitalToggles.Add(category, toggle);
        header.AddChild(toggle);
        header.AddChild(new Control { HorizontalExpand = true });

        var status = new Label
        {
            StyleClasses = { "font-small" },
            FontColorOverride = Color.FromHex("#8F9BAA"),
        };
        _genitalStatus.Add(category, status);
        header.AddChild(status);

        body.AddChild(header);

        var details = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 4,
        };
        _genitalDetails.Add(category, details);
        body.AddChild(details);

        details.AddChild(new PanelContainer { StyleClasses = { "LowDivider" } });

            details.AddChild(SectionCaption(Loc.GetString("genital-section-appearance")));

            var shapeRow = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                SeparationOverride = 6,
            };
            shapeRow.AddChild(SettingLabel(Loc.GetString("genital-setting-shape")));
            var shape = new OptionButton { HorizontalExpand = true };
            shape.OnItemSelected += args =>
            {
                if (_updatingGenitals)
                    return;
                if (!_genitalShapeNames.TryGetValue(category, out var names) || args.Id < 0 || args.Id >= names.Count)
                    return;
                shape.SelectId(args.Id);
                var selected = names[args.Id];
                UpdateGenital(category, data => data.Shape = selected);
            };
            _genitalShapes.Add(category, shape);
            _genitalShapeNames.Add(category, []);
            shapeRow.AddChild(shape);
            _genitalShapeRows[category] = shapeRow;
            details.AddChild(shapeRow);

            var colorRow = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                SeparationOverride = 6,
            };
            colorRow.AddChild(SettingLabel(Loc.GetString("genital-setting-color")));
            var skin = new CheckBox { Text = Loc.GetString("genital-skin") };
            skin.OnToggled += _ =>
            {
                if (_updatingGenitals)
                    return;
                UpdateGenital(category, data => data.UseSkinColor = skin.Pressed);
            };
            _genitalSkinToggles.Add(category, skin);
            colorRow.AddChild(skin);
            var color = new Button { Text = "#FFFFFF", MinWidth = 90 };
            color.OnPressed += _ => EditGenitalColor(category);
            _genitalColorButtons.Add(category, color);
            _genitalColorRows.Add(category, colorRow);
            colorRow.AddChild(color);
            details.AddChild(colorRow);

            var (minSize, maxSize) = GenitalProfileData.GetSizeRange(category);
            if (maxSize > minSize)
            {
                var sizePanel = new PanelContainer
                {
                    PanelOverride = new StyleBoxFlat
                    {
                        BackgroundColor = Color.FromHex("#20242C"),
                        BorderColor = Color.FromHex("#343A45"),
                        BorderThickness = new Thickness(1),
                        ContentMarginLeftOverride = 8,
                        ContentMarginTopOverride = 6,
                        ContentMarginRightOverride = 8,
                        ContentMarginBottomOverride = 6,
                    },
                };
                var sizeBox = new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Vertical,
                    SeparationOverride = 5,
                };
                sizeBox.AddChild(SectionCaption(Loc.GetString("genital-setting-size")));
                var sizeFields = new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Horizontal,
                    SeparationOverride = 8,
                };
                var size = new FloatSpinBox(1f, 0)
                {
                    HorizontalExpand = true,
                    IsValid = value => value >= minSize && value <= maxSize,
                    ToolTip = Loc.GetString("genital-setting-start-size-tip", ("minimum", minSize), ("maximum", maxSize)),
                };
                size.OnValueChanged += args =>
                {
                    if (_updatingGenitals)
                        return;
                    UpdateGenital(category, data =>
                    {
                        data.Size = args.Value;
                        data.MinSize = Math.Min(data.MinSize, data.Size);
                        data.MaxSize = Math.Max(data.MaxSize, data.Size);
                    });
                };
                _genitalSizes.Add(category, size);
                sizeFields.AddChild(LabeledField(Loc.GetString("genital-setting-start-size"), size));

                var minimum = new FloatSpinBox(1f, 0)
                {
                    HorizontalExpand = true,
                    IsValid = value => value >= minSize && value <= maxSize,
                    ToolTip = Loc.GetString("genital-setting-min-size-tip"),
                };
                minimum.OnValueChanged += args =>
                {
                    if (_updatingGenitals)
                        return;
                    UpdateGenital(category, data =>
                    {
                        data.MinSize = Math.Clamp(args.Value, minSize, maxSize);
                        data.MinSize = Math.Min(data.MinSize, data.Size);
                        data.MaxSize = Math.Max(data.MaxSize, data.MinSize);
                    });
                };
                _genitalMinSizes.Add(category, minimum);
                var minimumField = LabeledField(Loc.GetString("genital-setting-min-size"), minimum);
                sizeFields.AddChild(minimumField);

                var maximum = new FloatSpinBox(1f, 0)
                {
                    HorizontalExpand = true,
                    IsValid = value => value >= minSize && value <= maxSize,
                    ToolTip = Loc.GetString("genital-setting-max-size-tip"),
                };
                maximum.OnValueChanged += args =>
                {
                    if (_updatingGenitals)
                        return;
                    UpdateGenital(category, data =>
                    {
                        data.MaxSize = Math.Clamp(args.Value, minSize, maxSize);
                        data.MaxSize = Math.Max(data.MaxSize, data.Size);
                        data.MinSize = Math.Min(data.MinSize, data.MaxSize);
                    });
                };
                _genitalMaxSizes.Add(category, maximum);
                var maximumField = LabeledField(Loc.GetString("genital-setting-max-size"), maximum);
                sizeFields.AddChild(maximumField);
                sizeBox.AddChild(sizeFields);
                var sizeHelp = new Label
                {
                    Text = Loc.GetString("genital-setting-size-help"),
                    StyleClasses = { "font-small" },
                    FontColorOverride = Color.FromHex("#8F9BAA"),
                };
                sizeBox.AddChild(sizeHelp);
                sizePanel.AddChild(sizeBox);
                details.AddChild(sizePanel);
            }

            details.AddChild(SectionCaption(Loc.GetString("genital-section-round")));

            var visibilityRow = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                SeparationOverride = 6,
            };
            visibilityRow.AddChild(SettingLabel(Loc.GetString("genital-setting-visibility")));
            var visibility = new OptionButton { HorizontalExpand = true };
            foreach (GenitalVisibility mode in Enum.GetValues<GenitalVisibility>())
                visibility.AddItem(Loc.GetString($"genital-visibility-{VisibilityLocSuffix(mode)}"), (int) mode);
            visibility.OnItemSelected += args =>
            {
                if (_updatingGenitals)
                    return;
                visibility.SelectId(args.Id);
                UpdateGenital(category, data => data.Visibility = (GenitalVisibility) args.Id);
            };
            _genitalVisibility.Add(category, visibility);
            visibilityRow.AddChild(visibility);
            details.AddChild(visibilityRow);

            if (category == BreastsCategory)
            {
                var lactation = new CheckBox { Text = Loc.GetString("genital-setting-lactation") };
                lactation.OnToggled += _ =>
                {
                    if (_updatingGenitals)
                        return;
                    UpdateGenital(category, data => data.Lactating = lactation.Pressed);
                };
                _genitalLactationToggles.Add(category, lactation);
                details.AddChild(lactation);
            }

            if (category == BreastsCategory || category.Id == "Testicles" || category.Id == "Vagina")
            {
                var fluidRow = new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Horizontal,
                    SeparationOverride = 6,
                };
                fluidRow.AddChild(SettingLabel(Loc.GetString("genital-setting-fluid")));
                var fluid = new OptionButton { HorizontalExpand = true };
                fluid.OnItemSelected += args =>
                {
                    if (_updatingGenitals)
                        return;
                    fluid.SelectId(args.Id);
                    if (_genitalFluidIds.TryGetValue(category, out var ids) && args.Id >= 0 && args.Id < ids.Count)
                        UpdateGenital(category, data => data.FluidId = ids[args.Id]);
                };
                _genitalFluids.Add(category, fluid);
                _genitalFluidRows.Add(category, fluidRow);
                fluidRow.AddChild(fluid);
                details.AddChild(fluidRow);
            }

            if (CanPreviewArousal(category))
            {
                var preview = new CheckBox { Text = Loc.GetString("genital-preview-aroused") };
                preview.OnToggled += _ =>
                {
                    if (_updatingGenitals)
                        return;
                    SetGenitalArousalPreview(category, preview.Pressed);
                };
                _genitalPreviewToggles.Add(category, preview);
                details.AddChild(preview);
            }

        card.AddChild(body);
        _genitalCards.Add(category, card);
        _genitalEditor!.AddChild(card);
    }

    private static Label SettingLabel(string text)
    {
        return new Label
        {
            Text = text,
            MinSize = new Vector2(110, 0),
            VerticalAlignment = VAlignment.Center,
            FontColorOverride = Color.FromHex("#C7D5E0"),
        };
    }

    private static Label SectionCaption(string text)
    {
        return new Label
        {
            Text = text,
            StyleClasses = { "font-bold" },
            FontColorOverride = Color.FromHex("#AEB9C7"),
            Margin = new Thickness(0, 3, 0, 1),
        };
    }

    private static BoxContainer LabeledField(string text, Control field)
    {
        var box = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            SeparationOverride = 2,
        };
        box.AddChild(new Label
        {
            Text = text,
            StyleClasses = { "font-small" },
            FontColorOverride = Color.FromHex("#AEB6C2"),
        });
        box.AddChild(field);
        return box;
    }

    private string ShapeLocName(string shape)
    {
        var key = $"genital-shape-{shape.ToLowerInvariant()}";
        return Loc.TryGetString(key, out var text) ? text : shape;
    }

    private string FluidLocName(string fluidId)
    {
        if (_prototypeManager.TryIndex<ReagentPrototype>(fluidId, out var prototype))
            return prototype.LocalizedName;
        return fluidId;
    }

    private List<string> GetGenitalShapes(ProtoId<GenitalCategoryPrototype> category)
    {
        var speciesId = Profile?.Species.Id ?? string.Empty;
        var sex = Profile?.Sex ?? Sex.Male;
        return _prototypeManager.EnumeratePrototypes<GenitalVisualPrototype>()
            .Where(visual => visual.Category == category && !visual.Internal &&
                GenitalRestrictions.IsShapeAllowed(_prototypeManager, speciesId, sex, category, visual.Shape))
            .Select(visual => visual.Shape)
            .Distinct()
            .OrderBy(name => name)
            .ToList();
    }

    private static string VisibilityLocSuffix(GenitalVisibility mode)
    {
        return mode switch
        {
            GenitalVisibility.HiddenByClothes => "hidden-by-clothes",
            GenitalVisibility.HiddenByUnderwear => "hidden-by-underwear",
            GenitalVisibility.AlwaysHidden => "always-hidden",
            _ => "hidden-by-clothes",
        };
    }

    private void EditGenitalColor(ProtoId<GenitalCategoryPrototype> category)
    {
        if (_genitalColorTarget == category.Id && _genitalColorCard is { Visible: true })
        {
            HideGenitalColorPicker();
            return;
        }

        _genitalColorTarget = category.Id;
        if (_genitalColorPicker == null || _genitalEditor == null ||
            Profile?.Genitals.GetValueOrDefault(category) is not { } data)
            return;

        _genitalColorPicker.IsAlphaVisible = GenitalRestrictions.Config(_prototypeManager, Profile.Species.Id).AllowTransparentColor;
        _updatingGenitals = true;
        try
        {
            _genitalColorPicker.Color = data.Color;
        }
        finally
        {
            _updatingGenitals = false;
        }
        if (_genitalColorCard != null)
        {
            _genitalColorCard.Visible = true;
            if (_genitalCards.TryGetValue(category, out var card))
            {
                _genitalEditor.RemoveChild(_genitalColorCard);
                _genitalEditor.AddChild(_genitalColorCard);
                _genitalColorCard.SetPositionInParent(card.GetPositionInParent() + 1);
            }
        }
    }

    private void HideGenitalColorPicker()
    {
        _genitalColorTarget = null;
        if (_genitalColorCard != null)
            _genitalColorCard.Visible = false;
    }

    private void OnGenitalColorPicked(Color color)
    {
        if (_updatingGenitals || _genitalColorTarget == null)
            return;

        foreach (var (category, _) in _genitalColorButtons)
        {
            if (category.Id != _genitalColorTarget)
                continue;

            var allowTransparent = Profile != null &&
                GenitalRestrictions.Config(_prototypeManager, Profile.Species.Id).AllowTransparentColor;
            UpdateGenital(category, data =>
            {
                data.Color = allowTransparent
                    ? color.WithAlpha(Math.Max(color.A, GenitalProfileData.MinimumAlpha))
                    : color.WithAlpha(1f);
                data.UseSkinColor = false;
            });
            break;
        }
    }

    private void SetGenitalArousalPreview(ProtoId<GenitalCategoryPrototype> category, bool aroused)
    {
        if (_settingProfile || _updatingGenitals || Profile == null)
            return;

        var preview = _entManager.System<GenitalPreviewSystem>();
        if (aroused)
            preview.PreviewAroused.Add(category.Id);
        else
            preview.PreviewAroused.Remove(category.Id);

        ReloadProfilePreview();
    }

    private void SetGenitalPresence(ProtoId<GenitalCategoryPrototype> category, bool present)
    {
        if (_settingProfile || _updatingGenitals || Profile == null)
            return;

        var genitals = Profile.Genitals.ToDictionary(entry => entry.Key, entry => new GenitalProfileData(entry.Value));
        if (!genitals.TryGetValue(category, out var data))
        {
            data = GenitalProfileData.Default(category, true);
            genitals.Add(category, data);
        }

        data.Present = present;
        Profile = Profile.WithGenitals(genitals);
        ReloadProfilePreview();
        SyncGenitalCard(category);
    }

    private void UpdateGenital(ProtoId<GenitalCategoryPrototype> category, Action<GenitalProfileData> apply)
    {
        if (_settingProfile || _updatingGenitals || Profile == null)
            return;

        var genitals = Profile.Genitals.ToDictionary(entry => entry.Key, entry => new GenitalProfileData(entry.Value));
        if (!genitals.TryGetValue(category, out var data))
        {
            data = GenitalProfileData.Default(category);
            genitals.Add(category, data);
        }

        apply(data);
        Profile = Profile.WithGenitals(genitals);
        ReloadProfilePreview();
        SyncGenitalCard(category);
    }

    private void RefreshGenitalEditor()
    {
        if (Profile == null)
            return;

        Profile = Profile.WithSanitizedGenitals(_prototypeManager);

        _updatingGenitals = true;
        try
        {
            foreach (var category in GenitalOrder)
                SyncGenitalCardLocked(category);

            var preview = _entManager.System<GenitalPreviewSystem>();
            foreach (var (category, toggle) in _genitalPreviewToggles)
            {
                var pressed = preview.PreviewAroused.Contains(category.Id);
                if (toggle.Pressed != pressed)
                    toggle.Pressed = pressed;
            }

            if (_genitalColorPicker != null && _genitalColorTarget != null)
            {
                foreach (var (category, _) in _genitalColorButtons)
                {
                    if (category.Id != _genitalColorTarget)
                        continue;

                    if (Profile.Genitals.GetValueOrDefault(category) is { } data &&
                        _genitalColorPicker.Color != data.Color)
                    {
                        _genitalColorPicker.IsAlphaVisible = GenitalRestrictions.Config(_prototypeManager, Profile.Species.Id).AllowTransparentColor;
                        _genitalColorPicker.Color = data.Color;
                    }
                    break;
                }
            }
        }
        finally
        {
            _updatingGenitals = false;
        }
    }

    private void SyncGenitalCard(ProtoId<GenitalCategoryPrototype> category)
    {
        if (Profile == null)
            return;

        _updatingGenitals = true;
        try
        {
            SyncGenitalCardLocked(category);
        }
        finally
        {
            _updatingGenitals = false;
        }
    }

    private void SyncGenitalCardLocked(ProtoId<GenitalCategoryPrototype> category)
    {
        if (Profile == null)
            return;

        var allowed = GenitalRestrictions.IsCategoryAllowed(_prototypeManager, Profile.Species.Id, Profile.Sex, category);
        if (_genitalCards.TryGetValue(category, out var card))
            card.Visible = allowed;
        if (!allowed)
            return;

        var data = Profile.Genitals.GetValueOrDefault(category);
        var present = data?.Present == true;
        var fallback = GenitalProfileData.Default(category);
        var current = data ?? fallback;

        if (_genitalToggles.TryGetValue(category, out var toggle) && toggle.Pressed != present)
            toggle.Pressed = present;

        if (_genitalDetails.TryGetValue(category, out var details))
            details.Visible = present;

        if (_genitalVisibility.TryGetValue(category, out var visibility) &&
            visibility.SelectedId != (int) current.Visibility)
            visibility.SelectId((int) current.Visibility);

        var shapes = GetGenitalShapes(category);

        if (_genitalShapes.TryGetValue(category, out var shape))
        {
            if (!_genitalShapeNames.TryGetValue(category, out var cached) || !cached.SequenceEqual(shapes))
            {
                shape.Clear();
                foreach (var shapeName in shapes)
                    shape.AddItem(ShapeLocName(shapeName), shape.ItemCount);
                _genitalShapeNames[category] = shapes;
            }
            var index = shapes.FindIndex(name =>
                string.Equals(name, current.Shape, StringComparison.OrdinalIgnoreCase));
            if (shape.SelectedId != (index < 0 ? 0 : index))
                shape.SelectId(index < 0 ? 0 : index);
        }

        if (_genitalColorRows.TryGetValue(category, out var colorRow))
            colorRow.Visible = shapes.Count > 0;

        if (_genitalShapeRows.TryGetValue(category, out var shapeRow))
            shapeRow.Visible = shapes.Count > 1;

        if (_genitalSizes.TryGetValue(category, out var size) &&
            Math.Abs(size.Value - current.Size) > 0.001f)
            size.Value = current.Size;

        if (_genitalMinSizes.TryGetValue(category, out var minimum) &&
            Math.Abs(minimum.Value - current.MinSize) > 0.001f)
            minimum.Value = current.MinSize;

        if (_genitalMaxSizes.TryGetValue(category, out var maximum) &&
            Math.Abs(maximum.Value - current.MaxSize) > 0.001f)
            maximum.Value = current.MaxSize;

        if (_genitalRuntimeSizeControls.TryGetValue(category, out var runtimeSizeControls))
        {
            var visible = GenitalRestrictions.Config(_prototypeManager, Profile.Species.Id).AllowRuntimeSize;
            foreach (var control in runtimeSizeControls)
                control.Visible = visible;
        }

        if (_genitalSkinToggles.TryGetValue(category, out var skin) &&
            skin.Pressed != current.UseSkinColor)
            skin.Pressed = current.UseSkinColor;

        if (_genitalLactationToggles.TryGetValue(category, out var lactation) &&
            lactation.Pressed != current.Lactating)
            lactation.Pressed = current.Lactating;

        var allowedFluids = GenitalRestrictions.AllowedFluids(_prototypeManager, Profile.Species.Id, category);
        if (_genitalFluids.TryGetValue(category, out var fluid))
        {
            if (!_genitalFluidIds.TryGetValue(category, out var cachedFluids) || !cachedFluids.SequenceEqual(allowedFluids))
            {
                fluid.Clear();
                foreach (var fluidId in allowedFluids)
                    fluid.AddItem(FluidLocName(fluidId), fluid.ItemCount);
                _genitalFluidIds[category] = allowedFluids;
            }
            var index = allowedFluids.IndexOf(current.FluidId);
            if (index >= 0 && fluid.SelectedId != index)
                fluid.SelectId(index);
        }
        if (_genitalFluidRows.TryGetValue(category, out var fluidRow))
            fluidRow.Visible = allowedFluids.Count > 1;

        if (_genitalColorButtons.TryGetValue(category, out var color))
        {
            var hex = (data?.Color ?? Color.White).ToHex();
            if (color.Text != hex)
                color.Text = hex;
            var disabled = data?.UseSkinColor != false;
            if (color.Disabled != disabled)
                color.Disabled = disabled;
        }

        if (_genitalStatus.TryGetValue(category, out var status))
        {
            var text = data?.Present == true
                ? Loc.GetString($"genital-visibility-{VisibilityLocSuffix(data.Visibility)}")
                : Loc.GetString("genital-off");
            if (status.Text != text)
                status.Text = text;
        }
    }
}
