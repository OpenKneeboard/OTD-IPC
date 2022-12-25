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
        BinaryWriter? _writer;
        Timer? _timer;
        bool _waitingForConnection;
        bool _connected;

        public Server() {
            _timer = new ((_) => { this.Ping(); }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        public void SendMessage<T>(T message) where T : struct
        {
            if (_writer is null)
            {
                StartServer();
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
                _writer.Write(bytes);
                _writer.Flush();
            }
            catch (IOException)
            {
                System.Diagnostics.Debug.WriteLine("Error writing to named pipe, resetting server");
                _connected = false;
                StartServer();
            }
            finally
            {
                Marshal.FreeCoTaskMem(ptr);
            }

        }

        public bool HaveClient { get => _connected; }

        public event Action? ClientConnected;
        async Task StartServer()
        {
            if (_waitingForConnection)
            {
                return;
            }

            _waitingForConnection = true;

            _writer?.Close();
            _writer = null;

            _server?.Close();
            System.Diagnostics.Debug.WriteLine("Starting named pipe server");
            _server = new NamedPipeServerStream("com.fredemmott.openkneeboard.OTDIPC/v0.1", PipeDirection.Out, 1, PipeTransmissionMode.Message);
            System.Diagnostics.Debug.WriteLine("Waiting for connection");
            await _server.WaitForConnectionAsync();
            _waitingForConnection = false;
            _connected = true;

            _writer = new BinaryWriter(_server);
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
