using System;
using Microsoft.Xna.Framework;

using nkast.Wasm.Canvas;
using nkast.Wasm.Dom;
using Window = nkast.Wasm.Dom.Window;

namespace Gumknix
{
    public class EmbeddedMode
    {
        public Gumknix GumknixInstance { get; init; }

        private Div _outerPage;
        private Div _gumknixPlaceholder;

        private Div _canvasHolder;
        private Canvas _canvas;

        public bool EmbeddedInPage { get; private set; } = true;
        public bool Animating { get; set; }
        public bool SwapRequest { get; set; }

        private double _animateStartTime = 0f;
        private int _appletId = -1;

        private Vector2 outerPagePositionStart = Vector2.Zero;
        private Vector2 outerPagePositionEnd = Vector2.Zero;
        private Vector2 outerPageSizeStart = Vector2.Zero;
        private Vector2 outerPageSizeEnd = Vector2.Zero;

        private Vector2 canvasHolderPositionStart = Vector2.Zero;
        private Vector2 canvasHolderPositionEnd = Vector2.Zero;
        private Vector2 canvasHolderSizeStart = Vector2.Zero;
        private Vector2 canvasHolderSizeEnd = Vector2.Zero;

        public EmbeddedMode(Gumknix gumknix)
        {
            GumknixInstance = gumknix;

            _outerPage = Window.Current.Document.GetElementById<Div>("outer");
            _canvasHolder = Window.Current.Document.GetElementById<Div>("canvasHolder");
            _canvas = Window.Current.Document.GetElementById<Canvas>("theCanvas");
            _gumknixPlaceholder = Window.Current.Document.GetElementById<Div>("gumknixplaceholder1");

            EmbeddedInPage = (_gumknixPlaceholder != null) ? true : false;
        }

