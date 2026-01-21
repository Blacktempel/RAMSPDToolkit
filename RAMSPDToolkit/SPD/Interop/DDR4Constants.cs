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
    internal sealed class DDR4Constants
    {
        public const byte SPD_DDR4_ADDRESS_PAGE        = 0x36;
        public const byte SPD_DDR4_THERMAL_SENSOR_BYTE = 0x0E;
        public const byte SPD_DDR4_THERMAL_SENSOR_BIT  = 0x07;

        public const ushort SPD_DDR4_EEPROM_LENGTH     = 512;
        public const byte   SPD_DDR4_EEPROM_PAGE_SHIFT = 8;
        public const byte   SPD_DDR4_EEPROM_PAGE_MASK  = 0xFF;

        public const byte SPD_DDR4_PAGE_MIN = 0;
        public const byte SPD_DDR4_PAGE_MAX = 1;

        public const byte SPD_DDR4_THERMAL_SENSOR_CAPABILITIES_REGISTER  = 0x00;
        public const byte SPD_DDR4_THERMAL_SENSOR_CONFIGURATION_REGISTER = 0x01;
        public const byte SPD_DDR4_THERMAL_SENSOR_HIGH_LIMIT             = 0x02;
        public const byte SPD_DDR4_THERMAL_SENSOR_LOW_LIMIT              = 0x03;
        public const byte SPD_DDR4_THERMAL_SENSOR_CRIT_LIMIT             = 0x04;
        public const byte SPD_DDR4_TEMPERATURE_ADDRESS                   = 0x05;
        public const byte SPD_DDR4_THERMAL_SENSOR_MANUFACTURER           = 0x06;
        public const byte SPD_DDR4_THERMAL_SENSOR_DEVICEID               = 0x07;

        public const byte   SPD_DDR4_MODULE_SPD_REVISION           = 0x001;
        public const byte   SPD_DDR4_MODULE_MEMORY_TYPE            = 0x002;
        public const ushort SPD_DDR4_MODULE_DENSITY_BANKS          = 0x004;
        public const ushort SPD_DDR4_MODULE_PRIMARY_PACKAGE_TYPE   = 0x006;
        public const ushort SPD_DDR4_MODULE_SECONDARY_PACKAGE_TYPE = 0x00A;
        public const ushort SPD_DDR4_MODULE_ORGANIZATION           = 0x00C;
        public const ushort SPD_DDR4_MODULE_BUS_WIDTH              = 0x00D;

        public const byte SPD_DDR4_RANKMIX_SYMMETRICAL  = 0;
        public const byte SPD_DDR4_RANKMIX_ASYMMETRICAL = 1;

        public const ushort SPD_DDR4_MODULE_MANUFACTURER_CONTINUATION_CODE   = 0x140;
        public const ushort SPD_DDR4_MODULE_MANUFACTURER_ID_CODE             = 0x141;
        public const ushort SPD_DDR4_MODULE_MANUFACTURING_LOCATION           = 0x142;
        public const ushort SPD_DDR4_MODULE_MANUFACTURING_DATE_BEGIN         = 0x143;
        public const ushort SPD_DDR4_MODULE_MANUFACTURING_DATE_END           = 0x144;
        public const ushort SPD_DDR4_MODULE_SERIAL_NUMBER_BEGIN              = 0x145;
        public const ushort SPD_DDR4_MODULE_SERIAL_NUMBER_END                = 0x148;
        public const ushort SPD_DDR4_MODULE_PART_NUMBER_BEGIN                = 0x149;
        public const ushort SPD_DDR4_MODULE_PART_NUMBER_END                  = 0x15C;
        public const ushort SPD_DDR4_MODULE_REVISION_CODE                    = 0x15D;
        public const ushort SPD_DDR4_DRAM_MANUFACTURER_CONTINUATION_CODE     = 0x15E;
        public const ushort SPD_DDR4_DRAM_MANUFACTURER_ID_CODE               = 0x15F;
        public const ushort SPD_DDR4_MANUFACTURER_SPECIFIC_DATA_BEGIN        = 0x161;
        public const ushort SPD_DDR4_MANUFACTURER_SPECIFIC_DATA_END          = 0x17D;

        public const byte SPD_DDR4_MANUFACTURER_CONTINUATION_CODE_ODD_PARITY_BIT = 7;

        public const byte SPD_DDR4_MODULE_PART_NUMBER_UNUSED = 0x20;

        //Time base defaults as per JEDEC DDR4 specification
        public const decimal SPD_DDR4_TIMEBASE_MTB = 0.125M;  // 0.125 ns Medium Time Base
        public const decimal SPD_DDR4_TIMEBASE_FTB = 0.001M;  // 0.001 ns Fine Time Base

        //Registers related to tCKAVGmin
        public const ushort SPD_DDR4_MIN_CYCLE_TIME_MTB = 0x12; // Medium Time Base for tCKAVGmin
        public const ushort SPD_DDR4_MIN_CYCLE_TIME_FTB = 0x7D; // Fine Time Base for tCKAVGmin

        //Registers related to tCKAVGmax
        public const ushort SPD_DDR4_MAX_CYCLE_TIME_MTB = 0x13; // Medium Time Base for tCKAVGmax
        public const ushort SPD_DDR4_MAX_CYCLE_TIME_FTB = 0x7C; // Fine Time Base for tCKAVGmax

        //Registers related to CAS Latencies
        public const ushort SPD_DDR4_SUPPORTED_CAS_LATENCIES_START = 0x14; // Supported CAS Latencies start
        public const ushort SPD_DDR4_SUPPORTED_CAS_LATENCIES_END   = 0x17; // Supported CAS Latencies end

        //Registers related to tAA timings
        public const ushort SPD_DDR4_MIN_CAS_LATENCY_MTB = 0x18; // Medium Time Base for tAA
        public const ushort SPD_DDR4_MIN_CAS_LATENCY_FTB = 0x7B; // Fine Time Base for tAA

        //Registers related to tRCD timings
        public const ushort SPD_DDR4_MIN_RAS_TO_CAS_DELAY_MTB = 0x19; // Medium Time Base for tRCD
        public const ushort SPD_DDR4_MIN_RAS_TO_CAS_DELAY_FTB = 0x7A; // Fine Time Base for tRCD

        //Registers related to tRP timings
        public const ushort SPD_DDR4_MIN_ROW_PRECHARGE_DELAY_MTB = 0x1A; // Medium Time Base for tRP
        public const ushort SPD_DDR4_MIN_ROW_PRECHARGE_DELAY_FTB = 0x79; // Fine Time Base for tRP

        //Registers related to tRAS and tRC timings
        public const ushort SPD_DDR4_tRAS_AND_tRC_UPPER_NIBBLES        = 0x1B; // tRAS and tRC timings register
        public const ushort SPD_DDR4_MIN_ACTIVE_TO_PRECHARGE_DELAY_MTB = 0x1C; // Medium Time Base for tRAS
        public const ushort SPD_DDR4_MIN_ACTIVE_TO_ACTIVE_DELAY_MTB    = 0x1D; // Medium Time Base for tRC
        public const ushort SPD_DDR4_MIN_ACTIVE_TO_ACTIVE_DELAY_FTB    = 0x78; // Fine Time Base for tRC

        //Registers related to tRFC timings
        public const ushort SPD_DDR4_MIN_REFRESH_RECOVERY_DELAY1_LSB = 0x1E; // Least Significant Byte for tRFC1
        public const ushort SPD_DDR4_MIN_REFRESH_RECOVERY_DELAY1_MSB = 0x1F; // Most Significant Byte for tRFC1
        public const ushort SPD_DDR4_MIN_REFRESH_RECOVERY_DELAY2_LSB = 0x20; // Least Significant Byte for tRFC2
        public const ushort SPD_DDR4_MIN_REFRESH_RECOVERY_DELAY2_MSB = 0x21; // Most Significant Byte for tRFC2
        public const ushort SPD_DDR4_MIN_REFRESH_RECOVERY_DELAY4_LSB = 0x22; // Least Significant Byte for tRFC4
        public const ushort SPD_DDR4_MIN_REFRESH_RECOVERY_DELAY4_MSB = 0x23; // Most Significant Byte for tRFC4

        //Registers related to tFAW timings
        public const ushort SPD_DDR4_MIN_FOUR_ACTIVATE_UPPER_NIBBLE     = 0x24; // Most Significant nibble for tFAW (MTB)
        public const ushort SPD_DDR4_MIN_FOUR_ACTIVATE_WINDOW_DELAY_MTB = 0x25; // Medium Time Base for tFAW

        //Registers related to tRRD_smin timings
        public const ushort SPD_DDR4_ACTIVATE_TO_ACTIVATE_DELAY_DIFF_MTB = 0x26; // Medium Time Base for tRRD_smin
        public const ushort SPD_DDR4_ACTIVATE_TO_ACTIVATE_DELAY_DIFF_FTB = 0x77; // Fine Time Base for tRRD_smin

        //Registers related to tRRD_lmin timings
        public const ushort SPD_DDR4_ACTIVATE_TO_ACTIVATE_DELAY_SAME_MTB = 0x27; // Medium Time Base for tRRD_lmin
        public const ushort SPD_DDR4_ACTIVATE_TO_ACTIVATE_DELAY_SAME_FTB = 0x76; // Fine Time Base for tRRD_lmin

        //Registers related to tCCD_lmin timings
        public const ushort SPD_DDR4_CAS_TO_CAS_DELAY_SAME_MTB = 0x28; // Medium Time Base for tCCD_lmin
        public const ushort SPD_DDR4_CAS_TO_CAS_DELAY_SAME_FTB = 0x75; // Fine Time Base for tCCD_lmin

        //Registers related to tWR
        public const ushort SPD_DDR4_MINIMUM_WRITE_RECOVERY_UPPER_NIBBLE = 0x29; // Most Significant Nibble for tWR (MTB)
        public const ushort SPD_DDR4_MINIMUM_WRITE_RECOVERY_TIME_MTB     = 0x2A; // Medium Time Base for tWR

        //Registers related to tWTR timings
        public const ushort SPD_DDR4_MINIMUM_WRITE_TO_READ_DELAY_UPPER_NIBBLES = 0x2B; // Upper nibble for tWTR registers (MTB)
        public const ushort SPD_DDR4_MINIMUM_WRITE_TO_READ_DELAY_DIFF_MTB      = 0x2C; // Medium Time Base for tWTR_smin
        public const ushort SPD_DDR4_MINIMUM_WRITE_TO_READ_DELAY_SAME_MTB      = 0x2D; // Medium Time Base for tWTR_lmin
    }
}
