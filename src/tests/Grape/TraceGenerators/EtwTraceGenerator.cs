// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Session;


namespace Microsoft.Diagnostics.Grape
{
    public class EtwTraceGenerator
    {
        TestRunner _runner;
        string _pathToExe;
        string _traceName;
        List<EventPipeProvider> _providers;

        public EtwTraceGenerator(string pathToExe, string traceName, List<EventPipeProvider> providers)
        {
            _pathToExe = pathToExe;
            _traceName = traceName;
            _providers = providers;
        }

        public void Collect(int duration)
        {
            var pid = LaunchProcess(_pathToExe);
            TraceProcessForDuration(duration, _traceName);
        }

        private int LaunchProcess(string pathToExe)
        {
            _runner = new TestRunner(pathToExe);
             // Technically this doesn't have to sleep but I'm keeping it here to keep it consistent with EventPipe until we have EventPipe startup tracing.
            _runner.Start(2000);
            return _runner.Pid;
        }

        public void TraceProcessForDuration(int duration, string traceName)
        {
            var tracesession = new TraceEventSession("testname", _traceName);

            foreach (var provider in _providers)
            {
                tracesession.EnableProvider(provider.Name, (TraceEventLevel)provider.EventLevel, (ulong)provider.Keywords);
            }
            
            tracesession.EnableProvider("My-Test-EventSource");

            System.Threading.Thread.Sleep(duration * 1000);
            tracesession.Flush();

            tracesession.DisableProvider("My-Test-EventSource");
            tracesession.Dispose();
        }
    }
}
