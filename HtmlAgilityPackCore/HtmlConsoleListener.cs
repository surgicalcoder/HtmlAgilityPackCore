using System.Diagnostics;
#if !NETSTANDARD1_3 && !NETSTANDARD1_6 && !METRO
using System;

namespace HtmlAgilityPackCore
{
    internal class HtmlConsoleListener : TraceListener
    {
        public override void Write(string Message)
        {
            Write(Message, "");
        }

        public override void Write(string Message, string Category)
        {
            Console.Write("T:" + Category + ": " + Message);
        }

        public override void WriteLine(string Message)
        {
            Write(Message + "\n");
        }

        public override void WriteLine(string Message, string Category)
        {
            Write(Message + "\n", Category);
        }
    }
}
#endif