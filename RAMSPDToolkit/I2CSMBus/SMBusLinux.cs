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
using RAMSPDToolkit.I2CSMBus.Interop.Linux;
using RAMSPDToolkit.Logging;
using RAMSPDToolkit.PCI;
using System.Runtime.InteropServices;
using System.Text;

namespace RAMSPDToolkit.I2CSMBus
{
    /// <summary>
    /// SMBus class for systems with Linux.
    /// </summary>
    public sealed class SMBusLinux : SMBusInterface
    {
        #region Constructor

        SMBusLinux()
        {
        }

        #endregion

        #region I2CSMBusInterface

        protected override int I2CSMBusXfer(byte addr, byte read_write, byte command, int size, SMBusData data)
        {
            using (var handle = new I2CDeviceHandle(DeviceName))
            {
                var temp = new I2CSMBusIOCTLData();

                //Tell I2C host which slave address to transfer to
                Libc.ioctl_byte(handle.Handle, LinuxConstants.I2C_SLAVE_FORCE, addr);

                temp.ReadWrite = read_write;
                temp.Command = command;
                temp.Size = (uint)size;

                if (data != null)
                {
                    temp.Data = data.Pointer;
                }

                var status = Libc.ioctl_data(handle.Handle, LinuxConstants.I2C_SMBUS, ref temp);

                return status;
            }
        }

        protected override int I2CXfer(byte addr, byte read_write, int? size, byte[] data)
        {
            using (var handle = new I2CDeviceHandle(DeviceName))
            {
                var rdwr = new I2CRdwrIOCTLData();
                var msg = new I2CMsg();

                msg.Structure.Address = addr;
                msg.Structure.Flags = read_write;
                msg.Structure.Length = (ushort)size.GetValueOrDefault();
                msg.Buffer = new byte[msg.Structure.Length];

                rdwr.Msgs = new I2CMsgStructure[1] { msg.Structure };
                rdwr.NMsgs = 1;

                var ret = Libc.ioctl_data_rdwr(handle.Handle, LinuxConstants.I2C_RDWR, ref rdwr);

                msg.Structure = rdwr.Msgs[0];

                /*-------------------------------------------------*\
                | If operation was a read, copy read data and size  |
                \*-------------------------------------------------*/
                if (read_write == I2CConstants.I2C_SMBUS_READ)
                {
                    // size = msg.Length;

                    Array.Copy(msg.Buffer, 0, data, 0, msg.Structure.Length);
                }

                return ret;
            }
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
            bool ret = true;

            var driverPath = "/sys/bus/i2c/devices/";

            var dir = Libc.opendir(driverPath);

            if (dir == IntPtr.Zero)
            {
                return false;
            }

            //Loop through all entries in I2C-adapter list

            var buffer = new byte[100];

            IntPtr ent;

            while ((ent = Libc.readdir(dir)) != IntPtr.Zero)
            {
                var entStruct = Marshal.PtrToStructure<Dirent>(ent);

                if (entStruct.d_type == LinuxConstants.DT_DIR || entStruct.d_type == LinuxConstants.DT_LNK)
                {
                    var name = new string(entStruct.d_name);
                    name = name.Substring(0, name.IndexOf('\0'));

                    var i2cStr = "i2c-";

                    if (name.StartsWith(i2cStr))
                    {
                        var deviceString = driverPath + name + "/name";

                        var fileDescriptor = Libc.open(deviceString, LinuxConstants.O_RDONLY);

                        if (fileDescriptor != 0)
                        {
                            if (Libc.read(fileDescriptor, buffer, (uint)buffer.Length) < 0)
                            {
                                LogSimple.LogWarn($"{nameof(SMBusLinux)}: Failed to read I2C device name.");
                            }

                            Libc.close(fileDescriptor);

                            //Extract port ID
                            ushort portID = ushort.MaxValue;
                            var portStr = name.Substring(i2cStr.Length, name.Length - i2cStr.Length);

                            portID = Convert.ToUInt16(portStr);

                            //Get device path
                            var path = driverPath + name;

                            if (entStruct.d_type == LinuxConstants.DT_LNK)
                            {
                                var str = Libc.realpath(path, IntPtr.Zero);
                                if (str == null)
                                {
                                    continue;
                                }

                                path = str + "/..";
                            }
                            else
                            {
                                path += "/device";
                            }

                            //Get PCI Vendor
                            var pciVendor = GetPCIData(path + "/vendor");

                            //Get PCI Device
                            var pciDevice = GetPCIData(path + "/device");

                            //Get PCI Subsystem Vendor
                            var pciSubsystemVendor = GetPCIData(path + "/subsystem_vendor");

                            //Get PCI Subsystem Device
                            var pciSubsystemDevice = GetPCIData(path + "/subsystem_device");

                            //Filter by vendor Intel and AMD, ignore others
                            if (!LinuxConstants.AllowedVendorIDs.Contains(pciVendor))
                            {
                                continue;
                            }

                            if (!IsSMBusClass(path))
                            {
                                continue;
                            }

                            deviceString = "/dev/" + name;

                            using (var handle = new I2CDeviceHandle(deviceString))
                            {
                                if (!handle.IsValid)
                                {
                                    LogSimple.LogTrace($"Could not open '{deviceString}'.");
                                    ret = false;

                                    continue;
                                }

                                var bus = new SMBusLinux();
                                bus.DeviceName = deviceString;
                                bus.PCIDevice = pciDevice;
                                bus.PCIVendor = pciVendor;
                                bus.PCISubsystemDevice = pciSubsystemDevice;
                                bus.PCISubsystemVendor = pciSubsystemVendor;
                                bus.PortID = portID;

                                bus.HasSPDWriteProtection = CheckHasSPDWriteProtection(pciVendor, path);

                                SMBusManager.AddSMBus(bus);
                            }
                        }
                        else
                        {
                            ret = false;
                        }
                    }
                }
            }

            Libc.closedir(dir);

            return ret;
        }

