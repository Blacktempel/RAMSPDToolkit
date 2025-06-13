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
using RAMSPDToolkit.I2CSMBus.Interop.Intel;
using RAMSPDToolkit.I2CSMBus.Interop.Shared;
using RAMSPDToolkit.Logging;
using RAMSPDToolkit.Mutexes;
using RAMSPDToolkit.PCI;
using RAMSPDToolkit.Windows.Driver;
using System.Text.RegularExpressions;

using OS = RAMSPDToolkit.Software.OperatingSystem;

namespace RAMSPDToolkit.I2CSMBus
{
    /// <summary>
    /// SMBus class for Intel CPUs (I801).
    /// </summary>
    public sealed class SMBusI801 : SMBusInterface
    {
        #region Constructor

        SMBusI801(uint pciAddress)
        {
            if (!OS.IsWindows())
            {
                throw new PlatformNotSupportedException();
            }

            _PCIAddress = pciAddress;

            _PCICMDOriginal = DriverAccess.ReadPciConfigWord(_PCIAddress, SharedConstants.PCICMD);

            LogSimple.LogTrace($"{nameof(SMBusI801)}: PCICMD is 0x{_PCICMDOriginal:X4} ({_PCICMDOriginal}).");
        }

        #endregion

        #region Fields

        /// <summary>
        /// PCI address of the SMBus controller. Used for PCICMD modifications.
        /// </summary>
        readonly uint _PCIAddress = PCIConstants.PCI_DEVICE_INVALID;

        /// <summary>
        /// Initial value of the PCI Command Register (PCICMD) for this SMBus controller.
        /// </summary>
        readonly ushort _PCICMDOriginal;

        #endregion

        #region Properties

        /// <summary>
        /// Base address for SMBus.
        /// </summary>
        public ushort I801_SMBA { get; private set; } = 0xF000;

