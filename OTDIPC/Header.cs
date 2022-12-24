/*
 * Copyright (c) 2022 Fred Emmott <fred@fredemmott.com>
 *
 * SPDX-License-Identifier: ISC
 */
using System.Runtime.InteropServices;

namespace OTDIPC
{
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    struct Header {
        public MessageType MessageType;
        public UInt32 Size;
        public UInt16 VID;
        public UInt16 PID;
    }
}
