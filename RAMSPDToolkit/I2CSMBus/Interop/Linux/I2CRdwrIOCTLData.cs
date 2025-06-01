/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * Copyright (c) 2025 Florian K.
 *
 * Code inspiration, improvements and fixes are from, but not limited to, following projects:
 * LibreHardwareMonitor; Linux Kernel; OpenRGB; WinRing0 (QCute)
 */

using System.Runtime.InteropServices;

namespace RAMSPDToolkit.I2CSMBus.Interop.Linux
{
    public struct I2CRdwrIOCTLData
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public I2CMsgStructure[] Msgs;

        public uint NMsgs;
    }
}