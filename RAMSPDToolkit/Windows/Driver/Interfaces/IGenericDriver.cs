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

namespace RAMSPDToolkit.Windows.Driver.Interfaces
{
    /// <summary>
    /// Generic driver interface for driver implementation.
    /// </summary>
    public interface IGenericDriver : IDriver
    {
        byte   ReadIoPortByte (ushort port);
        ushort ReadIoPortWord (ushort port);
        uint   ReadIoPortDword(ushort port);
        
        bool ReadIoPortByteEx (ushort port, ref byte   value);
        bool ReadIoPortWordEx (ushort port, ref ushort value);
        bool ReadIoPortDwordEx(ushort port, ref uint   value);

        void WriteIoPortByte (ushort port, byte   value);
        void WriteIoPortWord (ushort port, ushort value);
        void WriteIoPortDword(ushort port, uint   value);

        bool WriteIoPortByteEx (ushort port, byte   value);
        bool WriteIoPortWordEx (ushort port, ushort value);
        bool WriteIoPortDwordEx(ushort port, uint   value);

        uint FindPciDeviceById(ushort vendorId, ushort deviceId, byte index);
        uint FindPciDeviceByClass(byte baseClass, byte subClass, byte programIf, byte index);

        byte   ReadPciConfigByte (uint pciAddress, byte regAddress);
        ushort ReadPciConfigWord (uint pciAddress, byte regAddress);
        uint   ReadPciConfigDword(uint pciAddress, byte regAddress);

        bool ReadPciConfigByteEx (uint pciAddress, uint regAddress, ref byte   value);
        bool ReadPciConfigWordEx (uint pciAddress, uint regAddress, ref ushort value);
        bool ReadPciConfigDwordEx(uint pciAddress, uint regAddress, ref uint   value);

        void WritePciConfigByte (uint pciAddress, byte regAddress, byte   value);
        void WritePciConfigWord (uint pciAddress, byte regAddress, ushort value);
        void WritePciConfigDword(uint pciAddress, byte regAddress, uint   value);

        bool WritePciConfigByteEx (uint pciAddress, uint regAddress, byte   value);
        bool WritePciConfigWordEx (uint pciAddress, uint regAddress, ushort value);
        bool WritePciConfigDwordEx(uint pciAddress, uint regAddress, uint   value);
    }
}