        public ushort SMBHSTSTS  => (ushort)( 0u + I801_SMBA);
        public ushort SMBHSTCNT  => (ushort)( 2u + I801_SMBA);
        public ushort SMBHSTCMD  => (ushort)( 3u + I801_SMBA);
        public ushort SMBHSTADD  => (ushort)( 4u + I801_SMBA);
        public ushort SMBHSTDAT0 => (ushort)( 5u + I801_SMBA);
        public ushort SMBHSTDAT1 => (ushort)( 6u + I801_SMBA);
        public ushort SMBBLKDAT  => (ushort)( 7u + I801_SMBA);
        public ushort SMBPEC     => (ushort)( 8u + I801_SMBA); /* ICH3 and later */
        public ushort SMBAUXSTS  => (ushort)(12u + I801_SMBA); /* ICH4 and later */
        public ushort SMBAUXCTL  => (ushort)(13u + I801_SMBA); /* ICH4 and later */
        public ushort SMBSLVSTS  => (ushort)(16u + I801_SMBA); /* ICH3 and later */
        public ushort SMBSLVCMD  => (ushort)(17u + I801_SMBA); /* ICH3 and later */
        public ushort SMBNTFDADD => (ushort)(20u + I801_SMBA); /* ICH3 and later */

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
                    using (var io = new PCICMDIOGuard(_PCIAddress, _PCICMDOriginal))
                    {
                        var result = i801Access(addr, read_write, command, size, data);

                        return result;
                    }
                }
            }
        }

        protected override int I2CXfer(byte addr, byte read_write, int? size, byte[] data)
        {
            throw new NotImplementedException();
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
                // Intel SMBus controllers do show I/O resources in Device Manager
                // Analysis of many Intel boards has shown that Intel SMBus adapter I/O space varies between boards
                // We can query Win32_PnPAllocatedResource entries and look up the PCI device ID to find the allocated I/O space
                // Intel SMBus adapters use the i801 driver
                if (item["Manufacturer"].IndexOf("Intel") != -1 ||
                    item["Manufacturer"].IndexOf("INTEL") != -1)
                {
                    var regex1 = new Regex($".+{pnpSignedDriver[0]["DeviceID"].Substring(4, 33)}.+");

                    var filters = new Dictionary<string, Regex>
                    {
                        { "Dependent" , regex1                },
                        { "Antecedent", new Regex(".*Port.*") },
                    };

                    var pnpAllocatedResource = wmi.Query("SELECT * FROM Win32_PnPAllocatedResource", filters);

                    var regex2 = new Regex(".*StartingAddress=\"(\\d+)\".*");

                    // Query the StartingAddress for the matching device ID and use it to enumerate the bus
                    if (pnpAllocatedResource.Count != 0)
                    {
                        var inputValue = pnpAllocatedResource[0]["Antecedent"];

                        var match = regex2.Match(inputValue);
                        if (match.Success)
                        {
                            //Address of SMBus detected in WMI
                            uint ioRangeStart = uint.Parse(match.Groups[1].Value);

                            TryAddSMBus(item, ioRangeStart);
                        }
                        else //Try adding by fixed address
                        {
                            LogSimple.LogTrace($"No {nameof(match)} found for '{inputValue}'.");

                            if (TryAddSMBus(item, IntelConstants.SMBUS_FIXED_ADDRESS))
                            {
                                LogSimple.LogTrace($"Detected SMBus at fixed address '0x{IntelConstants.SMBUS_FIXED_ADDRESS:X2}'.");
                            }
                        }
                    }
                    else //Try adding by fixed address
                    {
                        LogSimple.LogTrace($"No {nameof(pnpAllocatedResource)} found.");

                        if (TryAddSMBus(item, IntelConstants.SMBUS_FIXED_ADDRESS))
                        {
                            LogSimple.LogTrace($"Detected SMBus at fixed address '0x{IntelConstants.SMBUS_FIXED_ADDRESS:X2}'.");
                        }
                    }
                }
            }

            return true;
        }

        #endregion

        #region Internal

        internal static void Create(uint pciAddress, ushort smba, byte hostConfig, int vendor, int device, int subsysVendor, int subsysDevice, string deviceName)
        {
            var intelBus = new SMBusI801(pciAddress);
            intelBus.PortID = SMBusManager.RegisteredSMBuses.Count; // Assign next available port ID
            intelBus.PCIVendor = vendor;
            intelBus.PCIDevice = device;
            intelBus.PCISubsystemVendor = subsysVendor;
            intelBus.PCISubsystemDevice = subsysDevice;
            intelBus.DeviceName = deviceName;

            intelBus.I801_SMBA = smba;

            //Check if write protection is enabled
            intelBus.HasSPDWriteProtection = (hostConfig & IntelConstants.SMBHSTCFG_SPD_WD) != 0;

            SMBusManager.AddSMBus(intelBus);
        }

        #endregion

        #region Private

        static bool TryAddSMBus(Dictionary<string, string> item, uint ioRangeStart)
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

                var hostConfig = (byte)DriverAccess.ReadPciConfigWord(pciAddress, IntelConstants.SMBHSTCFG);
                if ((hostConfig & IntelConstants.SMBHSTCFG_HST_EN) == 0)
                {
                    return false;
                }

                Create(pciAddress, (ushort)ioRangeStart, hostConfig, venID, devID, sbvID, sbdID, item["Description"]);

                return true;
            }

            return false;
        }

        int i801Access(ushort addr, byte read_write, byte command, int size, SMBusData data)
        {
            int busy = i801CheckPre();
            if (busy < 0)
            {
                return busy;
            }

            int hwpec = 0;
            int block = 0;
            int ret   = 0;
            int xact  = 0;

            switch (size)
            {
                case I2CConstants.I2C_SMBUS_QUICK:
                    DriverAccess.WriteIoPortByte(SMBHSTADD, (byte)(((addr & 0x7F) << 1) | (read_write & 0x01)));
                    xact = IntelConstants.I801_QUICK;
                    break;
                case I2CConstants.I2C_SMBUS_BYTE:
                    DriverAccess.WriteIoPortByte(SMBHSTADD, (byte)(((addr & 0x7F) << 1) | (read_write & 0x01)));

                    if (read_write == I2CConstants.I2C_SMBUS_WRITE)
                    {
                        DriverAccess.WriteIoPortByte(SMBHSTCMD, command);
                    }

                    xact = IntelConstants.I801_BYTE;
                    break;
                case I2CConstants.I2C_SMBUS_BYTE_DATA:
                    DriverAccess.WriteIoPortByte(SMBHSTADD, (byte)(((addr & 0x7F) << 1) | (read_write & 0x01)));
                    DriverAccess.WriteIoPortByte(SMBHSTCMD, command);

                    if (read_write == I2CConstants.I2C_SMBUS_WRITE)
                    {
                        DriverAccess.WriteIoPortByte(SMBHSTDAT0, data.ByteData);
                    }

                    xact = IntelConstants.I801_BYTE_DATA;
                    break;
                case I2CConstants.I2C_SMBUS_WORD_DATA:
                    DriverAccess.WriteIoPortByte(SMBHSTADD, (byte)(((addr & 0x7F) << 1) | (read_write & 0x01)));
                    DriverAccess.WriteIoPortByte(SMBHSTCMD, command);

                    if (read_write == I2CConstants.I2C_SMBUS_WRITE)
                    {
                        DriverAccess.WriteIoPortByte(SMBHSTDAT0, (byte)(data.Word & 0xFF));
                        DriverAccess.WriteIoPortByte(SMBHSTDAT1, (byte)((data.Word & 0xFF00) >> 8));
                    }

                    xact = IntelConstants.I801_WORD_DATA;
                    break;
                case I2CConstants.I2C_SMBUS_BLOCK_DATA:
                    DriverAccess.WriteIoPortByte(SMBHSTADD, (byte)(((addr & 0x7F) << 1) | (read_write & 0x01)));
                    DriverAccess.WriteIoPortByte(SMBHSTCMD, command);

                    block = 1;
                    break;
                case I2CConstants.I2C_SMBUS_I2C_BLOCK_DATA:
                    /*
                    * Page 240 of ICH5 datasheet shows that the R/#W
                    * bit should be cleared here, even when reading.
                    * However if SPD Write Disable is set (Lynx Point and later),
                    * the read will fail if we don't set the R/#W bit.
                    */
                    DriverAccess.WriteIoPortByte(SMBHSTADD, (byte)(((addr & 0x7F) << 1) | (read_write & 0x01)));

                    if (read_write == I2CConstants.I2C_SMBUS_READ)
                    {
                        /* Page 240 of ICH5 datasheet also shows
                        * that DATA1 is the cmd field when reading */
                        DriverAccess.WriteIoPortByte(SMBHSTDAT1, command);
                    }
                    else
                    {
                        DriverAccess.WriteIoPortByte(SMBHSTCMD, command);
                    }

                    block = 1;
                    break;
                default:
                    return -SharedConstants.EOPNOTSUPP;
            }

            DriverAccess.WriteIoPortByte(SMBAUXCTL,
                (byte)
                (
                    DriverAccess.ReadIoPortByte(SMBAUXCTL) &
                    (~IntelConstants.SMBAUXCTL_CRC)
                )
            );

            if (block != 0)
            {
                ret = i801BlockTransaction(data, read_write, size, hwpec);
            }
            else
            {
                ret = i801Transaction(xact);
            }

            /* Some BIOSes don't like it when PEC is enabled at reboot or resume
            time, so we forcibly disable it after every transaction. Turn off
            E32B for the same reason. */
            DriverAccess.WriteIoPortByte(SMBAUXCTL,
                (byte)
                (
                    DriverAccess.ReadIoPortByte(SMBAUXCTL) &
                    ~(IntelConstants.SMBAUXCTL_CRC | IntelConstants.SMBAUXCTL_E32B)
                )
            );

            if (block != 0)
                return ret;
            if (ret != 0)
                return ret;
            if ((read_write == I2CConstants.I2C_SMBUS_WRITE) || (xact == IntelConstants.I801_QUICK))
                return ret;

            switch (xact & 0x7F)
            {
                case IntelConstants.I801_BYTE: /* Result put in SMBHSTDAT0 */
                case IntelConstants.I801_BYTE_DATA:
                    data.ByteData = DriverAccess.ReadIoPortByte(SMBHSTDAT0);
                    break;
                case IntelConstants.I801_WORD_DATA:
                    data.Word = (ushort)(DriverAccess.ReadIoPortByte(SMBHSTDAT0) + (DriverAccess.ReadIoPortByte(SMBHSTDAT1) << 8));
                    break;
            }

            return ret;
        }

        int i801BlockTransaction(SMBusData data, byte read_write, int command, int hwpec)
        {
            int result = 0;

            if (read_write == I2CConstants.I2C_SMBUS_WRITE || command == I2CConstants.I2C_SMBUS_I2C_BLOCK_DATA)
            {
                if (data[0] < 1)
                {
                    data[0] = 1;
                }
                if (data[0] > I2CConstants.I2C_SMBUS_BLOCK_MAX)
                {
                    data[0] = I2CConstants.I2C_SMBUS_BLOCK_MAX;
                }
            }
            else
            {
                data[0] = 32; /* max for SMBus block reads */
            }

            result = i801BlockTransactionByteByByte(data, read_write, command, hwpec);

            return result;
        }

        int i801Transaction(int xact)
        {
            DriverAccess.WriteIoPortByte(SMBHSTCNT, (byte)(DriverAccess.ReadIoPortByte(SMBHSTCNT) & ~IntelConstants.SMBHSTCNT_INTREN));

            /* the current contents of SMBHSTCNT can be overwritten, since PEC,
            * SMBSCMD are passed in xact */
            DriverAccess.WriteIoPortByte(SMBHSTCNT, (byte)(xact | IntelConstants.SMBHSTCNT_START));

            var status = i801WaitIntr();
            return i801CheckPost(status);
        }

        /*
        * For "byte-by-byte" block transactions:
        *   I2C write uses cmd = I801_BLOCK_DATA, I2C_EN = 1
        *   I2C read uses cmd = I801_I2C_BLOCK_DATA
        */
        int i801BlockTransactionByteByByte(SMBusData data, byte read_write, int command, int hwpec)
        {
            int result = i801CheckPre();

            if (result < 0)
            {
                return result;
            }

            var len = data[0];

            if (read_write == I2CConstants.I2C_SMBUS_WRITE)
            {
                DriverAccess.WriteIoPortByte(SMBHSTDAT0, len);
                DriverAccess.WriteIoPortByte(SMBBLKDAT, data[1]);
            }

            int smbcmd;
            if (command == I2CConstants.I2C_SMBUS_I2C_BLOCK_DATA && read_write == I2CConstants.I2C_SMBUS_READ)
            {
                smbcmd = IntelConstants.I801_I2C_BLOCK_DATA;
            }
            else
            {
                smbcmd = IntelConstants.I801_BLOCK_DATA;
            }

            int status;
            for (int i = 1; i <= len; ++i)
            {
                if (i == len && read_write == I2CConstants.I2C_SMBUS_READ)
                {
                    smbcmd |= IntelConstants.SMBHSTCNT_LAST_BYTE;
                }

                DriverAccess.WriteIoPortByte(SMBHSTCNT, (byte)smbcmd);

                if (i == 1)
                {
                    DriverAccess.WriteIoPortByte(SMBHSTCNT, (byte)(DriverAccess.ReadIoPortByte(SMBHSTCNT) | IntelConstants.SMBHSTCNT_START));
                }

                status = i801WaitByteDone();
                if (status != 0)
                {
                    return i801CheckPost(status);
                }

                if (i == 1 && read_write == I2CConstants.I2C_SMBUS_READ && command != I2CConstants.I2C_SMBUS_I2C_BLOCK_DATA)
                {
                    //Read the block length from SMBHSTDAT0
                    len = DriverAccess.ReadIoPortByte(SMBHSTDAT0);

                    if (len < 1 || len > I2CConstants.I2C_SMBUS_BLOCK_MAX)
                    {
                        /* Recover */
                        while ((DriverAccess.ReadIoPortByte(SMBHSTSTS) & IntelConstants.SMBHSTSTS_HOST_BUSY) != 0)
                        {
                            DriverAccess.WriteIoPortByte(SMBHSTSTS, IntelConstants.SMBHSTSTS_BYTE_DONE);
                        }

                        DriverAccess.WriteIoPortByte(SMBHSTSTS, IntelConstants.SMBHSTSTS_INTR);
                        return -SharedConstants.EPROTO;
                    }

                    data[0] = len;
                }

                /* Retrieve/store value in SMBBLKDAT */
                if (read_write == I2CConstants.I2C_SMBUS_READ)
                {
                    data[i] = DriverAccess.ReadIoPortByte(SMBBLKDAT);
                }

                if (read_write == I2CConstants.I2C_SMBUS_WRITE && i + 1 <= len)
                {
                    DriverAccess.WriteIoPortByte(SMBBLKDAT, data[i + 1]);
                }

                /* signals SMBBLKDAT ready */
                DriverAccess.WriteIoPortByte(SMBHSTSTS, IntelConstants.SMBHSTSTS_BYTE_DONE);
            }

            status = i801WaitIntr();

            return i801CheckPost(status);
        }

        /* Make sure the SMBus host is ready to start transmitting.
           Return 0 if it is, -EBUSY if it is not. */
        int i801CheckPre()
        {
            int status = DriverAccess.ReadIoPortByte(SMBHSTSTS);
            if ((status & IntelConstants.SMBHSTSTS_HOST_BUSY) != 0)
            {
                return -SharedConstants.EBUSY;
            }

            status &= IntelConstants.STATUS_FLAGS;
            if (status != 0)
            {
                //Clear status flags
                DriverAccess.WriteIoPortByte(SMBHSTSTS, (byte)status);

                status = DriverAccess.ReadIoPortByte(SMBHSTSTS) & IntelConstants.STATUS_FLAGS;

                if (status != 0)
                {
                    return -SharedConstants.EBUSY;
                }
            }

            return 0;
        }

        int i801WaitByteDone()
        {
            int timeout = 0;
            int status;

            /* We will always wait for a fraction of a second! */
            do
            {
                Thread.Sleep(1);

                status = DriverAccess.ReadIoPortByte(SMBHSTSTS);
            } while
            (
                (status & (IntelConstants.STATUS_ERROR_FLAGS | IntelConstants.SMBHSTSTS_BYTE_DONE)) == 0 &&
                (timeout++ < SharedConstants.MAX_RETRIES)
            );

            if (timeout > SharedConstants.MAX_RETRIES)
            {
                return -SharedConstants.ETIMEDOUT;
            }

            return status & IntelConstants.STATUS_ERROR_FLAGS;
        }

        int i801CheckPost(int status)
        {
            int result = 0;

            /*
            * If the SMBus is still busy, we give up
            * Note: This timeout condition only happens when using polling
            * transactions.  For interrupt operation, NAK/timeout is indicated by
            * DEV_ERR.
            */
            if (status < 0)
            {
                /* try to stop the current command */
                DriverAccess.WriteIoPortByte(SMBHSTCNT, (byte)(DriverAccess.ReadIoPortByte(SMBHSTCNT) | IntelConstants.SMBHSTCNT_KILL));

                Thread.Sleep(1);

                DriverAccess.WriteIoPortByte(SMBHSTCNT, (byte)(DriverAccess.ReadIoPortByte(SMBHSTCNT) & (~IntelConstants.SMBHSTCNT_KILL)));

                DriverAccess.WriteIoPortByte(SMBHSTSTS, IntelConstants.STATUS_FLAGS);
                return -SharedConstants.ETIMEDOUT;
            }

            if ((status & IntelConstants.SMBHSTSTS_FAILED) != 0)
            {
                result = -SharedConstants.EIO;
            }

            if ((status & IntelConstants.SMBHSTSTS_DEV_ERR) != 0)
            {
                result = -SharedConstants.ENXIO;
            }

            if ((status & IntelConstants.SMBHSTSTS_BUS_ERR) != 0)
            {
                result = -SharedConstants.EAGAIN;
            }

            //Clear status flags
            DriverAccess.WriteIoPortByte(SMBHSTSTS, (byte)status);

            return result;
        }

        /* Wait for BUSY being cleared and either INTR or an error flag being set */
        int i801WaitIntr()
        {
            int timeout = 0;
            int status;

            var sleepTime = TimeSpan.FromMilliseconds(0.25);

            do
            {
                /* We will always wait for a fraction of a second! */
                Thread.Sleep(sleepTime);

                status = DriverAccess.ReadIoPortByte(SMBHSTSTS);
            } while
            (
                (
                    (status & IntelConstants.SMBHSTSTS_HOST_BUSY) != 0 ||
                    (status & (IntelConstants.STATUS_ERROR_FLAGS | IntelConstants.SMBHSTSTS_INTR)) == 0
                ) &&
                (timeout++ < SharedConstants.MAX_RETRIES)
            );

            if (timeout > SharedConstants.MAX_RETRIES)
            {
                return -SharedConstants.ETIMEDOUT;
            }

            return status & (IntelConstants.STATUS_ERROR_FLAGS | IntelConstants.SMBHSTSTS_INTR);
        }

        #endregion
    }
}
