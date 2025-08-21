using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;

namespace Gumknix
{
    public class DesktopIcon : Button
    {
        private Desktop _desktop;
        private Gumknix _gumknix => _desktop.GumknixInstance;

        public enum IconTypes
        {
            Program,
            Folder,
            File
        }

        public IconTypes IconType { get; private set; }
        public TextRuntime IconText { get; private set; }
        public Type AppletType { get; set; } // todo private
        public FileSystemItem FileSystemItem { get; private set; }

        public Point DesktopGridSlot { get; set; }

        public DesktopIcon(Desktop desktop, Type appletType) : base()
        {
            _desktop = desktop;
            IconType = IconTypes.Program;
            AppletType = appletType;
            string name = BaseApplet.GetDefaultTitle(appletType);
            string icon = BaseApplet.GetDefaultIcon(appletType);
            Initialize(name, icon);
        }

        public DesktopIcon(Desktop desktop, FileSystemItem fileSystemItem) : base()
        {
            _desktop = desktop;
            IconType = fileSystemItem.Type == FileSystemItem.Types.Directory ? IconTypes.Folder : IconTypes.File;
            FileSystemItem = fileSystemItem;
            FileSystemItem.DesktopIcon = this;
            AppletType = FileSystemItem.GetDefaultAppletInfo().AppletType;
            Initialize(FileSystemItem.Name, FileSystemItem.Icon);
        }

        private void Initialize(string name, string icon)
        {
            Name = name;
            Text = name;

            Visual.Width = 80;
            Visual.MaxWidth = 80;
            Visual.Height = 110;
            Visual.MaxHeight = 110;

            if (Gum.Forms.DefaultVisuals.Styling.ActiveStyle == null)
            {
                ColoredRectangleRuntime ButtonBackground = Visual.GetGraphicalUiElementByName("ButtonBackground") as ColoredRectangleRuntime;
                ButtonBackground.Visible = false;
                ButtonBackground.Color = Color.Transparent;
            }
            else
            {
                NineSliceRuntime ButtonBackground = Visual.GetGraphicalUiElementByName("Background") as NineSliceRuntime;
                ButtonBackground.Visible = false;
                ButtonBackground.Color = Color.Transparent;
            }

            TextRuntime textRuntime = Visual.GetGraphicalUiElementByName("TextInstance") as TextRuntime;
            textRuntime.Y = 22;
            textRuntime.VerticalAlignment = VerticalAlignment.Top;
            textRuntime.HorizontalAlignment = HorizontalAlignment.Center;
            textRuntime.MaxNumberOfLines = 3;
            textRuntime.TextOverflowHorizontalMode = TextOverflowHorizontalMode.EllipsisLetter;

            IconText = new TextRuntime()
            {
                Font = "FluentSymbolSet",
                FontSize = 48,
                Text = icon,
                HorizontalAlignment = HorizontalAlignment.Center,
                Width = 0,
                WidthUnits = DimensionUnitType.RelativeToParent
            };
            Visual.AddChild(IconText);

            List<StateSave> states = Visual.Categories["ButtonCategory"].States;
            states.First(item => item.Name == EnabledStateName).Variables.GetVariableSave("FocusedIndicator.Visible").Value = false;
            states.First(item => item.Name == FocusedStateName).Variables.GetVariableSave("FocusedIndicator.Visible").Value = true;
            states.First(item => item.Name == HighlightedStateName).Variables.GetVariableSave("FocusedIndicator.Visible").Value = true;
            states.First(item => item.Name == HighlightedFocusedStateName).Variables.GetVariableSave("FocusedIndicator.Visible").Value = true;
            states.First(item => item.Name == PushedStateName).Variables.GetVariableSave("FocusedIndicator.Visible").Value = true;

            Click += (s, e) =>
            {
                if ((GumService.Default.Cursor.PrimaryDoubleClick) && (AppletType != null))
                    _gumknix.StartApplet(AppletType, (FileSystemItem != null) ? [FileSystemItem] : null);

                //RectangleRuntime focusedIndicator = Visual.GetGraphicalUiElementByName("FocusedIndicator") as RectangleRuntime;
                //focusedIndicator.Visible = true;
            };
        }
    }
}
