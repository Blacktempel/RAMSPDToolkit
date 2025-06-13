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

namespace RAMSPDToolkit.Windows.Driver
{
    /// <summary>
    /// Forward to driver. Wrapper class to abstract calls into driver.
    /// </summary>
    internal static class DriverAccess
    {
        #region Properties

        public static bool IsOpen
        {
            get { return DriverManager.Driver.IsOpen; }
        }

        #endregion

        #region Public

        /// <summary>
        /// Combine Bus Number, Device Number and Function Number to PCI Device Address.
        /// </summary>
        /// <param name="bus">Bus number.</param>
        /// <param name="dev">Device number.</param>
        /// <param name="func">Function number.</param>
        /// <returns>Constructed PCI address.</returns>
        public static uint PciBusDevFunc(uint bus, uint dev, uint func)
        {
            return ((bus & 0xFF) << 8) | ((dev & 0x1F) << 3) | (func & 7);
        }

        public static void WriteIoPortByte(ushort port, byte value)
        {
            DriverManager.Driver.WriteIoPortByte(port, value);
        }

        public static byte ReadIoPortByte(ushort port)
        {
            return DriverManager.Driver.ReadIoPortByte(port);
        }

        public static uint FindPciDeviceById(ushort vendorId, ushort deviceId, byte index)
        {
            return DriverManager.Driver.FindPciDeviceById(vendorId, deviceId, index);
        }

        public static byte ReadPciConfigByte(uint pciAddress, byte regAddress)
        {
            return DriverManager.Driver.ReadPciConfigByte(pciAddress, regAddress);
        }

        public static ushort ReadPciConfigWord(uint pciAddress, byte regAddress)
        {
            return DriverManager.Driver.ReadPciConfigWord(pciAddress, regAddress);
        }

        public static int ReadPciConfigDwordEx(uint pciAddress, uint regAddress, ref uint value)
        {
            return DriverManager.Driver.ReadPciConfigDwordEx(pciAddress, regAddress, ref value);
        }

        public static void WritePciConfigWord(uint pciAddress, byte regAddress, ushort value)
        {
            DriverManager.Driver.WritePciConfigWord(pciAddress, regAddress, value);
        }

        #endregion
    }
}