        public void CheckFuncs()
        {
            CheckFuncs(I2CConstants.I2C_SMBUS_BYTE);
            CheckFuncs(I2CConstants.I2C_SMBUS_BYTE_DATA);
            CheckFuncs(I2CConstants.I2C_SMBUS_WORD_DATA);
            CheckFuncs(I2CConstants.I2C_SMBUS_BLOCK_DATA);
            CheckFuncs(I2CConstants.I2C_SMBUS_I2C_BLOCK_DATA);
        }
            
        #endregion

        #region Private

        static ushort GetPCIData(string path)
        {
            ushort result = 0;

            var buffer = new byte[100];

            var test_fd = Libc.open(path, LinuxConstants.O_RDONLY);
            if (test_fd >= 0)
            {
                if (Libc.read(test_fd, buffer, (uint)buffer.Length) < 0)
                {
                    LogSimple.LogWarn($"{nameof(SMBusLinux)}: Failed to read I2C device at '{path}'.");
                }

                var tempStr = Encoding.Default.GetString(buffer);
                tempStr = tempStr.Substring(0, tempStr.IndexOf('\0')).Trim();

                result = tempStr.Length == 0 ? (ushort)0 : Convert.ToUInt16(tempStr, 16);

                Libc.close(test_fd);
            }

            return result;
        }

