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

namespace RAMSPDToolkit.SPD.Enums
{
    [Flags]
    public enum PageData : int
    {
        /// <summary>
        /// Nothing is available.
        /// </summary>
        Nothing = 0x0,

        /// <summary>
        /// Thermal data includes temperature and thermal configuration registers.
        /// </summary>
        ThermalData = 0x1,
        MemoryType  = 0x2,
        SPDRevision = 0x4,

        ModuleManufacturerID        =  0x1000,
        ModuleManufacturingLocation =  0x2000,
        ModuleManufacturingDate     =  0x4000,
        ModuleSerialNumber          =  0x8000,
        ModulePartNumber            = 0x10000,
        ModuleRevisionCode          = 0x20000,
        DRAMManufacturerCode        = 0x40000,
        ManufacturersSpecificData   = 0x80000,
    }
}
