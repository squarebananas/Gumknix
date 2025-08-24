using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Gum.Forms;
using Gum.Wireframe;
using MonoGameGum;
using MonoGameGum.GueDeriving;

#if BLAZORGL
using nkast.Wasm.Dom;
using nkast.Wasm.File;
using nkast.Wasm.FileSystem;
#endif

namespace Gumknix
{
    public class AppletMoknicoEditor : BaseApplet
    {
        public static readonly string DefaultTitle = "Moknico Studio";
        public static readonly string DefaultIcon = "\uEE71";

#if BLAZORGL
        private ModuleMonaco _monaco;

        private HTMLEmbed _editorHTMLEmbed;
        private HTMLEmbed _svgCutoutHTMLEmbed;

        public ModuleMonaco.LanguageDefinition[] LanguageDefinitions { get; private set; }
#endif

        public AppletMoknicoEditor(Gumknix gumknix, object[] args = null) : base(gumknix, args)
        {
            base.Initialize(DefaultTitle, DefaultIcon);

            ColoredRectangleRuntime navigationBarBackground = new();
            navigationBarBackground.Color = new Color(32, 32, 32);
            navigationBarBackground.Dock(Dock.Fill);
            navigationBarBackground.Anchor(Anchor.TopLeft);
            MainStackPanel.AddChild(navigationBarBackground);

#if BLAZORGL
            NineSliceRuntime background = Window.Visual.GetGraphicalUiElementByName("Background") as NineSliceRuntime;
            background.Color = new Color(70, 70, 80);
            background.Visible = false;

            _monaco = ModuleMonaco.Create();
            _monaco.OnScriptLoaded += (s, e) => _monaco.InitializeInstance();
            _monaco.OnInstanceLoaded += (s, e) =>
            {
                LanguageDefinitions = _monaco.GetLanguages();
                for (int i = 0; i < LanguageDefinitions.Length; i++)
                    for (int j = 0; j < LanguageDefinitions[i].Extensions?.Length; j++)
                        Gumknix.ExtensionsDefaultApplets.TryAdd(LanguageDefinitions[i].Extensions[j], new(typeof(AppletMoknicoEditor), DefaultIcon));

                if (args?.Length >= 1)
                {
                    FileSystemItem fileSystemItem = args[0] as FileSystemItem;
                    if (fileSystemItem != null)
                        ReadFile(fileSystemItem);
                }
            };

            _editorHTMLEmbed = HTMLEmbed.Create(
                $"""
                <div id = "editorUid{_monaco.Uid}" style = "position: absolute; display: none;">
                </div>
                """);

            _svgCutoutHTMLEmbed = HTMLEmbed.Create(
                $"""
                <svg id="svgCutoutUid{_monaco.Uid}" width="0" height="0" style="position:absolute;">
                </svg>
                """);
            
            _monaco.InitializeLoaderScript();
#endif
        }

        public override void Update()
        {
#if BLAZORGL
            if (Keyboard.GetState().IsKeyDown(Keys.F2))
            {
                _monaco.SetText("test123");
            }
#endif

            base.Update();
        }

