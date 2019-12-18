using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.NETCore.Client;

namespace Microsoft.Diagnostics.Grape
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                return;
            }

            if (args[0] == "tracegen")
            {
                var pathToExe = args[1];
                var providers = new List<EventPipeProvider>()
                {
                    new EventPipeProvider("Microsoft-Windows-DotNETRuntime", EventLevel.Informational, (long)(-1))
                };

                var eventpipeTracer = new EventPipeTraceGenerator(pathToExe, "trace.nettrace", providers);
                Console.WriteLine("Collecting EventPipe trace");
                eventpipeTracer.Collect(60);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var etwTracer = new EtwTraceGenerator(pathToExe, "trace.etl", providers);
                    Console.WriteLine("Collecting ETW trace");
                    etwTracer.Collect(60);
                }

                // TODO: Add Linux here
                Console.WriteLine("Done!");
            }
            else if (args[0] == "diff")
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
