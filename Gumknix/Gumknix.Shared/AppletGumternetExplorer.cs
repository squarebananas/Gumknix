using System;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Input;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;

using nkast.Wasm.Canvas;
using nkast.Wasm.Dom;
using nkast.Wasm.FileSystem;


namespace Gumknix
{
    public class AppletGumternetExplorer : BaseApplet
    {
        public static readonly string DefaultTitle = "Gumternet Explorer ";
        public static readonly string DefaultIcon = "\uEE71";

        private StackPanel _navigationBar;
        private TextBox _addressTextBox;

#if BLAZORGL
        private Canvas _canvas;
        private HTMLEmbed _containerHTMLEmbed;

        public HTMLElement<Div> _externalDiv;
#endif

        //public string Address { get; set; } = "https://www.squarebananas-games.com";
        public string Address { get; set; } = "https://www.youtube.com/embed/ZuvhN5OUHec?list=PLTWJSIs82sS1Chr8O-58NBraQCH-WBmWz&t=528";// "https://www.youtube.com/embed/8QJT7pAg8p4";

        public AppletGumternetExplorer(Gumknix gumknix, object[] args = null) : base(gumknix, args)
        {
            Initialize(DefaultTitle, DefaultIcon);

            _navigationBar = new();
            _navigationBar.Orientation = Orientation.Horizontal;
            _navigationBar.Visual.Dock(Dock.FillHorizontally);
            _navigationBar.Visual.Anchor(Anchor.TopLeft);
            _navigationBar.Visual.Height = 32;
            _navigationBar.Visual.HeightUnits = DimensionUnitType.Absolute;
            _navigationBar.Visual.StackSpacing = 0;
            _navigationBar.Visual.WrapsChildren = true;
            MainStackPanel.AddChild(_navigationBar);

            _addressTextBox = new TextBox();
            _addressTextBox.Text = Address;
            _addressTextBox.Dock(Dock.Fill);
            _addressTextBox.Visual.X = 150;
            _addressTextBox.Visual.XOrigin = HorizontalAlignment.Left;
            _addressTextBox.Visual.XUnits = GeneralUnitType.PixelsFromSmall;
            _addressTextBox.Visual.Y = 3;
            _addressTextBox.Width -= 180;
            //_addressTextBox.Visual.GetGraphicalUiElementByName("Background").Visible = false;
            _navigationBar.AddChild(_addressTextBox);

            _addressTextBox.KeyDown += (s, e) =>
            {
                if (e.Key == Keys.Enter)
                {
                    Address = _addressTextBox.Text;
                    Navigate(Address);
                    //string IFrameString = $"""
                    //    <iframe src="{Address}" style="border: none; width: 100%; height: 100%;" allow="xr-spatial-tracking" referrerpolicy="strict-origin-when-cross-origin"></iframe>
                    //    """;
                    //_containerHTMLEmbed.SetInnerHTML(IFrameString);
                        _addressTextBox.IsFocused = false;
                    }
                if (e.Key == Keys.Escape)
                {
                    _addressTextBox.Text = Address;
                    _addressTextBox.IsFocused = false;
                }
            };

            ColoredRectangleRuntime background = new();
            background.Color = Gum.Forms.DefaultVisuals.Styling.ActiveStyle.Colors.LightGray;
            background.Dock(Dock.Fill);
            background.Anchor(Anchor.TopLeft);
            Window.Visual.Children.Insert(1, background);

            //Window.Width = 600;
            //Window.Height = 300;

            if (args?.Length >= 1)
            {
                FileSystemItem fileSystemItem = args[0] as FileSystemItem;
                if (fileSystemItem != null)
                {
                    Task task = new(async () =>
                    {
                        FileSystemFileHandle fileSystemFileHandle = fileSystemItem.Handle as FileSystemFileHandle;
                        nkast.Wasm.File.File file = await fileSystemFileHandle.GetFile();
                        string url = nkast.Wasm.Url.Url.CreateObjectURL(file);
                        Navigate(url);
                    });
                    task.Start();
                }

                HTMLElement<Div> HTMLElement = args[0] as HTMLElement<Div>;
                if (HTMLElement != null)
                {
                    _externalDiv = HTMLElement;
                    DOMRect clientBounds = _externalDiv.GetBoundingClientRect();

                    Window.Visual.X = (int)(clientBounds.Left - 1);
                    Window.Visual.XOrigin = HorizontalAlignment.Left;
                    Window.Visual.XUnits = GeneralUnitType.PixelsFromSmall;
                    Window.Visual.Y = (int)(clientBounds.Top - 70);
                    Window.Visual.YOrigin = VerticalAlignment.Top;
                    Window.Visual.YUnits = GeneralUnitType.PixelsFromSmall;
                    Window.Visual.Width = (int)clientBounds.Width - 2;
                    Window.Visual.Height = (int)clientBounds.Height - 51;

                    Address = _addressTextBox.Text = Program.NavigationManager.Uri;
                }
            }

#if BLAZORGL
            if (_externalDiv == null)
                _containerHTMLEmbed = HTMLEmbed.Create($"""
                    <div id = "gumternetAppletId{AppletId}" style="position: absolute; display: none; background-color: #000000; z-index: 3;">
                    </div>
                    """);

            //_HTMLEmbed = HTMLEmbed.Create(
            //$"""
            //<div id = "gumternetAppletId{AppletId}" style="position: absolute; display: none; background-color: #000000">
            //    <iframe srcdoc="{TestPageHtmlBody().Replace("\"", "&quot;")}"
            //        style="border: none; width: 100%; height: 100%;" allow="xr-spatial-tracking"></iframe>
            //</div>
            //""");
            //_HTMLEmbed = HTMLEmbed.Create(
            //    $"""
            //    <div id = "gumternetAppletId{AppletId}" style="position: absolute; display: none; background-color: #000000">
            //    {TestPageDivAndCss().Replace("\"", "&quot;")}
            //    </div>
            //    """);
             //<iframe src="https://docs.monogame.net/articles/tutorials/building_2d_games/06_working_with_textures/index.html"
             //            style="border: none; width: 100%; height: 100%;" allow="xr-spatial-tracking"></iframe>

            //https://squarebananas.github.io/kni-unofficial-webxnar-experiments.github.io/exp2/test/index.html
            //https://docs.monogame.net/articles/tutorials/building_2d_games/06_working_with_textures/index.html
            //https://docs.flatredball.com/gum
#endif
        }

