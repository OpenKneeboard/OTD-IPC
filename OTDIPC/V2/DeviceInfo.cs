/*
 * Copyright (c) 2022 Fred Emmott <fred@fredemmott.com>
 *
 * SPDX-License-Identifier: MIT
 */

using System.Runtime.InteropServices;
using System.Text;

namespace OTDIPC.V2
{
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
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
        public UInt16 VendorId = 0; // Deprecated: use PersistentId instead. Used to allow matching with other data sources
        public UInt16 ProductId = 0; // Deprecated: use PersistentId instead. Used to allow matching with other data sources
        
        private const int PersistentIdMaxLength = 256;
        private fixed byte _PersistentId[PersistentIdMaxLength];
        
        private const int NameMaxLength = 256;
        private fixed byte _Name[NameMaxLength];
        
        public string PersistentId
        {
            get
            {
                fixed (byte* p = _PersistentId)
                {
                    var len = 0;
                    while (len < PersistentIdMaxLength && p[len] != 0)
                    {
                        ++len;
                    }

                    return Encoding.UTF8.GetString(p, len);
                }
            }

            set
            {
                var bytes = Encoding.UTF8.GetBytes(value);
                for (var i = 0; i < PersistentIdMaxLength; ++i)
                {
                    if (i < bytes.Length)
                    {
                        _PersistentId[i] = bytes[i];
                    }
                    else
                    {
                        _PersistentId[i] = 0;
                    }
                }
            }
        }


        public string Name
        {
            get
            {
                fixed (byte* p = _Name)
                {
                    var len = 0;
                    while (len < NameMaxLength && p[len] != 0)
                    {
                        ++len;
                    }

                    return Encoding.UTF8.GetString(p, len);
                }
            }

            set
            {
                var bytes = Encoding.UTF8.GetBytes(value);
                for (var i = 0; i < NameMaxLength; ++i)
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
    }
}