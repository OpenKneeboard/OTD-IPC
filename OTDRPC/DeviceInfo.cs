using System.Runtime.InteropServices;

namespace OTDIPC
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct DeviceInfo
    {
        public DeviceInfo()
        {
        }

        public Header Header = new Header { MessageType = MessageType.DeviceInfo };
        public bool IsValid = false;
        public float MaxX = 0;
        public float MaxY = 0;
        public UInt32 MaxPressure = 0;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string Name = "";
    }
}
