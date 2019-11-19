// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Microsoft.Diagnostics.NETCore.Client
{
    public class EventPipeSession : IDisposable
    {
        private IEnumerable<EventPipeProvider> _providers;
        private bool _requestRundown;
        private int _circularBufferMB;
        private long _sessionId;
        private int _processId;
        private bool disposedValue = false; // To detect redundant calls

        internal EventPipeSession(int processId, IEnumerable<EventPipeProvider> providers, bool requestRundown, int circularBufferMB)
        {
            _processId = processId;
            _providers = providers;
            _requestRundown = requestRundown;
            _circularBufferMB = circularBufferMB;
            
            var v2config = new EventPipeSessionConfiguration(circularBufferMB, EventPipeSerializationFormat.NetTrace, providers, requestRundown);
            var v2message = new IpcMessage(DiagnosticsServerCommandSet.EventPipe, (byte)EventPipeCommandId.CollectTracing2, v2config.SerializeV2());
            bool v2Failed = false;
            EventStream = IpcClient.SendMessage(processId, v2message, out var responsev2);
            switch ((DiagnosticsServerCommandId)responsev2.Header.CommandId)
            {
                case DiagnosticsServerCommandId.OK:
                    _sessionId = BitConverter.ToInt64(responsev2.Payload, 0);
                    break;
                case DiagnosticsServerCommandId.Error:
                    // Try again with v1
                    v2Failed = true;
                    break;
                default:
                    throw new ServerErrorException($"EventPipe session start failed - Server responded with unknown command");
            }

            if (v2Failed)
            {
                var v1config = new EventPipeSessionConfiguration(circularBufferMB, EventPipeSerializationFormat.NetTrace, providers);
                var v1message = new IpcMessage(DiagnosticsServerCommandSet.EventPipe, (byte)EventPipeCommandId.CollectTracing, v1config.SerializeV1());
                EventStream = IpcClient.SendMessage(processId, v1message, out var responsev1);
                switch ((DiagnosticsServerCommandId)responsev1.Header.CommandId)
                {
                    case DiagnosticsServerCommandId.OK:
                        _sessionId = BitConverter.ToInt64(responsev1.Payload, 0);
                        return;
                    case DiagnosticsServerCommandId.Error:
                        var hr = BitConverter.ToInt32(responsev1.Payload, 0);
                        throw new ServerErrorException($"EventPipe session start failed (HRESULT: 0x{hr:X8})");
                    default:
                        throw new ServerErrorException($"EventPipe session start failed - Server responded with unknown command");
                }
            }
        }

        public Stream EventStream { get; }

        ///<summary>
        /// Stops the given session
        ///</summary>
        public void Stop()
        {
            Debug.Assert(_sessionId != 0);

            byte[] payload = BitConverter.GetBytes(_sessionId);
            var response = IpcClient.SendMessage(_processId, new IpcMessage(DiagnosticsServerCommandSet.EventPipe, (byte)EventPipeCommandId.StopTracing, payload));

            switch ((DiagnosticsServerCommandId)response.Header.CommandId)
            {
                case DiagnosticsServerCommandId.OK:
                    return;
                case DiagnosticsServerCommandId.Error:
                    var hr = BitConverter.ToInt32(response.Payload, 0);
                    throw new ServerErrorException($"EventPipe session stop failed (HRESULT: 0x{hr:X8})");
                default:
                    throw new ServerErrorException($"EventPipe session stop failed - Server responded with unknown command");
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    EventStream?.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}