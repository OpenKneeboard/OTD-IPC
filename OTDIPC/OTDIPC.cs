/*
 * Copyright (c) 2022 Fred Emmott <fred@fredemmott.com>
 *
 * SPDX-License-Identifier: MIT
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
    public class OTDIPC : IPositionedPipelineElement<IDeviceReport>
    {
        State _state = new();
        DeviceInfo _deviceInfo = new();

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
        public PipelinePosition Position { get => PipelinePosition.Raw; }

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
                    _deviceInfo.Header.VID = (UInt16)id.VendorID;
                    _deviceInfo.Header.PID = (UInt16)id.ProductID;
                }

                _state = new();
                _state.Header.VID = _deviceInfo.Header.VID;
                _state.Header.PID = _deviceInfo.Header.PID;

                _server.SendMessage(_deviceInfo);
            }
        }
    }

}