        public override void Update()
        {
#if BLAZORGL
            _canvas ??= nkast.Wasm.Dom.Window.Current.Document.GetElementById<nkast.Wasm.Canvas.Canvas>("theCanvas");
            DOMRect clientBounds = _canvas.GetBoundingClientRect();

            if (_externalDiv != null)
            {
                //_externalDiv.Style.SetProperty("display", "block");
                _externalDiv.Style.SetProperty("overflow", "auto");
                _externalDiv.Style.SetProperty("left", $"{(int)(clientBounds.Left + Window.AbsoluteLeft + 1)}px");
                _externalDiv.Style.SetProperty("top", $"{(int)(clientBounds.Left + Window.AbsoluteTop + 70)}px");
                _externalDiv.Style.SetProperty("width", $"{(int)Window.ActualWidth - 2}px");
                _externalDiv.Style.SetProperty("height", $"{(int)Window.ActualHeight - 51}px");
            }

            if (_containerHTMLEmbed != null)
            {
                _containerHTMLEmbed.Style.SetProperty("display", "block");
                _containerHTMLEmbed.Style.SetProperty("overflow", "hidden");
                _containerHTMLEmbed.Style.SetProperty("left", $"{(int)(clientBounds.Left + Window.AbsoluteLeft + 1)}px");
                _containerHTMLEmbed.Style.SetProperty("top", $"{(int)(clientBounds.Left + Window.AbsoluteTop + 70)}px");
                _containerHTMLEmbed.Style.SetProperty("width", $"{(int)Window.ActualWidth - 2}px");
                _containerHTMLEmbed.Style.SetProperty("height", $"{(int)Window.ActualHeight - 51}px");
                //_HTMLEmbed.Style.SetProperty("mask", $"url(#cutoutUid{_monaco.Uid})");
            }
#endif

            if (MaximiseRequest && _externalDiv != null)
            {
                MaximiseRequest = false;
                GumknixInstance.EmbeddedMode.SwapRequest = true;
            }

            base.Update();
        }

        public void Navigate(string address)
        {
            Address = address;
            _addressTextBox.Text = Address;

            string IFrameString = $"""
                <iframe src="{Address}" style="border: none; width: 100%; height: 100%;" allow="xr xr-spatial-tracking"></iframe>
                """;
            _containerHTMLEmbed.SetInnerHTML(IFrameString);
            _externalDiv = null;
        }

        protected override void Close()
        {
#if BLAZORGL

            if (_externalDiv != null && !GumknixInstance.EmbeddedMode.Animating)
                _externalDiv.Style.SetProperty("display", "none");

            _containerHTMLEmbed?.Remove();
#endif
            base.Close();
        }

