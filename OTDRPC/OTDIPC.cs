using OpenTabletDriver;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OTDIPC;
using System.Numerics;

namespace OTDRPC
{
    [PluginName("OpenKneeboard (OTDIPC)")]
    public class OTDIPC : IOutputMode
    {
        State _state = new();
        DeviceInfo _deviceInfo = new();

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

            if (!dirty)
            {
                return;
            }
            // TODO: send message
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
                // TODO: update VID and PID in message structs
                // TODO: send DeviceInfo message
            }
        }


        public Matrix3x2 TransformationMatrix => Matrix3x2.Identity;
    }
}