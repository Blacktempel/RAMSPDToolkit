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

using BlackSharp.Core.Interop.Windows.Mutexes;
using RAMSPDToolkit.I2CSMBus.Interop;
using RAMSPDToolkit.I2CSMBus.Interop.NCT6775;
using RAMSPDToolkit.I2CSMBus.Interop.Shared;
using RAMSPDToolkit.Mutexes;
using RAMSPDToolkit.SuperInOut;
using RAMSPDToolkit.Windows.Driver;
using OS = BlackSharp.Core.Platform.OperatingSystem;

namespace RAMSPDToolkit.I2CSMBus
{
    /// <summary>
    /// SMBus class for NCT6775.
    /// </summary>
    public sealed class SMBusNCT6775 : SMBusInterface
    {
        #region Constructor

        SMBusNCT6775()
        {
            if (!OS.IsWindows())
            {
                throw new PlatformNotSupportedException();
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Base address for SMBus.
        /// </summary>
        public ushort NCT6775_SMBA { get; private set; } = 0x0290;

        public ushort SMBHSTDAT => (ushort)(  0 + NCT6775_SMBA);
        public ushort SMBBLKSZ  => (ushort)(  1 + NCT6775_SMBA);
        public ushort SMBHSTCMD => (ushort)(  2 + NCT6775_SMBA);
        public ushort SMBHSTIDX => (ushort)(  3 + NCT6775_SMBA); //Index field is command field on other controllers
        public ushort SMBHSTCTL => (ushort)(  4 + NCT6775_SMBA);
        public ushort SMBHSTADD => (ushort)(  5 + NCT6775_SMBA);
        public ushort SMBHSTERR => (ushort)(  9 + NCT6775_SMBA);
        public ushort SMBHSTSTS => (ushort)(0xE + NCT6775_SMBA);

        #endregion

        #region I2CSMBusInterface

        protected override int I2CSMBusXfer(byte addr, byte read_write, byte command, int size, SMBusData data)
        {
            using (var guard = new WorldMutexGuard(WorldMutexManager.WorldSMBusMutex))
            {
                var result = NCT6775Access(addr, read_write, command, size, data);

                return result;
            }
        }

        protected override int I2CXfer(byte addr, byte read_write, int? size, byte[] data)
        {
            return -1;
        }

        #endregion

        #region Public

        /// <summary>
        /// Detects if this SMBus is available for usage.<br/>
        /// If it is, an instance of this SMBus will be created and added into <see cref="SMBusManager.RegisteredSMBuses"/>.
        /// </summary>
        /// <returns>True if SMBus is available and false if not.</returns>
        public static bool SMBusDetect()
        {
            int superIOAddress = 0x2E;

            SuperIO.SuperIOEnter(superIOAddress);

            int val = (SuperIO.SuperIOReadByte(superIOAddress, SuperIOConstants.SIO_REG_DEVID) << 8)
                    | SuperIO.SuperIOReadByte(superIOAddress, SuperIOConstants.SIO_REG_DEVID + 1);

            switch (val & SuperIOConstants.SIO_ID_MASK)
            {
                case SuperIOConstants.SIO_NCT5577_ID:
                case SuperIOConstants.SIO_NCT6102_ID:
                case SuperIOConstants.SIO_NCT6793_ID:
                case SuperIOConstants.SIO_NCT6796_ID:
                case SuperIOConstants.SIO_NCT6798_ID:
                    //Create new NCT6775 bus and zero out the PCI ID information
                    var bus = new SMBusNCT6775();
                    bus.PCIVendor          = 0;
                    bus.PCIDevice          = 0;
                    bus.PCISubsystemVendor = 0;
                    bus.PCISubsystemDevice = 0;

                    //Set logical device register to get SMBus base address
                    SuperIO.SuperIOWriteByte(superIOAddress, SuperIOConstants.SIO_REG_LOGDEV, SuperIOConstants.SIO_LOGDEV_SMBUS);

                    //Get SMBus base address from configuration register
                    int smba = (SuperIO.SuperIOReadByte(superIOAddress, SuperIOConstants.SIO_REG_SMBA) << 8 )
                             | SuperIO.SuperIOReadByte(superIOAddress, SuperIOConstants.SIO_REG_SMBA + 1);

                    bus.NCT6775_SMBA = (ushort)smba;

                    //Set device name string
                    switch (val & SuperIOConstants.SIO_ID_MASK)
                    {
                        case SuperIOConstants.SIO_NCT5577_ID:
                            bus.DeviceName = $"Nuvoton NCT5577D SMBus at {smba:X2}";
                            break;
                        case SuperIOConstants.SIO_NCT6102_ID:
                            bus.DeviceName = $"Nuvoton NCT6102D/NCT6106D SMBus at {smba:X2}";
                            break;
                        case SuperIOConstants.SIO_NCT6793_ID:
                            bus.DeviceName = $"Nuvoton NCT6793D SMBus at {smba:X2}";
                            break;
                        case SuperIOConstants.SIO_NCT6796_ID:
                            bus.DeviceName = $"Nuvoton NCT6796D SMBus at {smba:X2}";
                            break;
                        case SuperIOConstants.SIO_NCT6798_ID:
                            bus.DeviceName = $"Nuvoton NCT6798D SMBus at {smba:X2}";
                            break;
                    }

                    SMBusManager.AddSMBus(bus);

                    break;
            }

            return true;
        }

        #endregion

        #region Private

        int NCT6775Access(ushort address, byte read_write, byte command, int size, SMBusData data)
        {
            DriverAccess.WriteIoPortByte(SMBHSTADD, NCT6775Constants.NCT6775_SOFT_RESET);

            byte length = 0;
            byte count = 0;

            switch (size)
            {
                case I2CConstants.I2C_SMBUS_QUICK:
                    DriverAccess.WriteIoPortByte(SMBHSTADD, (byte)((address << 1) | read_write));
                    break;
                case I2CConstants.I2C_SMBUS_BYTE_DATA:
                case I2CConstants.I2C_SMBUS_BYTE:
                    DriverAccess.WriteIoPortByte(SMBHSTADD, (byte)((address << 1) | read_write));
                    DriverAccess.WriteIoPortByte(SMBHSTIDX, command);

                    if (read_write ==  I2CConstants.I2C_SMBUS_WRITE)
                    {
                        DriverAccess.WriteIoPortByte(SMBHSTDAT, data.ByteData);
                        DriverAccess.WriteIoPortByte(SMBHSTCMD, NCT6775Constants.NCT6775_WRITE_BYTE);
                    }
                    else
                    {
                        DriverAccess.WriteIoPortByte(SMBHSTCMD, NCT6775Constants.NCT6775_READ_BYTE);
                    }
                    break;
                case I2CConstants.I2C_SMBUS_WORD_DATA:
                    DriverAccess.WriteIoPortByte(SMBHSTADD, (byte)((address << 1) | read_write));
                    DriverAccess.WriteIoPortByte(SMBHSTIDX, command);

                    if (read_write == I2CConstants.I2C_SMBUS_WRITE)
                    {
                        DriverAccess.WriteIoPortByte(SMBHSTDAT, (byte)(data.Word & 0xFF));
                        DriverAccess.WriteIoPortByte(SMBHSTDAT, (byte)((data.Word & 0xFF00) >> 8));
                        DriverAccess.WriteIoPortByte(SMBHSTCMD, NCT6775Constants.NCT6775_WRITE_WORD);
                    }
                    else
                    {
                        DriverAccess.WriteIoPortByte(SMBHSTCMD, NCT6775Constants.NCT6775_READ_WORD);
                    }
                    break;
                case I2CConstants.I2C_SMBUS_BLOCK_DATA:
                    DriverAccess.WriteIoPortByte(SMBHSTADD, (byte)((address << 1) | read_write));
                    DriverAccess.WriteIoPortByte(SMBHSTIDX, command);

                    if (read_write == I2CConstants.I2C_SMBUS_WRITE)
                    {
                        length = data[0];

                        if (length == 0 || length > I2CConstants.I2C_SMBUS_BLOCK_MAX)
                        {
                            return -SharedConstants.EINVAL;
                        }

                        DriverAccess.WriteIoPortByte(SMBBLKSZ, length);

                        //Load 4 bytes into FIFO
                        count = 1;

                        if (length >= 4)
                        {
                            for (int i = count; i <= 4; ++i)
                            {
                                DriverAccess.WriteIoPortByte(SMBHSTDAT, data[i]);
                            }

                            length -= 4;
                            count  += 4;
                        }
                        else
                        {
                            for (int i = count; i <= length; ++i)
                            {
                                DriverAccess.WriteIoPortByte(SMBHSTDAT, data[i]);
                            }

                            length = 0;
                        }

                        DriverAccess.WriteIoPortByte(SMBHSTCMD, NCT6775Constants.NCT6775_WRITE_BLOCK);
                    }
                    else
                    {
                        return -SharedConstants.EOPNOTSUPP;
                    }
                    break;
                default:
                    return -SharedConstants.EOPNOTSUPP;
            }

            DriverAccess.WriteIoPortByte(SMBHSTCTL, NCT6775Constants.NCT6775_MANUAL_START);

            while (size == I2CConstants.I2C_SMBUS_BLOCK_DATA && length > 0)
            {
                if (read_write == I2CConstants.I2C_SMBUS_WRITE)
                {
                    int timeout = 0;

                    while ((DriverAccess.ReadIoPortByte(SMBHSTSTS) & NCT6775Constants.NCT6775_FIFO_EMPTY) == 0)
                    {
                        if (timeout > SharedConstants.MAX_RETRIES)
                        {
                            return -SharedConstants.ETIMEDOUT;
                        }

                        Thread.Sleep(1);
                        ++timeout;
                    }

                    //Load more bytes into FIFO
                    if (length >= 4)
                    {
                        for (int i = count; i <= (count + 4); ++i)
                        {
                            DriverAccess.WriteIoPortByte(SMBHSTDAT, data[i]);
                        }

                        length -= 4;
                        count += 4;
                    }
                    else
                    {
                        for (int i = count; i <= (count + length); ++i)
                        {
                            DriverAccess.WriteIoPortByte(SMBHSTDAT, data[i]);
                        }

                        length = 0;
                    }
                }
                else
                {
                    return -SharedConstants.ENOTSUP;
                }
            }

            int timeoutX = 0;

            //Wait for manual mode to complete
            while ((DriverAccess.ReadIoPortByte(SMBHSTSTS) & NCT6775Constants.NCT6775_MANUAL_ACTIVE) != 0)
            {
                if (timeoutX > SharedConstants.MAX_RETRIES)
                {
                    return -SharedConstants.ETIMEDOUT;
                }

                Thread.Sleep(1);
                ++timeoutX;
            }

            if ((DriverAccess.ReadIoPortByte(SMBHSTERR) & NCT6775Constants.NCT6775_NO_ACK) != 0)
            {
                return -SharedConstants.EPROTO;
            }
            else if (read_write == I2CConstants.I2C_SMBUS_WRITE || size == I2CConstants.I2C_SMBUS_QUICK)
            {
                return 0;
            }

            switch (size)
            {
                case I2CConstants.I2C_SMBUS_QUICK:
                case I2CConstants.I2C_SMBUS_BYTE_DATA:
                    data.ByteData = DriverAccess.ReadIoPortByte(SMBHSTDAT);
                    break;
                case I2CConstants.I2C_SMBUS_WORD_DATA:
                    data.Word = (ushort)(DriverAccess.ReadIoPortByte(SMBHSTDAT) + (DriverAccess.ReadIoPortByte(SMBHSTDAT) << 8));
                    break;
            }

            return 0;
        }

        #endregion
    }
}
