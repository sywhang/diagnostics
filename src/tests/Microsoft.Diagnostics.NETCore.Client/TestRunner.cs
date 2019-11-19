// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

using Microsoft.Diagnostics.TestHelpers;

using Microsoft.Diagnostics.NETCore.Client;

namespace Microsoft.Diagnostics.NETCore.Client
{
    public class TestRunner
    {
        private Process testProcess;
        private ProcessStartInfo startInfo;
        private ITestOutputHelper outputHelper;

        public TestRunner(string testExePath, ITestOutputHelper _outputHelper=null)
        {
            startInfo = new ProcessStartInfo(testExePath);
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            outputHelper = _outputHelper;
        }

        public void AddEnvVar(string key, string value)
        {
            startInfo.EnvironmentVariables[key] = value;
        }

        public void Start(int timeoutInMS=0)
        {
            if (outputHelper != null)
                outputHelper.WriteLine("hi");
            testProcess = Process.Start(startInfo);
            Thread.Sleep(timeoutInMS);
        }

        public void Stop()
        {
            testProcess.Close();
        }

        public int Pid {
            get { return testProcess.Id; }
        }
    }
}
