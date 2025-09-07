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

            In = new StreamReader(inMemoryStream);
            Out = new StreamWriter(outMemoryStream) { AutoFlush = true };
            Error = new StreamWriter(errorMemoryStream);
            InputEncoding = Encoding.UTF8;
            OutputEncoding = Encoding.UTF8;

            ConsoleGridCells = CreateGrid(BufferWidth, BufferHeight);
            writeIn = new StreamWriter(inMemoryStream) { AutoFlush = true };
            readOut = new StreamReader(outMemoryStream, OutputEncoding, leaveOpen: true);
        }

        public TextReader In { get; }
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

                    if (!intercept)
                        writeIn.Write(key.KeyChar);

                    return key;
                }

                await Task.Delay(16);
            }
        }

        public TextWriter Out { get; }
        public TextWriter Error { get; }

        public bool IsInputRedirected { get; }
        public bool IsOutputRedirected { get; }
        public bool IsErrorRedirected { get; }
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

        public void Beep() { }
        public void Beep(int frequency, int duration) { }
        public void MoveBufferArea(int sourceLeft, int sourceTop, int sourceWidth, int sourceHeight, int targetLeft, int targetTop) { }
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

        public Stream OpenStandardInput() { return Stream.Null; }
        public Stream OpenStandardInput(int bufferSize) { return Stream.Null; }
        public Stream OpenStandardOutput() { return Stream.Null; }
        public Stream OpenStandardOutput(int bufferSize) { return Stream.Null; }
        public Stream OpenStandardError() { return Stream.Null; }
        public Stream OpenStandardError(int bufferSize) { return Stream.Null; }
        public void SetIn(TextReader newIn) { }
        public void SetOut(TextWriter newOut) { }
        public void SetError(TextWriter newError) { }
        public async Task<int> Read()
        {
            UpdateGridCells();

            for (int i = 0; i < consoleKeyBuffer.Count; i++)
                writeIn.Write(consoleKeyBuffer[i].KeyChar);
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
                    CursorLeft = 0;
                    CursorTop++;
                    return lineEntered;
                }

                lineEntered += character;

                if (character == (int)ConsoleKey.Backspace)
                {
                    CursorLeft--;
                    if (CursorLeft >= startCursorLeft)
                    {
                        ConsoleGridCell cell = ConsoleGridCells[CursorLeft][CursorTop];
                        ConsoleGridCells[CursorLeft][CursorTop] = new()
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
                    ConsoleGridCells[CursorLeft][CursorTop] = new()
                    {
                        Character = character,
                        ForegroundColor = ConsoleColorToColor(ForegroundColor),
                        BackgroundColor = ConsoleColorToColor(BackgroundColor)
                    };

                    CursorLeft++;
                    if (CursorLeft >= BufferWidth)
                    {
                        CursorLeft = 0;
                        CursorTop++;
                    }
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

        private StreamWriter writeIn;
        private StreamReader readOut;
        private int readInPosition = 0;

        private List<ConsoleKeyInfo> consoleKeyBuffer = [];

        public struct ConsoleGridCell
        {
            public char Character { get; init; }
            public Color ForegroundColor;
            public Color BackgroundColor;
        }

        public ConsoleGridCell[][] ConsoleGridCells { get; private set; }

        public void UpdateGridCells()
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
                        CursorLeft = 0;
                        CursorTop++;
                        continue;
                    }
                    if (character == '\r')
                    {
                        CursorLeft = 0;
                        continue;
                    }

                    ConsoleGridCells[CursorLeft][CursorTop] = new()
                    {
                        Character = character,
                        ForegroundColor = ConsoleColorToColor(ForegroundColor),
                        BackgroundColor = ConsoleColorToColor(BackgroundColor)
                    };

                    CursorLeft++;
                    if (CursorLeft >= BufferWidth)
                    {
                        CursorLeft = 0;
                        CursorTop++;
                        //if (CursorTop >= BufferHeight)
                        //    CursorTop = 0;
                    }

                }

                outMemoryStream.SetLength(0);
            }
        }

        public void AddKeyPresses(List<ConsoleKeyInfo> pressedKeys)
        {
            consoleKeyBuffer.AddRange(pressedKeys);
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
            writeIn?.Dispose();
            readOut?.Dispose();
        }
    }
}