        public override void PostGumUpdate()
        {
#if BLAZORGL
            Rectangle area = new(0, 0, (int)Window.ActualWidth, (int)Window.ActualHeight);
            int currentLayerIndex = GumService.Default.Renderer.Layers.IndexOf(Layer);

            List<Rectangle> rectangles = [];
            for (int i = 0; i < GumknixInstance.RunningApplets.Count; i++)
            {
                BaseApplet applet = GumknixInstance.RunningApplets[i];
                if ((applet != this) && applet.Window.IsVisible)
                {
                    int otherLayerIndex = GumService.Default.Renderer.Layers.IndexOf(applet.Layer);
                    if (otherLayerIndex < currentLayerIndex)
                        continue;

                    if (applet != this)
                    {
                        Rectangle windowArea = new(
                            (int)(applet.Window.Visual.AbsoluteLeft - (Window.AbsoluteLeft + 20)),
                            (int)(applet.Window.Visual.AbsoluteTop - (Window.AbsoluteTop + 50)),
                            (int)applet.Window.Visual.GetAbsoluteWidth(),
                            (int)applet.Window.Visual.GetAbsoluteHeight());
                        if (area.Intersects(windowArea))
                            rectangles.Add(windowArea);
                    }

                    for (int j=0; j < applet.Dialogs.Count;j++)
                    {
                        BaseDialog dialog = applet.Dialogs[j];
                        Rectangle dialogArea = new(
                            (int)(dialog.Window.Visual.AbsoluteLeft - (Window.AbsoluteLeft + 20)),
                            (int)(dialog.Window.Visual.AbsoluteTop - (Window.AbsoluteTop + 50)),
                            (int)dialog.Window.Visual.GetAbsoluteWidth(),
                            (int)dialog.Window.Visual.GetAbsoluteHeight());
                        if (area.Intersects(dialogArea))
                            rectangles.Add(dialogArea);
                    }
                }

            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"  <defs>");
            sb.AppendLine($"    <mask id=\"cutoutUid{_monaco.Uid}\">");
            //sb.AppendLine($"      <rect x=\"{0}\" y=\"{0}\" width=\"{1000}\" height=\"{1000}\" fill=\"black\"/>");
            sb.AppendLine($"      <rect x=\"{area.Left}\" y=\"{area.Top}\" width=\"{area.Width}\" height=\"{area.Height}\" fill=\"white\"/>");
            for (int i = 0; i < rectangles.Count; i++)
            {
                Rectangle r = rectangles[i];
                sb.AppendLine($"      <rect x=\"{r.Left}\" y=\"{r.Top}\" width=\"{r.Width}\" height=\"{r.Height}\" fill=\"black\"/>");
            }
            sb.AppendLine($"    </mask>");
            sb.AppendLine($"  </defs>");

            if (Window.IsVisible)
            {
                _svgCutoutHTMLEmbed.SetInnerHTML(sb.ToString());
                _editorHTMLEmbed.Style.SetProperty("display", "block");
                _editorHTMLEmbed.Style.SetProperty("left", $"{Window.AbsoluteLeft + 20}px");
                _editorHTMLEmbed.Style.SetProperty("top", $"{Window.AbsoluteTop + 50}px");
                _editorHTMLEmbed.Style.SetProperty("width", $"{Window.ActualWidth - 100}px");
                _editorHTMLEmbed.Style.SetProperty("height", $"{Window.ActualHeight - 100}px");
                _editorHTMLEmbed.Style.SetProperty("mask", $"url(#cutoutUid{_monaco.Uid})");
            }
            else
            {
                _editorHTMLEmbed.Style.SetProperty("display", "none");
            }
#endif
        }

        private void ReadFile(FileSystemItem fileItem)
        {
            try
            {
#if BLAZORGL
                FileSystemFileHandle fileSystemFileHandle = fileItem.Handle as FileSystemFileHandle;
                Task<File> file = fileSystemFileHandle.GetFile();
                file.ContinueWith(t =>
                {
                    if (t.IsCompletedSuccessfully && t.Result != null)
                    {
                        Blob blob = file.Result;

                        Task<string> textTask = blob.Text();
                        textTask.ContinueWith(t =>
                        {
                            if (t.IsCompletedSuccessfully && t.Result != null)
                            {
                                ModuleMonaco.LanguageDefinition language = GetLanguageDefinitionFromFileExtension(fileItem.Extension);
                                _monaco.SetLanguage(language?.Id ?? "plaintext");

                                string text = textTask.Result;
                                _monaco.SetText(text);
                            }
                        });
                    }
                });
#endif
            }
            catch (Exception e)
            {
            }
        }

        public ModuleMonaco.LanguageDefinition GetLanguageDefinitionFromFileExtension(string extension)
        {
            for (int i = 0; i < LanguageDefinitions.Length; i++)
            {
                ModuleMonaco.LanguageDefinition language = LanguageDefinitions[i];
                for (int j = 0; j < language.Extensions?.Length; j++)
                    if (extension == language.Extensions[j])
                        return language;
            }
            return null;
        }

        protected override void Close()
        {
#if BLAZORGL
            _monaco.Close();
            _svgCutoutHTMLEmbed.Remove();
            _editorHTMLEmbed.Remove();
#endif
            base.Close();
        }
    }
}
