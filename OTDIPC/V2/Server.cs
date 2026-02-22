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

namespace OTDIPC.V2;

public class Server : ServerBase
{
    private const string MyImplementationId = "otd-ipc.openkneeboard.com";
    private const UInt64 ProtocolVersion = 0x02_20260205_01;

    Ping _ping = new();
    Socket? _connection;
    Socket? _listener;
    DeviceInfo? _deviceInfo;
    private readonly string _socketPath = GetSocketPath();

    public Server(IDriver driver) : base(driver)
    {
        _deviceInfo = driver.DeviceInfo;
    }

    protected override void Driver_StateChanged(object? sender, State state)
    {
        System.Diagnostics.Debug.Assert(!RuntimeHelpers.IsReferenceOrContainsReferences<State>(),
            "V2.State must be unmanaged");
        this.SendMessage(state);
    }

    protected override void Driver_TabletChanged(object? sender, DeviceInfo info)
    {
        System.Diagnostics.Debug.Assert(!RuntimeHelpers.IsReferenceOrContainsReferences<DeviceInfo>(),
            "V2.DeviceInfo must be unmanaged");
        _deviceInfo = info;
        this.SendMessage(info);
    }

    void SendDebugMessage(string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        V2.Header header = new()
        {
            MessageType = V2.MessageType.DebugMessage,
            Size = (UInt32)(Marshal.SizeOf<V2.Header>() + bytes.Length),
        };
        System.Diagnostics.Debug.Assert(!RuntimeHelpers.IsReferenceOrContainsReferences<Header>(),
            "V2.Header must be unmanaged");
        SendMessage(header);
        WriteBytes(bytes);
    }

    public override void Dispose()
    {
        Log.Write("otd-ipc-v2", "disposing server");
        Interlocked.Exchange(ref _listener, null)?.Dispose();
        Interlocked.Exchange(ref _connection, null)?.Dispose();
        base.Dispose();
        Log.Write("otd-ipc-v2", "server disposed");
    }

    protected override void WriteBytes(ReadOnlySpan<byte> bytes)
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

    protected override void OnFailedWrite()
    {
        if (!_connected)
        {
            return;
        }

        Log.Write("otd-ipc-v2", "Error writing to unix domain socket, resetting server");
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

    protected override async void RunServerAsync()
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
            Log.Write("otd-ipc-v2", "Starting unix domain socket server at " + _socketPath);
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

        Log.Write("otd-ipc-v2", "Waiting for connection");
        try
        {
            _connection = await _listener.AcceptAsync();
        }
        catch (SocketException e)
        {
            Log.Write("otd-ipc-v2", "server failed to accept connection: " + e.Message);
            _connection = null;
            return;
        }

        Log.Write("otd-ipc-v2", "Client connected");
        _waitingForConnection = false;
        _connected = true;

        OnClientConnected();
        await HandleClientMessagesAsync();
    }

    private async Task<byte[]?> ReadBytes(int count)
    {
        var buffer = new byte[count];
        var totalRead = 0;
        while (totalRead < count)
        {
            if (_connection == null)
            {
                return null;
            }

            var read = await _connection.ReceiveAsync(
                new Memory<byte>(buffer, totalRead, count - totalRead),
                SocketFlags.None);

            if (read <= 0) return null; // Connection closed
            totalRead += read;
        }

        return buffer;
    }

    private async Task HandleClientMessagesAsync()
    {
        var headerSize = Marshal.SizeOf<V2.Header>();

        try
        {
            while (_connected && _connection != null)
            {
                var headerBuffer = await ReadBytes(headerSize);
                if (headerBuffer == null)
                {
                    break;
                }

                var header = MemoryMarshal.Read<V2.Header>(headerBuffer);

                int payloadSize = (int)header.Size - headerSize;
                byte[]? payload = null;
                if (payloadSize > 0)
                {
                    payload = await ReadBytes(payloadSize);
                }

                OnClientMessage(header, payload);
            }
        }
        catch (Exception e)
        {
            Log.Write("otd-ipc-v2", $"Connection lost while receiving: {e.Message}");
        }
    }

