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

#if !RELEASE_NDD

namespace RAMSPDToolkit.Windows.Driver.Implementations
{
    /// <summary>
    /// Available default implementations for drivers.
    /// </summary>
    public enum InternalDriver
    {
        /// <summary>
        /// Custom implementation of driver.<br/>
        /// This is an external implementation.
        /// </summary>
        Custom,

        /// <summary>
        /// OLS / WinRing0 driver.
        /// </summary>
        OLS,
    }
}

#endif
