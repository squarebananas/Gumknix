using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Gum.Wireframe;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;

namespace Gumknix
{
    public class AppletKniGameComponentRunner : BaseApplet
    {
        public static readonly string DefaultTitle = "Kni Game Component Runner";
        public static readonly string DefaultIcon = "\uEE6F";

        DrawableGameComponent drawableGameComponent;

        GraphicsDevice graphicsDevice;
        GameTime gameTime;

        private RenderTarget2D renderTarget;
        private RenderTargetBinding[] lastTargetBindings;

        private ContentManager contentManager;
        private SpriteBatch spriteBatch;
        private SpriteFont spriteFont;
        private Texture2D whitePixel;

        public AppletKniGameComponentRunner(Gumknix gumknix, object[] args = null) : base(gumknix, args)
        {
            base.Initialize(DefaultTitle, DefaultIcon, height: 600);

            graphicsDevice = (GumknixInstance.GameServiceContainer.GetService(
                typeof(IGraphicsDeviceService)) as IGraphicsDeviceService).GraphicsDevice;
            spriteBatch = new SpriteBatch(graphicsDevice);
            renderTarget = new RenderTarget2D(graphicsDevice, 800, 600 - 32);
            whitePixel = new Texture2D(graphicsDevice, 1, 1);
            whitePixel.SetData([Color.White]);

            ColoredRectangleRuntime background = new();
            background.Color = Color.Black;
            background.Dock(Dock.Fill);
            background.Anchor(Anchor.TopLeft);
            Window.Visual.Children.Insert(1, background);

            Sprite sprite = new(renderTarget);
            GraphicalUiElement spriteGue = new(sprite, null);
            spriteGue.X = 3;
            spriteGue.Y = 32;
            sprite.Width = spriteGue.Width = renderTarget.Width;
            spriteGue.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
            sprite.Height = spriteGue.Height = renderTarget.Height;
            spriteGue.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
            sprite.SourceRectangle = new System.Drawing.Rectangle(0, 0, renderTarget.Width, renderTarget.Height);
            Window.Visual.Children.Add(spriteGue);

            if (args?.Length >= 1)
            {
                drawableGameComponent = args[0] as DrawableGameComponent;
                drawableGameComponent.Initialize();
            }
            else
            {
                TestKNIGame.GumknixEntryPoint(GumknixInstance);
                CloseRequest = true;
            }
        }

        public override void Update()
        {
            TimeSpan totalTime = (gameTime != null) ? gameTime.TotalGameTime + GumknixInstance.GameTime.ElapsedGameTime : TimeSpan.Zero;
            gameTime = new GameTime(totalTime, GumknixInstance.GameTime.ElapsedGameTime);

            drawableGameComponent?.Update(gameTime);

            base.Update();
        }

        public override void Draw()
        {
            lastTargetBindings ??= new RenderTargetBinding[spriteBatch.GraphicsDevice.GetRenderTargets().Length];
            spriteBatch.GraphicsDevice.GetRenderTargets(lastTargetBindings);
            graphicsDevice.SetRenderTarget(renderTarget);

            spriteBatch.Begin();
            spriteBatch.Draw(whitePixel, renderTarget.Bounds, new Color(68, 34, 136, 255));
            spriteBatch.End();

            drawableGameComponent?.Draw(gameTime);

            graphicsDevice.SetRenderTargets(lastTargetBindings);

            base.Draw();
        }

        protected override void Close()
        {
            renderTarget.Dispose();
            spriteBatch.Dispose();

            if (drawableGameComponent != null)
                drawableGameComponent.Dispose();

            base.Close();
        }
    }

    public class TestKNIGame : DrawableGameComponent
    {
        private SpriteBatch spriteBatch;
        private Texture2D whitePixel;

        public Vector2 Position = new Vector2(100, 100);

        public TestKNIGame(Game game) : base(game)
        {
        }

        public override void Initialize()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            whitePixel = new Texture2D(GraphicsDevice, 1, 1);
            whitePixel.SetData([Color.White]);

            base.Initialize();
        }

        public override void Update(GameTime gameTime)
        {
            KeyboardState keyboardState = Keyboard.GetState();
            if (keyboardState.IsKeyDown(Keys.Up))
                Position.Y -= 100f * (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (keyboardState.IsKeyDown(Keys.Down))
                Position.Y += 100f * (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (keyboardState.IsKeyDown(Keys.Left))
                Position.X -= 100f * (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (keyboardState.IsKeyDown(Keys.Right))
                Position.X += 100f * (float)gameTime.ElapsedGameTime.TotalSeconds;

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            spriteBatch.Begin();
            spriteBatch.Draw(whitePixel, new Rectangle((int)Position.X, (int)Position.Y, 100, 100), Color.Yellow);
            spriteBatch.End();

            base.Draw(gameTime);
        }

        public static void GumknixEntryPoint(global::Gumknix.Gumknix gumknix)
        {
            Microsoft.Xna.Platform.GameStrategy gameStrategy = gumknix.GameServiceContainer.GetService(
                typeof(Microsoft.Xna.Platform.GameStrategy)) as Microsoft.Xna.Platform.GameStrategy;
            DrawableGameComponent testGame = new TestKNIGame(gameStrategy.Game);
            gumknix.StartApplet(typeof(AppletKniGameComponentRunner), [testGame]);
        }
    }
}
