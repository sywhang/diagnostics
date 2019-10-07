﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.RuntimeClient;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;

namespace Microsoft.Diagnostics.Tools.Trace
{
    internal static class Extensions
    {
        private static EventLevel defaultEventLevel = EventLevel.Verbose;
        public static List<Provider> ToProviders(string providers)
        {
            if (providers == null)
                throw new ArgumentNullException(nameof(providers));
            return string.IsNullOrWhiteSpace(providers) ?
                new List<Provider>() : providers.Split(',').Select(ToProvider).ToList();
        }

        private static EventLevel GetEventLevel(string token)
        {
            if (Int32.TryParse(token, out int level) && level >= 0)
            {
                return level > (int)EventLevel.Verbose ? EventLevel.Verbose : (EventLevel)level;
            }

            else
            {
                switch (token)
                {
                    case "Critical":
                        return EventLevel.Critical;
                    case "Error":
                        return EventLevel.Error;
                    case "Informational":
                        return EventLevel.Informational;
                    case "LogAlways":
                        return EventLevel.LogAlways;
                    case "Verbose":
                        return EventLevel.Verbose;
                    case "Warning":
                        return EventLevel.Warning;
                    default:
                        throw new ArgumentException($"Unknown EventLevel: {token}");
                }
            }
        }

        private static Provider ToProvider(string provider)
        {
            if (string.IsNullOrWhiteSpace(provider))
                throw new ArgumentNullException(nameof(provider));

            var tokens = provider.Split(new[] { ':' }, 4, StringSplitOptions.None); // Keep empty tokens;

            // Provider name
            string providerName = tokens.Length > 0 ? tokens[0] : null;

            // Check if the supplied provider is a GUID and not a name.
            if (Guid.TryParse(providerName, out _))
            {
                Console.WriteLine($"Warning: --provider argument {providerName} appears to be a GUID which is not supported by dotnet-trace. Providers need to be referenced by their textual name.");
            }

            if (string.IsNullOrWhiteSpace(providerName))
                throw new ArgumentException("Provider name was not specified.");

            // Keywords
            ulong keywords = tokens.Length > 1 && !string.IsNullOrWhiteSpace(tokens[1]) ?
                Convert.ToUInt64(tokens[1], 16) : ulong.MaxValue;

            // Level
            EventLevel eventLevel = tokens.Length > 2 && !string.IsNullOrWhiteSpace(tokens[2]) ?
                GetEventLevel(tokens[2]) : defaultEventLevel;

            // Event counters
            string filterData = tokens.Length > 3 ? tokens[3] : null;
            filterData = string.IsNullOrWhiteSpace(filterData) ? null : filterData;

            return new Provider(providerName, keywords, eventLevel, filterData);
        }
    }
}
