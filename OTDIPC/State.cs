/*
 * Copyright (c) 2022 Fred Emmott <fred@fredemmott.com>
 *
 * Permission to use, copy, modify, and/or distribute this software for any
 * purpose with or without fee is hereby granted, provided that the above
 * copyright notice and this permission notice appear in all copies.
 *
 * THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
 * WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
 * MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
 * ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
 * WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
 * ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
 * OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
 */
using System.Runtime.InteropServices;

namespace OTDIPC
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
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
