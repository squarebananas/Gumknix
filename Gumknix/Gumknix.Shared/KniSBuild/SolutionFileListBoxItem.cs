using System;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Wireframe;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;

namespace Gumknix
{
    public class SolutionFileListBoxItem : ListBoxItem
    {
        public TextRuntime Icon { get; private set; }

        public SolutionFileListBoxItem(InteractiveGue gue) : base(gue)
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
        }

        public override void UpdateToObject(object obj)
        {
            FileSystemItem fileSystemItem = obj as FileSystemItem;
            coreText.RawText = fileSystemItem.Name;
            Icon.Text = fileSystemItem.Icon;

            if (fileSystemItem.Extension == ".csproj" ||
                fileSystemItem.Extension == ".projitems")
            {

            }
            else
            {
                (Visual.GetGraphicalUiElementByName("TextInstance") as TextRuntime).X += 24;
                Icon.X += 24;
            }
        }
    }
}
