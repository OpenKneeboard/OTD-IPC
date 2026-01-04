/*
 * Copyright (c) 2022 Fred Emmott <fred@fredemmott.com>
 *
 * SPDX-License-Identifier: MIT
 */

using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace OTDIPC.V2
{
    public class Server
    {
        private const string MyImplementationId = "otd-ipc.openkneeboard.com";

        V2.Ping _Ping = new();
        Socket? _connection;
        Socket? _listener;
        Timer? _timer;
        bool _waitingForConnection;
        bool _connected;
        private readonly string _socketPath = GetSocketPath();

        public Server()
        {
            _timer = new((_) => { this.Ping(); }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        public void SendDebugMessage(string message)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            V2.Header header = new()
            {
                MessageType = V2.MessageType.DebugMessage,
                Size = (UInt32)(Marshal.SizeOf<V2.Header>() + bytes.Length),
            };
            SendMessage(header);
            WriteBytes(bytes);
        }

        public void SendMessage<T>(T message) where T : unmanaged
        {
            if (_connection == null)
            {
                RunServerAsync();
                return;
            }

            IntPtr ptr = IntPtr.Zero;
            try
            {
                Span<byte> bytes = stackalloc byte[Unsafe.SizeOf<T>()];
                MemoryMarshal.Write(bytes, ref message);
                WriteBytes(bytes);
            }
            catch (TimeoutException)
            {
                OnFailedWrite();
            }
            catch (IOException)
            {
                OnFailedWrite();
            }
            catch (SocketException)
            {
                OnFailedWrite();
            }
            catch (ObjectDisposedException)
            {
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

        void WriteBytes(ReadOnlySpan<byte> bytes)
        {
            if (_connection == null)
            {
                return;
            }

            // Send may not send all bytes in one call; loop until done
            int totalSent = 0;
            while (totalSent < bytes.Length)
            {
                int sent = _connection.Send(bytes[totalSent..]);
                if (sent <= 0)
                {
                    throw new SocketException();
                }

                totalSent += sent;
            }
        }

        void OnFailedWrite()
        {
            if (!_connected)
            {
                return;
            }

            System.Diagnostics.Debug.WriteLine("Error writing to unix domain socket, resetting server");
            _connected = false;
            try
            {
                _connection?.Dispose();
            }
            catch
            {
            }

            _connection = null;
            RunServerAsync();
        }

        public bool HaveClient
        {
            get => _connected;
        }

        public event Action? ClientConnected;

        async Task RunServerAsync()
        {
            if (_waitingForConnection)
            {
                return;
            }

            _waitingForConnection = true;

            try
            {
                _connection?.Dispose();
            }
            catch
            {
            }

            _connection = null;

            if (_listener == null)
            {
                Log.Write("otd-ipc", "Starting unix domain socket server at " + _socketPath);
                try
                {
                    // On Unix, ensure any previous socket file is removed
                    if (System.IO.File.Exists(_socketPath))
                    {
                        System.IO.File.Delete(_socketPath);
                    }

                    var directory = Path.GetDirectoryName(_socketPath)!;
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                }
                catch
                {
                }

                var listener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                listener.Bind(new UnixDomainSocketEndPoint(_socketPath));
                listener.Listen(1);
                _listener = listener;
                PublishDiscoveryData();
            }

            Log.Write("otd-ipc", "Waiting for connection");
            var client = await _listener.AcceptAsync();
            Log.Write("otd-ipc", "Client connected");
            _connection = client;
            _waitingForConnection = false;
            _connected = true;

            ClientConnected?.Invoke();
        }

        void Ping()
        {
            if (_waitingForConnection)
            {
                return;
            }

            _Ping.SequenceNumber++;
            SendMessage(_Ping);
        }

        static string GetSocketPath()
        {
            var prefix = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var path = Path.Join(prefix, "otd-ipc", "sock");
            return path;
        }

        static void PublishDiscoveryData()
        {
            var prefix = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var root = Path.Join(prefix, "otd-ipc", "servers", "v2");
            var metadataFile = Path.Join(root, "available", $"{MyImplementationId}.txt");

            try
            {
                var directory = Path.GetDirectoryName(metadataFile)!;
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
            catch (Exception e)
            {
                Log.Write("otd-ipc", $"failed to create discovery directory: {e.Message}", LogLevel.Error);
                return;
            }

            try
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                var semver = version != null
                    ? $"{version.Major}.{version.Minor}.{version.Build}+revision.{version.Revision}"
                    : "0.0.0+revision.0";

                var entry = Assembly.GetEntryAssembly()?.GetName();
                var contents = $@"
ID={MyImplementationId}
NAME=OpenTabletDriver OTD-IPC Plugin
SEMVER={semver}
DEBUG_VERSION=OTD-IPC v{version}/{entry?.Name} v{entry?.Version}
HOMEPAGE=https://github.com/OpenKneeboard/OTD-IPC
SOCKET={GetSocketPath()}
".TrimStart();
                File.WriteAllText(metadataFile, contents);
            }
            catch (Exception e)
            {
                Log.Write("otd-ipc", $"failed to write discovery metadata file: {e.Message}", LogLevel.Error);
                return;
            }

            Log.Write("otd-ipc", $"published discovery metadata file: {metadataFile}");

            var defaultPath = Path.Join(root, "default.txt");
            if (File.Exists(defaultPath))
            {
                Log.Debug("otd-ipc", $"discovery defaults file already exists: {defaultPath}");
                return;
            }

            try
            {
                File.WriteAllText(defaultPath, MyImplementationId);
            }
            catch (Exception e)
            {
                Log.Write("otd-ipc", $"failed to write discovery defaults file: {e.Message}", LogLevel.Error);
            }

            Log.Write("otd-ipc", $"published discovery defaults file: {defaultPath}");
        }
    }
}