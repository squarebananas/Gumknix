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

        private ColoredRectangleRuntime _background;
        private StackPanel _stackPanel;
        private TaskBarStartButton _startButton;

        private StackPanel _startStackPanel;
        private TextRuntime _startLabel;
        private ColoredRectangleRuntime _startBackground;
        private MenuItem _startMenu;
        private ColoredRectangleRuntime _startDropShadow;

        public bool IsStartOpen => _startStackPanel.Visual.Parent != null;

        internal TaskBar(Gumknix gumknix) : base(gumknix)
        {
            Gumknix = gumknix;

            _background = new()
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
                Height = 48,
                HeightUnits = DimensionUnitType.Absolute
            };
            _background.AddToRoot();
            GumService.Default.Renderer.MainLayer.Remove(_background);
            Layer.Add(_background);

            _stackPanel = new();
            _stackPanel.Orientation = Orientation.Horizontal;
            _stackPanel.Spacing = 10;
            _stackPanel.Visual.Anchor(Anchor.Bottom);
            _stackPanel.Visual.Height = 48f;
            _stackPanel.Visual.HeightUnits = DimensionUnitType.Absolute;
            _stackPanel.Visual.StackSpacing = 10;
            _stackPanel.Dock(Dock.FillHorizontally);
            _stackPanel.Visual.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
            _stackPanel.Visual.AddToRoot();
            GumService.Default.Renderer.MainLayer.Remove(_stackPanel.Visual);
            Layer.Add(_stackPanel.Visual);

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

            _startStackPanel = new();
            _startStackPanel.Orientation = Orientation.Horizontal;
            _startStackPanel.X = 20;
            _startStackPanel.Visual.XOrigin = HorizontalAlignment.Left;
            _startStackPanel.Visual.XUnits = GeneralUnitType.PixelsFromSmall;
            _startStackPanel.Y = -58;
            _startStackPanel.Visual.YOrigin = VerticalAlignment.Bottom;
            _startStackPanel.Visual.YUnits = GeneralUnitType.PixelsFromLarge;
            _startStackPanel.Visual.Width = 300;
            _startStackPanel.Visual.WidthUnits = DimensionUnitType.Absolute;
            _startStackPanel.Visual.Height = 400f;
            _startStackPanel.Visual.HeightUnits = DimensionUnitType.Absolute;

            ColoredRectangleRuntime startBackgroundBar = new()
            {
                Name = "StartBackgroundBar",
                Color = Color.White,
                Width = 30,
                WidthUnits = DimensionUnitType.Absolute
            };
            startBackgroundBar.Anchor(Anchor.Left);
            startBackgroundBar.Dock(Dock.FillVertically);
            _startStackPanel.Visual.AddChild(startBackgroundBar);

            _startBackground = new();
            _startBackground.Name = "StartBackground";
            _startBackground.Color = new Color(4, 120, 137);
            _startBackground.Width = -30;
            _startBackground.WidthUnits = DimensionUnitType.RelativeToParent;
            _startBackground.Anchor(Anchor.Left);
            _startBackground.Dock(Dock.FillVertically);
            _startStackPanel.Visual.AddChild(_startBackground);

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
                GraphicalUiElement gue = Menu.PopupRoot.Children[0] as GraphicalUiElement;
                if ((gue != null) && (gue.Layer != Layer))
                    if (Layer.ContainsRenderable(gue) == false)
                        Layer.Add(gue);

                if (GumService.Default.Cursor.PrimaryClick &&
                    (_startStackPanel.Visual.Parent != null) &&
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
            _startDropShadow.AddToRoot();
            _startStackPanel.AddToRoot();
            GumService.Default.Renderer.MainLayer.Remove(_startDropShadow);
            GumService.Default.Renderer.MainLayer.Remove(_startStackPanel.Visual);
            Layer.Add(_startDropShadow);
            Layer.Add(_startStackPanel.Visual);

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
            _startMenu.Show(Layer);

            (Menu.PopupRoot.Children[0].Children[0] as GraphicalUiElement).Visible = false;
        }

        public void CloseStart()
        {
            _startMenu.HidePopupRecursively();
            _startMenu = null;

            _startDropShadow.RemoveFromRoot();
            _startStackPanel.RemoveFromRoot();
            Layer.Remove(_startDropShadow);
            Layer.Remove(_startStackPanel.Visual);
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
            Layer.Add(dialog.Window.Visual);
        }

        internal void MoveToFront()
        {
            _background.RemoveFromRoot();
            _stackPanel.RemoveFromRoot();
            _background.AddToRoot();
            _stackPanel.AddToRoot();

            Layer.Remove(_background);
            Layer.Remove(_stackPanel.Visual);
            GumService.Default.Renderer.RemoveLayer(Layer);
            Layer = new();
            GumService.Default.Renderer.AddLayer(Layer);
            Layer.Add(_background);
            Layer.Add(_stackPanel.Visual);
        }

        internal void ApplyTheme(SettingsThemes.Theme theme)
        {
            _background.Color = theme.GumStyling.Colors.PrimaryDark;
            _startBackground.Color = theme.GumStyling.Colors.PrimaryDark;
            _startButton.ApplyTheme(theme);
        }
    }
}
