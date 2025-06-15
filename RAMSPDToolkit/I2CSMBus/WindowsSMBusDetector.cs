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

using RAMSPDToolkit.I2CSMBus.Interop.Intel;
using RAMSPDToolkit.I2CSMBus.Interop.Piix4;
using RAMSPDToolkit.I2CSMBus.Interop.Shared;
using RAMSPDToolkit.I2CSMBus.Interop.Shared.Structures;
using RAMSPDToolkit.Logging;
using RAMSPDToolkit.Mutexes;
using RAMSPDToolkit.PCI;
using RAMSPDToolkit.Windows.Driver;
using System.Runtime.InteropServices;

namespace RAMSPDToolkit.I2CSMBus
{
    /// <summary>
    /// Detects SMBuses for Windows using the SetupAPI and <see cref="DriverManager.Driver"/>.
    /// </summary>
    internal static class WindowsSMBusDetector
    {
        #region Constants

        const int BUFFER_SIZE = 1024;

        const int DIGCF_PRESENT    = 0x00000002;
        const int DIGCF_ALLCLASSES = 0x00000004;

        static readonly IntPtr INVALID_HANDLE = new IntPtr(-1);

        const int SPDRP_DEVICEDESC   = 0x00000000;
        const int SPDRP_HARDWAREID   = 0x00000001;
        const int SPDRP_BUSNUMBER    = 0x00000015;
        const int SPDRP_ADDRESS      = 0x0000001C;

        static readonly string CLASS_CODE_SMBUS = "CC_0C05";

        #endregion

        #region Public

