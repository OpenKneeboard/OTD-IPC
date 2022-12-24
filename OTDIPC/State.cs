using System.Runtime.InteropServices;

namespace OTDIPC
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct State
    {
        public State()
        {
        }

        public Header Header = new() {  MessageType = MessageType.State };

        public float X = 0;
        public float Y = 0;
        public UInt32 Pressure = 0;
        public UInt32 PenButtons = 0;
        public UInt32 AuxButtons = 0;
        public UInt32 HoverDistance = 0;
        public bool NearPromixity = false;
    }
}
