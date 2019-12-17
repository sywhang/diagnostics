using System;
using System.Diagnostics;
using Microsoft.Diagnostics.NETCore.Client;

namespace Microsoft.Diagnostics.Grape
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args[0] == "validate")
            {
                var pathToExe = args[1];
                var pathToDiff = args[2];

                TraceGenerator basegenerator = new TraceGenerator(pathToExe, "base.nettrace");
                TraceGenerator diffgenerator = new TraceGenerator(pathToExe, "diff.nettrace"); 

                Console.WriteLine("Collecting trace for base");
                basegenerator.CollectEventPipeTrace(60);
                Console.WriteLine("Done collecting trace for base");
                
                Console.WriteLine("Collecting trace for diff");
                diffgenerator.CollectEventPipeTrace(60);
                Console.WriteLine("Done collecting trace for diff");
            }
        }


        static void PrintUsage()
        {
            Console.WriteLine("dotnet run validate --diff <path-to-coreclr>");
        }
    }
}
