using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.NETCore.Client;

namespace Microsoft.Diagnostics.Grape
{
    class TraceDiffGenerator
    {
        private string _baseExe;
        private string _diffExe;
        private List<EventPipeProvider> _providers;

        public TraceDiffGenerator(string baseExe, string diffExe, List<EventPipeProvider> providers)
        {
            _baseExe = baseExe;
            _diffExe = diffExe;
            _providers = providers;
        }


        public int Start(int duration)
        {
            GenerateEventPipeTrace(duration);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                GenerateEtwTrace(duration);
            }
            return 1;
        }

        private void GenerateEventPipeTrace(int duration)
        {
            var baseTracer = new EventPipeTraceGenerator(_baseExe, "base.nettrace", _providers);
            var diffTracer = new EventPipeTraceGenerator(_diffExe, "diff.nettrace", _providers);

            // Collect base trace
            Console.WriteLine($"Collecting EventPipe trace for base exe: {_baseExe}");
            baseTracer.Collect(duration);
            Console.WriteLine($"Done collecting trace for base exe: {_baseExe}");

            // Collect diff trace
            Console.WriteLine($"Collecting EventPipe trace for diff exe: {_diffExe}");
            diffTracer.Collect(duration);
            Console.WriteLine($"Done collecting trace for diff exe: {_diffExe}");
        }

        private void GenerateEtwTrace(int duration)
        {
            var baseTracer = new EtwTraceGenerator(_baseExe, "base.etl", _providers);
            var diffTracer = new EtwTraceGenerator(_diffExe, "diff.etl", _providers);

            Console.WriteLine($"Collecting ETW trace for base exe: {_baseExe}");
            baseTracer.Collect(duration);
            Console.WriteLine($"Done collecting ETW trace for base exe: {_baseExe}");

            Console.WriteLine($"Collecting ETW trace for base exe: {_baseExe}");
            diffTracer.Collect(duration);
            Console.WriteLine($"Done collecting ETW trace for base exe: {_baseExe}");
        }
    }
}
