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

        // Obsolete because:
        // - some implementations (e.g. wintab) might not expose VendorID/ProductID
        // - some tablets ('10moons') do not have *unique* VendorID/ProductID
        // ... probably shouldn't be matching by them anyway. Just need a unique ID.
        [Obsolete(
            "Use PersistentId instead. This remains so that clients that previously saw a device with V1 can recognize them with V2")]
        public UInt16 VendorId = 0;

        [Obsolete(
            "Use PersistentId instead. This remains so that clients that previously saw a device with V1 can recognize them with V2")]
        public UInt16 ProductId = 0;

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