using OpenTabletDriver.Plugin.Tablet;
using OTDIPC.V2;

namespace OTDIPC;

public interface IDriver
{
    DeviceInfo? DeviceInfo { get; }
    TabletReference? Tablet { get; }

    event EventHandler<DeviceInfo> TabletChanged;
    event EventHandler<State> StateChanged;
}