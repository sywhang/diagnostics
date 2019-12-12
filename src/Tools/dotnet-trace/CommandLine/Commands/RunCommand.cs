// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.RuntimeClient;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Rendering;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Tools.Trace
{
	internal static class RunCommandHandler
    {
    	delegate Task<int> RunDelegate(CancellationToken ct, IConsole console, string executable, string providers, string profile);

    	private static async Task<int> Run(CancellationToken ct, IConsole console, string executable, string providers, string profile)
    	{
            // TODO: This should be File.Exists()? 
            if (executable.Length == 0)
            {
                Console.WriteLine("Please specify a valid executable.");
                return 1;
            }

    		int processId = Process.GetCurrentProcess().Id;
	        string pipeName = $"dotnet-trace-pipe-{processId}";


    		Task pipeTask = new Task(() => {
                
    			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
	            {
	            	Console.WriteLine($"Creating a pipe with the name: {pipeName}");
	                var pipeServer = new NamedPipeServerStream(
	                    pipeName, PipeDirection.InOut, 5, PipeTransmissionMode.Byte, PipeOptions.None);
                    Console.WriteLine("Waiting for client to connect...");

                    pipeServer.WaitForConnection();
                    Console.WriteLine("A client connected!!!");
                    using (var fs = new FileStream("./trace.nettrace", FileMode.Create, FileAccess.Write))
                    {
                    	var buffer = new byte[16 * 1024];
                        while (true)
                        {
                            int nBytesRead = pipeServer.Read(buffer, 0, buffer.Length);
                            if (nBytesRead <= 0)
                                break;
                            fs.Write(buffer, 0, nBytesRead);
                            Console.Write(".");
                        }
                    }
	            }
    		});

    		Task childPTask = new Task(() => {
	            // Creating child process
	    		var childProcessInfo = new ProcessStartInfo(executable);
	    		childProcessInfo.EnvironmentVariables["COMPlus_EnableEventPipe"] = "1";
	    		childProcessInfo.EnvironmentVariables["COMPlus_EventPipeConfig"] = "Microsoft-Windows-DotNETRuntime:ffffffffffffffff:4";
	    		
	    		Console.WriteLine($"launching executable: {executable}");

	    		Process childProcess = Process.Start(childProcessInfo);

	    		Console.WriteLine($"{childProcess.ProcessName} started with PID {childProcess.Id}");
	    		// Wait till this guy is done
	    		childProcess.WaitForExit();
	    		
	    		while(true) {} 
    		});

			pipeTask.Start();
    		childPTask.Start();

    		Task.WaitAny(pipeTask, childPTask);

    		await Task.Run(() => Console.WriteLine("Done"));
	    	return 1;
       	}

        private static Option ExecutableOption() =>
            new Option(
                alias: "--executable",
                description: @"Executable to launch.",
                argument: new Argument<string>(defaultValue: "") { Name = "executable" }, // TODO: Can we specify an actual type?
                isHidden: false);

        private static Option ProvidersOption() =>
            new Option(
                alias: "--providers",
                description: @"A list of EventPipe providers to be enabled. This is in the form 'Provider[,Provider]', where Provider is in the form: 'KnownProviderName[:Flags[:Level][:KeyValueArgs]]', and KeyValueArgs is in the form: '[key1=value1][;key2=value2]'. These providers are in addition to any providers implied by the --profile argument. If there is any discrepancy for a particular provider, the configuration here takes precedence over the implicit configuration from the profile.",
                argument: new Argument<string>(defaultValue: "") { Name = "list-of-comma-separated-providers" }, // TODO: Can we specify an actual type?
                isHidden: false);

        private static Option ProfileOption() =>
            new Option(
                alias: "--profile",
                description: @"A named pre-defined set of provider configurations that allows common tracing scenarios to be specified succinctly.",
                argument: new Argument<string>(defaultValue: "") { Name = "profile-name" },
                isHidden: false);

        public static Command RunCommand() =>
            new Command(
                name: "run",
                description: "Launches the specified executable and attaches the trace from start-up of the process",
                symbols: new Option[] {
                    ExecutableOption(),
                    ProvidersOption(),
                    ProfileOption()
                },
                handler: HandlerDescriptor.FromDelegate((RunDelegate)Run).GetCommandHandler());

    }
}