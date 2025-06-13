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

using RAMSPDToolkit.PCI;

namespace RAMSPDToolkit.I2CSMBus.Interop.Linux
{
    internal sealed class LinuxConstants
    {
        public const string SMBusClassID = "0x0c0500";

        public static IReadOnlyList<ushort> AllowedVendorIDs = new List<ushort>
        {
            PCIConstants.PCI_VENDOR_AMD_GPU, //Advanced Micro Devices, Inc. [AMD/ATI]
            PCIConstants.PCI_VENDOR_AMD    , //Advanced Micro Devices, Inc. [AMD]
            PCIConstants.PCI_VENDOR_INTEL  , //Intel Corporation
        };

        /* NOTE: Slave address is 7 or 10 bits, but 10-bit addresses
         * are NOT supported! (due to code brokenness)
         */
        public const int I2C_SLAVE       = 0x0703; /* Use this slave address */
        public const int I2C_SLAVE_FORCE = 0x0706; /* Use this slave address, even if it is already in use by a driver! */
        public const int I2C_TENBIT      = 0x0704; /* 0 for 7 bit addrs, != 0 for 10 bit */
        public const int I2C_FUNCS       = 0x0705; /* Get the adapter functionality mask */
        public const int I2C_RDWR        = 0x0707; /* Combined R/W transfer (one STOP only) */
        public const int I2C_PEC         = 0x0708; /* != 0 to use PEC with SMBus */
        public const int I2C_SMBUS       = 0x0720; /* SMBus transfer */

        public const int I2C_FUNC_I2C                    = 0x00000001;
        public const int I2C_FUNC_10BIT_ADDR             = 0x00000002; /* required for I2C_M_TEN */
        public const int I2C_FUNC_PROTOCOL_MANGLING      = 0x00000004; /* required for I2C_M_IGNORE_NAK etc. */
        public const int I2C_FUNC_SMBUS_PEC              = 0x00000008;
        public const int I2C_FUNC_NOSTART                = 0x00000010; /* required for I2C_M_NOSTART */
        public const int I2C_FUNC_SLAVE                  = 0x00000020;
        public const int I2C_FUNC_SMBUS_BLOCK_PROC_CALL  = 0x00008000; /* SMBus 2.0 or later */
        public const int I2C_FUNC_SMBUS_QUICK            = 0x00010000;
        public const int I2C_FUNC_SMBUS_READ_BYTE        = 0x00020000;
        public const int I2C_FUNC_SMBUS_WRITE_BYTE       = 0x00040000;
        public const int I2C_FUNC_SMBUS_READ_BYTE_DATA   = 0x00080000;
        public const int I2C_FUNC_SMBUS_WRITE_BYTE_DATA  = 0x00100000;
        public const int I2C_FUNC_SMBUS_READ_WORD_DATA   = 0x00200000;
        public const int I2C_FUNC_SMBUS_WRITE_WORD_DATA  = 0x00400000;
        public const int I2C_FUNC_SMBUS_PROC_CALL        = 0x00800000;
        public const int I2C_FUNC_SMBUS_READ_BLOCK_DATA  = 0x01000000; /* required for I2C_M_RECV_LEN */
        public const int I2C_FUNC_SMBUS_WRITE_BLOCK_DATA = 0x02000000;
        public const int I2C_FUNC_SMBUS_READ_I2C_BLOCK   = 0x04000000; /* I2C-like block xfer  */
        public const int I2C_FUNC_SMBUS_WRITE_I2C_BLOCK  = 0x08000000; /* w/ 1-byte reg. addr. */
        public const int I2C_FUNC_SMBUS_HOST_NOTIFY      = 0x10000000; /* SMBus 2.0 or later */

        /* For lseek */
        public const int SEEK_SET = 0; /* Seek relative to beginning of file. */
        public const int SEEK_CUR = 1; /* Seek relative to current file position. */
        public const int SEEK_END = 2; /* Seek relative to end of file. */

        public const int DT_DIR =  4;
        public const int DT_LNK = 10;

        public const int O_RDONLY = 0x00;
        public const int O_RDWR   = 0x02;
    }
}