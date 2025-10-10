using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Gum.Forms.Controls;
using Gum.Wireframe;
using MonoGameGum.GueDeriving;

#if BLAZORGL
using nkast.Wasm.Dom;
#endif

namespace Gumknix
{
    public class HTMLViewContainer : ContainerRuntime
    {
        public Gumknix GumknixInstance { get; init; }
        public FrameworkElement Parent { get; init; }

        private string _id;
        private Body _body;

        private HTMLElement<Div> _htmlElement;
        private HTMLElement<Div> _svgCutout;

        private bool? _lastVisible;
        private bool? _lastFocused;
        private Rectangle _lastArea;
        private List<Rectangle> _lastCutoutAreas = [];

        public HTMLViewContainer(Gumknix gumknix, FrameworkElement parent) : base()
        {
            GumknixInstance = gumknix;
            Parent = parent;
            FormsControlAsObject = this;
        }

        public void Create(string id)
        {
            _id = id;

            _body = Window.Current.Document.Body;

            Div tempDiv = Window.Current.Document.CreateElement<Div>("Div");
            tempDiv.InnerHTML =
                $"""
                <div id="HTMLViewContainer{_id}" style="position: absolute; display: none; overflow: hidden;">
                </div>
                """;
            _htmlElement = tempDiv.FirstElementChild();
            _body.AppendChild(_htmlElement);
            _htmlElement.Style.SetProperty("mask", $"url(#cutoutUid{_id})");
            _htmlElement.Style.SetProperty("z-index", "9999");

            tempDiv = Window.Current.Document.CreateElement<Div>("Div");
            tempDiv.InnerHTML =
                $"""
                <svg id="SvgCutout{id}" width="0" height="0" style="position:absolute; overflow: hidden;">
                </svg>
                """;
            _svgCutout = tempDiv.FirstElementChild();
            _body.AppendChild(_svgCutout);
        }

        public void SetInnerHtml(string innerHtml)
        {
            _htmlElement.InnerHTML = innerHtml;
        }

