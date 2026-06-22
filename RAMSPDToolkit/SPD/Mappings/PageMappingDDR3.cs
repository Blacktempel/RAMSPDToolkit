/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * Copyright (c) 2026 Florian K.
 *
 * Code inspiration, improvements and fixes are from, but not limited to, following projects:
 * LibreHardwareMonitor; Linux Kernel; OpenRGB; WinRing0 (QCute)
 */

using RAMSPDToolkit.SPD.Enums;

namespace RAMSPDToolkit.SPD.Mappings
{
    /// <summary>
    /// DDR3 SPD data mapping.
    /// </summary>
    public class PageMappingDDR3
    {
        /// <summary>
        /// DDR3 uses a single 256-byte address space without SPD page switching.
        /// </summary>
        public static readonly IReadOnlyDictionary<byte, PageData> PageDataMapping = new Dictionary<byte, PageData>()
        {
            { 0, PageData.ThermalData
               | PageData.MemoryType
               | PageData.SPDRevision
               | PageData.ModuleManufacturerID
               | PageData.ModuleManufacturingLocation
               | PageData.ModuleManufacturingDate
               | PageData.ModuleSerialNumber
               | PageData.ModulePartNumber
               | PageData.ModuleRevisionCode
               | PageData.DRAMManufacturerCode
               | PageData.ManufacturersSpecificData
            },
        };
    }
}