        static bool IsSMBusClass(string pciDevicePath)
        {
            var buffer = new byte[10];

            //Check if it is a SMBus device via class code
            var pathClass = pciDevicePath + "/class";
            var fileDescriptor = Libc.open(pathClass, LinuxConstants.O_RDONLY);

            if (Libc.read(fileDescriptor, buffer, (uint)buffer.Length) < 0)
            {
                LogSimple.LogWarn($"{nameof(SMBusLinux)}: Failed to read I2C class.");
            }

            var classString = Encoding.UTF8.GetString(buffer);
            if (!classString.StartsWith(LinuxConstants.SMBusClassID, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        static bool CheckHasSPDWriteProtection(ushort pciVendor, string pciDevicePath)
        {
            //Only Intel can have write protection bit set
            if (pciVendor != PCIConstants.PCI_VENDOR_INTEL)
            {
                return false;
            }

            var buffer = new byte[1];

            var pathConfig = pciDevicePath + "/config";
            var fileDescriptor = Libc.open(pathConfig, LinuxConstants.O_RDONLY);

            Libc.lseek(fileDescriptor, IntelConstants.SMBHSTCFG, LinuxConstants.SEEK_SET);

            if (Libc.read(fileDescriptor, buffer, (uint)buffer.Length) < 0)
            {
                LogSimple.LogWarn($"{nameof(SMBusLinux)}: Failed to read host config.");
            }

            //Check if write protection bit is set
            return (buffer[0] & IntelConstants.SMBHSTCFG_SPD_WD) != 0;
        }

        void CheckFuncs(int size)
        {
            using (var handle = new I2CDeviceHandle(DeviceName))
            {
                //Check adapter functionality
                if (Libc.ioctl_ulong(handle.Handle, LinuxConstants.I2C_FUNCS, out ulong funcs) < 0)
                {
                    return;
                }

                switch (size)
                {
                    case I2CConstants.I2C_SMBUS_BYTE:
                        if (0 != (funcs & LinuxConstants.I2C_FUNC_SMBUS_READ_BYTE))
                        {
                            LogSimple.LogTrace($"{nameof(LinuxConstants.I2C_FUNC_SMBUS_READ_BYTE)} is available.");
                        }
                        if (0 != (funcs & LinuxConstants.I2C_FUNC_SMBUS_WRITE_BYTE))
                        {
                            LogSimple.LogTrace($"{nameof(LinuxConstants.I2C_FUNC_SMBUS_WRITE_BYTE)} is available.");
                        }
                        break;
                    case I2CConstants.I2C_SMBUS_BYTE_DATA:
                        if (0 != (funcs & LinuxConstants.I2C_FUNC_SMBUS_READ_BYTE_DATA))
                        {
                            LogSimple.LogTrace($"{nameof(LinuxConstants.I2C_FUNC_SMBUS_READ_BYTE_DATA)} is available.");
                        }
                        if (0 != (funcs & LinuxConstants.I2C_FUNC_SMBUS_WRITE_BYTE_DATA))
                        {
                            LogSimple.LogTrace($"{nameof(LinuxConstants.I2C_FUNC_SMBUS_WRITE_BYTE_DATA)} is available.");
                        }
                        break;
                    case I2CConstants.I2C_SMBUS_WORD_DATA:
                        if (0 != (funcs & LinuxConstants.I2C_FUNC_SMBUS_READ_WORD_DATA))
                        {
                            LogSimple.LogTrace($"{nameof(LinuxConstants.I2C_FUNC_SMBUS_READ_WORD_DATA)} is available.");
                        }
                        if (0 != (funcs & LinuxConstants.I2C_FUNC_SMBUS_WRITE_WORD_DATA))
                        {
                            LogSimple.LogTrace($"{nameof(LinuxConstants.I2C_FUNC_SMBUS_WRITE_WORD_DATA)} is available.");
                        }
                        break;
                    case I2CConstants.I2C_SMBUS_BLOCK_DATA:
                        if (0 != (funcs & LinuxConstants.I2C_FUNC_SMBUS_READ_BLOCK_DATA))
                        {
                            LogSimple.LogTrace($"{nameof(LinuxConstants.I2C_FUNC_SMBUS_READ_BLOCK_DATA)} is available.");
                        }
                        if (0 != (funcs & LinuxConstants.I2C_FUNC_SMBUS_WRITE_BLOCK_DATA))
                        {
                            LogSimple.LogTrace($"{nameof(LinuxConstants.I2C_FUNC_SMBUS_WRITE_BLOCK_DATA)} is available.");
                        }
                        break;
                    case I2CConstants.I2C_SMBUS_I2C_BLOCK_DATA:
                        if (0 != (funcs & LinuxConstants.I2C_FUNC_SMBUS_READ_I2C_BLOCK))
                        {
                            LogSimple.LogTrace($"{nameof(LinuxConstants.I2C_FUNC_SMBUS_READ_I2C_BLOCK)} is available.");
                        }
                        if (0 != (funcs & LinuxConstants.I2C_FUNC_SMBUS_WRITE_I2C_BLOCK))
                        {
                            LogSimple.LogTrace($"{nameof(LinuxConstants.I2C_FUNC_SMBUS_WRITE_I2C_BLOCK)} is available.");
                        }
                        break;
                }
            }
        }

        #endregion
    }
}
