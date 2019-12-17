using System;
using System.Diagnostics.Tracing;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;



namespace Microsoft.Diagnostics.Grape
{
	public class TraceGenerator
	{
		TestRunner _runner;
		string _pathToExe;
		string _traceName;
		List<EventPipeProvider> _providers;

		public TraceGenerator(string pathToExe, string traceName)
		{
			_pathToExe = pathToExe;
			_traceName = traceName;
			_providers = new List<EventPipeProvider>()
		    {
		        new EventPipeProvider("Microsoft-Windows-DotNETRuntime",
		            EventLevel.Informational, (long)(-1))
		    };
		}

	    public void CollectEventPipeTrace(int duration)
	    {
	    	var pid = LaunchProcess(_pathToExe);
	    	TraceProcessForDuration(pid, duration, _traceName);
	    }

		private int LaunchProcess(string pathToExe)
	    {
	        _runner = new TestRunner(pathToExe);
	        _runner.Start(2000); // Let's give it the same amount of time to sleep after it starts artificially...
	    	return _runner.Pid;
	    }

	    public void TraceProcessForDuration(int processId, int duration, string traceName)
		{
		    var cpuProviders = new List<EventPipeProvider>()
		    {
		        new EventPipeProvider("Microsoft-Windows-DotNETRuntime", EventLevel.Informational, (long)ClrTraceEventParser.Keywords.Default),
		        new EventPipeProvider("Microsoft-DotNETCore-SampleProfiler", EventLevel.Informational, (long)ClrTraceEventParser.Keywords.None),
   		        new EventPipeProvider("My-Test-EventSource", EventLevel.Verbose, 0x1)
		    };
		    var client = new DiagnosticsClient(processId);
		    using (var traceSession = client.StartEventPipeSession(cpuProviders))
		    {
		        Task copyTask = Task.Run(async () =>
		        {
		            using (FileStream fs = new FileStream(traceName, FileMode.Create, FileAccess.Write))
		            {
		                await traceSession.EventStream.CopyToAsync(fs);
		            }
		        });

		        copyTask.Wait(duration * 1000);
		        traceSession.Stop();
		    }
		}
	}
}
