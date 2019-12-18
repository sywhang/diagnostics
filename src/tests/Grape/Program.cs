using System;
using System.Diagnostics;
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

                var diffGen = new TraceDiffGenerator(pathToExe, pathToDiff);
            }
        }


        static void PrintUsage()
        {
            Console.WriteLine("dotnet run validate --diff <path-to-coreclr>");
        }
    }
}
