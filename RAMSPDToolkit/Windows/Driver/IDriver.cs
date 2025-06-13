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
    /// Driver interface for driver implementation.
    /// </summary>
    public interface IDriver
    {
        bool IsOpen { get; }

        bool Load();

        void Unload();

        byte   ReadIoPortByte (ushort port);
        ushort ReadIoPortWord (ushort port);
        uint   ReadIoPortDword(ushort port);
        
        int ReadIoPortByteEx (ushort port, ref byte   value);
        int ReadIoPortWordEx (ushort port, ref ushort value);
        int ReadIoPortDwordEx(ushort port, ref uint   value);

        void WriteIoPortByte (ushort port, byte   value);
        void WriteIoPortWord (ushort port, ushort value);
        void WriteIoPortDword(ushort port, uint   value);

        int WriteIoPortByteEx (ushort port, byte   value);
        int WriteIoPortWordEx (ushort port, ushort value);
        int WriteIoPortDwordEx(ushort port, uint   value);

        uint FindPciDeviceById(ushort vendorId, ushort deviceId, byte index);
        uint FindPciDeviceByClass(byte baseClass, byte subClass, byte programIf, byte index);

        byte   ReadPciConfigByte (uint pciAddress, byte regAddress);
        ushort ReadPciConfigWord (uint pciAddress, byte regAddress);
        uint   ReadPciConfigDword(uint pciAddress, byte regAddress);

        int ReadPciConfigByteEx (uint pciAddress, uint regAddress, ref byte   value);
        int ReadPciConfigWordEx (uint pciAddress, uint regAddress, ref ushort value);
        int ReadPciConfigDwordEx(uint pciAddress, uint regAddress, ref uint   value);

        void WritePciConfigByte (uint pciAddress, byte regAddress, byte   value);
        void WritePciConfigWord (uint pciAddress, byte regAddress, ushort value);
        void WritePciConfigDword(uint pciAddress, byte regAddress, uint   value);

        int WritePciConfigByteEx (uint pciAddress, uint regAddress, byte   value);
        int WritePciConfigWordEx (uint pciAddress, uint regAddress, ushort value);
        int WritePciConfigDwordEx(uint pciAddress, uint regAddress, uint   value);
    }
}
