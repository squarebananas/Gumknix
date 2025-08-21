using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;

namespace Gumknix
{
    public class TaskBarStartButton : Button
    {
        VariableSave backgroundEnabledColor;
        VariableSave backgroundHighlightedColor;
        VariableSave backgroundPushedColor;

        VariableSave textEnabledColor;
        VariableSave textHighlightedColor;
        VariableSave textPushedColor;

        VariableSave iconEnabledColor;
        VariableSave iconHighlightedColor;
        VariableSave iconPushedColor;

        internal TaskBarStartButton(TaskBar taskBar)
        {
            Text = "LetsAGo";
            GetVisual("TextInstance").X = 10;
            Visual.Width -= 10;
            Visual.Anchor(Gum.Wireframe.Anchor.Left);
            Visual.X = 20;
            Visual.AddChild(new TextRuntime()
            {
                Name = "Icon",
                Font = "FluentSymbolSet",
                FontSize = 48,
                FontScale = 0.5f,
                Text = "\uE67E",
                X = 8,
                Y = 3,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Width = 0,
                WidthUnits = DimensionUnitType.RelativeToParent
            });

            List<StateSave> states = Visual.Categories["ButtonCategory"].States;

            StateSave enabledState = states.First(item => item.Name == EnabledStateName);
            StateSave highlightedState = states.First(item => item.Name == HighlightedStateName);
            StateSave pushedState = states.First(item => item.Name == PushedStateName);

            bool v2Style = Gum.Forms.DefaultVisuals.Styling.ActiveStyle != null;

            string variableName = (v2Style ? "" : "Button") + "Background.Color";
            backgroundEnabledColor = enabledState.Variables.GetVariableSave(variableName);
            backgroundHighlightedColor = highlightedState.Variables.GetVariableSave(variableName);
            backgroundPushedColor = pushedState.Variables.GetVariableSave(variableName);

            variableName = "TextInstance.Color";
            textEnabledColor = enabledState.Variables.GetVariableSave(variableName);
            textHighlightedColor = highlightedState.Variables.GetVariableSave(variableName);
            textPushedColor = pushedState.Variables.GetVariableSave(variableName);

            variableName = "Icon.Color";
            iconEnabledColor = new VariableSave() { Name = variableName };
            enabledState.Variables.Add(iconEnabledColor);
            iconHighlightedColor = new VariableSave() { Name = variableName };
            highlightedState.Variables.Add(iconHighlightedColor);
            iconPushedColor = new VariableSave() { Name = variableName };
            pushedState.Variables.Add(iconPushedColor);

            UpdateState();

            Click += (s, e) =>
            {
                if (taskBar.IsStartOpen == false)
                    taskBar.ShowStart();
                else
                    taskBar.CloseStart();
            };
        }

        internal void ApplyTheme(SettingsThemes.Theme theme)
        {
            backgroundEnabledColor.Value = theme.StartButtonColor;
            backgroundHighlightedColor.Value = theme.StartButtonHighlightedColor;
            backgroundPushedColor.Value = theme.StartButtonPushedColor;

            textEnabledColor.Value = iconEnabledColor.Value = theme.StartButtonFontColor;
            textHighlightedColor.Value = iconHighlightedColor.Value = theme.StartButtonFontHighlightedColor;
            textPushedColor.Value = iconPushedColor.Value = theme.StartButtonFontPushedColor;

            UpdateState();
        }
    }
}
