using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Reflection;


namespace dotnet_counters
{
    class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestEnvironmentSet()
        {
            Stopwatch sw = new Stopwatch();
            var i = 0;
            sw.Start();
            while (i < 10_000)
            {
                long ws = Environment.WorkingSet;
                i += 1;
            }
            sw.Stop();

            Console.WriteLine($"Environment.WorkingSet took {sw.Elapsed.Seconds} sec");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestNewAPI(MethodInfo mi)
        {
            Stopwatch sw = new Stopwatch();
            var i = 0;
            sw.Start();
            while (i < 10_000)
            {
                long ws = (long)mi.Invoke(null, null);
                i += 1;
            }
            sw.Stop();
            Console.WriteLine($"RuntimeEventSourceHelper.GetWorkingSet took {sw.Elapsed.Seconds} sec");
        }

        static void Main(string[] args)
        {
            Assembly SPC = typeof(System.Diagnostics.Tracing.EventSource).Assembly;
            if (SPC == null)
            {
                Console.WriteLine("Failed to get System.Private.CoreLib assembly");
                return;
            }
            Type runtimeEventSourceHelperType = SPC.GetType("System.Diagnostics.Tracing.RuntimeEventSourceHelper");
            if (runtimeEventSourceHelperType == null)
            {
                Console.WriteLine("Failed to get System.Private.CoreLib assembly");
                return;
            }
            MethodInfo mi = runtimeEventSourceHelperType.GetMethod("GetWorkingSet", BindingFlags.NonPublic | BindingFlags.Static);
            if (mi == null)
            {
                Console.WriteLine("Failed to get GetWorkingSet method");
                return;
            }

            TestEnvironmentSet();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            TestNewAPI(mi);

            Console.WriteLine("Hello World!");
        }
    }
}
