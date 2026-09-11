// SPDX-FileCopyrightText: 2026 Space Veil Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Veil.Genitals;
using Content.Shared.Preferences;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using System.Linq;
using System.Numerics;
using Robust.Shared.Utility;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Client._Veil.Genitals;

[UsedImplicitly]
public sealed class GenitalManagerBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private DefaultWindow? _window;
    private BoxContainer? _content;
    private Label? _organsHeader;
    private Label? _emptyLabel;
    private BoxContainer? _organsBox;
    private readonly SpriteSystem _sprite = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SpriteSystem>();
    private readonly Dictionary<string, OrganCard> _cards = new();
    private bool _building;

    private static readonly Dictionary<string, int> CategoryOrder = new()
    {
        ["Penis"] = 0,
        ["Testicles"] = 1,
        ["Vagina"] = 2,
        ["Breasts"] = 3,
        ["Butt"] = 4,
        ["Anus"] = 5,
    };

    private static IOrderedEnumerable<GenitalManagerEntry> OrderOrgans(List<GenitalManagerEntry> organs)
    {
        return organs
            .OrderBy(o => CategoryOrder.TryGetValue(o.Category, out var index) ? index : 100)
            .ThenBy(o => o.Category, StringComparer.Ordinal);
    }

    private static Label SectionLabel(string text)
    {
        return new Label
        {
            Text = text,
            StyleClasses = { "font-bold" },
        };
    }

    protected override void Open()
    {
        base.Open();
        _cards.Clear();
        _window = this.CreateWindow<DefaultWindow>();
        _window.Title = Loc.GetString("genital-manager-title");
        _window.SetSize = new Vector2(600, 800);
        _window.MinSize = new Vector2(400, 500);

        _content = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 8,
            Margin = new Thickness(8),
        };
        _window.Contents.AddChild(_content);

        var genitalsScroll = new ScrollContainer { VerticalExpand = true, HScrollEnabled = false };
        var genitalsContent = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 8,
            Margin = new Thickness(8),
        };
        _organsHeader = SectionLabel(Loc.GetString("genital-manager-organs-header"));
        genitalsContent.AddChild(_organsHeader);
        _emptyLabel = new Label { Text = Loc.GetString("genital-manager-empty") };
        genitalsContent.AddChild(_emptyLabel);
        _organsBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 8,
        };
        genitalsContent.AddChild(_organsBox);
        genitalsScroll.AddChild(genitalsContent);
        _content.AddChild(genitalsScroll);

        if (State != null)
            UpdateState(State);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (_content == null || _organsBox == null || state is not GenitalManagerUiState manager)
            return;

        _building = true;
        try
        {
            if (_emptyLabel != null)
                _emptyLabel.Visible = manager.Organs.Count == 0;
            if (_organsHeader != null)
                _organsHeader.Visible = manager.Organs.Count > 0;

            var seen = new HashSet<string>();
            foreach (var organ in OrderOrgans(manager.Organs))
            {
                seen.Add(organ.Category);
                if (!_cards.TryGetValue(organ.Category, out var card))
                {
                    card = new OrganCard(this, organ.Category);
                    _cards.Add(organ.Category, card);
                    _organsBox.AddChild(card.Root);
                }
                card.Sync(organ);
            }

            var position = 0;
            foreach (var organ in OrderOrgans(manager.Organs))
            {
                if (_cards.TryGetValue(organ.Category, out var card))
                    card.Root.SetPositionInParent(position++);
            }

            foreach (var category in _cards.Keys.ToArray())
            {
                if (seen.Contains(category))
                    continue;

                if (_organsBox != null)
                    _organsBox.RemoveChild(_cards[category].Root);
                _cards.Remove(category);
            }
        }
        finally
        {
            _building = false;
        }
    }

    private static StyleBoxFlat PanelStyle(string background, string border)
    {
        return new StyleBoxFlat
        {
            BackgroundColor = Color.FromHex(background),
            BorderColor = Color.FromHex(border),
            BorderThickness = new Thickness(1),
        };
    }

    private void SendInput(BoundUserInterfaceMessage message)
    {
        if (_building)
            return;

        SendMessage(message);
    }

    private sealed class OrganCard
    {
        public readonly PanelContainer Root;
        private readonly BoxContainer _body;
        private readonly GenitalManagerBoundUserInterface _ui;
        private readonly string _category;
        private readonly Label _status;
        private readonly OptionButton _visibility;
        private readonly Label _sizeLabel;
        private readonly Label _equipmentLabel;
        private readonly Button _removeEquipment;
        private readonly TextureRect _preview;
        private readonly Slider _size;
        private readonly Label _sizeLimits;
        private readonly Label _milk;
        private CheckBox _arousedBox;
        public OrganCard(GenitalManagerBoundUserInterface ui, string category)
        {
            _ui = ui;
            _category = category;

            Root = new PanelContainer
            {
                HorizontalExpand = true,
                PanelOverride = PanelStyle("#1B1E24", "#3F4651"),
            };
            _body = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                SeparationOverride = 4,
                Margin = new Thickness(10),
            };
            Root.AddChild(_body);
            var header = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                SeparationOverride = 6,
            };
            header.AddChild(new Label
            {
                Text = Loc.GetString($"humanoid-profile-editor-genitals-{category.ToLowerInvariant()}"),
                StyleClasses = { "font-bold" },
                HorizontalExpand = true,
            });
            _status = new Label { StyleClasses = { "font-small" } };
            header.AddChild(_status);
            _body.AddChild(header);
            _body.AddChild(new PanelContainer { StyleClasses = { "LowDivider" } });

            _preview = new TextureRect
            {
                SetSize = new Vector2(96, 96),
                Stretch = TextureRect.StretchMode.KeepAspectCentered,
                HorizontalAlignment = Control.HAlignment.Center,
            };
            _body.AddChild(_preview);

            var visibilityRow = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                SeparationOverride = 8,
            };
            visibilityRow.AddChild(new Label
            {
                Text = Loc.GetString("genital-setting-visibility"),
                MinWidth = 140,
                VerticalAlignment = Control.VAlignment.Center,
            });
            _visibility = new OptionButton { HorizontalExpand = true };
            foreach (GenitalVisibility mode in Enum.GetValues<GenitalVisibility>())
                _visibility.AddItem(Loc.GetString($"genital-visibility-{VisibilityLocSuffix(mode)}"), (int) mode);
            _visibility.OnItemSelected += args =>
            {
                _visibility.SelectId(args.Id);
                ui.SendInput(new GenitalSetVisibilityMessage(_category, (GenitalVisibility) args.Id));
            };
            visibilityRow.AddChild(_visibility);
            _body.AddChild(visibilityRow);

            _sizeLabel = new Label { StyleClasses = { "font-small" } };
            var (minSize, maxSize) = GenitalProfileData.GetSizeRange(new(category));
            _sizeLabel.Visible = maxSize > minSize;
            _body.AddChild(_sizeLabel);
            _size = new Slider
            {
                HorizontalExpand = true,
                Rounded = true,
            };
            _size.OnValueChanged += _ =>
            {
                _sizeLabel.Text = Loc.GetString("genital-manager-size",
                    ("current", _size.Value.ToString("F1")));
            };
            _size.OnReleased += _ => ui.SendInput(new GenitalSetSizeMessage(_category, _size.Value));
            _body.AddChild(_size);
            _sizeLimits = new Label { StyleClasses = { "font-small" } };
            _body.AddChild(_sizeLimits);

            _equipmentLabel = new Label { StyleClasses = { "font-small" } };
            _body.AddChild(_equipmentLabel);
            _milk = new Label { StyleClasses = { "font-small" }, Visible = false };
            _body.AddChild(_milk);
            _removeEquipment = new Button
            {
                Text = Loc.GetString("genital-equipment-remove"),
                HorizontalExpand = true,
            };
            _removeEquipment.OnPressed += _ => ui.SendInput(new GenitalEquipmentRemoveMessage(_category));
            _body.AddChild(_removeEquipment);
            _arousedBox = new CheckBox { Text = Loc.GetString("genital-manager-aroused"), Visible = false };
            _arousedBox.OnToggled += _ =>
            {
                _ui.SendInput(new GenitalSetArousedMessage(_category, _arousedBox.Pressed));
            };
            _body.AddChild(_arousedBox);
        }

        public void Sync(GenitalManagerEntry organ)
        {
            if (_visibility.SelectedId != (int) organ.Visibility)
                _visibility.SelectId((int) organ.Visibility);

            SyncOptional(organ);

            var sizeText = Loc.GetString("genital-manager-size",
                ("current", organ.Size.ToString("F1")));
            if (_sizeLabel.Text != sizeText)
                _sizeLabel.Text = sizeText;

            _size.Visible = organ.CanResize;
            _sizeLabel.Visible = organ.CanResize;
            _sizeLimits.Visible = organ.CanResize;
            if (organ.CanResize)
            {
                _size.MinValue = organ.MinSize;
                _size.MaxValue = organ.MaxSize;
                if (!_size.Grabbed && Math.Abs(_size.Value - organ.Size) > 0.001f)
                    _size.Value = organ.Size;
                _sizeLimits.Text = Loc.GetString("genital-runtime-size-limits",
                    ("minimum", organ.MinSize.ToString("F1")), ("maximum", organ.MaxSize.ToString("F1")));
            }

            _preview.Visible = !string.IsNullOrEmpty(organ.PreviewRsi) && !string.IsNullOrEmpty(organ.PreviewState);
            if (_preview.Visible)
            {
                _preview.Texture = _ui._sprite.Frame0(new SpriteSpecifier.Rsi(
                    SpriteSpecifierSerializer.TextureRoot / new ResPath(organ.PreviewRsi), organ.PreviewState));
                _preview.ModulateSelfOverride = organ.PreviewColor;
            }

            var statusText = Loc.GetString($"genital-visibility-{VisibilityLocSuffix(organ.Visibility)}");
            if (_status.Text != statusText)
                _status.Text = statusText;

            _equipmentLabel.Visible = organ.EquipmentName != null;
            _removeEquipment.Visible = organ.EquipmentName != null;
            _equipmentLabel.Text = organ.EquipmentName == null
                ? string.Empty
                : Loc.GetString("genital-equipment-installed", ("item", organ.EquipmentName));

            _milk.Visible = organ.MilkAmount != null;
            if (_category == "Vagina" || _category == "Testicles")
            {
                _milk.Text = organ.MilkAmount == null
                    ? string.Empty
                    : Loc.GetString("genital-fluid-level",
                        ("fluid", organ.FluidName ?? string.Empty),
                        ("amount", organ.MilkAmount.Value.ToString("F1")),
                        ("capacity", organ.MilkCapacity?.ToString("F1") ?? "0"));
            }
            else
            {
                _milk.Text = organ.MilkAmount == null
                    ? string.Empty
                    : Loc.GetString("genital-lactation-level",
                        ("amount", organ.MilkAmount.Value.ToString("F1")),
                        ("capacity", organ.MilkCapacity?.ToString("F1") ?? "0"));
            }

        }

        private void SyncOptional(GenitalManagerEntry organ)
        {
            _arousedBox.Visible = organ.CanArouse;
            if (_arousedBox.Pressed != organ.Aroused)
                _arousedBox.Pressed = organ.Aroused;
        }
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
}
