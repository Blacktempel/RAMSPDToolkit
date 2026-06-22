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
    internal sealed class DDR3Constants
    {
        public const byte SPD_DDR3_THERMAL_SENSOR_BYTE = 0x20;
        public const byte SPD_DDR3_THERMAL_SENSOR_BIT  = 0x07;

        public const ushort SPD_DDR3_EEPROM_LENGTH = 256;

        public const byte SPD_DDR3_PAGE = 0;

        public const byte SPD_DDR3_THERMAL_SENSOR_CAPABILITIES_REGISTER  = 0x00;
        public const byte SPD_DDR3_THERMAL_SENSOR_CONFIGURATION_REGISTER = 0x01;
        public const byte SPD_DDR3_THERMAL_SENSOR_HIGH_LIMIT             = 0x02;
        public const byte SPD_DDR3_THERMAL_SENSOR_LOW_LIMIT              = 0x03;
        public const byte SPD_DDR3_THERMAL_SENSOR_CRITICAL_LIMIT         = 0x04;
        public const byte SPD_DDR3_TEMPERATURE_ADDRESS                   = 0x05;
        public const byte SPD_DDR3_THERMAL_SENSOR_MANUFACTURER           = 0x06;
        public const byte SPD_DDR3_THERMAL_SENSOR_DEVICE_ID              = 0x07;

        public const ushort SPD_DDR3_MODULE_SPD_REVISION  = 0x001;
        public const ushort SPD_DDR3_MODULE_MEMORY_TYPE   = 0x002;
        public const ushort SPD_DDR3_MODULE_DENSITY_BANKS = 0x004;
        public const ushort SPD_DDR3_MODULE_ORGANIZATION  = 0x007;
        public const ushort SPD_DDR3_MODULE_BUS_WIDTH     = 0x008;

        public const byte SPD_DDR3_SDRAM_DENSITY_MASK          = 0x0F;
        public const byte SPD_DDR3_SDRAM_DEVICE_WIDTH_MASK     = 0x07;
        public const byte SPD_DDR3_MODULE_RANKS_MASK           = 0x07;
        public const byte SPD_DDR3_MODULE_RANKS_SHIFT          = 3;
        public const byte SPD_DDR3_PRIMARY_BUS_WIDTH_MASK      = 0x07;
        public const byte SPD_DDR3_MAX_SDRAM_DENSITY_CODE      = 0x06;
        public const byte SPD_DDR3_MAX_SDRAM_DEVICE_WIDTH_CODE = 0x03;
        public const byte SPD_DDR3_MAX_PRIMARY_BUS_WIDTH_CODE  = 0x03;

        public const ushort SPD_DDR3_MODULE_MANUFACTURER_CONTINUATION_CODE = 0x075;
        public const ushort SPD_DDR3_MODULE_MANUFACTURER_ID_CODE           = 0x076;
        public const ushort SPD_DDR3_MODULE_MANUFACTURING_LOCATION         = 0x077;
        public const ushort SPD_DDR3_MODULE_MANUFACTURING_DATE_BEGIN       = 0x078;
        public const ushort SPD_DDR3_MODULE_MANUFACTURING_DATE_END         = 0x079;
        public const ushort SPD_DDR3_MODULE_SERIAL_NUMBER_BEGIN            = 0x07A;
        public const byte   SPD_DDR3_MODULE_SERIAL_NUMBER_LENGTH           = 4;
        public const ushort SPD_DDR3_MODULE_PART_NUMBER_BEGIN              = 0x080;
        public const byte   SPD_DDR3_MODULE_PART_NUMBER_LENGTH             = 18;
        public const ushort SPD_DDR3_MODULE_REVISION_CODE                  = 0x092;
        public const ushort SPD_DDR3_MODULE_REVISION_CODE_EXTENSION        = 0x093;
        public const ushort SPD_DDR3_DRAM_MANUFACTURER_CONTINUATION_CODE   = 0x094;
        public const ushort SPD_DDR3_DRAM_MANUFACTURER_ID_CODE             = 0x095;
        public const ushort SPD_DDR3_MANUFACTURER_SPECIFIC_DATA_BEGIN      = 0x096;
        public const byte   SPD_DDR3_MANUFACTURER_SPECIFIC_DATA_LENGTH     = 26;

        public const byte SPD_DDR3_MANUFACTURER_CONTINUATION_CODE_ODD_PARITY_BIT = 7;

        public const ushort SPD_DDR3_FINE_TIMEBASE_DIVIDEND_DIVISOR = 0x009;
        public const ushort SPD_DDR3_MEDIUM_TIMEBASE_DIVIDEND       = 0x00A;
        public const ushort SPD_DDR3_MEDIUM_TIMEBASE_DIVISOR        = 0x00B;

        public const ushort SPD_DDR3_MIN_CYCLE_TIME_MTB = 0x00C;
        public const ushort SPD_DDR3_MIN_CYCLE_TIME_FTB = 0x022;

        public const ushort SPD_DDR3_SUPPORTED_CAS_LATENCIES_LSB = 0x00E;
        public const ushort SPD_DDR3_SUPPORTED_CAS_LATENCIES_MSB = 0x00F;
        public const byte   SPD_DDR3_LOWEST_CAS_LATENCY          = 4;
        public const byte   SPD_DDR3_CAS_LATENCY_BITS            = 15;

        public const ushort SPD_DDR3_MIN_CAS_LATENCY_MTB = 0x010;
        public const ushort SPD_DDR3_MIN_CAS_LATENCY_FTB = 0x023;

        public const ushort SPD_DDR3_MIN_WRITE_RECOVERY_TIME_MTB = 0x011;

        public const ushort SPD_DDR3_MIN_RAS_TO_CAS_DELAY_MTB = 0x012;
        public const ushort SPD_DDR3_MIN_RAS_TO_CAS_DELAY_FTB = 0x024;

        public const ushort SPD_DDR3_MIN_ACTIVATE_TO_ACTIVATE_DELAY_MTB = 0x013;

        public const ushort SPD_DDR3_MIN_ROW_PRECHARGE_DELAY_MTB = 0x014;
        public const ushort SPD_DDR3_MIN_ROW_PRECHARGE_DELAY_FTB = 0x025;

        public const ushort SPD_DDR3_tRAS_AND_tRC_UPPER_NIBBLES          = 0x015;
        public const ushort SPD_DDR3_MIN_ACTIVE_TO_PRECHARGE_DELAY_MTB   = 0x016;
        public const ushort SPD_DDR3_MIN_ACTIVE_TO_ACTIVE_DELAY_MTB      = 0x017;
        public const ushort SPD_DDR3_MIN_ACTIVE_TO_ACTIVE_DELAY_FTB      = 0x026;

        public const ushort SPD_DDR3_MIN_REFRESH_RECOVERY_DELAY_LSB = 0x018;
        public const ushort SPD_DDR3_MIN_REFRESH_RECOVERY_DELAY_MSB = 0x019;

        public const ushort SPD_DDR3_MIN_WRITE_TO_READ_DELAY_MTB = 0x01A;
        public const ushort SPD_DDR3_MIN_READ_TO_PRECHARGE_MTB   = 0x01B;

        public const ushort SPD_DDR3_MIN_FOUR_ACTIVATE_UPPER_NIBBLE     = 0x01C;
        public const ushort SPD_DDR3_MIN_FOUR_ACTIVATE_WINDOW_DELAY_MTB = 0x01D;
    }
}
