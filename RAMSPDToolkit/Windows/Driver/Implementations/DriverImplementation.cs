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

namespace RAMSPDToolkit.Windows.Driver.Implementations
{
    /// <summary>
    /// Available implementations for drivers.
    /// </summary>
    public enum DriverImplementation
    {
        /// <summary>
        /// Driver implementation is invalid / not available.
        /// </summary>
        Invalid,

        /// <summary>
        /// Custom implementation of driver.<br/>
        /// This is an external implementation and must be based on <see cref="Interfaces.IGenericDriver"/>.
        /// </summary>
        Custom,

        /// <summary>
        /// WinRing0 / OLS driver.
        /// </summary>
        WinRing0,

        /// <summary>
        /// PawnIO driver.
        /// </summary>
        PawnIO,
    }
}
