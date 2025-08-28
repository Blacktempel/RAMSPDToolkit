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

namespace RAMSPDToolkit.SPD.Interop.Shared
{
    /// <summary>
    /// SPD constants.
    /// </summary>
    public sealed class SPDConstants
    {
        /// <summary>
        /// I/O delay in milliseconds.
        /// </summary>
        public const int SPD_IO_DELAY = 1;

        /// <summary>
        /// Address of first RAM stick.
        /// </summary>
        public const byte SPD_BEGIN = 0x50;

        /// <summary>
        /// Address of last possible RAM stick.
        /// </summary>
        public const byte SPD_END = 0x57;

        /// <summary>
        /// Default value for amount of retries to read from thermal sensor, if SMBus is busy on read.
        /// </summary>
        public const byte SPD_TS_RETRIES = 12;

        /// <summary>
        /// Default value for amount of retries to read SPD data.
        /// </summary>
        public const byte SPD_DATA_RETRIES = 5;

        /// <summary>
        /// Default value for amount of retries to read configuration data, if SMBus is busy on read.
        /// </summary>
        public const byte SPD_CFG_RETRIES = 50;
    }
}
