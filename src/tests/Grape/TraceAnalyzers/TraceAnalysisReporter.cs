using Microsoft.Diagnostics.Grape;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Microsoft.Diagnostics.Tracing.EventPipe;
using Microsoft.Diagnostics.Tracing;

namespace Grape.TraceAnalyzers
{
    class TraceAnalysisReporter
    {
        /// <summary>
        /// List of trace files to analyze
        /// </summary>
        private List<string> traceFiles;


        public TraceAnalysisReporter(TraceGeneratorConfiguration traceConfig)
        {
            traceFiles = new List<string>();
            var traceName = traceConfig.traceName;

            traceFiles.Add($"{traceName}.etl");
            traceFiles.Add($"{traceName}.nettrace");
        }

        public void Report()
        {
            // Sanity check before we start parsing the trace
            AssertTracesExist();

            foreach (var traceFile in traceFiles)
            {
                if (traceFile.EndsWith(".etl"))
                {
                    // TODO: EtwTraceAnalyzer
                }
                else if (traceFile.EndsWith(".nettrace"))
                {
                    var analyzer = new EventPipeTraceAnalyzer(traceFile);
                    var result = analyzer.Report();
                    result.WriteToConsole();
                }
            }
        }

        private void AssertTracesExist()
        {
            foreach (var traceFile in traceFiles)
            {
                if (!File.Exists(traceFile))
                {
                    throw new ArgumentException($"{traceFile} does not exist!");
                }
            }
        }
    }
}
