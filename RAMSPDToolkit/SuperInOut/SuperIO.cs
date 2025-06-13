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

using RAMSPDToolkit.Windows.Driver;

using OS = RAMSPDToolkit.Software.OperatingSystem;

namespace RAMSPDToolkit.SuperInOut
{
    /// <summary>
    /// Super IO access.
    /// </summary>
    public sealed class SuperIO
    {
        #region Public

        /// <summary>
        /// Put the Super IO Chip into Extended Function Mode.
        /// </summary>
        /// <param name="ioReg"></param>
        public static void SuperIOEnter(int ioReg)
        {
            if (OS.IsWindows())
            {
                DriverAccess.WriteIoPortByte((ushort)ioReg, 0x87);
                DriverAccess.WriteIoPortByte((ushort)ioReg, 0x87);
            }
            else if (OS.IsLinux())
            {
                SuperIOLinux.SuperIOEnter(ioReg);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Write a byte to the Super IO configuration register.
        /// </summary>
        /// <param name="ioReg"></param>
        /// <param name="reg"></param>
        /// <param name="val"></param>
        public static void SuperIOWriteByte(int ioReg, byte reg, byte val)
        {
            if (OS.IsWindows())
            {
                DriverAccess.WriteIoPortByte((ushort)ioReg, reg);
                DriverAccess.WriteIoPortByte((ushort)(ioReg + 1), val);
            }
            else if (OS.IsLinux())
            {
                SuperIOLinux.SuperIOWriteByte(ioReg, reg, val);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Read a byte from the Super IO configuration register.
        /// </summary>
        /// <param name="ioReg"></param>
        /// <param name="reg"></param>
        public static byte SuperIOReadByte(int ioReg, byte reg)
        {
            if (OS.IsWindows())
            {
                DriverAccess.WriteIoPortByte((ushort)ioReg, reg);
                return DriverAccess.ReadIoPortByte((ushort)(ioReg + 1));
            }
            else if (OS.IsLinux())
            {
                return SuperIOLinux.SuperIOReadByte(ioReg, reg);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        #endregion
    }
}
