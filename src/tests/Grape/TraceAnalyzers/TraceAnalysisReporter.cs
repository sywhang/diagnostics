using Microsoft.Diagnostics.Grape;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Microsoft.Diagnostics.Tracing.EventPipe;
using Microsoft.Diagnostics.Tracing;
using System.Runtime.InteropServices;

namespace Grape.TraceAnalyzers
{
    class TraceAnalysisReporter
    {
        /// <summary>
        /// List of trace files to analyze
        /// </summary>
        private List<string> _traceFiles;
        private TraceGeneratorConfiguration _traceConfig;
        private Dictionary<string, EventRecord> _recordHolder;

        public TraceAnalysisReporter(TraceGeneratorConfiguration traceConfig)
        {
            _recordHolder = new Dictionary<string, EventRecord>();
            _traceConfig = traceConfig;
            _traceFiles = new List<string>();
            var traceName = traceConfig.traceName;

            _traceFiles.Add($"{traceName}.nettrace");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _traceFiles.Add($"{traceName}.etl");
            }
        }

        public void Report()
        {
            // Sanity check before we start parsing the trace
            AssertTracesExist();

            foreach (var traceFile in _traceFiles)
            {
                if (traceFile.EndsWith(".etl"))
                {
                    // TODO: EtwTraceAnalyzer
                }
                else if (traceFile.EndsWith(".nettrace"))
                {
                    var analyzer = new EventPipeTraceAnalyzer(traceFile);
                    _recordHolder.Add($"{_traceConfig.traceName} (EventPipe)", analyzer.Report());
                }
            }

        }

        public void WriteToConsole()
        {
            var traceCnt = _recordHolder.Count;
            Console.WriteLine("");
            Console.Write(String.Format($"0, -{80 + traceCnt * 20}", "Event"));
            foreach (var record in _recordHolder)
            {
                Console.Write(" | ");
                Console.Write(String.Format("{0, -20}", record.Key));
            }
            Console.Write('\n');
            Console.WriteLine(new string('-', 60 + traceCnt * 20));
            foreach (var record in _recordHolder)
            {
                foreach (var provEventCnt in record.Value.eventCounts)
                {
                    var providerName = provEventCnt.Key;
                    foreach (var eventCnt in provEventCnt.Value)
                    {
                        var eventName = eventCnt.Key;
                        var count = eventCnt.Value;
                        Console.Write(String.Format("{0, -80}", $"{providerName} / {eventName}"));
                        Console.Write(String.Format("{0, -20}", $" | {count}"));
                        Console.Write('\n');
                        Console.WriteLine(new string('-', 100));
                    }
                }
            }

            
        }

        private void AssertTracesExist()
        {
            foreach (var traceFile in _traceFiles)
            {
                if (!File.Exists(traceFile))
                {
                    throw new ArgumentException($"{traceFile} does not exist!");
                }
            }
        }
    }
}
