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

namespace RAMSPDToolkit.SPD.Interop
{
    internal sealed class DDR5Constants
    {
        public const byte SPD_DDR5_DEVICE_TYPE_MOST  = 0x0;
        public const byte SPD_DDR5_DEVICE_TYPE_LEAST = 0x1;

        public const byte SPD_DDR5_DEVICE_TYPE_MOST_EXPECTED  = 0x51;
        public const byte SPD_DDR5_DEVICE_TYPE_LEAST_EXPECTED = 0x18;

        public const ushort SPD_DDR5_EEPROM_LENGTH     = 2048;
        public const byte   SPD_DDR5_EEPROM_PAGE_SHIFT = 7;
        public const byte   SPD_DDR5_EEPROM_PAGE_MASK  = 0x7F;
        public const byte   SPD_DDR5_MREG_VIRTUAL_PAGE = 0x0B;

        public const byte SPD_DDR5_PAGE_MIN = 0;
        public const byte SPD_DDR5_PAGE_MAX = 7;

        public const float SPD_DDR5_TEMPERATURE_RESOLUTION = 0.25f;

        public const byte SPD_DDR5_DEVICE_CAPABILITY = 0x05;

        public const byte SPD_DDR5_THERMAL_SENSOR_ENABLED                           = 0x1A;
        public const byte SPD_DDR5_THERMAL_SENSOR_HIGH_LIMIT_CONFIGURATION          = 0x1C;
        public const byte SPD_DDR5_THERMAL_SENSOR_LOW_LIMIT_CONFIGURATION           = 0x1E;
        public const byte SPD_DDR5_THERMAL_SENSOR_CRITICAL_HIGH_LIMIT_CONFIGURATION = 0x20;
        public const byte SPD_DDR5_THERMAL_SENSOR_CRITICAL_LOW_LIMIT_CONFIGURATION  = 0x22;
        public const byte SPD_DDR5_TEMPERATURE_ADDRESS                              = 0x31;
        public const byte SPD_DDR5_THERMAL_SENSOR_STATUS                            = 0x33;

        public const ushort SPD_DDR5_MODULE_SPD_REVISION = 0x01;
        public const ushort SPD_DDR5_MODULE_MEMORY_TYPE  = 0x02;

        public const ushort SPD_DDR5_MODULE_MANUFACTURER_CONTINUATION_CODE   = 0x200;
        public const ushort SPD_DDR5_MODULE_MANUFACTURER_ID_CODE             = 0x201;
        public const ushort SPD_DDR5_MODULE_MANUFACTURING_LOCATION           = 0x202;
        public const ushort SPD_DDR5_MODULE_MANUFACTURING_DATE_BEGIN         = 0x203;
        public const ushort SPD_DDR5_MODULE_MANUFACTURING_DATE_END           = 0x204;
        public const ushort SPD_DDR5_MODULE_SERIAL_NUMBER_BEGIN              = 0x205;
        public const ushort SPD_DDR5_MODULE_SERIAL_NUMBER_END                = 0x208;
        public const ushort SPD_DDR5_MODULE_PART_NUMBER_BEGIN                = 0x209;
        public const ushort SPD_DDR5_MODULE_PART_NUMBER_END                  = 0x226;
        public const ushort SPD_DDR5_MODULE_REVISION_CODE                    = 0x227;
        public const ushort SPD_DDR5_DRAM_MANUFACTURER_CONTINUATION_CODE     = 0x228;
        public const ushort SPD_DDR5_DRAM_MANUFACTURER_ID_CODE               = 0x229;
        public const ushort SPD_DDR5_MANUFACTURER_SPECIFIC_DATA_BEGIN        = 0x22B;
        public const ushort SPD_DDR5_MANUFACTURER_SPECIFIC_DATA_END          = 0x27F;

        public const byte SPD_DDR5_MANUFACTURER_CONTINUATION_CODE_ODD_PARITY_BIT = 7;

        public const byte SPD_DDR5_MODULE_PART_NUMBER_UNUSED = 0x20;
    }
}
