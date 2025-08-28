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

        public const byte SPD_DDR5_WRITE_RECOVERY_TIME = 0x06;

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

        // Time base defaults as per JEDEC DDR5 specification
        public const decimal SPD_DDR5_TIMEBASE_MTB = 1.0M;  // 1 ns Medium Time Base
        public const decimal SPD_DDR5_TIMEBASE_FTB = 0.001M; // 0.001 ns Fine Time Base

        // Registers related to tCKAVGmin
        public const ushort SPD_DDR5_MIN_CYCLE_TIME = 0x14; // Start address of tCKAVGmin (FTB)

        // Registers related to tCKAVGmax
        public const ushort SPD_DDR5_MAX_CYCLE_TIME = 0x16; // Start address of tCKAVGmax (FTB)

        // Registers related to CAS Latencies
        public const ushort SPD_DDR5_SUPPORTED_CAS_LATENCIES_START = 0x18; // Start address of supported CAS latencies
        public const ushort SPD_DDR5_SUPPORTED_CAS_LATENCIES_END   = 0x1C; // End address of supported CAS latencies
        public const int SPD_DDR5_LOWEST_SUPPORTED_CAS_LATENCY     = 20;   // Lowest supported CAS latency for DDR5

        // Registers related to tAA timings
        public const ushort SPD_DDR5_MIN_CAS_LATENCY = 0x1E; // Start address of tAA timings (FTB)

        // Registers related to tRCD timings
        public const ushort SPD_DDR5_MIN_RAS_TO_CAS_DELAY = 0x20; // Start address of tRCD timings (FTB)

        // Registers related to tRP timings
        public const ushort SPD_DDR5_MIN_ROW_PRECHARGE_DELAY = 0x22; // Start address of tRP timings (FTB)

        // Registers related to tRAS timings
        public const ushort SPD_DDR5_MIN_ACTIVE_TO_PRECHARGE_DELAY = 0x24; // Start address of tRAS timings (FTB)

        // Registers related to tRC timings
        public const ushort SPD_DDR5_MIN_ACTIVE_TO_ACTIVE_DELAY = 0x26; // Start address of tRC timings (FTB)

        // Registers related to tWR timings
        public const ushort SPD_DDR5_MIN_WRITE_RECOVERY_TIME = 0x28; // Start address of tWR timings (FTB)

        // Registers related to tRFC timings
        public const ushort SPD_DDR5_NORMAL_REFRESH_RECOVERY_TIME                       = 0x2A; // Start address of tRFC1 timings (MTB)
        public const ushort SPD_DDR5_FINE_GRANULARITY_REFRESH_RECOVERY_TIME             = 0x2C; // Start address of tRFC2 timings (MTB)
        public const ushort SPD_DDR5_SAME_BANK_REFRESH_RECOVERY_TIME                    = 0x2E; // Start address of tRFCsb timings (MTB)
        public const ushort SPD_DDR5_NORMAL_REFRESH_RECOVERY_TIME_DIFFRANK              = 0x30; // Start address of tRFC1_dlr timings (MTB)
        public const ushort SPD_DDR5_FINE_GRANULARITY_REFRESH_RECOVERY_TIME_DIFFRANK    = 0x32; // Start address of tRFC2_dlr timings (MTB)
        public const ushort SPD_DDR5_SAME_BANK_REFRESH_RECOVERY_TIME_DIFFRANK           = 0x34; // Start address of tRFCsb_dlr timings (MTB)

        // Registers related to tRRD_L timings
        public const ushort SPD_DDR5_ACTIVATE_TO_ACTIVATE_DELAY_SAME = 0x46; // Start address of tRRD_L timings (FTB)

        // Registers related to tCCD (Command to Command delays) timings
        public const ushort SPD_DDR5_READ_TO_READ_DELAY_SAME_GROUP              = 0x49; // Start address of tCCD_L timings (FTB)
        public const ushort SPD_DDR5_WRITE_TO_WRITE_DELAY_SAME_GROUP            = 0x4C; // Start address of tCCD_L_WR timings (FTB)
        public const ushort SPD_DDR5_WRITE_TO_WRITE_DELAY_SAME_GROUP_SECOND     = 0x4F; // Start address of tCCD_L_WR2 timings (FTB)
        public const ushort SPD_DDR5_WRITE_TO_READ_DELAY_DIFF_GROUP             = 0x58; // Start address of tCCD_S_WTR timings (FTB)
        public const ushort SPD_DDR5_READ_TO_READ_DELAY_DIFF_BANK_SAME_GROUP    = 0x5E; // Start address of tCCD_M timings (FTB)
        public const ushort SPD_DDR5_WRITE_TO_WRITE_DELAY_DIFF_BANK_SAME_GROUP  = 0x61; // Start address of tCCD_M_WR timings (FTB)
        public const ushort SPD_DDR5_WRITE_TO_READ_DELAY_DIFF_BANK_SAME_GROUP   = 0x64; // Start address of tCCD_M_WTR timings (FTB)

        // Registers related to tFAW timings
        public const ushort SPD_DDR5_FOUR_ACTIVATE_WINDOW = 0x52; // Start address of tFAW timings (FTB)

        // Registers related to tRTP timings
        public const ushort SPD_DDR5_READ_TO_PRECHARGE_DELAY = 0x5B; // Start address of tRTP timings (FTB)
    }
}
