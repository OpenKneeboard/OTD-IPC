/*
 * Copyright (c) 2022 Fred Emmott <fred@fredemmott.com>
 *
 * SPDX-License-Identifier: MIT
 */

using System.Reflection;
using System.Runtime.InteropServices;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OTDIPC.V2;

namespace OTDIPC
{
    [PluginName("OpenKneeboard (OTD-IPC)")]
    public class OTDIPC : IPositionedPipelineElement<IDeviceReport>, IDriver
    {
        State _state = new();
        private DeviceInfo? _deviceInfo;

        DeviceInfo? IDriver.DeviceInfo => _deviceInfo;

        public event EventHandler<DeviceInfo>? TabletChanged;
        public event EventHandler<State>? StateChanged;


        private static UInt32 _nextNonPersistentTabletId = 1;

        private readonly IServer[] _servers;

        public OTDIPC()
        {
            var servers = new List<IServer> { new V2.Server(this) };

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                servers.Add(new V1.Server(this));
            }

            _servers = servers.ToArray();
        }

        public void Consume(IDeviceReport deviceReport)
        {
            if (!_servers.Any(s => s.HaveClient))
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

            if (!changed)
            {
                return;
            }

            StateChanged?.Invoke(this, _state);
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
                var specs = _tablet.Properties.Specifications.Digitizer;
                var info = new DeviceInfo
                {
                    Name = _tablet.Properties.Name,
                    MaxX = specs.MaxX,
                    MaxY = specs.MaxY,
                    MaxPressure = _tablet.Properties.Specifications.Pen.MaxPressure,
                };

                var id = _tablet.Identifiers.First();
                if (id != null)
                {
                    var vendorId = (UInt16)id.VendorID;
                    var productId = (UInt16)id.ProductID;
                    info.PersistentId =
                        $"otd-ipc.openkneeboard.com/vid-pid/{vendorId:X4}-{productId:X4}";
                    // Marked obsolete, but we still want to populate them for existing clients
#pragma warning disable CS0618
                    info.VendorId = vendorId;
                    info.ProductId = productId;
#pragma warning restore CS0618
                }

                info.Header.NonPersistentTabletId = _nextNonPersistentTabletId++;

                _state = new();
                _state.Header.NonPersistentTabletId = info.Header.NonPersistentTabletId;
                _deviceInfo = info;

                TabletChanged?.Invoke(this, info);
            }
        }
    }
}