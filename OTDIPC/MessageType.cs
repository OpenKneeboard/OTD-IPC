/*
 * Copyright (c) 2022 Fred Emmott <fred@fredemmott.com>
 *
 * SPDX-License-Identifier: MIT
 */
using System.Runtime.InteropServices;

namespace OTDIPC
{
    enum MessageType : UInt32
    {
        DeviceInfo = 1,
        State = 2,
        Ping = 3,
    }
}
