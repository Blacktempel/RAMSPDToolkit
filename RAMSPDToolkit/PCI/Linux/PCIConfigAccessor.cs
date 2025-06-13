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

using RAMSPDToolkit.I2CSMBus.Interop.Linux;
using RAMSPDToolkit.Logging;

namespace RAMSPDToolkit.PCI.Linux
{
    internal static class PCIConfigAccessor
    {
        internal static byte ReadPCIConfig(string pciDevicePath, byte configAddress)
        {
            var buffer = new byte[1];

            var pathConfig = pciDevicePath + "/config";
            var fileDescriptor = Libc.open(pathConfig, LinuxConstants.O_RDONLY);

            Libc.lseek(fileDescriptor, configAddress, LinuxConstants.SEEK_SET);

            if (Libc.read(fileDescriptor, buffer, (uint)buffer.Length) < 0)
            {
                LogSimple.LogWarn($"{nameof(PCIConfigAccessor)}: Failed to read PCI config at 0x{configAddress:X2}.");
            }

            return buffer[0];
        }

        internal static void WritePCIConfig(string pciDevicePath, byte configAddress, ushort value)
        {
            var buffer = BitConverter.GetBytes(value);

            var pathConfig = pciDevicePath + "/config";
            var fileDescriptor = Libc.open(pathConfig, LinuxConstants.O_RDWR);

            Libc.lseek(fileDescriptor, configAddress, LinuxConstants.SEEK_SET);

            if (Libc.write(fileDescriptor, buffer, (uint)buffer.Length) < 0)
            {
                LogSimple.LogWarn($"{nameof(PCIConfigAccessor)}: Failed to write PCI config at 0x{configAddress:X2}.");
            }
        }
    }
}