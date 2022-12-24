using System.Runtime.InteropServices;

namespace OTDIPC
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
    struct DeviceInfo
    {
        public DeviceInfo()
        {
        }

        public Header Header = new() { MessageType = MessageType.DeviceInfo };

        public bool IsValid = false;
        public float MaxX = 0;
        public float MaxY = 0;
        public UInt32 MaxPressure = 0;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string Name = "";
    }
}
