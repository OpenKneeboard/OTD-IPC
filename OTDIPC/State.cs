/*
 * Copyright (c) 2022 Fred Emmott <fred@fredemmott.com>
 *
 * SPDX-License-Identifier: ISC
 */
using System.Runtime.InteropServices;

namespace OTDIPC
{
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    struct State
    {
        public State()
        {
        }

        public Header Header = new()
        {
            MessageType = MessageType.State,
            Size = (UInt32)Marshal.SizeOf(typeof(State)),
        };

        public bool PositionValid = false;
        public float X = 0;
        public float Y = 0;

        public bool PressureValid = false;
        public UInt32 Pressure = 0;

        public bool PenButtonsValid = false;
        public UInt32 PenButtons = 0;

        public bool AuxButtonsValid = false;
        public UInt32 AuxButtons = 0;

        public bool ProximityValid = false;
        public UInt32 HoverDistance = 0;
        public bool NearPromixity = false;

    }
}
