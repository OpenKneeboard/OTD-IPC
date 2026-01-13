/*
 * Copyright (c) 2022 Fred Emmott <fred@fredemmott.com>
 *
 * SPDX-License-Identifier: MIT
 */

using OpenTabletDriver.Plugin;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace OTDIPC;

public abstract class ServerBase : IServer
{
    protected readonly IDriver _driver;
    protected Timer? _timer;
    protected bool _waitingForConnection;
    protected bool _connected;

    public bool HaveClient => _connected;

    protected ServerBase(IDriver driver)
    {
        _driver = driver;
        _timer = new((_) => { this.Ping(); }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

        driver.TabletChanged += Driver_TabletChanged;
        driver.StateChanged += Driver_StateChanged;
    }

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
        _timer?.Dispose();
        _driver.TabletChanged -= Driver_TabletChanged;
        _driver.StateChanged -= Driver_StateChanged;
    }

    protected void SendMessage<T>(T message)
    {
        if (!HaveClient)
        {
            RunServerAsync();
            return;
        }

        try
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                // Slow path for types with managed references (e.g., V1.DeviceInfo with string)
                IntPtr ptr = IntPtr.Zero;
                try
                {
                    int size = Marshal.SizeOf<T>();
                    ptr = Marshal.AllocCoTaskMem(size);
                    Marshal.StructureToPtr(message, ptr, false);
                    byte[] bytes = new byte[size];
                    Marshal.Copy(ptr, bytes, 0, size);
                    WriteBytes(bytes);
                }
                finally
                {
                    if (ptr != IntPtr.Zero)
                    {
                        Marshal.FreeCoTaskMem(ptr);
                    }
                }
            }
            else
            {
                // Fast path with stackalloc for unmanaged types
                // We know T is unmanaged because IsReferenceOrContainsReferences returned false
                int size = Unsafe.SizeOf<T>();
                Span<byte> bytes = stackalloc byte[size];
                unsafe
                {
                    fixed (byte* ptr = bytes)
                    {
                        Unsafe.Copy(ptr, ref message);
                    }
                }

                WriteBytes(bytes);
            }
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
    }

    protected abstract void WriteBytes(ReadOnlySpan<byte> bytes);
    protected abstract void OnFailedWrite();
    protected abstract void RunServerAsync();

    protected abstract void Ping();
    protected abstract void Driver_TabletChanged(object? sender, V2.DeviceInfo info);
    protected abstract void Driver_StateChanged(object? sender, V2.State state);
}