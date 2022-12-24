using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using System.IO.Pipes;
using System.Numerics;
using System.Runtime.InteropServices;

namespace OTDIPC
{
    [PluginName("OpenKneeboard (OTDIPC)")]
    public class OTDIPC : IOutputMode
    {
        State _state = new();
        DeviceInfo _deviceInfo = new();
        NamedPipeServerStream? _server;
        Task? _serverTask;
        BinaryWriter? _writer;

        public void Consume(IDeviceReport deviceReport)
        {
            bool dirty = false;
            if (deviceReport is ITabletReport tabletReport) {
                dirty = true;
                _state.X = tabletReport.Position.X;
                _state.Y = tabletReport.Position.Y;
                _state.Pressure = tabletReport.Pressure;
                var buttons = tabletReport.PenButtons;
                for (int i = 0; i < buttons.Length; i++)
                {
                    if (buttons[i])
                    {
                        _state.PenButtons |= (UInt32) (1 << i);
                    }
                    else
                    {
                        _state.PenButtons &= (UInt32) ~(1 << i);
                    }
                }
            }

            if (deviceReport is IProximityReport proximityReport)
            {
                dirty = true;
                _state.NearPromixity = proximityReport.NearProximity;
                _state.HoverDistance = proximityReport.HoverDistance;
            }

            if (deviceReport is IAuxReport auxReport)
            {
                dirty = true;
                var buttons = auxReport.AuxButtons;
                for (int i = 0; i < buttons.Length; ++i)
                {
                    if (buttons[i])
                    {
                        _state.AuxButtons |= (UInt32) (1 << i);
                    }
                    else
                    {
                        _state.AuxButtons &= (UInt32) ~(1 << i);
                    }
                }
            }

            if (dirty)
            {
                SendMessage(_state);
            }
        }

        public void Read(IDeviceReport deviceReport)
        {
            Consume(deviceReport);
        }

        public event Action<IDeviceReport>? Emit;

        public IList<IPositionedPipelineElement<IDeviceReport>> Elements { get; set; }


        TabletReference _tablet;
        public TabletReference Tablet
        {
            get => _tablet;
            set
            {
                _tablet = value;
                _deviceInfo.Name = _tablet.Properties.Name;
                var specs = _tablet.Properties.Specifications.Digitizer;
                _deviceInfo.MaxX = specs.MaxX;
                _deviceInfo.MaxY = specs.MaxY;
                _deviceInfo.MaxPressure = _tablet.Properties.Specifications.Pen.MaxPressure;
                _deviceInfo.IsValid = true;
                _state = new();
                // TODO: update VID and PID in message structs
                SendMessage(_deviceInfo);
            }
        }

        public Matrix3x2 TransformationMatrix => Matrix3x2.Identity;

        void SendMessage<T>(T message) where T : struct
        {
            StartServer();
            if (_writer is null)
            {
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
                System.Diagnostics.Debug.WriteLine("Error writing to named pipe, restarting server");
                RestartServer();
            }
            finally
            {
                Marshal.FreeCoTaskMem(ptr);
            }

        }
        void ShutdownServer()
        {
            _writer = null;
            _server?.Close();
            _server = null;
            _serverTask = null;

        }
        void RestartServer()
        {
            ShutdownServer();
            StartServer();
        }

        void StartServer()
        {
            if (_serverTask is not null)
            {
                System.Diagnostics.Debug.WriteLine("Already have named pipe server");
                return;
            }
            System.Diagnostics.Debug.WriteLine("Starting server task");
            _serverTask = RunServerAsync();
        }

        async Task RunServerAsync() {
            System.Diagnostics.Debug.WriteLine("Initializing named pipe server");
            _server = new NamedPipeServerStream("com.fredemmott.openkneeboard.OTDIPC/v0.1", PipeDirection.Out, 1, PipeTransmissionMode.Message);
            await _server.WaitForConnectionAsync();
            _writer = new BinaryWriter(_server);
            if (_deviceInfo.IsValid)
            {
                SendMessage(_deviceInfo);
                SendMessage(_state);
            }
        }
    }
}