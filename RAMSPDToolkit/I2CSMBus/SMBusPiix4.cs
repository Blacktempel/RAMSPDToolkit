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

using RAMSPDToolkit.I2CSMBus.Interop;
using RAMSPDToolkit.I2CSMBus.Interop.Piix4;
using RAMSPDToolkit.I2CSMBus.Interop.Shared;
using RAMSPDToolkit.Mutexes;
using RAMSPDToolkit.PCI;
using RAMSPDToolkit.Windows.Driver;
using OS = RAMSPDToolkit.Software.OperatingSystem;

namespace RAMSPDToolkit.I2CSMBus
{
    /// <summary>
    /// SMBus class for AMD CPUs (Piix4).
    /// </summary>
    public sealed class SMBusPiix4 : SMBusInterface
    {
        #region Constructor

        SMBusPiix4(uint pciAddress)
        {
            if (!OS.IsWindows())
            {
                throw new PlatformNotSupportedException();
            }

            _PCIAddress = pciAddress;
        }

        #endregion

        #region Fields

        /// <summary>
        /// PCI address of the SMBus controller. Used for PCICMD modifications.
        /// </summary>
        readonly uint _PCIAddress = PCIConstants.PCI_DEVICE_INVALID;

        #endregion

        #region Properties

        /// <summary>
        /// SMBus base address.
        /// </summary>
        public ushort Piix4_SMBA { get; private set; } = 0x0B00;

        // PIIX4 SMBus address offsets
        public ushort SMBHSTSTS  => (ushort)(  0 + Piix4_SMBA);
        public ushort SMBHSLVSTS => (ushort)(  1 + Piix4_SMBA);
        public ushort SMBHSTCNT  => (ushort)(  2 + Piix4_SMBA);
        public ushort SMBHSTCMD  => (ushort)(  3 + Piix4_SMBA);
        public ushort SMBHSTADD  => (ushort)(  4 + Piix4_SMBA);
        public ushort SMBHSTDAT0 => (ushort)(  5 + Piix4_SMBA);
        public ushort SMBHSTDAT1 => (ushort)(  6 + Piix4_SMBA);
        public ushort SMBBLKDAT  => (ushort)(  7 + Piix4_SMBA);
        public ushort SMBSLVCNT  => (ushort)(  8 + Piix4_SMBA);
        public ushort SMBSHDWCMD => (ushort)(  9 + Piix4_SMBA);
        public ushort SMBSLVEVT  => (ushort)(0xA + Piix4_SMBA);
        public ushort SMBSLVDAT  => (ushort)(0xC + Piix4_SMBA);

        #endregion

        #region I2CSMBusInterface

