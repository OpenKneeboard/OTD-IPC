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
            Position = 1 << 0,
            Pressure = 1 << 1,
            PenButtons = 1 << 2,
            AuxButtons = 1 << 3,
            Proximity = 1 << 4,
        }

        public ValidMask ValidBits = ValidMask.None;

        public float X = 0;
        public float Y = 0;

        public UInt32 Pressure = 0;
        public UInt32 PenButtons = 0;
        public UInt32 AuxButtons = 0;
        public UInt32 HoverDistance = 0;
        public bool NearPromixity = false;
    }
}