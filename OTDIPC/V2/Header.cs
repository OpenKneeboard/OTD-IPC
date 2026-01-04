/*
 * Copyright (c) 2022 Fred Emmott <fred@fredemmott.com>
 *
 * SPDX-License-Identifier: MIT
 */
using System.Runtime.InteropServices;

namespace OTDIPC.V2
{
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    struct Header {
        public MessageType MessageType;
        public UInt32 Size;
        public UInt32 NonPersistentTabletId;
    }
}
