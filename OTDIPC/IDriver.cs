using OTDIPC.V2;

namespace OTDIPC;

public interface IDriver
{
    DeviceInfo? DeviceInfo { get; }

    event EventHandler<DeviceInfo> TabletChanged;
    event EventHandler<State> StateChanged;
}