        public void Update()
        {
#if BLAZORGL
            bool visible = Parent.IsVisible;
            bool focused = (GumknixInstance.FocusedApplet?.Window == Parent) && visible;

            if (visible != _lastVisible)
                _htmlElement.Style.SetProperty("display", visible ? "block" : "none");
            if (focused != _lastFocused)
                _htmlElement.Style.SetProperty("pointer-events", focused ? "auto" : "none");

            _lastVisible = visible;
            _lastFocused = focused;

            if (visible == false)
                return;

            Rectangle area = new((int)AbsoluteLeft, (int)AbsoluteTop, (int)GetAbsoluteWidth(), (int)GetAbsoluteHeight());
            int currentLayerIndex = GumknixInstance.GumRenderables.IndexOf(Parent.Visual);

            List<Rectangle> cutoutAreas = [];
            for (int i = 0; i < GumknixInstance.RunningApplets.Count; i++)
            {
                BaseApplet applet = GumknixInstance.RunningApplets[i];
                if ((applet.Window != Parent) && applet.Window.IsVisible)
                {
                    int otherLayerIndex = GumknixInstance.GumRenderables.IndexOf(applet.Window.Visual);
                    if (otherLayerIndex < currentLayerIndex)
                        continue;

                    if (applet.Window != Parent)
                    {
                        Rectangle windowArea = new(
                            (int)(applet.Window.Visual.AbsoluteLeft),
                            (int)(applet.Window.Visual.AbsoluteTop),
                            (int)applet.Window.Visual.GetAbsoluteWidth(),
                            (int)applet.Window.Visual.GetAbsoluteHeight());
                        if (area.Intersects(windowArea))
                            cutoutAreas.Add(windowArea);
                    }

                    for (int j = 0; j < applet.Dialogs.Count; j++)
                    {
                        BaseDialog dialog = applet.Dialogs[j];
                        Rectangle dialogArea = new(
                            (int)(dialog.Window.Visual.AbsoluteLeft),
                            (int)(dialog.Window.Visual.AbsoluteTop),
                            (int)dialog.Window.Visual.GetAbsoluteWidth(),
                            (int)dialog.Window.Visual.GetAbsoluteHeight());
                        if (area.Intersects(dialogArea))
                            cutoutAreas.Add(dialogArea);
                    }
                }
            }

            GraphicalUiElement taskBarBackground = GumknixInstance.TaskBar.Background;
            Rectangle taskBarArea = new(
                (int)(taskBarBackground.AbsoluteLeft),
                (int)(taskBarBackground.AbsoluteTop),
                (int)taskBarBackground.GetAbsoluteWidth(),
                (int)taskBarBackground.GetAbsoluteHeight());
            if (area.Intersects(taskBarArea))
                cutoutAreas.Add(taskBarArea);

            if (GumknixInstance.TaskBar.IsStartOpen)
            {
                GraphicalUiElement taskBarStartPanel = GumknixInstance.TaskBar.StartStackPanel.Visual;
                Rectangle taskBarStartPanelArea = new(
                    (int)(taskBarStartPanel.AbsoluteLeft),
                    (int)(taskBarStartPanel.AbsoluteTop),
                    (int)taskBarStartPanel.GetAbsoluteWidth(),
                    (int)taskBarStartPanel.GetAbsoluteHeight());
                if (area.Intersects(taskBarStartPanelArea))
                    cutoutAreas.Add(taskBarStartPanelArea);
            }

            for (int i = 0; i < FrameworkElement.PopupRoot.Children.Count; i++)
            {
                GraphicalUiElement gue = FrameworkElement.PopupRoot.Children[i] as GraphicalUiElement;
                Rectangle popupArea = new(
                    (int)(gue.AbsoluteLeft),
                    (int)(gue.AbsoluteTop),
                    (int)gue.GetAbsoluteWidth(),
                    (int)gue.GetAbsoluteHeight());
                if (area.Intersects(popupArea))
                    cutoutAreas.Add(popupArea);
            }

            bool changed = false;
            if (area != _lastArea)
            {
                changed = true;
            }
            else if (cutoutAreas.Count != _lastCutoutAreas.Count)
            {
                changed = true;
            }
            else
            {
                for (int i = 0; i < cutoutAreas.Count; i++)
                {
                    if (cutoutAreas[i] != _lastCutoutAreas[i])
                    {
                        changed = true;
                        break;
                    }
                }
            }

            if (!changed)
                return;

            _lastArea = area;
            _lastCutoutAreas = cutoutAreas;

            StringBuilder stringBuilder = new();
            stringBuilder.AppendLine($"  <defs>");
            stringBuilder.AppendLine($"    <mask id=\"cutoutUid{_id}\">");
            stringBuilder.AppendLine($"      <rect x=\"0\" y=\"0\" width=\"{area.Width}\" height=\"{area.Height}\" fill=\"white\"/>");
            for (int i = 0; i < cutoutAreas.Count; i++)
            {
                Rectangle r = cutoutAreas[i];
                stringBuilder.Append($"      <rect x=\"{r.Left - area.Left}\" y=\"{r.Top - area.Top}\" ");
                stringBuilder.AppendLine($"width=\"{r.Width}\" height=\"{r.Height}\" fill=\"black\"/>");
            }
            stringBuilder.AppendLine($"    </mask>");
            stringBuilder.AppendLine($"  </defs>");

            _svgCutout.InnerHTML = stringBuilder.ToString();
            _htmlElement.Style.SetProperty("left", $"{area.Left}px");
            _htmlElement.Style.SetProperty("top", $"{area.Top}px");
            _htmlElement.Style.SetProperty("width", $"{area.Width}px");
            _htmlElement.Style.SetProperty("height", $"{area.Height}px");
#endif
        }

        public void Remove()
        {
            Div htmlElement = Window.Current.Document.GetElementById<Div>($"HTMLViewContainer{_id}");
            _body.RemoveChild(htmlElement);

            Div svgCutout = Window.Current.Document.GetElementById<Div>($"SvgCutout{_id}");
            _body.RemoveChild(svgCutout);
        }
    }
}
