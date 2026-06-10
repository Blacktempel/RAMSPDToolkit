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

using BlackSharp.Core.Extensions;
using BlackSharp.Core.Interop.Windows.Mutexes;
using RAMSPDToolkit.I2CSMBus.Interfaces;
using RAMSPDToolkit.I2CSMBus.Interop;
using RAMSPDToolkit.I2CSMBus.Interop.Intel;
using RAMSPDToolkit.I2CSMBus.Interop.Linux;
using RAMSPDToolkit.I2CSMBus.Interop.Shared;
using RAMSPDToolkit.Logging;
using RAMSPDToolkit.Mutexes;
using RAMSPDToolkit.PCI;
using RAMSPDToolkit.PCI.Linux;
using RAMSPDToolkit.Windows.Driver;
using OS = BlackSharp.Core.Platform.OperatingSystem;

namespace RAMSPDToolkit.I2CSMBus
{
    /// <summary>
    /// SMBus class for Intel Skylake IMC SMBus controllers.
    /// </summary>
    public class SMBusSkylakeIMC : SMBusInterface, IIntelSkylakeIMCSMBus
    {
        #region Constructor

        SMBusSkylakeIMC(uint pciAddress, byte smbusIndex)
        {
            _PCIAddress = pciAddress;
            SMBusIndex = smbusIndex;
        }

        #endregion

        #region Fields

        readonly uint _PCIAddress = PCIConstants.PCI_DEVICE_INVALID;

        #endregion

        #region Properties

        public byte SMBusIndex { get; }

        uint CmdReg => (uint)(IMCConstants.CmdBase + SMBusIndex * IMCConstants.RegStep);
        uint StsReg => (uint)(IMCConstants.StsBase + SMBusIndex * IMCConstants.RegStep);
        uint DatReg => (uint)(IMCConstants.DatBase + SMBusIndex * IMCConstants.RegStep);

        #endregion

        #region I2CSMBusInterface

        protected override int I2CSMBusXfer(byte addr, byte read_write, byte command, int size, SMBusData data)
        {
            int result;

            if (OS.IsWindows())
            {
                //The global SMBus mutex has to be used to stay compliant with other software.
                //For example we have received confirmation of HWiNFO and SIV to use the SMBus mutex for this access.

                //Lock SMBus mutex
                using (var pci = new WorldMutexGuard(WorldMutexManager.WorldSMBusMutex))
                {
                    result = I2CSMBusXferCore(addr, read_write, command, size, data);
                }
            }
            else if (OS.IsLinux())
            {
                result = I2CSMBusXferCore(addr, read_write, command, size, data);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }

            return result;
        }

        protected override int I2CXfer(byte addr, byte read_write, int? size, byte[] data)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Public

