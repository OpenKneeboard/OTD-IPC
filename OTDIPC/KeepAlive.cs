/*
 * Copyright (c) 2022 Fred Emmott <fred@fredemmott.com>
 *
 * SPDX-License-Identifier: ISC
 */
using System.Runtime.InteropServices;

namespace OTDIPC
{
    [StructLayout(LayoutKind.Sequential, Pack = 0, CharSet = CharSet.Unicode)]
    struct KeepAlive
    {
        public KeepAlive()
        {
        }

        public Header Header = new() {
            MessageType = MessageType.KeepAlive,
            Size = (UInt32) Marshal.SizeOf(typeof(KeepAlive)),
        };

        public UInt64 SequenceNumber = 0;
    }
}
