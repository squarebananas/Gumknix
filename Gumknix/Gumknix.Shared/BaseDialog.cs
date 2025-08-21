using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;

namespace Gumknix
{
    public abstract class BaseDialog
    {
        public BaseSystemVisual SystemVisual;

        public string Title { get; private set; }
        public string Icon { get; private set; }

        public Window Window { get; private set; }
        protected StackPanel MainStackPanel { get; set; }

        private Panel _titleBarPanel;
        private ColoredRectangleRuntime _titleBarColoredRectangle;
        private TextRuntime _titleBarIcon;
        private TextRuntime _titleBarLabel;
        private Button _titleBarClose;

        public bool CloseRequest { get; set; }
        public bool IsClosed { get; private set; }


        public BaseDialog(BaseSystemVisual systemVisual, string title, string icon)
        {
            SystemVisual = systemVisual;
            Title = title;
            Icon = icon;

            Window = new();
            Window.Visual.Anchor(Anchor.Center);
            Window.ResizeMode = ResizeMode.NoResize;

            GraphicalUiElement titleBar = Window.Visual.GetGraphicalUiElementByName("TitleBarInstance") as InteractiveGue;
            titleBar.X = 0;
            titleBar.XOrigin = HorizontalAlignment.Left;
            titleBar.XUnits = GeneralUnitType.PixelsFromSmall;
            titleBar.Width = -32;
            titleBar.WidthUnits = DimensionUnitType.RelativeToParent;

            MainStackPanel = new();
            MainStackPanel.Orientation = Orientation.Vertical;
            MainStackPanel.Visual.Dock(Dock.Fill);
            MainStackPanel.Visual.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
            Window.AddChild(MainStackPanel);

            _titleBarPanel = new();
            _titleBarPanel.Visual.Height = 32;
            _titleBarPanel.Visual.HeightUnits = DimensionUnitType.Absolute;
            _titleBarPanel.Visual.Dock(Dock.FillHorizontally);
            MainStackPanel.AddChild(_titleBarPanel);

            _titleBarColoredRectangle = new();
            _titleBarColoredRectangle.Color = new Color(4, 120, 137);
            _titleBarColoredRectangle.Dock(Dock.Fill);
            _titleBarPanel.Visual.AddChild(_titleBarColoredRectangle);

            _titleBarIcon = new();
            _titleBarIcon.Font = "FluentSymbolSet";
            _titleBarIcon.FontSize = 48;
            _titleBarIcon.FontScale = 0.5f;
            _titleBarIcon.Text = Icon;
            _titleBarIcon.X = 5;
            _titleBarIcon.Y = 3;
            _titleBarPanel.Visual.AddChild(_titleBarIcon);

            _titleBarLabel = new();
            _titleBarLabel.Text = Title;
            _titleBarLabel.X = 30;
            _titleBarLabel.Y = 5;
            _titleBarPanel.Visual.AddChild(_titleBarLabel);

            _titleBarClose = new Button();
            _titleBarClose.Visual.Width = 32;
            _titleBarClose.Visual.WidthUnits = DimensionUnitType.Absolute;
            _titleBarClose.Visual.Height = 32;
            _titleBarClose.Visual.HeightUnits = DimensionUnitType.Absolute;
            _titleBarClose.Text = "X";
            _titleBarClose.Visual.Anchor(Anchor.Right);
            _titleBarPanel.AddChild(_titleBarClose);
            _titleBarClose.Click += (s, e) => CloseRequest = true;

            ColoredRectangleRuntime dropShadowColoredRectangle = new();
            dropShadowColoredRectangle.Color = new Color(0, 0, 0, 20);
            dropShadowColoredRectangle.Dock(Dock.Fill);
            dropShadowColoredRectangle.X = -2;
            dropShadowColoredRectangle.XUnits = GeneralUnitType.PixelsFromSmall;
            dropShadowColoredRectangle.XOrigin = HorizontalAlignment.Left;
            dropShadowColoredRectangle.Y = -2;
            dropShadowColoredRectangle.YUnits = GeneralUnitType.PixelsFromSmall;
            dropShadowColoredRectangle.YOrigin = VerticalAlignment.Top;
            dropShadowColoredRectangle.Height = 10;
            dropShadowColoredRectangle.HeightUnits = DimensionUnitType.RelativeToParent;
            dropShadowColoredRectangle.Width = 10;
            dropShadowColoredRectangle.WidthUnits = DimensionUnitType.RelativeToParent;
            Window.Visual.Children.Insert(0, dropShadowColoredRectangle);
        }

        public virtual void Update()
        {
            if (CloseRequest && !IsClosed)
                Close();
            if (IsClosed)
                return;
        }

        protected virtual void Close()
        {
            SystemVisual.Layer.Remove(Window.Visual);
            Window.Close();
            Window.RemoveFromRoot();
            IsClosed = true;
        }

        public virtual FileSystemItem CursorOverFileItem() { return null; }

        public virtual Task FilesDropped(List<FileSystemItem> files, bool userRequestedCopy, bool userRequestedMove) { return null; }
    }
}
