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
using Console = PseudoSystem.Console;
using ConsoleKeyInfo = PseudoSystem.ConsoleKeyInfo;

namespace Gumknix
{
    public class AppletConsole : BaseApplet
    {
        public static readonly string DefaultTitle = "Console";
        public static readonly string DefaultIcon = "\uEE6F";

        private Console console = new();

        private RenderTarget2D renderTarget;
        private RenderTargetBinding[] lastTargetBindings;

        private ContentManager contentManager;
        private SpriteBatch spriteBatch;
        private SpriteFont spriteFont;
        private Texture2D whitePixel;

        List<ConsoleKeyInfo> keyBuffer = new();

        Task runningConsoleTask;

        KeyboardState keyboardState;
        KeyboardState lastKeyboardState;
        List<Keys> keysUnreleasedSinceComplete;

        public AppletConsole(Gumknix gumknix, object[] args = null) : base(gumknix, args)
        {
            base.Initialize(DefaultTitle, DefaultIcon);

            if (args?.Length >= 1)
            {
                Console consoleToAttach = args[0] as Console;
                if (consoleToAttach != null)
                    console = consoleToAttach;
            }

            console ??= new();

            GraphicsDevice graphicsDevice = (GumknixInstance.GameServiceContainer.GetService(
                typeof(IGraphicsDeviceService)) as IGraphicsDeviceService).GraphicsDevice;
            GameWindow gameWindow = (GumknixInstance.GameServiceContainer.GetService(
                typeof(Microsoft.Xna.Platform.GameStrategy)) as Microsoft.Xna.Platform.GameStrategy).Window;

            gameWindow.TextInput += (s, e) =>
            {
                bool shift = keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift);
                bool alt = keyboardState.IsKeyDown(Keys.LeftAlt) || keyboardState.IsKeyDown(Keys.RightAlt);
                bool control = keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl);
                keyBuffer.Add(new ConsoleKeyInfo(e.Character, (ConsoleKey)e.Key, shift, alt, control));
            };

            contentManager = new ContentManager(GumknixInstance.GameServiceContainer, "Content");

            spriteBatch = new SpriteBatch(graphicsDevice);
            spriteFont = contentManager.Load<SpriteFont>("Font1");

            renderTarget = new RenderTarget2D(graphicsDevice, 800, 600 - 32);
            whitePixel = new Texture2D(graphicsDevice, 1, 1);
            whitePixel.SetData([Color.White]);

            Window.Visual.Height = 600;

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
        }

        public void StartTask(Func<Task> task)
        {
            runningConsoleTask = Task.Run(task);
            runningConsoleTask.ContinueWith(t =>
            {
                console.WriteLine();
                console.WriteLine();
                console.WriteLine("Press any key to close this window . . .");
                keysUnreleasedSinceComplete = Keyboard.GetState().GetPressedKeys().ToList();
            });
        }

        public override void Update()
        {
            if (console.Title?.Length >= 1)
                SetTitle(console.Title);

            console.UpdateGridCells();
            console.AddKeyPresses(keyBuffer);
            keyBuffer.Clear();

            lastKeyboardState = keyboardState;
            keyboardState = Keyboard.GetState();

            if ((runningConsoleTask?.IsCompleted == true) && (keysUnreleasedSinceComplete != null))
            {
                for (int i = 0; i < keysUnreleasedSinceComplete.Count; i++)
                {
                    if (lastKeyboardState.IsKeyUp(keysUnreleasedSinceComplete[i]))
                    {
                        keysUnreleasedSinceComplete.RemoveAt(i);
                        i--;
                    }
                }

                if (keyboardState.GetPressedKeyCount() >= 1)
                {
                    Keys[] pressedKeys = keyboardState.GetPressedKeys();
                    for (int i = 0; i < pressedKeys.Length; i++)
                    {
                        if (keysUnreleasedSinceComplete.Contains(pressedKeys[i]) == false)
                        {
                            CloseRequest = true;
                            break;
                        }
                    }
                }
            }

            base.Update();
        }

        public override void Draw()
        {
            lastTargetBindings ??= new RenderTargetBinding[spriteBatch.GraphicsDevice.GetRenderTargets().Length];
            spriteBatch.GraphicsDevice.GetRenderTargets(lastTargetBindings);
            renderTarget.GraphicsDevice.SetRenderTarget(renderTarget);

            spriteBatch.Begin();

            spriteBatch.Draw(whitePixel, renderTarget.Bounds, Console.ConsoleColorToColor(console.BackgroundColor).ToXNA());

            for (int windowX = console.WindowLeft; windowX < (console.WindowLeft + console.WindowWidth); windowX++)
            {
                for (int windowY = console.WindowTop; windowY < (console.WindowTop + console.WindowHeight); windowY++)
                {
                    Console.ConsoleGridCell cell = console.ConsoleGridCells[windowX][windowY];

                    Point size = new(12, console.CursorSize + 0);
                    Point position = new((windowX - console.WindowLeft) * size.X,
                        (windowY - console.WindowTop) * size.Y);

                    //position += new Point((int)Window.AbsoluteLeft + 5, (int)Window.AbsoluteTop + 30);
                    //position += new Point((int)Window.X + 5, (int)Window.Y + 30);

                    spriteBatch.Draw(whitePixel, new Rectangle(position.X, position.Y, size.X, size.Y), cell.BackgroundColor.ToXNA());
                    if (cell.Character != '\0')
                        spriteBatch.DrawString(spriteFont, cell.Character.ToString(), position.ToVector2(), cell.ForegroundColor.ToXNA());
                }
            }

            spriteBatch.End();

            renderTarget.GraphicsDevice.SetRenderTargets(lastTargetBindings);
        }

        protected override void Close()
        {
            console.Dispose();
            renderTarget.Dispose();
            spriteBatch.Dispose();
            contentManager.Dispose();
            base.Close();
        }
    }
}
