// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.NETCore.Client;
using System;
using Xunit;
using Xunit.Abstractions;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Trace
{

    public class TraceUntilExitTest
    {
        private const int processTimeout = 60_000;
        private int traceePid;
        private ManualResetEvent traceeStarted;

        // Pass ITestOutputHelper into the test class, which xunit provides per-test
        public TraceUntilExitTest(ITestOutputHelper outputHelper)
        {
            OutputHelper = outputHelper;
        }

        private ITestOutputHelper OutputHelper { get; }

        private void LaunchTracee()
        {
            string exitCodeTraceePath = CommonHelper.GetTraceePathWithArgs(traceeName: "Tracee", targetFramework: "net5.0");
            ProcessStartInfo startInfo = new ProcessStartInfo(CommonHelper.HostExe, $"{exitCodeTraceePath}");

            OutputHelper.WriteLine($"Launching: {startInfo.FileName} {startInfo.Arguments}");
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardOutput = true;

            using (Process process = Process.Start(startInfo))
            {
                traceePid = process.Id;
                traceeStarted.Set();
                bool processExitedCleanly = process.WaitForExit(processTimeout);
                if (!processExitedCleanly)
                {
                    OutputHelper.WriteLine($"Forced kill of process after {processTimeout}ms");
                    process.Kill();
                }
                Assert.True(processExitedCleanly, "Launched process failed to exit");
            }
        }

        private void LaunchDotNetTrace(string command, out string stdOut, out string stdErr)
        {
            string dotnetTracePathWithArgs = CommonHelper.GetTraceePathWithArgs(traceeName: "dotnet-trace").Replace("net5.0", "netcoreapp2.1");
            ProcessStartInfo startInfo = new ProcessStartInfo(CommonHelper.HostExe, $"{dotnetTracePathWithArgs} collect -o traceuntilexit.nettrace -p {traceePid}");

            traceeStarted.WaitOne();

            OutputHelper.WriteLine($"Launching: {startInfo.FileName} {startInfo.Arguments}");
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardOutput = true;

            using (Process process = Process.Start(startInfo))
            {
                OutputHelper.WriteLine("StdErr");
                stdErr = process.StandardError.ReadToEnd();
                OutputHelper.WriteLine(stdErr);
                OutputHelper.WriteLine("StdOut");
                stdOut = process.StandardOutput.ReadToEnd();
                OutputHelper.WriteLine(stdOut);
                process.WaitForExit(processTimeout);
            }
            
        }

        [Fact]
        public void VerifyNoException()
        {
            string stdOut="", stdErr="";
            traceeStarted = new ManualResetEvent(false);
            Task traceeTask = Task.Run(() => LaunchTracee());
            Task dotnetTraceTask = Task.Run(() => LaunchDotNetTrace($"", out stdOut, out stdErr));
            Task.WaitAll(new Task[] { traceeTask, dotnetTraceTask });
            Assert.DoesNotContain("[ERROR]", stdOut);
        }
    }
}
