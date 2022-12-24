using OpenTabletDriver;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using System.Numerics;

namespace OTDRPC
{
    [PluginName("OpenKneeboard (IPC)")]
    public class OTDRPC : IOutputMode
    {
        public OTDRPC() {
            System.Diagnostics.Debug.WriteLine("INIT");
        }

        public void Consume(IDeviceReport deviceReport)
        {
            System.Diagnostics.Debug.WriteLine("Type: {}", deviceReport.GetType().FullName);
            if (deviceReport is ITabletReport tabletReport) {
                System.Diagnostics.Debug.WriteLine(
                    "Pos: ({0}, {1})", tabletReport.Position.X, tabletReport.Position.Y);
            }
        }

        public void Read(IDeviceReport deviceReport)
        {
            Consume(deviceReport);
        }

        public event Action<IDeviceReport>? Emit;

        public IList<IPositionedPipelineElement<IDeviceReport>> Elements { get; set; }

        public TabletReference Tablet { set; get; }

        public Matrix3x2 TransformationMatrix => Matrix3x2.Identity;
    }
}