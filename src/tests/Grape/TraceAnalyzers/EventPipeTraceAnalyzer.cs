// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Grape;
using Microsoft.Diagnostics.Tracing;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Grape.TraceAnalyzers
{
    /// <summary>
    /// A class for parsing and analyzing an EventPipe trace (.nettrace)
    /// </summary>
    public class EventPipeTraceAnalyzer
    {
        /// <summary>
        /// The EventPipeEventSource that contains the target file
        /// </summary>
        private readonly EventPipeEventSource source;

        /// <summary>
        /// Keeps track of the event counts
        /// </summary>
        private EventRecord eventRecord;

        public EventPipeTraceAnalyzer(string traceName)
        {
            this.source = new EventPipeEventSource(traceName);
            this.eventRecord = new EventRecord();

            Action<TraceEvent> handler = delegate (TraceEvent data)
            {
                eventRecord.Add(data);
            };
            source.Clr.All += handler;
        }

        public EventRecord Report()
        {
            source.Process();
            return eventRecord;
        }
    }
}
