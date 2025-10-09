using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Wireframe;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Gum.Forms.DefaultVisuals;

namespace Gumknix
{
    public class TaskBar : BaseSystemVisual
    {
        public Gumknix Gumknix { get; private set; }

        public Panel Panel { get; init; }

        public int Height { get; private set; }

        public ColoredRectangleRuntime Background { get; init; }
        private StackPanel _stackPanel;
        private TaskBarStartButton _startButton;

        public StackPanel StartStackPanel { get; init; }
        private TextRuntime _startLabel;
        private ColoredRectangleRuntime _startBackground;
        private MenuItem _startMenu;
        private ColoredRectangleRuntime _startDropShadow;

        private IRenderableIpso PopupElement;

        public bool IsStartOpen => StartStackPanel.Visual.Parent != null;

        internal TaskBar(Gumknix gumknix) : base(gumknix)
        {
            Gumknix = gumknix;

            Panel = new();
            Panel.Name = "TaskBar";
            Panel.Visual.Dock(Dock.Fill);
            Panel.Visual.Anchor(Anchor.TopLeft);
            GumknixInstance.GumRenderables.Add(Panel.Visual);

            Height = 48;

            Background = new()
            {
                Name = "Background",
                Color = Styling.ActiveStyle.Colors.PrimaryDark,
                XOrigin = HorizontalAlignment.Left,
                XUnits = GeneralUnitType.PixelsFromSmall,
                X = 0,
                YOrigin = VerticalAlignment.Bottom,
                YUnits = GeneralUnitType.PixelsFromLarge,
                Y = 0,
                Width = 0,
                WidthUnits = DimensionUnitType.RelativeToParent,
                Height = Height,
                HeightUnits = DimensionUnitType.Absolute
            };
            Panel.Visual.AddChild(Background);

            _stackPanel = new();
            _stackPanel.Orientation = Orientation.Horizontal;
            _stackPanel.Spacing = 10;
            _stackPanel.Visual.Anchor(Anchor.Bottom);
            _stackPanel.Visual.Height = Height;
            _stackPanel.Visual.HeightUnits = DimensionUnitType.Absolute;
            _stackPanel.Visual.StackSpacing = 10;
            _stackPanel.Dock(Dock.FillHorizontally);
            _stackPanel.Visual.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
            Panel.Visual.AddChild(_stackPanel);

            _startButton = new(this);
            _stackPanel.AddChild(_startButton);

            _startDropShadow = new();
            _startDropShadow.Color = new Color(0, 0, 0, 20);
            _startDropShadow.X = 17;
            _startDropShadow.XOrigin = HorizontalAlignment.Left;
            _startDropShadow.XUnits = GeneralUnitType.PixelsFromSmall;
            _startDropShadow.Y = -51;
            _startDropShadow.YOrigin = VerticalAlignment.Bottom;
            _startDropShadow.YUnits = GeneralUnitType.PixelsFromLarge;
            _startDropShadow.Width = 310;
            _startDropShadow.WidthUnits = DimensionUnitType.Absolute;
            _startDropShadow.Height = 410f;
            _startDropShadow.HeightUnits = DimensionUnitType.Absolute;

            StartStackPanel = new();
            StartStackPanel.Orientation = Orientation.Horizontal;
            StartStackPanel.X = 20;
            StartStackPanel.Visual.XOrigin = HorizontalAlignment.Left;
            StartStackPanel.Visual.XUnits = GeneralUnitType.PixelsFromSmall;
            StartStackPanel.Y = -58;
            StartStackPanel.Visual.YOrigin = VerticalAlignment.Bottom;
            StartStackPanel.Visual.YUnits = GeneralUnitType.PixelsFromLarge;
            StartStackPanel.Visual.Width = 300;
            StartStackPanel.Visual.WidthUnits = DimensionUnitType.Absolute;
            StartStackPanel.Visual.Height = 400f;
            StartStackPanel.Visual.HeightUnits = DimensionUnitType.Absolute;

            ColoredRectangleRuntime startBackgroundBar = new()
            {
                Name = "StartBackgroundBar",
                Color = Color.White,
                Width = 30,
                WidthUnits = DimensionUnitType.Absolute
            };
            startBackgroundBar.Anchor(Anchor.Left);
            startBackgroundBar.Dock(Dock.FillVertically);
            StartStackPanel.Visual.AddChild(startBackgroundBar);

            _startBackground = new();
            _startBackground.Name = "StartBackground";
            _startBackground.Color = new Color(4, 120, 137);
            _startBackground.Width = -30;
            _startBackground.WidthUnits = DimensionUnitType.RelativeToParent;
            _startBackground.Anchor(Anchor.Left);
            _startBackground.Dock(Dock.FillVertically);
            StartStackPanel.Visual.AddChild(_startBackground);

            _startLabel = new();
            _startLabel.Text = "Gumknix";
            _startLabel.Color = Color.Black;
            _startLabel.X = 25;
            _startLabel.XOrigin = HorizontalAlignment.Left;
            _startLabel.XUnits = GeneralUnitType.PixelsFromSmall;
            _startLabel.Rotation = 90;
            _startLabel.Y = -15;
            _startLabel.YOrigin = VerticalAlignment.Bottom;
            _startLabel.YUnits = GeneralUnitType.PixelsFromLarge;
            startBackgroundBar.AddChild(_startLabel);
        }