    void OnClientMessage(V2.Header header, byte[]? payload)
    {
        if (header.MessageType == V2.MessageType.DebugMessage && payload != null)
        {
            var message = Encoding.UTF8.GetString(payload);
            Log.Write("otd-ipc-v2", $"Client debug message: {message}");
            return;
        }

        if (header.MessageType == V2.MessageType.Hello && payload != null)
        {
            var fullMessage = new byte[header.Size];
            MemoryMarshal.Write(fullMessage, ref header);
            payload.CopyTo(fullMessage, Marshal.SizeOf<V2.Header>());
            var msg = MemoryMarshal.AsRef<V2.Hello>(fullMessage);
            Log.Write("otd-ipc-v2",
                $"Client hello: {msg.HumanReadableName} {msg.HumanReadableVersion} (proto 0x{msg.ProtocolVersion:x}, ID '{msg.ImplementationId}'/ cv {msg.CompatibilityVersion})");
            return;
        }


        Log.Write("otd-ipc-v2", $"Client sent unexpected message type: {(uint)header.MessageType}");
    }

    void OnClientConnected()
    {
        Log.Write("otd-ipc-v2", "Sending hello");
        var self = Assembly.GetExecutingAssembly().GetName();
        var otd = Assembly.GetEntryAssembly()?.GetName();
        var hello = new Hello
        {
            ProtocolVersion = ProtocolVersion,
            ImplementationId = MyImplementationId,
            HumanReadableName = $"`{self.Name}/{otd?.Name}`",
            HumanReadableVersion = $"`v{self.Version}/OTDv{otd?.Version}`",
            CompatibilityVersion = 1,
        };
        SendMessage(hello);
        if (!_deviceInfo.HasValue)
        {
            Log.Write("otd-ipc-v2", "Device not seen - not sending device info");
            return;
        }

        Log.Write("otd-ipc-v2", "Sending device info");
        this.SendMessage(_deviceInfo.Value);
    }

    protected override void Ping()
    {
        if (_waitingForConnection)
        {
            return;
        }

        _ping.SequenceNumber++;
        System.Diagnostics.Debug.Assert(!RuntimeHelpers.IsReferenceOrContainsReferences<Ping>(),
            "V2.Ping must be unmanaged");
        SendMessage(_ping);
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
            Log.Write("otd-ipc-v2", $"failed to create discovery directory: {e.Message}", LogLevel.Error);
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
SOCKET={GetSocketPath()}
HUMAN_READABLE_NAME=OpenTabletDriver OTD-IPC Plugin
HUMAN_READABLE_VERSION=v{version}/{entry?.Name} v{entry?.Version}
HOMEPAGE=https://github.com/OpenKneeboard/OTD-IPC
COMPATIBILITY_VERSION=1
".TrimStart();
            File.WriteAllText(metadataFile, contents);
        }
        catch (Exception e)
        {
            Log.Write("otd-ipc-v2", $"failed to write discovery metadata file: {e.Message}", LogLevel.Error);
            return;
        }

        Log.Write("otd-ipc-v2", $"published discovery metadata file: {metadataFile}");

        var defaultPath = Path.Join(root, "default.txt");
        if (File.Exists(defaultPath))
        {
            Log.Write("otd-ipc-v2", $"discovery defaults file already exists: {defaultPath}");
            return;
        }

        try
        {
            File.WriteAllText(defaultPath, MyImplementationId);
        }
        catch (Exception e)
        {
            Log.Write("otd-ipc-v2", $"failed to write discovery defaults file: {e.Message}", LogLevel.Error);
        }

        Log.Write("otd-ipc-v2", $"published discovery defaults file: {defaultPath}");
    }
}
