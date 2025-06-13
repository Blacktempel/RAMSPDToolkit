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

namespace RAMSPDToolkit.I2CSMBus.Interop
{
    public sealed class I2CConstants
    {
        //Data for SMBus Messages
        public const int I2C_SMBUS_BLOCK_MAX = 32;

        //i2c_smbus_xfer read or write markers
        public const byte I2C_SMBUS_READ  = 1;
        public const byte I2C_SMBUS_WRITE = 0;

        //SMBus transaction types
        public const int I2C_SMBUS_QUICK            = 0;
        public const int I2C_SMBUS_BYTE             = 1;
        public const int I2C_SMBUS_BYTE_DATA        = 2;
        public const int I2C_SMBUS_WORD_DATA        = 3;
        public const int I2C_SMBUS_PROC_CALL        = 4;
        public const int I2C_SMBUS_BLOCK_DATA       = 5;
        public const int I2C_SMBUS_I2C_BLOCK_BROKEN = 6;
        public const int I2C_SMBUS_BLOCK_PROC_CALL  = 7; /* SMBus 2.0 */
        public const int I2C_SMBUS_I2C_BLOCK_DATA   = 8;

        public const uint INFINITE_TIME = 0xFFFFFFFF;
    }
}
