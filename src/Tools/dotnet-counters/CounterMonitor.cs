// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Diagnostics.Tools.RuntimeClient;
using System;
using System.CommandLine;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Counters
{
    public class CounterMonitor
    {
        private string outputPath;
        private ulong sessionId;

        private int _processId;
        private int _interval;
        private string _counterList;
        private CancellationToken _ct;
        private IConsole _console;

        public CounterMonitor()
        {
        }

        public async Task<int> Monitor(CancellationToken ct, string counterList, IConsole console, int processId, int interval)
        {
            try
            {
                _ct = ct;
                _counterList = counterList;
                _console = console;
                _processId = processId;
                _interval = interval;

                return await StartMonitor();
            }

            catch (OperationCanceledException)
            {
                // try
                // {
                //     EventPipeClient.DisableTracingToFile(_processId, sessionId);    
                // }
                // catch (Exception) {} // Swallow all exceptions for now.
                
                console.Out.WriteLine($"Tracing stopped. Trace files written to {outputPath}");
                console.Out.WriteLine($"Complete");
                return 1;
            }
        }

        private async Task<int> StartMonitor()
        {
            if (_processId == 0) {
                _console.Error.WriteLine("ProcessId is required.");
                return 1;
            }

            if (_interval == 0) {
                _console.Error.WriteLine("interval is required.");
                return 1;
            }

            outputPath = Path.Combine(Directory.GetCurrentDirectory(), $"dotnet-counters-{_processId}.netperf"); // TODO: This can be removed once events can be streamed in real time.

            String providerString;

            if (string.IsNullOrEmpty(_counterList))
            {
                CounterProvider defaultProvider = null;
                _console.Out.WriteLine($"counter_list is unspecified. Monitoring all counters by default.");

                // Enable the default profile if nothing is specified
                if (!KnownData.TryGetProvider("System.Runtime", out defaultProvider))
                {
                    _console.Error.WriteLine("No providers or profiles were specified and there is no default profile available.");
                    return 1;
                }
                providerString = defaultProvider.ToProviderString(_interval);
            }
            else
            {
                string[] counters = _counterList.Split(" ");
                CounterProvider provider = null;
                StringBuilder sb = new StringBuilder("");
                for (var i = 0; i < counters.Length; i++)
                {
                    if (!KnownData.TryGetProvider(counters[i], out provider))
                    {
                        _console.Error.WriteLine($"No known provider called {counters[i]}.");
                        return 1;
                    }
                    sb.Append(provider.ToProviderString(_interval));
                    if (i != counters.Length - 1)
                    {
                        sb.Append(",");
                    }
                }
                providerString = sb.ToString();
            }


            try
            {
                var configuration = new SessionConfiguration(
                    1000,
                    outputPath,
                    Provider.ToProviders(providerString));
                var binaryReader = EventPipeClient.StreamTracingToFile(_processId, configuration, out var sessionId);
                _console.Out.WriteLine($"SessionId=0x{sessionId:X16}");
                var tBytesRead = 0;
                EventBlockParser parser = new EventBlockParser();
                if (sessionId != 0)
                {
                    while(true)
                    {
                        var buffer = new byte[1024];
                        int nBytesRead = binaryReader.Read(buffer, 0, buffer.Length);
                        _console.Out.WriteLine($"Read {nBytesRead}. Parsing..");
                        parser.ParseBlock(buffer, nBytesRead);

                        tBytesRead += nBytesRead;
                    }
                    
                }
                _console.Out.WriteLine($"Read {tBytesRead} bytes in total.");

            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERROR]: {ex.ToString()}");
                return 1;
            }
            
            return 0;
        }
    }
}
