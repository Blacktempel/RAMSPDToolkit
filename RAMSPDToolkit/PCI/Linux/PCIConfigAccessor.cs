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
        internal static bool ReadPCIConfigByte(string pciDevicePath, int configAddress, out byte value)
        {
            var ok = ReadPCIConfig(pciDevicePath, configAddress, 1, out var val);
            value = (byte)val;
            return ok;
        }

        internal static bool ReadPCIConfigInt32(string pciDevicePath, int configAddress, out uint value)
        {
            var ok = ReadPCIConfig(pciDevicePath, configAddress, 4, out var val);
            value = (uint)val;
            return ok;
        }

        internal static bool ReadPCIConfig(string pciDevicePath, int configAddress, int bufferSize, out ulong value)
        {
            value = 0;

            if (bufferSize <= 0)
            {
                LogSimple.LogWarn($"{nameof(PCIConfigAccessor)}: Invalid buffer size {bufferSize} for reading PCI config at 0x{configAddress:X2}.");
                return false;
            }

            if (!Directory.Exists(pciDevicePath))
            {
                pciDevicePath = pciDevicePath.ToLower();

                if (!Directory.Exists(pciDevicePath))
                {
                    LogSimple.LogWarn($"{nameof(ReadPCIConfig)}: path not found '{pciDevicePath}'.");
                    return false;
                }
            }

            var buffer = new byte[bufferSize];

            var pathConfig = pciDevicePath + "/config";
            var fileDescriptor = Libc.open(pathConfig, LinuxConstants.O_RDONLY);

            Libc.lseek(fileDescriptor, configAddress, LinuxConstants.SEEK_SET);

            if (Libc.read(fileDescriptor, buffer, (uint)buffer.Length) < 0)
            {
                LogSimple.LogWarn($"{nameof(PCIConfigAccessor)}: Failed to read PCI config at 0x{configAddress:X2}.");
            }

            if (bufferSize == 1)
            {
                value = buffer[0];
                return true;
            }
            else
            {
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(buffer);
                }

                switch (bufferSize)
                {
                    case 2:
                        value = BitConverter.ToUInt16(buffer, 0);
                        return true;
                    case 4:
                        value = BitConverter.ToUInt32(buffer, 0);
                        return true;
                    case 8:
                        value = BitConverter.ToUInt64(buffer, 0);
                        return true;
                    default:
                        LogSimple.LogWarn($"{nameof(PCIConfigAccessor)}: Unsupported buffer size {bufferSize} for reading PCI config at 0x{configAddress:X2}.");
                        return false;
                }
            }
        }

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

        internal static void WritePCIConfigInt16(string pciDevicePath, int configAddress, ushort value)
        {
            var buffer = BitConverter.GetBytes(value);

            WritePCIConfig(pciDevicePath, configAddress, buffer);
        }

        internal static void WritePCIConfigInt32(string pciDevicePath, int configAddress, uint value)
        {
            var buffer = BitConverter.GetBytes(value);

            WritePCIConfig(pciDevicePath, configAddress, buffer);
        }

        internal static void WritePCIConfig(string pciDevicePath, int configAddress, byte[] value)
        {
            if (!Directory.Exists(pciDevicePath))
            {
                pciDevicePath = pciDevicePath.ToLower();

                if (!Directory.Exists(pciDevicePath))
                {
                    LogSimple.LogWarn($"{nameof(WritePCIConfig)}: path not found '{pciDevicePath}'.");
                    return;
                }
            }

            var pathConfig = pciDevicePath + "/config";
            var fileDescriptor = Libc.open(pathConfig, LinuxConstants.O_RDWR);

            Libc.lseek(fileDescriptor, configAddress, LinuxConstants.SEEK_SET);

            if (Libc.write(fileDescriptor, value, (uint)value.Length) < 0)
            {
                LogSimple.LogWarn($"{nameof(PCIConfigAccessor)}: Failed to write PCI config at 0x{configAddress:X2}.");
            }
        }
    }
}