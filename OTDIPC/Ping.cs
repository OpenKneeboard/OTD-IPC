/*
 * Copyright (c) 2022 Fred Emmott <fred@fredemmott.com>
 *
 * SPDX-License-Identifier: MIT
 */
using System.Runtime.InteropServices;

namespace OTDIPC
{
    [StructLayout(LayoutKind.Sequential, Pack = 0, CharSet = CharSet.Unicode)]
    struct Ping
    {
        public Ping()
        {
        }

        public Header Header = new() {
            MessageType = MessageType.Ping,
            Size = (UInt32) Marshal.SizeOf(typeof(Ping)),
        };

        public UInt64 SequenceNumber = 0;
    }
}
