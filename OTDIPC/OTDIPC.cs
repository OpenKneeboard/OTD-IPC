/*
 * Copyright (c) 2022 Fred Emmott <fred@fredemmott.com>
 *
 * SPDX-License-Identifier: MIT
 */

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
            if (OperatingSystem.IsWindows())
            {
                _servers = new IServer[]
                {
                    new V1.Server(this),
                    new V2.Server(this),
                };
            }
            else
            {
                _servers = new IServer[]
                {
                    new V2.Server(this),
                };
            }
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
                _state.ValidBits |= State.ValidMask.Position;
            }

            if (deviceReport is ITabletReport tabletReport)
            {
                changed = true;
                _state.Pressure = tabletReport.Pressure;
                _state.ValidBits |= State.ValidMask.Pressure;

                _state.PenButtons = 0;
                var buttons = tabletReport.PenButtons;
                for (int i = 0; i < buttons.Length; i++)
                {
                    if (buttons[i])
                    {
                        _state.PenButtons |= (UInt32)(1 << i);
                    }
                }

                _state.PenButtons <<= 1;
                if (_state.Pressure > 0)
                {
                    _state.PenButtons |= 1;
                }

                _state.ValidBits |= State.ValidMask.PenButtons;
            }

            if (deviceReport is IProximityReport proximityReport)
            {
                changed = true;
                _state.PenIsNearSurface = proximityReport.NearProximity;
                _state.HoverDistance = proximityReport.HoverDistance;
                _state.ValidBits |= State.ValidMask.PenIsNearSurface | State.ValidMask.HoverDistance;
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

                _state.ValidBits |= State.ValidMask.AuxButtons;
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