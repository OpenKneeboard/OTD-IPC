/*
 * Copyright (c) 2022 Fred Emmott <fred@fredemmott.com>
 *
 * SPDX-License-Identifier: MIT
 */

using OpenTabletDriver.Plugin;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace OTDIPC.V1;

[SupportedOSPlatform("windows")]
public class Server : ServerBase
{
    private const string MyImplementationId = "otd-ipc.openkneeboard.com";
    private const string PipeName = "com.fredemmott.openkneeboard.OTDIPC/v0.1";

    Ping _ping = new();
    NamedPipeServerStream? _connection;

    private V1.DeviceInfo _deviceInfo = new()
    {
        IsValid = true,
    };

    public Server(IDriver driver) : base(driver)
    {
        if (driver.DeviceInfo.HasValue)
        {
            _deviceInfo = ConvertDeviceInfo(driver.DeviceInfo.Value);
        }
    }

    protected override void Driver_StateChanged(object? sender, V2.State v2State)
    {
        V1.State v1State = ConvertState(v2State);
        this.SendMessage(v1State);
    }

    protected override void Driver_TabletChanged(object? sender, V2.DeviceInfo v2Info)
    {
        _deviceInfo = ConvertDeviceInfo(v2Info);
        this.SendMessage(_deviceInfo);
    }

    protected override void WriteBytes(ReadOnlySpan<byte> bytes)
    {
        if (_connection == null || !_connection.IsConnected)
        {
            return;
        }

        // Write all bytes
        _connection.Write(bytes);
    }

    protected override void OnFailedWrite()
    {
        if (!_connected)
        {
            return;
        }

        System.Diagnostics.Debug.WriteLine("Error writing to named pipe, resetting server");
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

        Log.Write("otd-ipc", "Starting named pipe server at " + PipeName);

        var pipe = new NamedPipeServerStream(
            PipeName,
            PipeDirection.Out,
            1,
            PipeTransmissionMode.Message,
            PipeOptions.Asynchronous);
        _connection = pipe;

        Log.Write("otd-ipc", "Waiting for connection");
        await pipe.WaitForConnectionAsync();
        Log.Write("otd-ipc", "Client connected");
        _waitingForConnection = false;
        _connected = true;

        OnClientConnected();
    }

    void OnClientConnected()
    {
        System.Diagnostics.Debug.WriteLine("V1 client connected; sending device info");
        this.SendMessage(_deviceInfo);
    }

    protected override void Ping()
    {
        if (_waitingForConnection)
        {
            return;
        }

        _ping.SequenceNumber++;
        SendMessage(_ping);
    }

    private static V1.DeviceInfo ConvertDeviceInfo(V2.DeviceInfo v2)
    {
        var v1 = new V1.DeviceInfo
        {
            IsValid = true,
            MaxX = v2.MaxX,
            MaxY = v2.MaxY,
            MaxPressure = v2.MaxPressure,
            Name = v2.Name
        };

#pragma warning disable CS0618
        v1.Header.VID = v2.VendorId;
        v1.Header.PID = v2.ProductId;
#pragma warning restore CS0618

        return v1;
    }

    private static V1.State ConvertState(V2.State v2)
    {
        return new V1.State
        {
            Header = new V1.Header
            {
                MessageType = V1.MessageType.State,
                Size = (UInt32)Marshal.SizeOf(typeof(V1.State)),
#pragma warning disable CS0618
                VID = 0, // V1 doesn't track this per-state
                PID = 0
#pragma warning restore CS0618
            },
            PositionValid = v2.ValidBits.HasFlag(V2.State.ValidMask.Position),
            X = v2.X,
            Y = v2.Y,
            PressureValid = v2.ValidBits.HasFlag(V2.State.ValidMask.Pressure),
            Pressure = v2.Pressure,
            PenButtonsValid = v2.ValidBits.HasFlag(V2.State.ValidMask.PenButtons),
            PenButtons = v2.PenButtons,
            AuxButtonsValid = v2.ValidBits.HasFlag(V2.State.ValidMask.AuxButtons),
            AuxButtons = v2.AuxButtons,
            ProximityValid = v2.ValidBits.HasFlag(V2.State.ValidMask.PenIsNearSurface),
            HoverDistance = v2.HoverDistance,
            NearProximity = v2.PenIsNearSurface
        };
    }
}