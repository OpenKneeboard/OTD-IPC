/*
 * Copyright (c) 2022 Fred Emmott <fred@fredemmott.com>
 *
 * SPDX-License-Identifier: MIT
 */

using System.Runtime.InteropServices;

namespace OTDIPC.V2
{
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct State
    {
        public State()
        {
        }

        public Header Header = new()
        {
            MessageType = MessageType.State,
            Size = (UInt32)Marshal.SizeOf(typeof(State)),
        };

        [Flags]
        public enum ValidMask : UInt32
        {
            None = 0,

            PositionX = 1 << 0,
            PositionY = 1 << 1,
            Pressure = 1 << 2,
            PenButtons = 1 << 3,
            AuxButtons = 1 << 4,
            PenIsNearSurface = 1 << 5,
            HoverDistance = 1 << 6,

            Position = PositionX | PositionY,
        }

        public ValidMask ValidBits = ValidMask.None;

        public float X = 0;
        public float Y = 0;

        public UInt32 Pressure = 0;
        public UInt32 PenButtons = 0;
        public UInt32 AuxButtons = 0;
        public UInt32 HoverDistance = 0;
        public bool PenIsNearSurface = false;
    }
}