        public bool SetBank(byte bankIndex)
        {
            if (OS.IsWindows())
            {
                //The global SMBus mutex has to be used to stay compliant with other software.
                //For example we have received confirmation of HWiNFO and SIV to use the SMBus mutex for this access.
                using (var pci = new WorldMutexGuard(WorldMutexManager.WorldSMBusMutex))
                {
                    return SetBankCore(bankIndex);
                }
            }
            else if (OS.IsLinux())
            {
                return SetBankCore(bankIndex);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
        }

        public static bool SMBusDetect()
        {
            //Intel IMC SMBus Device IDs range from 0x2080 to 0x208E
            for (ushort i = 0x2080; i < 0x208F; ++i)
            {
                //Hard filter for known Device ID
                if (i != 0x2085)
                {
                    continue;
                }

                //Construct PCI address
                var pciAddress = FindPciDeviceById(PCIConstants.PCI_VENDOR_INTEL, i, 0);

                if (pciAddress != 0 && pciAddress != PCIConstants.PCI_DEVICE_INVALID)
                {
                    //Check for each possible SMBus controller
                    for (byte smbusIndex = 0; smbusIndex < IMCConstants.MaxSMBusControllers; ++smbusIndex)
                    {
                        uint cmdOff = (uint)(IMCConstants.CmdBase + smbusIndex * IMCConstants.RegStep);
                        uint stsOff = (uint)(IMCConstants.StsBase + smbusIndex * IMCConstants.RegStep);
                        uint datOff = (uint)(IMCConstants.DatBase + smbusIndex * IMCConstants.RegStep);

                        uint cmd = 0;
                        uint sts = 0;
                        uint dat = 0;

                        if (!ReadPciConfigDwordEx(pciAddress, cmdOff, ref cmd)
                         || !ReadPciConfigDwordEx(pciAddress, stsOff, ref sts)
                         || !ReadPciConfigDwordEx(pciAddress, datOff, ref dat))
                        {
                            continue;
                        }

                        //Filter invalid
                        if (cmd == 0xFFFF_FFFF || sts == 0xFFFF_FFFF || dat == 0xFFFF_FFFF)
                        {
                            continue;
                        }

                        //Filter invalid
                        if (cmd == 0x0000_0000 && sts == 0x0000_0000 && dat == 0x0000_0000)
                        {
                            continue;
                        }

                        LogSimple.LogTrace($"{nameof(SMBusSkylakeIMC)}.{nameof(SMBusDetect)}: Detected at PCI Device ID 0x{i:X4}, SMBus Index {smbusIndex}.");

                        //Create SMBus
                        var smbus = new SMBusSkylakeIMC(pciAddress, smbusIndex)
                        {
                            PortID = smbusIndex,
                            PCIVendor = PCIConstants.PCI_VENDOR_INTEL,
                            PCIDevice = i,
                        };

                        SMBusManager.AddSMBus(smbus);
                    }
                }
            }

            return true;
        }

        #endregion

        #region Private

        static uint FindPciDeviceById(ushort vendorId, ushort deviceId, byte index)
        {
            if (OS.IsWindows())
            {
                return DriverAccess.FindPciDeviceById(vendorId, deviceId, index);
            }
            else if (OS.IsLinux())
            {
                const string PCIRoot = "/sys/bus/pci/devices";

                byte matchCount = 0;

                foreach (var devDir in Directory.EnumerateDirectories(PCIRoot))
                {
                    var vendorPath = Path.Combine(devDir, "vendor");
                    var devicePath = Path.Combine(devDir, "device");

                    if (!File.Exists(vendorPath) || !File.Exists(devicePath))
                    {
                        continue;
                    }

                    if (!LinuxUtilityMethods.TryReadHex16(vendorPath, out ushort ven)
                     || !LinuxUtilityMethods.TryReadHex16(devicePath, out ushort dev))
                    {
                        continue;
                    }

                    if (ven == vendorId && dev == deviceId)
                    {
                        if (matchCount == index)
                        {
                            //Example: "0000:00:1f.3"
                            var bdfStr = Path.GetFileName(devDir);

                            if (!LinuxUtilityMethods.TryParseBusDeviceFunction(bdfStr, out int bus, out int device, out int function))
                            {
                                continue;
                            }

                            uint pciAddress = (uint)((bus << 8) | (device << 3) | function);
                            return pciAddress;
                        }

                        ++matchCount;
                    }
                }

                return PCIConstants.PCI_DEVICE_INVALID;
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
        }

        static bool ReadPciConfigDwordEx(uint pciAddress, uint regAddress, ref uint value)
        {
            if (OS.IsWindows())
            {
                return DriverAccess.ReadPciConfigDwordEx(pciAddress, regAddress, ref value);
            }
            else if (OS.IsLinux())
            {
                //Decode pciAddress
                int bus = (int)((pciAddress >> 8) & 0xFF);
                int device = (int)((pciAddress >> 3) & 0x1F);
                int function = (int)(pciAddress & 0x07);

                var path = $"/sys/bus/pci/devices/0000:{bus:X2}:{device:X2}.{function:X1}";

                if (!PCIConfigAccessor.ReadPCIConfigInt32(path, (int)regAddress, out var val))
                {
                    return false;
                }

                value = val;

                return true;
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
        }

        static void WritePciConfigDwordEx(uint pciAddress, uint regAddress, uint value)
        {
            if (OS.IsWindows())
            {
                DriverAccess.WritePciConfigDwordEx(pciAddress, regAddress, value);
            }
            else if (OS.IsLinux())
            {
                //Decode pciAddress
                int bus = (int)((pciAddress >> 8) & 0xFF);
                int device = (int)((pciAddress >> 3) & 0x1F);
                int function = (int)(pciAddress & 0x07);

                var path = $"/sys/bus/pci/devices/0000:{bus:X2}:{device:X2}.{function:X1}";

                PCIConfigAccessor.WritePCIConfigInt32(path, (int)regAddress, value);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
        }

        int I2CSMBusXferCore(byte addr, byte read_write, byte command, int size, SMBusData data)
        {
            if (IsDDR4PageSelectorAddress(addr))
            {
                if (read_write != I2CConstants.I2C_SMBUS_WRITE)
                {
                    return -SharedConstants.ENOTSUP;
                }

                if (size.AnyOf(I2CConstants.I2C_SMBUS_QUICK, I2CConstants.I2C_SMBUS_BYTE_DATA))
                {
                    return SetBankCore((byte)(addr - IMCConstants.DDR4PageAddress0)) ? 0 : -SharedConstants.EIO;
                }

                return -SharedConstants.ENOTSUP;
            }

            if (size.AnyOf(I2CConstants.I2C_SMBUS_BLOCK_DATA, I2CConstants.I2C_SMBUS_I2C_BLOCK_DATA))
            {
                return ReadBlockDataByByte(addr, read_write, command, data);
            }

            var status = PrepareImcTransfer(addr, command, size, out byte offset, out byte opcode, out byte commandSlot);
            if (status < 0)
            {
                return status;
            }

            status = imcAccess(offset, read_write, opcode, commandSlot, size, data);

            return status;
        }

        static int PrepareImcTransfer(byte addr, byte command, int size, out byte offset, out byte opcode, out byte commandSlot)
        {
            offset = 0;
            opcode = 0;
            commandSlot = 0;

            if (size != I2CConstants.I2C_SMBUS_BYTE_DATA
             && size != I2CConstants.I2C_SMBUS_WORD_DATA)
            {
                return -SharedConstants.ENOTSUP;
            }

            if (size == I2CConstants.I2C_SMBUS_WORD_DATA && IsDDR4ThermalSensorAddress(addr))
            {
                offset = command;
                opcode = IMCConstants.ThermalSensorOpcode;
                commandSlot = (byte)(addr & 0x07);

                return 0;
            }

            if (size == I2CConstants.I2C_SMBUS_BYTE_DATA && IsSPDDeviceAddress(addr))
            {
                offset = command;
                opcode = IMCConstants.SPDOpcode;
                commandSlot = (byte)(addr & 0x07);

                return 0;
            }

            return -SharedConstants.ENOTSUP;
        }

        int ReadBlockDataByByte(byte addr, byte read_write, byte command, SMBusData data)
        {
            if (read_write != I2CConstants.I2C_SMBUS_READ)
            {
                return -SharedConstants.ENOTSUP;
            }

            if (data == null)
            {
                return -SharedConstants.EINVAL;
            }

            var length = data[0];
            if (length == 0 || length > I2CConstants.I2C_SMBUS_BLOCK_MAX)
            {
                return -SharedConstants.EINVAL;
            }

            if (command + length > IMCConstants.AddressSpace8BitSize)
            {
                return -SharedConstants.EINVAL;
            }

            data[0] = 0;

            for (int index = 0; index < length; ++index)
            {
                var currentCommand = (byte)(command + index);

                var status = PrepareImcTransfer(addr, currentCommand, I2CConstants.I2C_SMBUS_BYTE_DATA, out byte offset, out byte opcode, out byte commandSlot);
                if (status < 0)
                {
                    return status;
                }

                using (var byteData = new SMBusData())
                {
                    status = imcAccess(offset, I2CConstants.I2C_SMBUS_READ, opcode, commandSlot, I2CConstants.I2C_SMBUS_BYTE_DATA, byteData);
                    if (status < 0)
                    {
                        return status;
                    }

                    data[index + 1] = byteData.ByteData;
                    data[0] = (byte)(index + 1);
                }
            }

            return 0;
        }

        static bool IsDDR4PageSelectorAddress(byte addr)
        {
            return addr == IMCConstants.DDR4PageAddress0 || addr == IMCConstants.DDR4PageAddress1;
        }

        static bool IsDDR4ThermalSensorAddress(byte addr)
        {
            return addr >= IMCConstants.DDR4ThermalSensorBegin && addr <= IMCConstants.DDR4ThermalSensorEnd;
        }

        static bool IsSPDDeviceAddress(byte addr)
        {
            return addr >= IMCConstants.SPDAddressBegin && addr <= IMCConstants.SPDAddressEnd;
        }

        int imcAccess(byte offset, byte read_write, byte opcode, byte commandSlot, int size, SMBusData data)
        {
            if (!size.AnyOf(I2CConstants.I2C_SMBUS_BYTE_DATA, I2CConstants.I2C_SMBUS_WORD_DATA))
            {
                return -SharedConstants.ENOTSUP;
            }

            if (read_write == I2CConstants.I2C_SMBUS_WRITE)
            {
                return -SharedConstants.ENOTSUP;
            }

            if (data == null)
            {
                return -SharedConstants.EINVAL;
            }

            for (int i = 0; i < IMCConstants.StartRetries; ++i)
            {
                uint oldCommand = 0;
                if (!ReadPciConfigDwordEx(_PCIAddress, CmdReg, ref oldCommand))
                {
                    return -SharedConstants.EIO;
                }

                if (!ClearTsodStateIfNeeded(oldCommand))
                {
                    continue;
                }

                uint cmd = IMCConstants.CommandPrefix
                         | ((uint)(opcode & 0x0F) << IMCConstants.OpShift)
                         | ((uint)(commandSlot & 0x07) << IMCConstants.SlotShift)
                         | offset;

                if (size == I2CConstants.I2C_SMBUS_WORD_DATA)
                {
                    cmd |= IMCConstants.WordBit;
                }

                WritePciConfigDwordEx(_PCIAddress, CmdReg, cmd);

                if (WaitTransferComplete(out _))
                {
                    uint dataReg = 0;

                    var readOk = ReadPciConfigDwordEx(_PCIAddress, DatReg, ref dataReg);

                    WritePciConfigDwordEx(_PCIAddress, CmdReg, oldCommand);

                    if (!readOk)
                    {
                        return -SharedConstants.EIO;
                    }

                    switch (size)
                    {
                        case I2CConstants.I2C_SMBUS_BYTE_DATA:
                            data.ByteData = (byte)(dataReg & 0xFF);
                            return 0;
                        case I2CConstants.I2C_SMBUS_WORD_DATA:
                            data.Word = (ushort)(((dataReg & 0xFF00) >> 8) | ((dataReg & 0x00FF) << 8));
                            return 0;
                    }
                }

                WritePciConfigDwordEx(_PCIAddress, CmdReg, oldCommand);
            }

            return -SharedConstants.ETIMEDOUT;
        }

        bool ClearTsodStateIfNeeded(uint oldCommand)
        {
            if ((oldCommand & IMCConstants.TsodActiveBit) == 0)
            {
                return true;
            }

            uint status = 0;
            for (int i = 0; i < IMCConstants.PageStatusRetries; ++i)
            {
                if (!ReadPciConfigDwordEx(_PCIAddress, StsReg, ref status))
                {
                    return false;
                }

                if ((status & IMCConstants.StsBusy) == 0 && (status & IMCConstants.StsDoneMask) != 0)
                {
                    break;
                }
            }

            WritePciConfigDwordEx(_PCIAddress, CmdReg, oldCommand & IMCConstants.CommandKeepMask);

            for (int i = 0; i < IMCConstants.PageCommandRetries; ++i)
            {
                uint command = 0;
                if (!ReadPciConfigDwordEx(_PCIAddress, CmdReg, ref command))
                {
                    return false;
                }

                if ((command & IMCConstants.GoBit) == 0)
                {
                    return true;
                }
            }

            return false;
        }

        bool WaitTransferComplete(out uint lastStatus)
        {
            lastStatus = 0;

            for (int i = 0; i < IMCConstants.TransferStatusRetries; ++i)
            {
                if (!ReadPciConfigDwordEx(_PCIAddress, StsReg, ref lastStatus))
                {
                    return false;
                }

                if ((lastStatus & IMCConstants.StsBusy) == 0 && (lastStatus & IMCConstants.StsDoneMask) != 0)
                {
                    break;
                }
            }

            if ((lastStatus & IMCConstants.StsStateMask) == IMCConstants.StsBusy)
            {
                for (int i = 0; i < IMCConstants.TransferToggleRetries; ++i)
                {
                    uint command = 0;
                    if (!ReadPciConfigDwordEx(_PCIAddress, CmdReg, ref command))
                    {
                        return false;
                    }

                    WritePciConfigDwordEx(_PCIAddress, CmdReg, command ^ IMCConstants.CommandToggleBit);

                    if (!ReadPciConfigDwordEx(_PCIAddress, StsReg, ref lastStatus))
                    {
                        return false;
                    }

                    if ((lastStatus & IMCConstants.StsStateMask) == IMCConstants.StsBusy)
                    {
                        break;
                    }
                }

                for (int i = 0; i < IMCConstants.TransferToggleRetries; ++i)
                {
                    if (!ReadPciConfigDwordEx(_PCIAddress, StsReg, ref lastStatus))
                    {
                        return false;
                    }

                    if ((lastStatus & IMCConstants.StsBusy) == 0 && (lastStatus & IMCConstants.StsDoneMask) != 0)
                    {
                        break;
                    }
                }
            }

            return (lastStatus & IMCConstants.StsDoneMask) == IMCConstants.StsDoneOk;
        }

        bool SetBankCore(byte bankIndex)
        {
            if (bankIndex > 1)
            {
                LogSimple.LogWarn($"{nameof(SMBusSkylakeIMC)}.{nameof(SetBank)}: Index not in range '{bankIndex}'.");
                return false;
            }

            for (int i = 0; i < IMCConstants.StartRetries; ++i)
            {
                uint oldCommand = 0;
                if (!ReadPciConfigDwordEx(_PCIAddress, CmdReg, ref oldCommand))
                {
                    return false;
                }

                if (!ClearTsodStateIfNeeded(oldCommand))
                {
                    continue;
                }

                uint cmd = (((uint)(bankIndex & 1) | IMCConstants.PageCommand) << 8);

                WritePciConfigDwordEx(_PCIAddress, CmdReg, cmd);

                uint lastStatus = 0;
                bool completed = false;

                for (int j = 0; j < IMCConstants.PageStatusRetries; ++j)
                {
                    if (!ReadPciConfigDwordEx(_PCIAddress, StsReg, ref lastStatus))
                    {
                        break;
                    }

                    if ((lastStatus & 0x03) != IMCConstants.StsBusy)
                    {
                        completed = true;
                        break;
                    }
                }

                WritePciConfigDwordEx(_PCIAddress, CmdReg, oldCommand);

                if (completed)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}
