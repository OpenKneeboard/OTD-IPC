using System.Runtime.InteropServices;

namespace OTDIPC
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct Header {
        public MessageType MessageType;
        public UInt32 Size;
        public UInt16 VID;
        public UInt16 PID;
    }
}
