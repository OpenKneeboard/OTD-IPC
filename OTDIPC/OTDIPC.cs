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
    public class OTDIPC : IOutputMode
    {
        State _state = new();
        DeviceInfo _deviceInfo = new();

        static Server _server = new();

        public OTDIPC() {
            _server.ClearEventSubscribers();
            _server.ClientConnected += () => { this.OnClientConnected(); };
        }

        void OnClientConnected() {
            if (!_deviceInfo.IsValid) {
                return;
            }
            _server.SendMessage(_deviceInfo);
            _server.SendMessage(_state);
        }

        public void Consume(IDeviceReport deviceReport)
        {
            bool dirty = false;
            if (deviceReport is IAbsolutePositionReport absolutePositionReport)
            {
                dirty = true;
                _state.X = absolutePositionReport.Position.X;
                _state.Y = absolutePositionReport.Position.Y;
                _state.PositionValid = true;
            }
            if (deviceReport is ITabletReport tabletReport) {
                dirty = true;
                _state.Pressure = tabletReport.Pressure;
                _state.PressureValid = true;
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
                _state.PenButtonsValid = true;
            }

            if (deviceReport is IProximityReport proximityReport)
            {
                dirty = true;
                _state.NearPromixity = proximityReport.NearProximity;
                _state.HoverDistance = proximityReport.HoverDistance;
                _state.ProximityValid = true;
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
                _state.AuxButtonsValid = true;
            }

            if (dirty)
            {
                _server.SendMessage(_state);
            }
        }

        public void Read(IDeviceReport deviceReport)
        {
            Consume(deviceReport);
        }

        public event Action<IDeviceReport>? Emit;

        public IList<IPositionedPipelineElement<IDeviceReport>>? Elements { get; set; }


        TabletReference? _tablet;
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
                if (id != null) {
                    _deviceInfo.Header.VID = (UInt16) id.VendorID;
                    _deviceInfo.Header.PID = (UInt16) id.ProductID;
                }

                _state = new();
                _state.Header.VID = _deviceInfo.Header.VID;
                _state.Header.PID = _deviceInfo.Header.PID;

                _server.SendMessage(_deviceInfo);
                _server.SendMessage(_state);
            }
        }

        public Matrix3x2 TransformationMatrix => Matrix3x2.Identity;
    }

}