        public string TestPageDivAndCss()
        {
            return """
            <style>
                body {
                    font-family: Segoe UI, Arial, sans-serif;
                    background: #181A20;
                    color: #E0E0E0;
                    margin: 0;
                    padding: 0;
                }
                .container {
                    margin-left: 220px;
                    max-width: 900px;
                    background: #23252B;
                    border-radius: 8px;
                    box-shadow: 0 2px 8px #0008;
                    padding: 32px;
                    min-height: 100vh;
                }
                .sidebar {
                    position: fixed;
                    left: 0;
                    top: 0;
                    width: 200px;
                    height: 100vh;
                    background: #20232A;
                    color: #E0E0E0;
                    border-right: 1px solid #2A2D34;
                    padding: 32px 16px 16px 16px;
                    box-sizing: border-box;
                }
                .sidebar h2 {
                    font-size: 1.1em;
                    margin-bottom: 1em;
                    color: #7FB3FF;
                }
                .sidebar ul {
                    list-style: none;
                    padding: 0;
                    margin: 0;
                }
                .sidebar li {
                    margin-bottom: 1em;
                }
                .sidebar a {
                    color: #E0E0E0;
                    text-decoration: none;
                    transition: color 0.2s;
                }
                .sidebar a:hover {
                    color: #7FB3FF;
                }
                h1 {
                    font-size: 2em;
                    margin-bottom: 0.2em;
                    color: #7FB3FF;
                }
                h2 {
                    margin-top: 2em;
                    color: #7FB3FF;
                }
                .type {
                    margin-bottom: 2em;
                }
                .member {
                    margin-bottom: 1em;
                }
                .member.name {
                    font-weight: bold;
                    color: #7FB3FF;
                }
                .member.summary {
                    color: #B0B0B0;
                    margin-left: 1em;
                }
                .section-title {
                    font-size: 1.2em;
                    margin-top: 1.5em;
                    color: #7FB3FF;
                }
                .code {
                    background: #181A20;
                    font-family: Consolas, monospace;
                    padding: 2px 6px;
                    border-radius: 4px;
                    color: #FFD479;
                }
            </style>
            <div class="sidebar">
                <h2>Navigation</h2>
                <ul>
                    <li><a href="#applet">AppletGumternetExplorer</a></li>
                    <li><a href="#htmlembed">HTMLEmbed</a></li>
                    <li><a href="#vector2">Vector2</a></li>
                </ul>
            </div>
            <div class="container">
                <h1>Gumknix API Reference</h1>
                <p>Version: 1.0.0<br>Last updated: August 31, 2025</p>
                <div class="type" id="applet">
                    <h2>Class: <span class="code">Gumknix.AppletGumternetExplorer</span></h2>
                    <div class="summary">Represents a fake applet for browsing the Gumternet.</div>
                    <div class="section-title">Properties</div>
                    <div class="member">
                        <span class="name code">DefaultTitle</span>
                        <span class="summary">Gets the default window title.</span>
                    </div>
                    <div class="member">
                        <span class="name code">DefaultIcon</span>
                        <span class="summary">Gets the default window icon.</span>
                    </div>
                    <div class="section-title">Methods</div>
                    <div class="member">
                        <span class="name code">Update()</span>
                        <span class="summary">Updates the applet UI and state.</span>
                    </div>
                    <div class="member">
                        <span class="name code">Close()</span>
                        <span class="summary">Closes the applet and releases resources.</span>
                    </div>
                </div>
                <div class="type" id="htmlembed">
                    <h2>Class: <span class="code">Gumknix.HTMLEmbed</span></h2>
                    <div class="summary">Embeds HTML content in the applet window.</div>
                    <div class="section-title">Methods</div>
                    <div class="member">
                        <span class="name code">Create(string html)</span>
                        <span class="summary">Creates a new HTML embed element.</span>
                    </div>
                    <div class="member">
                        <span class="name code">SetInnerHTML(string html)</span>
                        <span class="summary">Sets the inner HTML of the embed.</span>
                    </div>
                </div>
                <div class="type" id="vector2">
                    <h2>Struct: <span class="code">Gumknix.Vector2</span></h2>
                    <div class="summary">Describes a 2D vector.</div>
                    <div class="section-title">Fields</div>
                    <div class="member">
                        <span class="name code">float X</span>
                        <span class="summary">The X coordinate.</span>
                    </div>
                    <div class="member">
                        <span class="name code">float Y</span>
                        <span class="summary">The Y coordinate.</span>
                    </div>
                    <div class="section-title">Methods</div>
                    <div class="member">
                        <span class="name code">Add(Vector2 left, Vector2 right)</span>
                        <span class="summary">Returns the sum of two vectors.</span>
                    </div>
                    <div class="member">
                        <span class="name code">Dot(Vector2 left, Vector2 right)</span>
                        <span class="summary">Returns the dot product of two vectors.</span>
                    </div>
                </div>
            </div>
            """;
        }

        public string TestPageHtmlBody()
        {
            return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="UTF-8">
                <title>Gumknix API Reference</title>
                {TestPageDivAndCss().Substring(0, TestPageDivAndCss().IndexOf("<div class=\"sidebar\">"))}
            </head>
            <body>
                {TestPageDivAndCss().Substring(TestPageDivAndCss().IndexOf("<div class=\"sidebar\">"))}
            </body>
            </html>
            """;
        }
    }
}
