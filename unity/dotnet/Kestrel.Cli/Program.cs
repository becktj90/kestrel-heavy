using System;
using Kestrel.Core.Cli;

namespace Kestrel.Cli
{
    static class Program
    {
        static int Main(string[] args)
        {
            int code = Commands.Run(args, out string stdout);
            Console.WriteLine(stdout);
            return code;
        }
    }
}
