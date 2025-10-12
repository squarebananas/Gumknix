using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace PseudoSystem
{
    public class Console
    {
        public Console()
        {
            inMemoryStream = new MemoryStream();
            outMemoryStream = new MemoryStream();
            errorMemoryStream = new MemoryStream();

            inMemoryStream = new();
            outMemoryStream = new();
            errorMemoryStream = new();

            InputEncoding = Encoding.UTF8;
            OutputEncoding = Encoding.UTF8;

            ConsoleGridCells = CreateGrid(BufferWidth, BufferHeight);

            standardInWriter = new StreamWriter(inMemoryStream) { AutoFlush = true };
            standardInReader = new StreamReader(inMemoryStream, InputEncoding, leaveOpen: true);

            standardOutWriter = new StreamWriter(outMemoryStream) { AutoFlush = true };
            standardOutReader = new StreamReader(outMemoryStream, OutputEncoding, leaveOpen: true);

            standardErrorWriter = new StreamWriter(errorMemoryStream) { AutoFlush = true };
            standardErrorReader = new StreamReader(errorMemoryStream, OutputEncoding, leaveOpen: true);

            In = standardInReader;
            Out = standardOutWriter;
            Error = standardErrorWriter;
        }

        public TextReader In { get; private set; }
        public Encoding InputEncoding { get; set; }
        public Encoding OutputEncoding { get; set; }
        public bool KeyAvailable => consoleKeyBuffer.Count >= 1;

        public async Task<ConsoleKeyInfo> ReadKey()
        {
            return await ReadKey(false);
        }
        public async Task<ConsoleKeyInfo> ReadKey(bool intercept)
        {
            UpdateGridCells();

            while (true)
            {
                if (consoleKeyBuffer.Count >= 1)
                {
                    ConsoleKeyInfo key = consoleKeyBuffer[0];
                    consoleKeyBuffer.RemoveAt(0);

                    if (!IsInputRedirected && !intercept && key.KeyChar != '\0')
                        standardInWriter.Write(key.KeyChar);

                    return key;
                }

                await Task.Delay(16);
            }
        }

        public TextWriter Out { get; private set; }
        public TextWriter Error { get; private set; }

        public bool IsInputRedirected => In != standardInReader;
        public bool IsOutputRedirected => Out != standardOutWriter;
        public bool IsErrorRedirected => Error != standardErrorWriter;
        public int CursorSize { get; set; } = 25;

        public bool NumberLock { get; }
        public bool CapsLock { get; }

        public ConsoleColor BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                UpdateGridCells();
                _backgroundColor = value;
            }
        }
        private ConsoleColor _backgroundColor = ConsoleColor.Black;

        public ConsoleColor ForegroundColor
        {
            get => _foregroundColor;
            set
            {
                UpdateGridCells();
                _foregroundColor = value;
            }
        }
        private ConsoleColor _foregroundColor = ConsoleColor.Gray;

        public void ResetColor()
        {
            BackgroundColor = ConsoleColor.Black;
            ForegroundColor = ConsoleColor.Gray;
        }

        public int BufferWidth { get; set; } = 80;
        public int BufferHeight { get; set; } = 300;

        public void SetBufferSize(int width, int height)
        {
            if (width < 0 || width >= short.MaxValue ||
                height < 0 || height >= short.MaxValue)
                throw new ArgumentOutOfRangeException((width < 0 || width >= short.MaxValue) ? nameof(width) : nameof(height),
                    "The console buffer size must not be less than the current size and position of the console window, nor greater than or equal to short.MaxValue. ");
            //"(Parameter '"+
            //width + "')" + Environment.NewLine +
            //"Actual value was - 1.'";

            BufferWidth = width;
            BufferHeight = height;
            ConsoleGridCells = CreateGrid(BufferWidth, BufferHeight);
        }

        public int WindowLeft { get; set; } = 0;
        public int WindowTop { get; set; } = 0;
        public int WindowWidth { get; set; } = 80;
        public int WindowHeight { get; set; } = 25;

        public void SetWindowPosition(int left, int top)
        {
            WindowLeft = left;
            WindowTop = top;
        }

        public void SetWindowSize(int width, int height)
        {
            WindowWidth = width;
            WindowHeight = height;
        }

        public int LargestWindowWidth { get; }
        public int LargestWindowHeight { get; }
        public bool CursorVisible { get; set; }
        public int CursorLeft { get; set; }
        public int CursorTop { get; set; }

        public (int Left, int Top) GetCursorPosition() { return (CursorLeft, CursorTop); }

        public string Title { get; set; }

        public async Task Beep() => await Beep(frequency: 800, duration: 200);

        public async Task Beep(int frequency, int duration)
        {
            BeepRequested = new(frequency, duration);
            await Task.Delay(duration);
        }
        public void MoveBufferArea(int sourceLeft, int sourceTop, int sourceWidth, int sourceHeight, int targetLeft, int targetTop,
            char sourceChar, ConsoleColor sourceForeColor, ConsoleColor sourceBackColor)
        { }
        public void Clear() { }
        public void SetCursorPosition(int left, int top)
        {
            CursorLeft = left;
            CursorTop = top;
        }

        public event ConsoleCancelEventHandler? CancelKeyPress;

        public bool TreatControlCAsInput { get; set; }

        public Stream OpenStandardInput() => inMemoryStream;
        public Stream OpenStandardInput(int bufferSize) => inMemoryStream;
        public Stream OpenStandardOutput() => outMemoryStream;
        public Stream OpenStandardOutput(int bufferSize) => outMemoryStream;
        public Stream OpenStandardError() => errorMemoryStream;
        public Stream OpenStandardError(int bufferSize) => errorMemoryStream;
        public void SetIn(TextReader newIn) => In = newIn;
        public void SetOut(TextWriter newOut) => Out = newOut;
        public void SetError(TextWriter newError) => Error = newError;
        public async Task<int> Read()
        {
            UpdateGridCells();

            if (IsInputRedirected)
            {
                int redirectedValue = In.Read();
                return redirectedValue;
            }

            for (int i = 0; i < consoleKeyBuffer.Count; i++)
                if (consoleKeyBuffer[i].KeyChar != '\0')
                    standardInWriter.Write(consoleKeyBuffer[i].KeyChar);
            consoleKeyBuffer.Clear();

            long writePosition = inMemoryStream.Position;
            inMemoryStream.Position = readInPosition;
            int value = In.Read();
            readInPosition++;

            if (readInPosition < writePosition)
            {
                inMemoryStream.Position = writePosition;
            }
            else
            {
                inMemoryStream.SetLength(0);
                readInPosition = 0;
            }

            return value;
        }
        public async Task<string?> ReadLine()
        {
            UpdateGridCells();

            if (IsInputRedirected)
            {
                string redirectedValue = In.ReadLine();
                return redirectedValue;
            }

            string lineEntered = "";
            int startCursorLeft = CursorLeft;

            while (true)
            {
                int value = await Read();
                if (value < 0)
                {
                    await Task.Delay(16);
                    continue;
                }

                char character = (char)value;

                if (character == (int)ConsoleKey.Enter)
                {
                    NextLine();
                    return lineEntered;
                }

                lineEntered += character;

                int wrappedBufferY = (CursorTop + BufferLineZero) % BufferHeight;

                if (character == (int)ConsoleKey.Backspace)
                {
                    CursorLeft--;
                    if (CursorLeft >= startCursorLeft)
                    {
                        ConsoleGridCell cell = ConsoleGridCells[CursorLeft][wrappedBufferY];
                        ConsoleGridCells[CursorLeft][wrappedBufferY] = new()
                        {
                            Character = '\0',
                            ForegroundColor = cell.ForegroundColor,
                            BackgroundColor = cell.BackgroundColor
                        };
                    }
                    else
                    {
                        CursorLeft = startCursorLeft;
                    }
                }
                else
                {
                    ConsoleGridCells[CursorLeft][wrappedBufferY] = new()
                    {
                        Character = character,
                        ForegroundColor = ConsoleColorToColor(ForegroundColor),
                        BackgroundColor = ConsoleColorToColor(BackgroundColor)
                    };

                    CursorLeft++;
                    if (CursorLeft >= BufferWidth)
                        NextLine();
                }
            }
        }
        public void WriteLine() => Out.WriteLine();
        public void WriteLine(bool value) => Out.WriteLine(value);
        public void WriteLine(char value) => Out.WriteLine(value);
        public void WriteLine(char[]? buffer) => Out.WriteLine(buffer);
        public void WriteLine(char[] buffer, int index, int count) => Out.WriteLine(buffer, index, count);
        public void WriteLine(decimal value) => Out.WriteLine(value);
        public void WriteLine(double value) => Out.WriteLine(value);
        public void WriteLine(float value) => Out.WriteLine(value);
        public void WriteLine(int value) => Out.WriteLine(value);
        public void WriteLine(uint value) => Out.WriteLine(value);
        public void WriteLine(long value) => Out.WriteLine(value);
        public void WriteLine(ulong value) => Out.WriteLine(value);
        public void WriteLine(object? value) => Out.WriteLine(value);
        public void WriteLine(string? value) => Out.WriteLine(value);
        public void WriteLine([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg0) =>
            Out.WriteLine(format, arg0);
        public void WriteLine([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg0, object? arg1) =>
            Out.WriteLine(format, arg0, arg1);
        public void WriteLine([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg0, object? arg1, object? arg2) =>
            Out.WriteLine(format, arg0, arg1, arg2);
        public void WriteLine([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, params object?[]? arg) =>
            Out.WriteLine(format, arg);
        public void Write([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg0) =>
            Out.Write(format, arg0);
        public void Write([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg0, object? arg1) =>
            Out.Write(format, arg0, arg1);
        public void Write([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg0, object? arg1, object? arg2) =>
            Out.Write(format, arg0, arg1, arg2);
        public void Write([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, params object?[]? arg) =>
            Out.Write(format, arg);
        public void Write(bool value) => Out.Write(value);
        public void Write(char value) => Out.Write(value);
        public void Write(char[]? buffer) => Out.Write(buffer);
        public void Write(char[] buffer, int index, int count) => Out.Write(buffer, index, count);
        public void Write(double value) => Out.Write(value);
        public void Write(decimal value) => Out.Write(value);
        public void Write(float value) => Out.Write(value);
        public void Write(int value) => Out.Write(value);
        public void Write(uint value) => Out.Write(value);
        public void Write(long value) => Out.Write(value);
        public void Write(ulong value) => Out.Write(value);
        public void Write(object? value) => Out.Write(value);
        public void Write(string? value) => Out.Write(value);

        private MemoryStream inMemoryStream;
        private MemoryStream outMemoryStream;
        private MemoryStream errorMemoryStream;

        private TextWriter standardInWriter;
        private TextReader standardInReader;
        private TextWriter standardOutWriter;
        private TextReader standardOutReader;
        private TextWriter standardErrorWriter;
        private TextReader standardErrorReader;
        private int readInPosition = 0;

        private List<ConsoleKeyInfo> consoleKeyBuffer = [];

        public struct ConsoleGridCell
        {
            public char Character { get; init; }
            public Color ForegroundColor;
            public Color BackgroundColor;
        }

        public ConsoleGridCell[][] ConsoleGridCells { get; private set; }

        public int BufferLineZero { get; private set; }
        public int TotalLinesWritten { get; private set; }

        public (int frequency, int duration)? BeepRequested { get; set; }

        {
            if (outMemoryStream.Position >= 1)
            {
                outMemoryStream.Position = 0;

                while (true)
                {
                    int value = readOut.Read();
                    if (value < 0)
                        break;
                    char character = (char)value;

                    if (character == '\n')
                    {
                        NextLine();
                        continue;
                    }
                    if (character == '\r')
                    {
                        CursorLeft = 0;
                        continue;
                    }

                    int wrappedBufferY = (CursorTop + BufferLineZero) % BufferHeight;
                    ConsoleGridCells[CursorLeft][wrappedBufferY] = new()
                    {
                        Character = character,
                        ForegroundColor = ConsoleColorToColor(ForegroundColor),
                        BackgroundColor = ConsoleColorToColor(BackgroundColor)
                    };

                    CursorLeft++;
                    if (CursorLeft >= BufferWidth)
                        NextLine();
                }

                outMemoryStream.SetLength(0);
            }
        }

        public void AddKeyPresses(List<ConsoleKeyInfo> pressedKeys)
        {
            consoleKeyBuffer.AddRange(pressedKeys);
        }

        public void NextLine()
        {
            CursorLeft = 0;
            if (CursorTop < (WindowHeight - 1))
                CursorTop++;
            else
                BufferLineZero++;
            TotalLinesWritten++;
        }

        ConsoleGridCell[][] CreateGrid(int width, int height)
        {
            ConsoleGridCell[][] grid = new ConsoleGridCell[width][];
            for (int x = 0; x < width; x++)
            {
                grid[x] = new ConsoleGridCell[height];
                for (int y = 0; y < height; y++)
                {
                    grid[x][y] = new ConsoleGridCell
                    {
                        Character = '\0',
                        ForegroundColor = ConsoleColorToColor(ForegroundColor),
                        BackgroundColor = ConsoleColorToColor(BackgroundColor)
                    };
                }
            }
            return grid;
        }

        //public void SyncOutToGrid()
        //{
        //    Out.Flush();
        //    outMemoryStream.Position = 0;
        //    using StreamReader streamReader = new StreamReader(outMemoryStream, OutputEncoding, leaveOpen: true);
        //    string output = streamReader.ReadToEnd();
        //    string[] lines = output.Split(["\r\n", "\n"], StringSplitOptions.None);
        //    for (int lineIndex = 0; lineIndex < BufferHeight; lineIndex++)
        //    {
        //        string line = (lineIndex < lines.Length) ? lines[lineIndex] : null;
        //        for (int charIndex = 0; charIndex < BufferWidth; charIndex++)
        //            ConsoleGridCells[charIndex][lineIndex].Character = (charIndex < line?.Length) ? line[charIndex] : '\0';
        //    }
        //}

        public static Color ConsoleColorToColor(ConsoleColor consoleColor)
        {
            return consoleColor switch
            {
                ConsoleColor.Black => Color.Black,
                ConsoleColor.DarkBlue => Color.Navy,
                ConsoleColor.DarkGreen => Color.Green,
                ConsoleColor.DarkCyan => Color.Teal,
                ConsoleColor.DarkRed => Color.Maroon,
                ConsoleColor.DarkMagenta => Color.Purple,
                ConsoleColor.DarkYellow => Color.Olive,
                ConsoleColor.Gray => Color.Silver,
                ConsoleColor.DarkGray => Color.Gray,
                ConsoleColor.Blue => Color.Blue,
                ConsoleColor.Green => Color.Lime,
                ConsoleColor.Cyan => Color.Cyan,
                ConsoleColor.Red => Color.Red,
                ConsoleColor.Magenta => Color.Magenta,
                ConsoleColor.Yellow => Color.Yellow,
                ConsoleColor.White => Color.White,
                _ => Color.Transparent
            };
        }

        public void Dispose()
        {
            inMemoryStream?.Dispose();
            outMemoryStream?.Dispose();
            errorMemoryStream?.Dispose();

            standardInWriter?.Dispose();
            standardInReader?.Dispose();
            standardOutWriter?.Dispose();
            standardOutReader?.Dispose();
            standardErrorWriter?.Dispose();
            standardErrorReader?.Dispose();
        }
    }
}
