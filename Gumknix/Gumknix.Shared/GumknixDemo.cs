using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Gum.Forms;
using MonoGameGum;

namespace Gumknix
{
    public class GumknixDemo : Game
    {
        private GraphicsDeviceManager graphics;

        private GumService GumService => GumService.Default;

        private Gumknix Gumknix;

        public GumknixDemo()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.GraphicsProfile = GraphicsProfile.HiDef;
            Content.RootDirectory = "Content";

            graphics.SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;
#if (ANDROID || iOS)
            graphics.IsFullScreen = true;
#endif
            IsMouseVisible = true;
            Window.AllowUserResizing = true;
        }

        protected override void Initialize()
        {
            GumService.Initialize(this, DefaultVisualsVersion.V2);

            Gumknix = new Gumknix();

            base.Initialize();
        }

        protected override void LoadContent()
        {
        }

        protected override void UnloadContent()
        {
        }

        protected override void Update(GameTime gameTime)
        {
            Gumknix.UpdatePreGum();

            Gum.Wireframe.GraphicalUiElement.CanvasWidth = Window.ClientBounds.Width;
            Gum.Wireframe.GraphicalUiElement.CanvasHeight = Window.ClientBounds.Height;
            GumService.Update(gameTime);

            Gumknix.UpdatePostGum();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Gumknix.SettingsThemes.CurrentTheme.DesktopColor);

            GumService.Draw();

            base.Draw(gameTime);
        }
    }
}
