using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;

#if BLAZORGL
using nkast.Wasm.Dom;
#endif

namespace Gumknix
{
    public abstract class BaseApplet : BaseSystemVisual
    {
        public int AppletId { get; init; } = nextAppletId++;
        private static int nextAppletId = 0;

        public string Title { get; private set; }
        public string Icon { get; private set; }

        public Gum.Forms.Window Window { get; private set; }
        public StackPanel MainStackPanel { get; private set; }

        private Panel _titleBarPanel;
        private ColoredRectangleRuntime _titleBarColoredRectangle;
        private TextRuntime _titleBarIcon;
        private TextRuntime _titleBarLabel;
        private Button _titleBarMinimize;
        private Button _titleBarMaximise;
        private Button _titleBarClose;
        private ColoredRectangleRuntime _dropShadowColoredRectangle;

        public TaskBarButton TaskBarButton { get; private set; }

        public bool IsMaximised { get; private set; }
        public Vector2 LastNonMaximisedPosition { get; private set; }
        public Vector2 LastNonMaximisedSize { get; private set; }
        public bool IsClosed { get; private set; }

        public bool MoveToFrontRequest { get; set; }
        public bool MinimizeRequest { get; set; }
        public bool MaximiseRequest { get; set; }
        public bool ResizeRequest { get; set; }
        public bool CloseRequest { get; set; }

        public BaseApplet(Gumknix gumknix, object[] args = null) : base(gumknix) { }

        protected virtual void Initialize(string title, string icon, ResizeMode resizeMode = ResizeMode.CanResize)
        {
            Title = title;
            Icon = icon;

            Window = new();
            Window.Visual.Width = 900;
            Window.Visual.Height = 450;
            Window.Visual.Anchor(Anchor.Center);
            Window.ResizeMode = resizeMode;

            GraphicalUiElement titleBar = Window.Visual.GetGraphicalUiElementByName("TitleBarInstance") as InteractiveGue;
            titleBar.X = 0;
            titleBar.XOrigin = HorizontalAlignment.Left;
            titleBar.XUnits = GeneralUnitType.PixelsFromSmall;
            titleBar.Width = -96;
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

            _titleBarMinimize = new();
            _titleBarMinimize.Visual.Width = 32;
            _titleBarMinimize.Visual.WidthUnits = DimensionUnitType.Absolute;
            _titleBarMinimize.Visual.Height = 32;
            _titleBarMinimize.Visual.HeightUnits = DimensionUnitType.Absolute;
            _titleBarMinimize.Text = "-";
            _titleBarMinimize.Visual.Anchor(Anchor.Right);
            _titleBarMinimize.Visual.X = (resizeMode == ResizeMode.CanResize) ? -62 : -31;
            _titleBarPanel.AddChild(_titleBarMinimize);
            _titleBarMinimize.Click += (s, e) => MinimizeRequest = true;

            if (resizeMode == ResizeMode.CanResize)
            {
                _titleBarMaximise = new();
                _titleBarMaximise.Visual.Width = 32;
                _titleBarMaximise.Visual.WidthUnits = DimensionUnitType.Absolute;
                _titleBarMaximise.Visual.Height = 32;
                _titleBarMaximise.Visual.HeightUnits = DimensionUnitType.Absolute;
                _titleBarMaximise.Text = "[]";
                _titleBarMaximise.Visual.Anchor(Anchor.Right);
                _titleBarMaximise.Visual.X = -31;
                _titleBarPanel.AddChild(_titleBarMaximise);
                _titleBarMaximise.Click += (s, e) =>
                {
                    if (IsMaximised)
                        ResizeRequest = true;
                    else
                        MaximiseRequest = true;
                };
            }

            _titleBarClose = new();
            _titleBarClose.Visual.Width = 32;
            _titleBarClose.Visual.WidthUnits = DimensionUnitType.Absolute;
            _titleBarClose.Visual.Height = 32;
            _titleBarClose.Visual.HeightUnits = DimensionUnitType.Absolute;
            _titleBarClose.Text = "X";
            _titleBarClose.Visual.Anchor(Anchor.Right);
            _titleBarPanel.AddChild(_titleBarClose);
            _titleBarClose.Click += (s, e) => CloseRequest = true;

            _dropShadowColoredRectangle = new();
            _dropShadowColoredRectangle.Color = new Color(0, 0, 0, 20);
            _dropShadowColoredRectangle.Dock(Dock.Fill);
            _dropShadowColoredRectangle.X = -2;
            _dropShadowColoredRectangle.XUnits = GeneralUnitType.PixelsFromSmall;
            _dropShadowColoredRectangle.XOrigin = HorizontalAlignment.Left;
            _dropShadowColoredRectangle.Y = -2;
            _dropShadowColoredRectangle.YUnits = GeneralUnitType.PixelsFromSmall;
            _dropShadowColoredRectangle.YOrigin = VerticalAlignment.Top;
            _dropShadowColoredRectangle.Height = 10;
            _dropShadowColoredRectangle.HeightUnits = DimensionUnitType.RelativeToParent;
            _dropShadowColoredRectangle.Width = 10;
            _dropShadowColoredRectangle.WidthUnits = DimensionUnitType.RelativeToParent;
            Window.Visual.Children.Insert(0, _dropShadowColoredRectangle);

            TaskBarButton = new();
            TaskBarButton.applet = this;
            TaskBarButton.Text = Title;
            TaskBarButton.Visual.Width = 165;
            TaskBarButton.Visual.Anchor(Anchor.Left);
            TextRuntime textInstance = TaskBarButton.GetVisual("TextInstance") as TextRuntime;
            textInstance.MaxWidth = 135;
            textInstance.Anchor(Anchor.Left);
            textInstance.X = 30;
            textInstance.MaxNumberOfLines = 1;
            TaskBarButton.Visual.AddChild(new TextRuntime()
            {
                Name = "IconInstance",
                Font = "FluentSymbolSet",
                FontSize = 48,
                FontScale = 0.5f,
                Text = Icon,
                X = 5,
                Y = 3,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Width = 0,
                WidthUnits = DimensionUnitType.RelativeToParent
            });
            TaskBarButton.Click += (s, e) =>
            {
                if (GumknixInstance.FocusedApplet != this)
                {
                    MoveToFront();
                    Window.Visual.ApplyState(FrameworkElement.FocusedStateName);
                }
                else
                {
                    GumknixInstance.UnfocusApplet();
                    Window.IsVisible = false;
                }
            };
        }

