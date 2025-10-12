using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Gum.Forms.Controls;
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

        private Point cellSize => new(11, console.CursorSize + 0);

        Vector2 lastWindowSize;

        private RenderTarget2D renderTarget;
        private RenderTargetBinding[] lastTargetBindings;
        private Sprite sprite;
        private GraphicalUiElement spriteGue;

        private ContentManager contentManager;
        private SpriteBatch spriteBatch;
        private SpriteFont spriteFont;
        private Texture2D whitePixel;

        List<ConsoleKeyInfo> keyBuffer = new();

        Task runningConsoleTask;

        KeyboardState keyboardState;
        KeyboardState lastKeyboardState;
        List<Keys> keysUnreleasedSinceComplete;

        ScrollBar scrollBar;

        private DynamicSoundEffectInstance beepSound;

        public AppletConsole(Gumknix gumknix, object[] args = null) : base(gumknix, args)
        {
            base.Initialize(DefaultTitle, DefaultIcon, width: 900, height: 600);

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
                scrollBar.Value = scrollBar.Maximum;
            };

            contentManager = new ContentManager(GumknixInstance.GameServiceContainer, "Content");

            spriteBatch = new SpriteBatch(graphicsDevice);
            spriteFont = contentManager.Load<SpriteFont>("FontCascadia");

            ColoredRectangleRuntime background = new();
            background.Color = Color.Black;
            background.Dock(Dock.Fill);
            background.Anchor(Anchor.TopLeft);
            Window.Visual.Children.Insert(1, background);

            whitePixel = new Texture2D(graphicsDevice, 1, 1);
            whitePixel.SetData([Color.White]);

            scrollBar = new();
            scrollBar.Dock(Dock.FillVertically);
            scrollBar.Anchor(Anchor.TopRight);
            scrollBar.Height -= TitleBarHeight;
            scrollBar.Maximum = 0;
            MainStackPanel.AddChild(scrollBar);

            WindowSizeChanged();
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

            if ((Window.ActualWidth != lastWindowSize.X) ||
                (Window.ActualHeight != lastWindowSize.Y))
                WindowSizeChanged();

            if (console.TotalLinesWritten >= console.WindowHeight)
            {
                int lastMaximum = (int)scrollBar.Maximum;
                scrollBar.Maximum = Math.Min(console.TotalLinesWritten + 1, console.BufferHeight) - console.WindowHeight;
                if ((scrollBar.IsEnabled == false) || (scrollBar.Value == lastMaximum))
                    scrollBar.Value = scrollBar.Maximum;
                scrollBar.IsEnabled = true;
            }
            else
            {
                scrollBar.IsEnabled = false;
                scrollBar.Value = 0;
                scrollBar.Maximum = 0;
            }

            if (console.BeepRequested.HasValue)
            {
                PlayBeep(console.BeepRequested.Value.frequency, console.BeepRequested.Value.duration);
                console.BeepRequested = null;
            }
            if ((runningConsoleTask?.IsCompleted == true) && (keysUnreleasedSinceComplete != null))
                CloseOnKeyPress();

            base.Update();
        }

        private void WindowSizeChanged()
        {
            int maxX = (int)((Window.Visual.GetAbsoluteWidth() - 6 - scrollBar.ActualWidth) / cellSize.X);
            int maxY = (int)((Window.Visual.GetAbsoluteHeight() - TitleBarHeight) / cellSize.Y);

            int pixelWidth = maxX * cellSize.X;
            int pixelHeight = maxY * cellSize.Y;

            bool changed = false;
            if (renderTarget == null ||
                renderTarget.Width != pixelWidth ||
                renderTarget.Height != pixelHeight)
                changed = true;

            if (!changed)
                return;

            console.WindowWidth = Math.Clamp(maxX, 1, console.BufferWidth);
            console.WindowHeight = Math.Clamp(maxY, 1, console.BufferHeight);

            renderTarget?.Dispose();
            renderTarget = new RenderTarget2D(spriteBatch.GraphicsDevice, pixelWidth, pixelHeight);

            sprite ??= new(null);
            sprite.Texture = renderTarget;
            sprite.SourceRectangle = new(0, 0, renderTarget.Width, renderTarget.Height);
            sprite.Width = renderTarget.Width;
            sprite.Height = renderTarget.Height;

            spriteGue ??= new(sprite, null);
            spriteGue.X = 3;
            spriteGue.Y = TitleBarHeight;
            spriteGue.Width = renderTarget.Width;
            spriteGue.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
            spriteGue.Height = renderTarget.Height;
            spriteGue.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;

            if (Window.Visual.Children.Contains(spriteGue) == false)
                Window.Visual.Children.Add(spriteGue);

            lastWindowSize = new Vector2(Window.ActualWidth, Window.ActualHeight);
        }

        public void PlayBeep(int frequency, int duration) // to do
        {
            int sampleRate = 48000;
            beepSound ??= new(sampleRate, AudioChannels.Mono);
            beepSound.Volume = 0.5f;

            int bufferSize = beepSound.GetSampleSizeInBytes(TimeSpan.FromMilliseconds(duration));
            byte[] buffer = new byte[bufferSize];
            for (int i = 0; i < bufferSize; i += 2)
            {
                double time = (double)i / sampleRate;
                short currentSample = (short)(Math.Sign(Math.Sin(2 * Math.PI * frequency * time)) * short.MaxValue);
                buffer[i + 0] = (byte)(currentSample % 256);
                buffer[i + 1] = (byte)(currentSample / 256);
            }

            //if (beepSound.State == SoundState.Playing)
            //    beepSound.Stop();
            beepSound.SubmitBuffer(buffer);
            beepSound.Play();
        }

        private void ShowCloseMessage()
        {
            console.WriteLine();
            console.WriteLine();
            console.WriteLine("Press any key to close this window . . .");
            console.UpdateGridCells();
            CancellationTokenSource.Cancel();
        }
        private void CloseOnKeyPress()
        {
            for (int i = keysUnreleasedSinceComplete.Count - 1; i >= 0; i--)
                if (lastKeyboardState.IsKeyUp(keysUnreleasedSinceComplete[i]))
                    keysUnreleasedSinceComplete.RemoveAt(i);

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
                    int scrollBarAdjust = (int)Math.Max(0, scrollBar.Maximum - scrollBar.Value);
                    int wrappedBufferY = (windowY - scrollBarAdjust + console.BufferLineZero) % console.BufferHeight;
                    Console.ConsoleGridCell cell = console.ConsoleGridCells[windowX][wrappedBufferY];

                    Point position = new((windowX - console.WindowLeft) * cellSize.X,
                        (windowY - console.WindowTop) * cellSize.Y);

                    spriteBatch.Draw(whitePixel, new Rectangle(position.X, position.Y, cellSize.X, cellSize.Y), cell.BackgroundColor.ToXNA());
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
