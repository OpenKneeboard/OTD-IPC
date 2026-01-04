/*
 * Copyright (c) 2022 Fred Emmott <fred@fredemmott.com>
 *
 * SPDX-License-Identifier: MIT
 */

using System.Reflection;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;

namespace OTDIPC
{
    [PluginName("OpenKneeboard (OTD-IPC)")]
    public class OTDIPC : IPositionedPipelineElement<IDeviceReport>
    {
        V2.State _state = new();
        V2.DeviceInfo _deviceInfo = new();
        private readonly string _implementationIdDebugMessage = GenerateImplementationIdDebugMessage();
        private static UInt32 _nextNonPersistentTabletId = 1;

        static Server _server = new();
        Action? _clientConnectedHandler;

        public OTDIPC()
        {
            WeakReference<OTDIPC> weakThis = new(this);
            _clientConnectedHandler = () =>
            {
                OTDIPC? self;
                if (weakThis.TryGetTarget(out self))
                {
                    self?.OnClientConnected();
                }
            };
            _server.ClientConnected += _clientConnectedHandler;
        }

        ~OTDIPC()
        {
            _server.ClientConnected -= _clientConnectedHandler;
        }

        void OnClientConnected()
        {
            System.Diagnostics.Debug.WriteLine("Sending hello");
            _server.SendDebugMessage(_implementationIdDebugMessage);
            System.Diagnostics.Debug.WriteLine("Sending device info");
            _server.SendMessage(_deviceInfo);
        }

        public void Consume(IDeviceReport deviceReport)
        {
            if (!_server.HaveClient)
            {
                Emit?.Invoke(deviceReport);
                return;
            }

            bool changed = false;

            if (deviceReport is IAbsolutePositionReport absolutePositionReport)
            {
                changed = true;
                _state.X = absolutePositionReport.Position.X;
                _state.Y = absolutePositionReport.Position.Y;
                _state.PositionValid = true;
            }

            if (deviceReport is ITabletReport tabletReport)
            {
                changed = true;
                _state.Pressure = tabletReport.Pressure;
                _state.PressureValid = true;
                var buttons = tabletReport.PenButtons;
                for (int i = 0; i < buttons.Length; i++)
                {
                    if (buttons[i])
                    {
                        _state.PenButtons |= (UInt32)(1 << i);
                    }
                    else
                    {
                        _state.PenButtons &= (UInt32)~(1 << i);
                    }
                }

                _state.PenButtonsValid = true;
            }

            if (deviceReport is IProximityReport proximityReport)
            {
                changed = true;
                _state.NearPromixity = proximityReport.NearProximity;
                _state.HoverDistance = proximityReport.HoverDistance;
                _state.ProximityValid = true;
            }

            if (deviceReport is IAuxReport auxReport)
            {
                changed = true;
                var buttons = auxReport.AuxButtons;
                for (int i = 0; i < buttons.Length; ++i)
                {
                    if (buttons[i])
                    {
                        _state.AuxButtons |= (UInt32)(1 << i);
                    }
                    else
                    {
                        _state.AuxButtons &= (UInt32)~(1 << i);
                    }
                }

                _state.AuxButtonsValid = true;
            }

            if (!_deviceInfo.IsValid)
            {
                return;
            }

            if (!changed)
            {
                return;
            }

            _server.SendMessage(_state);
        }

        public event Action<IDeviceReport>? Emit;

        public PipelinePosition Position
        {
            get => PipelinePosition.Raw;
        }

        TabletReference? _tablet;

        [TabletReference]
        public TabletReference Tablet
        {
            get => _tablet;
            set
            {
                _tablet = value;
                _deviceInfo = new();
                _deviceInfo.Name = _tablet.Properties.Name;
                var specs = _tablet.Properties.Specifications.Digitizer;
                _deviceInfo.MaxX = specs.MaxX;
                _deviceInfo.MaxY = specs.MaxY;
                _deviceInfo.MaxPressure = _tablet.Properties.Specifications.Pen.MaxPressure;
                _deviceInfo.IsValid = true;

                var id = _tablet.Identifiers.First();
                if (id != null)
                {
                    _deviceInfo.PersistentId =
                        $"otd-ipc.openkneeboard.com/vid-pid/{_deviceInfo.VendorId:X4}-{_deviceInfo.ProductId:X4}";
                    // Marked obsolete, but we still want to populate them for existing clients
#pragma warning disable CS0618
                    _deviceInfo.VendorId = (UInt16)id.VendorID;
                    _deviceInfo.ProductId = (UInt16)id.ProductID;
#pragma warning restore CS0618
                }

                _deviceInfo.Header.NonPersistentTabletId = _nextNonPersistentTabletId++;

                _state = new();
                _state.Header.NonPersistentTabletId = _deviceInfo.Header.NonPersistentTabletId;

                _server.SendMessage(_deviceInfo);
            }
        }

        private static string GenerateImplementationIdDebugMessage()
        {
            var self = Assembly.GetExecutingAssembly().GetName();
            var otd = Assembly.GetEntryAssembly()?.GetName();
            return $"OTD-IPC: `{self.Name}` v{self.Version} running on `{otd?.Name}` v{otd?.Version}";
        }
    }
}