        protected override int I2CSMBusXfer(byte addr, byte read_write, byte command, int size, SMBusData data)
        {
            //Lock SMBus mutex
            using (var smbm = new WorldMutexGuard(WorldMutexManager.WorldSMBusMutex))
            {
                //Lock PCI mutex
                using (var pci = new WorldMutexGuard(WorldMutexManager.WorldPCIMutex))
                {
                    //Modify (if necessary) and restore PCICMD
                    using (var io = new PCICMDIOGuard(_PCIAddress))
                    {
                        var result = Piix4Access(addr, read_write, command, size, data);

                        return result;
                    }
                }
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
            var wmi = new WMI();

            // Query WMI for Win32_PnPSignedDriver entries with names matching "SMBUS" or "SM BUS"
            // These devices may be browsed under Device Manager -> System Devices
            var pnpSignedDriver = wmi.Query("SELECT * FROM Win32_PnPSignedDriver WHERE Description LIKE '%SMBUS%' OR Description LIKE '%SM BUS%'");

            if (pnpSignedDriver.Count == 0)
            {
                return false;
            }

            // For each detected SMBus adapter, try enumerating it as either AMD or Intel
            foreach (var item in pnpSignedDriver)
            {
                // AMD SMBus controllers do not show any I/O resources allocated in Device Manager
                // Analysis of many AMD boards has shown that AMD SMBus controllers have two adapters with fixed I/O spaces at 0x0B00 and 0x0B20
                // AMD SMBus adapters use the PIIX4 driver
                if (item["Manufacturer"].IndexOf("Advanced Micro Devices, Inc") != -1)
                {
                    var pnpStr = item["DeviceID"];

                    var venLoc = pnpStr.IndexOf("VEN_");
                    var devLoc = pnpStr.IndexOf("DEV_");
                    var subLoc = pnpStr.IndexOf("SUBSYS_");

                    if (venLoc != -1 && devLoc != -1 && subLoc != -1)
                    {
                        var venStr = pnpStr.Substring(venLoc + 4, 4);
                        var devStr = pnpStr.Substring(devLoc + 4, 4);
                        var sbvStr = pnpStr.Substring(subLoc + 11, 4);
                        var sbdStr = pnpStr.Substring(subLoc + 7, 4);

                        int venID = Convert.ToInt32(venStr, 16);
                        int devID = Convert.ToInt32(devStr, 16);
                        int sbvID = Convert.ToInt32(sbvStr, 16);
                        int sbdID = Convert.ToInt32(sbdStr, 16);

                        var pciAddress = DriverAccess.FindPciDeviceById((ushort)venID, (ushort)devID, 0);
                        if (pciAddress == PCIConstants.PCI_DEVICE_INVALID)
                        {
                            return false;
                        }

                        Create(pciAddress, 0x0B00, venID, devID, sbvID, sbdID, item["Description"] + " at 0x0B00");

                        Create(pciAddress, 0x0B20, venID, devID, sbvID, sbdID, item["Description"] + " at 0x0B20");
                    }
                }
            }

            return true;
        }

        #endregion

        #region Internal

        internal static void Create(uint pciAddress, ushort smba, int vendor, int device, int subsysVendor, int subsysDevice, string deviceName)
        {
            var piix4Bus = new SMBusPiix4(pciAddress);
            piix4Bus.PortID = SMBusManager.RegisteredSMBuses.Count; // Assign next available port ID
            piix4Bus.PCIVendor = vendor;
            piix4Bus.PCIDevice = device;
            piix4Bus.PCISubsystemVendor = subsysVendor;
            piix4Bus.PCISubsystemDevice = subsysDevice;
            piix4Bus.DeviceName = deviceName;

            piix4Bus.Piix4_SMBA = smba;

            SMBusManager.AddSMBus(piix4Bus);
        }

        #endregion

        #region Private

        int Piix4Access(ushort addr, byte read_write, byte command, int size, SMBusData data)
        {
            /* Make sure the SMBus host is ready to start transmitting */
            var temp = DriverAccess.ReadIoPortByte(SMBHSTSTS);

            if (temp != 0x00)
            {
                DriverAccess.WriteIoPortByte(SMBHSTSTS, temp);

                temp = DriverAccess.ReadIoPortByte(SMBHSTSTS);

                if (temp != 0x00)
                {
                    return -SharedConstants.EBUSY;
                }
            }

            switch (size)
            {
                case I2CConstants.I2C_SMBUS_QUICK:
                    DriverAccess.WriteIoPortByte(SMBHSTADD, (byte)((addr << 1) | read_write));
                    size = Piix4Constants.PIIX4_QUICK;
                    break;
                case I2CConstants.I2C_SMBUS_BYTE:
                    DriverAccess.WriteIoPortByte(SMBHSTADD, (byte)((addr << 1) | read_write));

                    if (read_write == I2CConstants.I2C_SMBUS_WRITE)
                    {
                        DriverAccess.WriteIoPortByte(SMBHSTCMD, command);
                    }

                    size = Piix4Constants.PIIX4_BYTE;
                    break;
                case I2CConstants.I2C_SMBUS_BYTE_DATA:
                    DriverAccess.WriteIoPortByte(SMBHSTADD, (byte)((addr << 1) | read_write));
                    DriverAccess.WriteIoPortByte(SMBHSTCMD, command);

                    if (read_write == I2CConstants.I2C_SMBUS_WRITE)
                    {
                        DriverAccess.WriteIoPortByte(SMBHSTDAT0, data.ByteData);
                    }

                    size = Piix4Constants.PIIX4_BYTE_DATA;
                    break;
                case I2CConstants.I2C_SMBUS_WORD_DATA:
                    DriverAccess.WriteIoPortByte(SMBHSTADD, (byte)((addr << 1) | read_write));
                    DriverAccess.WriteIoPortByte(SMBHSTCMD, command);

                    if (read_write == I2CConstants.I2C_SMBUS_WRITE)
                    {
                        DriverAccess.WriteIoPortByte(SMBHSTDAT0, (byte)(data.Word & 0xFF));
                        DriverAccess.WriteIoPortByte(SMBHSTDAT1, (byte)((data.Word & 0xFF00) >> 8));
                    }

                    size = Piix4Constants.PIIX4_WORD_DATA;
                    break;
                case I2CConstants.I2C_SMBUS_BLOCK_DATA:
                    DriverAccess.WriteIoPortByte(SMBHSTADD, (byte)((addr << 1) | read_write));
                    DriverAccess.WriteIoPortByte(SMBHSTCMD, command);

                    if (read_write == I2CConstants.I2C_SMBUS_WRITE)
                    {
                        var length = data[0];

                        if (length == 0 || length > I2CConstants.I2C_SMBUS_BLOCK_MAX)
                        {
                            return -SharedConstants.EINVAL;
                        }

                        DriverAccess.WriteIoPortByte(SMBHSTDAT0, length);
                        DriverAccess.ReadIoPortByte(SMBHSTCNT);

                        for (int i = 1; i <= length; ++i)
                        {
                            DriverAccess.WriteIoPortByte(SMBBLKDAT, data[i]);
                        }
                    }

                    size = Piix4Constants.PIIX4_BLOCK_DATA;
                    break;
                default:
                    return -SharedConstants.EOPNOTSUPP;
            }

            DriverAccess.WriteIoPortByte(SMBHSTCNT, (byte)(size & 0x1C));

            var status = Piix4Transaction();

            if (status != 0)
            {
                return status;
            }

            if ((read_write == I2CConstants.I2C_SMBUS_WRITE) || (size == Piix4Constants.PIIX4_QUICK))
            {
                return 0;
            }

            switch (size)
            {
                case Piix4Constants.PIIX4_BYTE:
                case Piix4Constants.PIIX4_BYTE_DATA:
                    data.ByteData = DriverAccess.ReadIoPortByte(SMBHSTDAT0);
                    break;
                case Piix4Constants.PIIX4_WORD_DATA:
                    data.Word = (ushort)(DriverAccess.ReadIoPortByte(SMBHSTDAT0) + (DriverAccess.ReadIoPortByte(SMBHSTDAT1) << 8));
                    break;
                case Piix4Constants.PIIX4_BLOCK_DATA:
                    data[0] = DriverAccess.ReadIoPortByte(SMBHSTDAT0);

                    if (data[0] == 0 || data[0] > I2CConstants.I2C_SMBUS_BLOCK_MAX)
                    {
                        return -SharedConstants.EPROTO;
                    }

                    DriverAccess.ReadIoPortByte(SMBHSTCNT);
                    for (int i = 1; i <= data[0]; ++i)
                    {
                        data[i] = DriverAccess.ReadIoPortByte(SMBBLKDAT);
                    }
                    break;
            }

            return 0;
        }

        //Logic adapted from piix4_transaction() in i2c-piix4.c
        int Piix4Transaction()
        {
            /* start the transaction by setting bit 6 */
            var temp = DriverAccess.ReadIoPortByte(SMBHSTCNT);
            DriverAccess.WriteIoPortByte(SMBHSTCNT, (byte)(temp | 0x040));

            int timeout = 0;

            /* We will always wait for a fraction of a second! (See PIIX4 docs errata) */

            /*---------------------------------------------------------------------------------------------------------------*\
            | The above comment from 2002 or earlier is still relevant for the Zen 4 architecture.                            |
            | AMD takes bug-for-bug compatibility seriously. It's the least they can do since they've classified Ryzen        |
            | system programming documentation as trade secret.                                                               |
            |                                                                                                                 |
            | Instead of just waiting for an unspecified amount of time, we try to follow exactly what the                    |
            | Intel 83271 Specification Update says about SMBHSTSTS.                                                          |
            |                                                                                                                 |
            |-----------------------------------------------------------------------------------------------------------------|
            | [Bit] 1  SMBus Interrupt/Host Completion (INTER)—R/WC.                                                          |
            | 1 = Indicates that the host transaction has completed or that the source of an SMBus interrupt was the          |
            | completion of the last host command.                                                                            |
            | 0 = Host transaction has not completed or that an SMBus interrupt was not caused by host command completion.    |
            | This bit is only set by hardware and can only be reset by writing a 1 to this bit position.                     |
            |-----------------------------------------------------------------------------------------------------------------|
            | [Bit] 0  Host Busy (HOST_BUSY)—RO.                                                                              |
            | 1 = Indicates that the SMBus controller host interface is in the process of completing a command.               |
            | 0 = SMBus controller host interface is not processing a command. None of the other registers should be accessed |
            | if this bit is set. Note that there may be moderate latency before the transaction begins and the Host Busy bit |
            | gets set.                                                                                                       |
            \*---------------------------------------------------------------------------------------------------------------*/

            var sleepTime = TimeSpan.FromMilliseconds(0.25);

            do
            {
                Thread.Sleep(sleepTime);
                temp = DriverAccess.ReadIoPortByte(SMBHSTSTS);
            }
            while ((++timeout < Piix4Constants.MAX_TIMEOUT) && ((temp & 0x03) != 0x02));

            int result = 0;

            /* If the SMBus is still busy, we give up */
            if ((temp & 0x01) != 0)
            {
                result = -SharedConstants.ETIMEDOUT;
            }

            if ((temp & 0x10) != 0)
            {
                result = -SharedConstants.EIO;
            }

            if ((temp & 0x08) != 0)
            {
                //Bus collision - SMBus may be locked until next hard reset
                result = -SharedConstants.EIO;
            }

            if ((temp & 0x04) != 0)
            {
                result = -SharedConstants.ENXIO;
            }

            temp = DriverAccess.ReadIoPortByte(SMBHSTSTS);

            if (temp != 0x00)
            {
                DriverAccess.WriteIoPortByte(SMBHSTSTS, temp);
            }

            return result;
        }

        #endregion
    }
}