        public virtual void Update()
        {
            if (CloseRequest && !IsClosed)
                Close();
            if (IsClosed)
                return;

            for (int i = 0; i < Menu.PopupRoot.Children.Count; i++)
            {
                GraphicalUiElement gue = Menu.PopupRoot.Children[i] as GraphicalUiElement;
                if ((gue != null) && (gue.Layer != Layer))
                    if (Layer.ContainsRenderable(gue) == false)
                        Layer.Add(gue);
            }

            if (Dialogs.Count >= 1)
                Window.IsEnabled = false;

            UpdateDialogs();
            Window.IsEnabled = Dialogs.Count == 0;

            if (MoveToFrontRequest)
                MoveToFront();

            if (MinimizeRequest)
                Minimize();

            if (MaximiseRequest)
                Maximise();

            if (ResizeRequest)
                Resize();
        }

        public virtual void PostGumUpdate() { }

        public void SetTitle(string title)
        {
            Title = title;
            _titleBarLabel.Text = title;
            TaskBarButton.Text = title;
        }

        public string GetDefaultTitle()
        {
            return GetDefaultTitle(GetType());
        }

        public static string GetDefaultTitle(Type type)
        {
            return type.GetField("DefaultTitle",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)?.GetValue(null) as string;
        }

        public void SetIcon(string icon)
        {
            Icon = icon;
            _titleBarIcon.Text = Icon;
            (TaskBarButton.GetVisual("IconInstance") as TextRuntime).Text = icon;
        }

        public string GetDefaultIcon()
        {
            return GetDefaultIcon(GetType());
        }

        public static string GetDefaultIcon(Type type)
        {
            return type.GetField("DefaultIcon",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).GetValue(null) as string;
        }