        internal void Update()
        {
            if (Menu.PopupRoot.Children.Count >= 1)
            {
                if (GumService.Default.Cursor.PrimaryClick &&
                    (StartStackPanel.Visual.Parent != null) &&
                    (_startButton.GetVisual(
                    (Gum.Forms.DefaultVisuals.Styling.ActiveStyle == null) ? "ButtonBackground" : "Background").HasCursorOver(
                    GumService.Default.Cursor.X, GumService.Default.Cursor.Y) == false) &&
                    (_startBackground.HasCursorOver(GumService.Default.Cursor.X, GumService.Default.Cursor.Y) == false))
                {
                    CloseStart();
                }
            }

            UpdateDialogs();
        }

        public void ShowStart()
        {
            GumknixInstance.GumRenderables.Add(_startDropShadow);
            GumknixInstance.GumRenderables.Add(StartStackPanel.Visual);

            _startMenu = new();
            _startMenu.Header = "";
            _startMenu.Visual.X = _startBackground.AbsoluteLeft - 2;
            _startMenu.Visual.Y = _startBackground.AbsoluteTop;
            _startMenu.Visual.Width = 270;
            _startMenu.Visual.WidthUnits = DimensionUnitType.Absolute;
            if (Gum.Forms.DefaultVisuals.Styling.ActiveStyle == null)
                (_startMenu.Visual.GetGraphicalUiElementByName("Background") as ColoredRectangleRuntime).Color =  Color.Transparent;
            else
                (_startMenu.Visual.GetGraphicalUiElementByName("Background") as NineSliceRuntime).Color = Color.Transparent;

            List<StateSave> states = _startMenu.Visual.Categories["MenuItemCategory"].States;
            for (int i = 0; i < states.Count; i++)
            {
                states[i].Variables.GetVariableSave("Background.Visible").Value = false;
                if (states[i].Variables.GetVariableSave("Background.Color") != null)
                    states[i].Variables.GetVariableSave("Background.Color").Value = Color.White;
            }
            _startMenu.UpdateState();

            MenuItem MenuItemAllApplets = new();
            MenuItemAllApplets.Header = "All Applets                          >";
            MenuItemAllApplets.Visual.Width = 270;
            MenuItemAllApplets.Visual.WidthUnits = DimensionUnitType.Absolute;
            MenuItemAllApplets.Visual.Height = 40;
            MenuItemAllApplets.Visual.HeightUnits = DimensionUnitType.Absolute;
            (MenuItemAllApplets.Visual.GetGraphicalUiElementByName("TextInstance") as TextRuntime).Dock(Dock.FillVertically);
            (MenuItemAllApplets.Visual.GetGraphicalUiElementByName("TextInstance") as TextRuntime).VerticalAlignment =
                VerticalAlignment.Center;
            _startMenu.Items.Add(MenuItemAllApplets);

            for (int i = 0; i < Gumknix.AvailableApplets.Count; i++)
            {
                Type appletType = Gumknix.AvailableApplets[i];
                MenuItem AppletItem = new();
                AppletItem.Header = BaseApplet.GetDefaultTitle(appletType);
                AppletItem.Visual.Width = 220;
                AppletItem.Visual.WidthUnits = DimensionUnitType.Absolute;
                AppletItem.Clicked += (s, e) =>
                {
                    Gumknix.StartApplet(appletType, [null]);
                    CloseStart();
                };
                MenuItemAllApplets.Items.Add(AppletItem);
            }

            MenuItem MenuItemSettings = new();
            MenuItemSettings.Header = "Settings";
            MenuItemSettings.Visual.Width = 270;
            MenuItemSettings.Visual.WidthUnits = DimensionUnitType.Absolute;
            MenuItemSettings.Visual.Height = 40;
            MenuItemSettings.Visual.HeightUnits = DimensionUnitType.Absolute;
            (MenuItemSettings.Visual.GetGraphicalUiElementByName("TextInstance") as TextRuntime).Dock(Dock.FillVertically);
            (MenuItemSettings.Visual.GetGraphicalUiElementByName("TextInstance") as TextRuntime).VerticalAlignment =
                VerticalAlignment.Center;
            _startMenu.Items.Add(MenuItemSettings);

            _startMenu.IsFocused = true;
            _startMenu.IsSelected = true;
            _startMenu.Dock(Dock.SizeToChildren);

            PopupElement = FrameworkElement.PopupRoot.Children[^1];
            (PopupElement.Children[0] as GraphicalUiElement).Visible = false;
        }

        public void CloseStart()
        {
            _startMenu.HidePopupRecursively();
            for (int i = 0; i < _startMenu.Items.Count; i++)
                (_startMenu.Items[i] as MenuItem).Visual.RemoveFromRoot();
            _startMenu.Visual.RemoveFromRoot();
            _startMenu.Close();
            _startMenu = null;

            _startDropShadow.RemoveFromRoot();
            StartStackPanel.Visual.RemoveFromRoot();

            Menu.PopupRoot.Children.Remove(PopupElement);
            PopupElement = null;
        }

        internal void AddRunningApplet(BaseApplet runningApplet)
        {
            _stackPanel.Visual.Children.Add(runningApplet.TaskBarButton.Visual);
        }

        internal void RemoveRunningApplet(BaseApplet runningApplet)
        {
            _stackPanel.Visual.Children.Remove(runningApplet.TaskBarButton.Visual);
        }

        public override void ShowDialog(BaseDialog dialog)
        {
            Dialogs.Add(dialog);
            int panelIndex = GumknixInstance.GumRenderables.IndexOf(Panel.Visual);
            GumknixInstance.GumRenderables.Insert(panelIndex + 1, dialog.Window.Visual);
        }

        internal void ApplyTheme(SettingsThemes.Theme theme)
        {
            Background.Color = theme.GumStyling.Colors.PrimaryDark;
            _startBackground.Color = theme.GumStyling.Colors.PrimaryDark;
            _startButton.ApplyTheme(theme);
        }
    }
}
