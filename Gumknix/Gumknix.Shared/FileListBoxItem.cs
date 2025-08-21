using System;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Wireframe;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;

namespace Gumknix
{
    public class FileListBoxItem : ListBoxItem
    {
        public TextRuntime Icon { get; private set; }

        private TextRuntime _lastModified;
        private TextRuntime _size;

        public FileListBoxItem(InteractiveGue gue) : base(gue)
        {
            TextRuntime text = Visual.GetGraphicalUiElementByName("TextInstance") as TextRuntime;
            text.X = 30;
            text.XOrigin = HorizontalAlignment.Left;
            text.XUnits = GeneralUnitType.PixelsFromSmall;

            Icon = new TextRuntime()
            {
                Font = "FluentSymbolSet",
                FontSize = 48,
                FontScale = 0.5f,
                Text = "\uE651",
                X = 3,
                Y = 3,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Width = -100,
                WidthUnits = DimensionUnitType.RelativeToParent
            };
            Visual.AddChild(Icon);

            _lastModified = new TextRuntime()
            {
                Text = " ",
                X = -100,
                XOrigin = HorizontalAlignment.Right,
                XUnits = GeneralUnitType.PixelsFromLarge,
                Y = 0,
                YOrigin = VerticalAlignment.Center,
                YUnits = GeneralUnitType.PixelsFromMiddle,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Width = 0,
                WidthUnits = DimensionUnitType.RelativeToParent
            };
            Visual.AddChild(_lastModified);

            _size = new TextRuntime()
            {
                Text = " ",
                X = -10,
                XOrigin = HorizontalAlignment.Right,
                XUnits = GeneralUnitType.PixelsFromLarge,
                Y = 0,
                YOrigin = VerticalAlignment.Center,
                YUnits = GeneralUnitType.PixelsFromMiddle,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Width = 0,
                WidthUnits = DimensionUnitType.RelativeToParent
            };
            Visual.AddChild(_size);
        }

        public override void UpdateToObject(object obj)
        {
            FileSystemItem fileSystemItem = obj as FileSystemItem;
            fileSystemItem.FileListBoxItem = this;
            coreText.RawText = fileSystemItem.Name;

            Icon.Text = fileSystemItem.Icon;
            if (fileSystemItem.Type == FileSystemItem.Types.File)
            {
                if (fileSystemItem.LastModified != DateTime.MinValue)
                    _lastModified.Text = fileSystemItem.LastModified.ToLocalTime().ToString();

                if (fileSystemItem.Size > 0)
                    _size.Text = FileSystemItem.GetMetricSize(fileSystemItem.Size);
            }
        }

        public void UpdateText(string text)
        {
            coreText.RawText = text;
        }
    }
}
