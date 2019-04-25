// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using Microsoft.Diagnostics.Tracing.Parsers;

namespace Microsoft.Diagnostics.Tools.Counters
{
    internal static class KnownData
    {
        private static readonly IReadOnlyDictionary<string, CounterProvider> _knownProviders =
            CreateKnownProviders().ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);


        private static IEnumerable<CounterProvider> CreateKnownProviders()
        {
            yield return new CounterProvider(
                "System.Runtime", // Name
                "A default set of performance counters provided by the .NET runtime.", // Description
                "0xffffffff", // Keywords
                "0x5", // Level 
                new[] { // Counters
                    // NOTE: For now, the set of counters below doesn't really matter because 
                    // we don't really display any counters in real time. (We just collect .netperf files) 
                    // In the future (with IPC), we should filter counter payloads by name provided below to display.  
                    // These are mainly here as placeholders. 
                    new CounterProfile{ Name="cpu-usage", Description="Amount of time the process has utilized the CPU (ms)", DisplayName="CPU Usage (%)", Type=CounterType.PollingCounter },
                    new CounterProfile{ Name="working-set", Description="Amount of working set used by the process (MB)", DisplayName="Working Set (MB)", Type=CounterType.PollingCounter },
                    new CounterProfile{ Name="gc-heap-size", Description="Total heap size reported by the GC (MB)", DisplayName="GC Heap Size (MB)", Type=CounterType.PollingCounter },
                    new CounterProfile{ Name="gen-0-gc-count", Description="Number of Gen 0 GCs", DisplayName="Gen 0 GC / sec", Type=CounterType.IncrementingPollingCounter },
                    new CounterProfile{ Name="gen-1-gc-count", Description="Number of Gen 1 GCs", DisplayName="Gen 1 GC / sec", Type=CounterType.IncrementingPollingCounter },
                    new CounterProfile{ Name="gen-2-gc-count", Description="Number of Gen 2 GCs", DisplayName="Gen 2 GC / sec", Type=CounterType.IncrementingPollingCounter },
                    new CounterProfile{ Name="exception-count", Description="Number of Exceptions / Sec", DisplayName="Exceptions / sec", Type=CounterType.IncrementingPollingCounter },
                });
            // TODO: Add more providers (ex. ASP.NET ones)
        }

        public static IReadOnlyList<CounterProvider> GetAllProviders() => _knownProviders.Values.ToList();

        public static bool TryGetProvider(string providerName, out CounterProvider provider) => _knownProviders.TryGetValue(providerName, out provider);
    }
}
