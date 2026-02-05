/*
 * Copyright (c) 2026 Fred Emmott <fred@fredemmott.com>
 *
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace OTDIPC.V2
{
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public unsafe struct Hello
    {
        public Hello()
        {
        }

        public Header Header = new()
        {
            MessageType = MessageType.Hello,
            Size = (UInt32)Marshal.SizeOf(typeof(Hello)),
        };

        // 0xAAYYYYMMDDBB
        public UInt64 ProtocolVersion = 0;

        private const int StringFieldLength = 256;

        private fixed byte _HumanReadableName[StringFieldLength];
        private fixed byte _HumanReadableVersion[StringFieldLength];
        private fixed byte _ImplementationId[StringFieldLength];

        public byte CompatibilityVersion = 0;

        public string HumanReadableName
        {
            get
            {
                fixed (byte* p = _HumanReadableName)
                {
                    return GetString(p);
                }
            }
            set
            {
                fixed (byte* p = _HumanReadableName)
                {
                    SetString(p, value);
                }
            }
        }

        public string HumanReadableVersion
        {
            get
            {
                fixed (byte* p = _HumanReadableVersion)
                {
                    return GetString(p);
                }
            }
            set
            {
                fixed (byte* p = _HumanReadableVersion)
                {
                    SetString(p, value);
                }
            }
        }

        public string ImplementationId
        {
            get
            {
                fixed (byte* p = _ImplementationId)
                {
                    return GetString(p);
                }
            }
            set
            {
                fixed (byte* p = _ImplementationId)
                {
                    SetString(p, value);
                }
            }
        }


        private string GetString(byte* field)
        {
            var len = 0;
            while (len < StringFieldLength && field[len] != 0)
            {
                ++len;
            }

            return Encoding.UTF8.GetString(field, len);
        }

        private void SetString(byte* field, string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            for (var i = 0; i < StringFieldLength; ++i)
            {
                field[i] = (i < bytes.Length) ? bytes[i] : (byte)0;
            }
        }
    }
}