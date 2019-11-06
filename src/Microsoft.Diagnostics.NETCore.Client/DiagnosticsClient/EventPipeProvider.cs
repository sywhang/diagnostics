// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Text;

namespace Microsoft.Diagnostics.NETCore.Client
{
    public class EventPipeProvider
    {
        public EventPipeProvider(string name, EventLevel eventLevel, long keywords = 0, IDictionary<string, string> arguments = null)
        {
            Name = name;
            EventLevel = eventLevel;
            Keywords = keywords;
            Arguments = arguments;
        }

        public long Keywords { get; }

        public EventLevel EventLevel { get; }

        public string Name { get; }

        public IDictionary<string, string> Arguments { get; }

        public override string ToString()
        {
            return $"{Name}:0x{Keywords:X16}:{(uint)EventLevel}{(Arguments == null ? "" : $":{GetArgumentString()}")}";
        }
        
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            
            return this == (EventPipeProvider)obj;
        }

        public override int GetHashCode()
        {
            int hash = 0;
            hash ^= this.Name.GetHashCode();
            hash ^= this.Keywords.GetHashCode();
            hash ^= this.EventLevel.GetHashCode();
            hash ^= this.Arguments.GetHashCode();
            return hash;
        }

        public static bool operator ==(EventPipeProvider left, EventPipeProvider right)
        {
            return left.Name == right.Name &&
                left.Keywords == right.Keywords &&
                left.EventLevel == right.EventLevel && 
                left.Arguments == right.Arguments; // TODO: FIX THE ARGUMENT CHECK!!!
        }

        public static bool operator !=(EventPipeProvider left, EventPipeProvider right)
        {
            return !(left == right);    
        }

        internal string GetArgumentString()
        {
            StringBuilder sb = new StringBuilder();
            foreach(var argument in Arguments)
            {
                sb.Append($"{argument.Key}={argument.Value};");
            }
            return sb.ToString();
        }

    }
}