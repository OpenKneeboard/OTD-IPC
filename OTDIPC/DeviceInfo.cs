/*
 * Copyright (c) 2022 Fred Emmott <fred@fredemmott.com>
 *
 * SPDX-License-Identifier: MIT
 */

using System.Runtime.InteropServices;
using System.Text;

namespace OTDIPC
{
    [StructLayout(LayoutKind.Sequential, Pack = 0, CharSet = CharSet.Unicode)]
    unsafe struct DeviceInfo
    {
        public DeviceInfo()
        {
        }

        public Header Header = new()
        {
            MessageType = MessageType.DeviceInfo,
            Size = (UInt32)Marshal.SizeOf(typeof(DeviceInfo)),
        };

        public bool IsValid = false;
        public float MaxX = 0;
        public float MaxY = 0;
        public UInt32 MaxPressure = 0;

        public string Name
        {
            get
            {
                fixed (byte* p = _Name)
                {
                    var len = 0;
                    while (len < 64 && p[len] != 0)
                    {
                        ++len;
                    }

                    return Encoding.UTF8.GetString(p, len);
                }
            }

            set
            {
                var bytes = Encoding.UTF8.GetBytes(value);
                for (var i = 0; i < 64; ++i)
                {
                    if (i < bytes.Length)
                    {
                        _Name[i] = bytes[i];
                    }
                    else
                    {
                        _Name[i] = 0;
                    }
                }
            }
        }

        private fixed byte _Name[64];
    }
}