        /// <summary>
        /// Detects available SMBuses on Windows for usage.<br/>
        /// If detected, an instance of the detected SMBus will be created and added into <see cref="SMBusManager.RegisteredSMBuses"/>.
        /// </summary>
        /// <returns>Boolean value to indicate whether detection was successful.</returns>
        public static bool DetectSMBuses()
        {
            var devInfo = SetupAPI.SetupDiGetClassDevs(IntPtr.Zero, null, IntPtr.Zero,
                                                       DIGCF_PRESENT | DIGCF_ALLCLASSES);

            if (devInfo == INVALID_HANDLE)
            {
                return false;
            }

            uint index = 0;
            var infoData = new SP_DEVINFO_DATA();
            infoData.cbSize = (uint)Marshal.SizeOf(infoData);

            //Enumerate devices
            while (SetupAPI.SetupDiEnumDeviceInfo(devInfo, index, ref infoData))
            {
                ++index;

                var buffer = new char[BUFFER_SIZE];

                //Get hardware IDs of device
                if (SetupAPI.SetupDiGetDeviceRegistryProperty(devInfo, ref infoData, SPDRP_HARDWAREID, out _, buffer, (uint)buffer.Length, out _))
                {
                    //Convert to string
                    string str = new string(buffer);

                    int end = -1;

                    //Find end of data
                    for (int i = 0; i < str.Length - 1; ++i)
                    {
                        if (str[i] == '\0' && str[i + 1] == '\0')
                        {
                            end = i;
                            break;
                        }
                    }

                    var rawHwIDs = str.Substring(0, end);

                    //Split hardware IDs for device
                    var hwIDs = rawHwIDs.Split('\0');

                    foreach (var hwID in hwIDs)
                    {
                        //Check if device is SMBus
                        if (hwID.IndexOf(CLASS_CODE_SMBUS, StringComparison.OrdinalIgnoreCase) != -1)
                        {
                            LogSimple.LogTrace($"Device with hardware-ID '{hwID}' is detected as SMBus.");

                            bool ok = true;

                            uint bus = 0;
                            uint address = 0;

                            //Get Bus, Dev, Func
                            ok &= SetupAPI.SetupDiGetDeviceRegistryProperty(devInfo, ref infoData, SPDRP_BUSNUMBER, out _, ref bus    , (uint)Marshal.SizeOf(bus    ), out _);
                            ok &= SetupAPI.SetupDiGetDeviceRegistryProperty(devInfo, ref infoData, SPDRP_ADDRESS  , out _, ref address, (uint)Marshal.SizeOf(address), out _);

                            if (ok)
                            {
                                using (var guard = new WorldMutexGuard(WorldMutexManager.WorldPCIMutex))
                                {
                                    var dev  = (address >> 16) & 0xFFFF;
                                    var func = address & 0xFFFF;

                                    //Get PCI address
                                    var pciAddress = DriverAccess.PciBusDevFunc(bus, dev, func);

                                    LogSimple.LogTrace($"Bus {bus}, Dev {dev}, Func {func} = PCIAddress {pciAddress}.");

                                    //Verify SMBus device
                                    var subClass  = DriverAccess.ReadPciConfigByte(pciAddress, PCIConstants.PCI_SUBCLASS );
                                    var baseClass = DriverAccess.ReadPciConfigByte(pciAddress, PCIConstants.PCI_BASECLASS);

                                    if (baseClass != PCIConstants.PCI_BASECLASS_SERIAL_BUS_CONTROLLER
                                     || subClass  != PCIConstants.PCI_SUBCLASS_SMBUS)
                                    {
                                        LogSimple.LogWarn($"Verifying device from PCI configuration as SMBus was not successful ({nameof(baseClass)} = '0x{baseClass:X2}' and {nameof(subClass)} = '0x{subClass:X2}').");
                                        continue;
                                    }

                                    //Read IDs from PCI configuration register
                                    var vendor       = DriverAccess.ReadPciConfigWord(pciAddress, PCIConstants.PCI_VENDOR_ID          );
                                    var device       = DriverAccess.ReadPciConfigWord(pciAddress, PCIConstants.PCI_DEVICE_ID          );
                                    var subsysVendor = DriverAccess.ReadPciConfigWord(pciAddress, PCIConstants.PCI_SUBSYSTEM_VENDOR_ID);
                                    var subsysDevice = DriverAccess.ReadPciConfigWord(pciAddress, PCIConstants.PCI_SUBSYSTEM_DEVICE_ID);

                                    LogSimple.LogTrace($"SMBus PCI V: 0x{vendor:X4} ({vendor}); D = 0x{device:X4} ({device}); SV = 0x{subsysVendor:X4} ({subsysVendor}); SD = 0x{subsysDevice:X4} ({subsysDevice}).");

                                    string deviceName = null;

                                    //Get device description
                                    if (SetupAPI.SetupDiGetDeviceRegistryProperty(devInfo, ref infoData, SPDRP_DEVICEDESC, out _, buffer, (uint)buffer.Length, out _))
                                    {
                                        //We are only interested in first description, cut the rest
                                        var devName = new string(buffer);
                                        end = devName.IndexOf('\0');

                                        if (end == -1)
                                        {
                                            end = 0;
                                        }

                                        deviceName = devName.Substring(0, end);
                                    }

                                    uint smba = 0;

                                    //Direct to SMBus implementation
                                    switch (vendor)
                                    {
                                        case PCIConstants.PCI_VENDOR_INTEL:
                                            if (ReadSMBA(pciAddress, PCIConstants.PCI_CFG_REG_INTEL_SMBA, out smba))
                                            {
                                                var hostConfig = (byte)DriverAccess.ReadPciConfigWord(pciAddress, IntelConstants.SMBHSTCFG);
                                                if ((hostConfig & IntelConstants.SMBHSTCFG_HST_EN) == 0)
                                                {
                                                    continue;
                                                }

                                                SMBusI801.Create(pciAddress, (ushort)smba, hostConfig, vendor, device, subsysVendor, subsysDevice, deviceName);
                                            }
                                            break;
                                        case PCIConstants.PCI_VENDOR_AMD:
                                            if (ReadSMBA(pciAddress, PCIConstants.PCI_CFG_REG_AMD_SMBA, out smba))
                                            {
                                                //If the SMBA doesn't read out zero, the SMBus base address has been initialized and can be linked to the I/O space
                                                if (smba != 0)
                                                {
                                                    SMBusPiix4.Create(pciAddress, (ushort)smba, vendor, device, subsysVendor, subsysDevice, deviceName);
                                                }
                                                else
                                                {
                                                    //If the SMBA reads out zero, the SMBus base address has not been initialized and can't be linked to the I/O space.
                                                    LogSimple.LogWarn($"SMBA is invalid (0x{smba:X4} ({smba})) and assumed to be uninitialized.");

                                                    Piix4InitDefaultSMBA(pciAddress, vendor, device, subsysVendor, subsysDevice, deviceName);
                                                }
                                            }
                                            break;
                                        default:
                                            LogSimple.LogWarn($"No SMBus implementation for vendor 0x{vendor:X4} ({vendor}) available.");
                                            break;
                                    }

                                    //Done, get out of this loop
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            SetupAPI.SetupDiDestroyDeviceInfoList(devInfo);

            return true;
        }

        #endregion

        #region Private

        static bool ReadSMBA(uint pciAddress, uint pciRegisterAddress, out uint smba)
        {
            smba = 0;

            //Read base address
            var result = DriverAccess.ReadPciConfigDwordEx(pciAddress, pciRegisterAddress, ref smba);

            if (!result)
            {
                return false;
            }

            //Adjust
            smba &= ~0x1Fu;

            LogSimple.LogTrace($"Read SMBA is 0x{smba:X4} ({smba}).");

            return true;
        }

        static void Piix4InitDefaultSMBA(uint pciAddress, ushort vendor, ushort device, ushort subsysVendor, ushort subsysDevice, string deviceName)
        {
            //Analysis of many AMD boards has shown that AMD SMBus controllers have two adapters with fixed I/O spaces
            SMBusPiix4.Create(pciAddress, Piix4Constants.PIIX4_SMBA_0, vendor, device, subsysVendor, subsysDevice, deviceName);
            SMBusPiix4.Create(pciAddress, Piix4Constants.PIIX4_SMBA_1, vendor, device, subsysVendor, subsysDevice, deviceName);
        }

        #endregion
    }
}
