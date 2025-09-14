using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using global::Gumknix;

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
        Color colorFromOtherClass = TestKNIGameClass2.GetColor();
        spriteBatch.Draw(whitePixel, new Rectangle((int)Position.X, (int)Position.Y, 100, 100), colorFromOtherClass);
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
