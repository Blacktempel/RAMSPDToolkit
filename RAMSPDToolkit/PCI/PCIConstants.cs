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

namespace RAMSPDToolkit.PCI
{
    internal class PCIConstants
    {
        public const string PCIMutexName = "Access_PCI";

        public const int PCI_VENDOR_AMD     = 0x1022;
        public const int PCI_VENDOR_AMD_GPU = 0x1002;
        public const int PCI_VENDOR_INTEL   = 0x8086;
        public const int PCI_VENDOR_NVIDIA  = 0x10DE;

        //PCI Configuration Register
        public const byte PCI_VENDOR_ID           = 0x00;
        public const byte PCI_DEVICE_ID           = 0x02;
        public const byte PCI_SUBCLASS            = 0x0A;
        public const byte PCI_BASECLASS           = 0x0B;
        public const byte PCI_SUBSYSTEM_VENDOR_ID = 0x2C;
        public const byte PCI_SUBSYSTEM_DEVICE_ID = 0x2E;

        public const uint PCI_CFG_REG_INTEL_SMBA = 0x20;
        public const uint PCI_CFG_REG_AMD_SMBA   = 0x90;

        public const uint PCI_BASECLASS_SERIAL_BUS_CONTROLLER = 0x0C;
        public const uint PCI_SUBCLASS_SMBUS                  = 0x05;

        public const uint PCI_DEVICE_INVALID = 0xFFFFFFFF;
    }
}
