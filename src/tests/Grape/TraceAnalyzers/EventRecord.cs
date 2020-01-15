using Microsoft.Diagnostics.Tracing;
using System;
using System.Collections.Generic;
using System.Text;

namespace Grape.TraceAnalyzers
{
    /// <summary>
    /// This keeps the per-provider & per-event tally of all events seen in this EventPipe trace
    /// </summary>
    public class EventRecord
    {
        public Dictionary<string, Dictionary<string, int>> eventCounts;

        public EventRecord()
        {
            eventCounts = new Dictionary<string, Dictionary<string, int>>();
        }

        public void Add(TraceEvent eventData)
        {
            var providerName = eventData.ProviderName;
            var eventName = eventData.EventName;
            if (eventCounts.ContainsKey(providerName))
            {
                if (eventCounts[providerName].ContainsKey(eventName))
                {
                    eventCounts[providerName][eventName] += 1;
                }
                else
                {
                    eventCounts[providerName].Add(eventName, 1);
                }
            }
            else
            {
                eventCounts.Add(providerName, new Dictionary<string, int>()
                {
                    { eventName, 1 }
                });
            }
        }

        public void WriteToConsole()
        {
            Console.WriteLine("");
            Console.Write(String.Format("{0, -80}", "Event"));
            Console.Write(" | ");
            Console.Write(String.Format("{0, -20}", "Count"));
            Console.Write('\n');
            Console.WriteLine(new string('-', 100));
            foreach (var provEventCnt in eventCounts)
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
}