        internal void Update(GameTime gameTime)
        {
            if (_gumknixPlaceholder == null)
                return;

            GraphicsDeviceManager graphicsDeviceManager = (GumknixInstance.GameServiceContainer.GetService(
                typeof(IGraphicsDeviceManager)) as GraphicsDeviceManager);

            DOMRect gumknixPlaceholderRect = _gumknixPlaceholder.GetBoundingClientRect();

            if (EmbeddedInPage && !Animating)
            {
                _canvasHolder.Style["left"] = (int)gumknixPlaceholderRect.Left + "px";
                _canvasHolder.Style["top"] = (int)gumknixPlaceholderRect.Top + "px";

                if ((_canvas.Width != gumknixPlaceholderRect.Width) ||
                    (_canvas.Height != gumknixPlaceholderRect.Height))
                {
                    _canvasHolder.Style["width"] = (int)gumknixPlaceholderRect.Width + "px";
                    _canvasHolder.Style["height"] = (int)gumknixPlaceholderRect.Height + "px";
                    _canvas.Width = (int)gumknixPlaceholderRect.Width;
                    _canvas.Height = (int)gumknixPlaceholderRect.Height;
                    graphicsDeviceManager.PreferredBackBufferWidth = _canvas.Width;
                    graphicsDeviceManager.PreferredBackBufferHeight = _canvas.Height;
                    graphicsDeviceManager.ApplyChanges();
                }
            }

            if (SwapRequest && !Animating)
            {
                SwapRequest = false;
                Animating = true;
                _animateStartTime = gameTime.TotalGameTime.TotalSeconds;

                outerPagePositionStart = new(
                    int.Parse(_outerPage.Style["left"].Replace("px", "")),
                    int.Parse(_outerPage.Style["top"].Replace("px", "")));
                outerPageSizeStart = new(_outerPage.ClientWidth, _outerPage.ClientHeight);

                canvasHolderPositionStart = new(
                    int.Parse(_canvasHolder.Style["left"].Replace("px", "")),
                    int.Parse(_canvasHolder.Style["top"].Replace("px", "")));
                canvasHolderSizeStart = new(_canvasHolder.ClientWidth, _canvasHolder.ClientHeight);

                if (EmbeddedInPage)
                {
                    _outerPage.Style["position"] = "absolute";
                    outerPagePositionEnd = new(200f, 200f);
                    outerPageSizeEnd = new(600f, 400f);

                    _canvasHolder.Style["position"] = "absolute";
                    canvasHolderPositionEnd = new(0f, 0f);
                    canvasHolderSizeEnd = outerPageSizeStart;
                }
                else
                {
                    _outerPage.Style["position"] = "absolute";
                    outerPagePositionEnd = new(0f, 0f);
                    outerPageSizeEnd = canvasHolderSizeStart;

                    _canvasHolder.Style["position"] = "absolute";
                    canvasHolderSizeEnd = new(600f, 400f);

                    if (_appletId >= 0)
                        for (int i = 0; i < GumknixInstance.RunningApplets.Count; i++)
                            if (GumknixInstance.RunningApplets[i].AppletId == _appletId)
                            {
                                GumknixInstance.RunningApplets[i].CloseRequest = true;
                                _appletId = -1;
                                break;
                            }
                }

                EmbeddedInPage = !EmbeddedInPage;
            }

            if (Animating)
            {
                float animateLerp = (float)(gameTime.TotalGameTime.TotalSeconds - _animateStartTime) / 0.4f;
                animateLerp = ((float)Math.Sin((animateLerp * MathHelper.ToRadians(180)) - MathHelper.ToRadians(90)) + 1f) / 2f;
                if (animateLerp >= 1f)
                {
                    animateLerp = 1f;
                    Animating = false;
                    if (!EmbeddedInPage)
                        _appletId = GumknixInstance.StartApplet(typeof(AppletGumternetExplorer), [_outerPage]).AppletId;
                }

                if (EmbeddedInPage)
                {
                    canvasHolderPositionEnd.X = (int)gumknixPlaceholderRect.X;
                    canvasHolderPositionEnd.Y = (int)gumknixPlaceholderRect.Y;
                    canvasHolderSizeEnd.X = (int)gumknixPlaceholderRect.Width;
                    canvasHolderSizeEnd.Y = (int)gumknixPlaceholderRect.Height;
                }

                _outerPage.Style["left"] = (int)MathHelper.Lerp(outerPagePositionStart.X, outerPagePositionEnd.X, animateLerp) + "px";
                _outerPage.Style["top"] = (int)MathHelper.Lerp(outerPagePositionStart.Y, outerPagePositionEnd.Y, animateLerp) + "px";
                _outerPage.Style["width"] = (int)MathHelper.Lerp(outerPageSizeStart.X, outerPageSizeEnd.X, animateLerp) + "px";
                _outerPage.Style["height"] = (int)MathHelper.Lerp(outerPageSizeStart.Y, outerPageSizeEnd.Y, animateLerp) + "px";
                _canvasHolder.Style["left"] = (int)MathHelper.Lerp(canvasHolderPositionStart.X, canvasHolderPositionEnd.X, animateLerp) + "px";
                _canvasHolder.Style["top"] = (int)MathHelper.Lerp(canvasHolderPositionStart.Y, canvasHolderPositionEnd.Y, animateLerp) + "px";
                _canvasHolder.Style["width"] = (int)MathHelper.Lerp(canvasHolderSizeStart.X, canvasHolderSizeEnd.X, animateLerp) + "px";
                _canvasHolder.Style["height"] = (int)MathHelper.Lerp(canvasHolderSizeStart.Y, canvasHolderSizeEnd.Y, animateLerp) + "px";

                if (animateLerp >= 0.5f)
                {
                    _outerPage.Style["z-index"] = !EmbeddedInPage ? "2" : "1";
                    _canvasHolder.Style["z-index"] = !EmbeddedInPage ? "1" : "2";
                }

                _canvas.Width = (int)MathHelper.Lerp(_canvas.Width, _canvasHolder.ClientWidth, animateLerp);
                _canvas.Height = (int)MathHelper.Lerp(_canvas.Height, _canvasHolder.ClientHeight, animateLerp);
                graphicsDeviceManager.PreferredBackBufferWidth = _canvas.Width;
                graphicsDeviceManager.PreferredBackBufferHeight = _canvas.Height;
                graphicsDeviceManager.ApplyChanges();
            }
        }
    }
}