        public override void ShowDialog(BaseDialog dialog)
        {
            Dialogs.Add(dialog);
            MoveToFrontRequest = true;
        }

        public virtual void MoveToFront()
        {
            Window.IsVisible = true;
            for (int i = 0; i < Dialogs.Count; i++)
                Dialogs[i].Window.IsVisible = true;

            Window.RemoveFromRoot();
            Window.AddToRoot();
            for (int i = 0; i < Dialogs.Count; i++)
            {
                Dialogs[i].Window.RemoveFromRoot();
                Dialogs[i].Window.AddToRoot();
            }

            if (Layer != null)
            {
                Layer.Remove(Window.Visual);
                for (int i = 0; i < Dialogs.Count; i++)
                    Layer.Remove(Dialogs[i].Window.Visual);
                GumService.Default.Renderer.RemoveLayer(Layer);
            }

            Layer = new();
            Layer.LayerCameraSettings ??= new LayerCameraSettings() { Zoom = 1f };
            GumService.Default.Renderer.AddLayer(Layer);
            Layer.Add(Window.Visual);
            for (int i = 0; i < Dialogs.Count; i++)
                Layer.Add(Dialogs[i].Window.Visual);

            GumknixInstance.FocusApplet(this);
            GumknixInstance.TaskBar.MoveToFront();
            GumknixInstance.TooltipsMoveToFront();

            MoveToFrontRequest = false;
        }

        protected virtual void Minimize()
        {
            if (GumknixInstance.FocusedApplet == this)
                GumknixInstance.UnfocusApplet();
            Window.IsVisible = false;
            for (int j = 0; j < Dialogs.Count; j++)
                Dialogs[j].Window.IsVisible = false;
            MinimizeRequest = false;
        }

        protected virtual void Maximise()
        {
            IsMaximised = true;
            LastNonMaximisedPosition = new Vector2(Window.Visual.AbsoluteLeft, Window.Visual.AbsoluteTop);
            LastNonMaximisedSize = new Vector2(Window.Visual.GetAbsoluteWidth(), Window.Visual.GetAbsoluteHeight());
            Window.IsVisible = true;
            Window.Visual.XOrigin = HorizontalAlignment.Left;
            Window.Visual.XUnits = GeneralUnitType.PixelsFromSmall;
            Window.X = 0;
            Window.Visual.YOrigin = VerticalAlignment.Top;
            Window.Visual.YUnits = GeneralUnitType.PixelsFromSmall;
            Window.Y = 0;
            Window.Width = 0;
            Window.Visual.WidthUnits = DimensionUnitType.RelativeToParent;
            Window.Height = -48;
            Window.Visual.HeightUnits = DimensionUnitType.RelativeToParent;
            MaximiseRequest = false;
        }

        protected virtual void Resize()
        {
            IsMaximised = false;
            Window.IsVisible = true;
            Window.Visual.XOrigin = HorizontalAlignment.Left;
            Window.Visual.XUnits = GeneralUnitType.PixelsFromSmall;
            Window.X = LastNonMaximisedPosition.X;
            Window.Visual.YOrigin = VerticalAlignment.Top;
            Window.Visual.YUnits = GeneralUnitType.PixelsFromSmall;
            Window.Y = LastNonMaximisedPosition.Y;
            Window.Width = LastNonMaximisedSize.X;
            Window.Visual.WidthUnits = DimensionUnitType.Absolute;
            Window.Height = LastNonMaximisedSize.Y;
            Window.Visual.HeightUnits = DimensionUnitType.Absolute;
            ResizeRequest = false;
        }

        protected virtual void Close()
        {
            GumknixInstance.TaskBar.RemoveRunningApplet(this);

            Layer.Remove(Window.Visual);
            GumService.Default.Renderer.RemoveLayer(Layer);

            Window.Close();
            Window.RemoveFromRoot();

            IsClosed = true;
        }

        public virtual FileSystemItem CursorOverFileItem() { return null; }

        public virtual Task FilesDropped(List<FileSystemItem> files, bool userRequestedCopy, bool userRequestedMove) { return null; }

    }
}
