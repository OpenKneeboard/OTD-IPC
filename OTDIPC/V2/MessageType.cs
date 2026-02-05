/*
 * Copyright (c) 2022 Fred Emmott <fred@fredemmott.com>
 *
 * SPDX-License-Identifier: MIT
 */

namespace OTDIPC.V2
{
    public enum MessageType : UInt32
    {
        DeviceInfo = 1,
        State = 2,
        Ping = 3,

        // { Header, Size, char... }
        DebugMessage = 4,

        /* { Header, Size, GUID, ... }
         *
         * The actual message type is identified by the GUID.
         *
         * GUID should be in the Win32/C# GUID struct binary layout, i.e. 16 bytes of endian-sensitive binary data
         *
         * Please consider contributing a new message type to OTD-IPC instead; this message type is primarily intended
         * to support private experimentation without a risk of conflicts.
         */
        Experimental = 5,
        Hello = 6,
    }
}