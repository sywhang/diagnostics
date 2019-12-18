using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using Microsoft.Diagnostics.NETCore.Client;

namespace Microsoft.Diagnostics.Grape
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args[0] == "diff")
            {
                var pathToExe = args[1];
                var pathToDiff = args[2];
                var providers = new List<EventPipeProvider>()
                {
                    new EventPipeProvider("Microsoft-Windows-DotNETRuntime", EventLevel.Informational, (long)(-1))
                };

                var diffGen = new TraceDiffGenerator(pathToExe, pathToDiff, providers);
                diffGen.Start(3000);
            }
        }


        static void PrintUsage()
        {
            Console.WriteLine("dotnet run validate --diff <path-to-coreclr>");
        }
    }
}
