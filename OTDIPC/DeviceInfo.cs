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
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
    struct DeviceInfo
    {
        public DeviceInfo()
        {
        }

        public Header Header = new() {
            MessageType = MessageType.DeviceInfo,
            Size = (UInt32) Marshal.SizeOf(typeof(DeviceInfo)),
        };

        public bool IsValid = false;
        public float MaxX = 0;
        public float MaxY = 0;
        public UInt32 MaxPressure = 0;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string Name = "";
    }
}
