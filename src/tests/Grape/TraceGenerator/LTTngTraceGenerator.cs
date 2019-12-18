using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing.Session;

namespace Microsoft.Diagnostics.Grape
{
    class LTTngTraceGenerator
    {
        TestRunner _runner;
        string _pathToExe;
        string _traceName;
        List<EventPipeProvider> _providers;

        /// <summary>
        /// This generates an LTTng trace for a given program. Note that LTTng trace works with perfcollect, so it just has to launch a child process
        /// that runs perfcollect in a separate shell. This kind of sucks, but we have to live with it...
        /// </summary>
        /// <param name="pathToExe"></param>
        /// <param name="traceName"></param>
        /// <param name="providers"></param>
        public LTTngTraceGenerator(string pathToExe, string traceName, List<EventPipeProvider> providers)
        {
            _pathToExe = pathToExe;
            _traceName = traceName;
            _providers = providers;
        }

        public void CollectEventPipeTrace(int duration)
        {
            var pid = LaunchProcess(_pathToExe);
            TraceProcessForDuration(duration, _traceName);
        }

        private int LaunchProcess(string pathToExe)
        {
            _runner = new TestRunner(pathToExe);
            _runner.AddEnvVar("COMPlus_PerfMapEnabled", "1");
            _runner.AddEnvVar("COMPlus_EnableEventLog", "1");
            _runner.Start(2000); // Let's give it the same amount of time to sleep after it starts artificially...
            return _runner.Pid;
        }

        private string GetLttngConfigString()
        {
            var configStr = "";
            foreach (var provider in _providers)
            {
                configStr += provider.ToString();
                configStr += ",";
            }
            return configStr.Substring(0, configStr.Length - 1); 
        }

        public void TraceProcessForDuration(int duration, string traceName)
        {
            var _tracerProcess = new TestRunner("bash", "perfcollect.sh");
        }
    }
}
