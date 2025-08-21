using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;

namespace Gumknix
{
    public class DialogMultiChoice : BaseDialog
    {
        public delegate void OnChoiceSelectedDelegate(object sender, int? choiceIndex);
        public event OnChoiceSelectedDelegate OnChoiceSelected;

        private Label _message;
        private StackPanel _choicesPanel;
        private List<Button> _choiceButtons = [];

        public DialogMultiChoice(BaseSystemVisual systemVisual, string title, string icon, string message, string[] choices) :
            base(systemVisual, title, icon)
        {
            ColoredRectangleRuntime background = new();
            background.Color = new Color(32, 32, 32);
            background.Dock(Dock.Fill);
            background.Anchor(Anchor.TopLeft);
            Window.Visual.Children.Insert(1, background);

            _message = new Label();
            _message.Text = message;
            _message.Visual.MaxWidth = 500;
            (_message.Visual as TextRuntime).HorizontalAlignment = HorizontalAlignment.Center;
            (_message.Visual as TextRuntime).VerticalAlignment = VerticalAlignment.Top;
            _message.Dock(Dock.FillVertically);
            _message.Anchor(Anchor.Top);
            _message.Y = 25;
            MainStackPanel.AddChild(_message);

            _choicesPanel = new StackPanel();
            _choicesPanel.Orientation = Orientation.Horizontal;
            _choicesPanel.Dock(Dock.SizeToChildren);
            _choicesPanel.Anchor(Anchor.Bottom);
            _choicesPanel.Y -= 80;
            _choicesPanel.Visual.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
            _choicesPanel.Visual.StackSpacing = 10;
            MainStackPanel.AddChild(_choicesPanel);

            for (int i = 0; i < choices.Length; i++)
            {
                int index = i;
                Button choiceButton = new();
                choiceButton.Text = choices[index];
                choiceButton.Click += (s, e) =>
                {
                    CloseRequest = true;
                    OnChoiceSelected.Invoke(this, index);
                };
                _choiceButtons.Add(choiceButton);
                _choicesPanel.AddChild(choiceButton);
            }

            MainStackPanel.Dock(Dock.SizeToChildren);
            MainStackPanel.Width = 50;

            Window.Width = MainStackPanel.ActualWidth;
            Window.Height = MainStackPanel.ActualHeight;
            Window.ResizeMode = ResizeMode.NoResize;
        }
    }
}
