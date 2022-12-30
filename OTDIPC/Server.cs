/*
 * Copyright (c) 2022 Fred Emmott <fred@fredemmott.com>
 *
 * SPDX-License-Identifier: ISC
 */
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using System.IO.Pipes;
using System.Numerics;
using System.Runtime.InteropServices;

namespace OTDIPC
{
    [PluginName("OpenKneeboard (OTD-IPC)"), SupportedPlatformAttribute(PluginPlatform.Windows)]
    public class Server
    {
        Ping _Ping = new();
        NamedPipeServerStream? _server;
        Timer? _timer;
        bool _waitingForConnection;
        bool _connected;

        public Server() {
            _timer = new ((_) => { this.Ping(); }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        public void SendMessage<T>(T message) where T : struct
        {
            if (_server == null)
            {
                RunServerAsync();
                return;
            }

            IntPtr ptr = IntPtr.Zero;
            try
            {
                var size = Marshal.SizeOf(typeof(T));
                byte[] bytes = new byte[size];

                ptr = Marshal.AllocCoTaskMem(size);
                Marshal.StructureToPtr(message, ptr, false);
                Marshal.Copy(ptr, bytes, 0, size);
                WriteAsync(bytes).GetAwaiter().GetResult();
            }
            catch (TimeoutException)
            {
                OnFailedWrite();
            }
            catch (IOException)
            {
                OnFailedWrite();
            }
            catch (ObjectDisposedException) {
                // If we think the client's hung, we can close the connection
                // while a write is in progress; this is especially common
                // for ping writes.
                OnFailedWrite();
            }
            finally
            {
                Marshal.FreeCoTaskMem(ptr);
            }
        }

        async Task WriteAsync(byte[] bytes) {
            if (_server == null) {
                return;
            }
            await _server.WriteAsync(bytes).AsTask().WaitAsync(TimeSpan.FromMilliseconds(100));
        }

        void OnFailedWrite()
        {
            if (!_connected)
            {
                return;
            }
            System.Diagnostics.Debug.WriteLine("Error writing to named pipe, resetting server");
            _connected = false;
            RunServerAsync();
        }

        public bool HaveClient { get => _connected; }

        public event Action? ClientConnected;
        async Task RunServerAsync()
        {
            if (_waitingForConnection)
            {
                return;
            }

            _waitingForConnection = true;

            _server?.Close();
            _server = null;

            System.Diagnostics.Debug.WriteLine("Starting named pipe server");
            // Only the most recent connection will receive data, but allow multiple so that we can instantly
            // reset in case of a hung client.
            //
            // If we have a hung client, we can't free the pipe until the other end has unwedged itself.
            var server = new NamedPipeServerStream("com.fredemmott.openkneeboard.OTDIPC/v0.1", PipeDirection.Out, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Message);
            System.Diagnostics.Debug.WriteLine("Waiting for connection");
            await server.WaitForConnectionAsync();
            _server = server;
            _waitingForConnection = false;
            _connected = true;

            ClientConnected?.Invoke();
        }

        void Ping() {
            if (_waitingForConnection)
            {
                return;
            }
            _Ping.SequenceNumber++;
            SendMessage(_Ping);
        }


    }

}
