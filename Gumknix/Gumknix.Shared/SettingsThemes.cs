using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Gum.Forms.DefaultVisuals;

namespace Gumknix
{
    public class SettingsThemes
    {
        public Gumknix GumknixInstance { get; init; }

        public class Theme
        {
            public string Name { get; set; }
            public Styling GumStyling { get; set; }

            public Color DesktopColor { get; set; }

            public Color StartButtonColor { get; set; } = Color.White;
            public Color StartButtonHighlightedColor { get; set; } = Color.LightGray;
            public Color StartButtonPushedColor { get; set; } = Color.Black;
            public Color StartButtonFontColor { get; set; } = Color.Black;
            public Color StartButtonFontHighlightedColor { get; set; } = Color.Black;
            public Color StartButtonFontPushedColor { get; set; } = Color.White;

        }

        public List<Theme> AllThemes { get; private set; } = new()
        {
            new Theme()
            {
                Name = "GumflowerBlue",
                GumStyling = new Styling(Styling.ActiveStyle.SpriteSheet),
                DesktopColor = Color.CornflowerBlue
            },
            new Theme()
            {
                Name = "Dark",
                GumStyling = new Styling(Styling.ActiveStyle.SpriteSheet)
                {
                    Colors =
                    {
                        Primary = new Color(45, 45, 48),
                        PrimaryLight = new Color(63, 63, 70),
                        PrimaryDark = new Color(28, 28, 28),
                        Accent = new Color(0, 122, 204),
                        Warning = new Color(232, 17, 35),
                        DarkGray = new Color(51, 51, 55),
                        LightGray = new Color(191, 191, 191),
                        White = Color.White,
                        Black = Color.Black,
                    }
                },
                DesktopColor = new Color(10, 10, 10)
            },
            new Theme()
            {
                Name = "Moonlight GB",
                GumStyling = new Styling(Styling.ActiveStyle.SpriteSheet)
                {
                    Colors =
                    {
                        Primary = new Color(32, 54, 113),
                        PrimaryLight = new Color(64, 86, 145),
                        PrimaryDark = new Color(16, 38, 97),
                        Accent = new Color(54, 134, 143),
                        Warning = new Color(74, 122, 150),
                        DarkGray = new Color(41, 40, 49),
                        White = new Color(95, 199, 93)
                    }
                },
                DesktopColor = new Color(15, 5, 45)
            },
            new Theme()
            {
                Name = "Twilight 5",
                GumStyling = new Styling(Styling.ActiveStyle.SpriteSheet)
                {
                    Colors =
                    {
                        Primary = new Color(238, 134, 149),
                        PrimaryLight = new Color(255, 155, 175),
                        PrimaryDark = new Color(210, 110, 120),
                        Accent = new Color(74, 122, 150),
                        Warning = new Color(74, 122, 150),
                        DarkGray = new Color(41, 40, 49),
                        White = new Color(95, 199, 93)
                    }
                },
                DesktopColor = new Color(51, 63, 88)
            },
            new Theme()
            {
                Name = "Sun Set Drive",
                GumStyling = new Styling(Styling.ActiveStyle.SpriteSheet)
                {
                    Colors =
                    {
                        Primary = new Color(217, 99, 48),
                        PrimaryLight = new Color(229, 156, 82),
                        PrimaryDark = new Color(182, 53, 27),
                        White = new Color(247, 232, 181),
                        Accent = new Color(144, 19, 75),
                        Danger = new Color(99, 13, 72)
                    }
                },
                DesktopColor = new Color(240, 200, 132)
            },
            new Theme()
            {
                Name = "Ash Persimmon Six",
                GumStyling = new Styling(Styling.ActiveStyle.SpriteSheet)
                {
                    Colors =
                    {
                        Primary = new Color(98, 65, 65),
                        White = new Color(225, 202, 209),
                        PrimaryLight = new Color(132, 105, 100),
                        PrimaryDark = new Color(36, 17, 18),
                        Accent = new Color(255, 161, 74)
                    }
                },
                DesktopColor = new Color(225, 202, 209)
            },
            new Theme()
            {
                Name = "Megahard Framedglass XP",
                GumStyling = new Styling(Styling.ActiveStyle.SpriteSheet)
                {
                    Colors =
                    {
                        Primary = new Color(98, 65, 65),
                        White = new Color(225, 202, 209),
                        PrimaryLight = new Color(132, 105, 100),
                        PrimaryDark = new Color(22, 33, 201),
                        Accent = new Color(255, 161, 74)
                    }
                },
                DesktopColor = Color.CornflowerBlue,
                StartButtonColor = new(70, 170, 70),
                StartButtonHighlightedColor = new(80, 180, 80),
                StartButtonPushedColor = new(35, 85, 35),
                StartButtonFontColor = Color.White,
                StartButtonFontHighlightedColor = Color.Black,
                StartButtonFontPushedColor = Color.Black
            }
        };

        public Theme CurrentTheme { get; private set; }

        public SettingsThemes(Gumknix gumknix)
        {
            GumknixInstance = gumknix;
            ApplyTheme(AllThemes[0]);
        }

        public void ApplyTheme(Theme theme)
        {
            if (CurrentTheme == theme)
                return;

            CurrentTheme = theme;
            Styling.ActiveStyle = CurrentTheme.GumStyling;

            GumknixInstance.TaskBar.ApplyTheme(theme);
        }
    }
}
