using System;

namespace PseudoSystem
{
    public delegate void ConsoleCancelEventHandler(object? sender, ConsoleCancelEventArgs e);

    public class ConsoleCancelEventArgs : EventArgs
    {
        public bool Cancel { get; set; }

        public ConsoleSpecialKey SpecialKey { get; set; }
    }
}
