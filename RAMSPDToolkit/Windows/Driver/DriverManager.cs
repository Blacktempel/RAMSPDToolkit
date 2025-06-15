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

using RAMSPDToolkit.Logging;

#if !RELEASE_NDD

using RAMSPDToolkit.Windows.Driver.Implementations;
using RAMSPDToolkit.Windows.Driver.Implementations.WinRing0;

#endif

namespace RAMSPDToolkit.Windows.Driver
{
    /// <summary>
    /// Manage driver implementation.
    /// </summary>
    public static class DriverManager
    {
        #region Properties

#if !RELEASE_NDD
        /// <summary>
        /// Identifies current driver implementation.<br/>
        /// This is only valid after a driver has been set to <see cref="Driver"/> or after <see cref="InitDriver"/> was called.
        /// </summary>
        public static InternalDriver DriverImplementation { get; private set; }
            = InternalDriver.Custom;
#endif

        static IDriver _Driver;
        /// <summary>
        /// Current driver implementation instance.
        /// </summary>
        /// <remarks>Once it has been set, it cannot be changed.
        /// </remarks>
        public static IDriver Driver
        {
            get { return _Driver; }
            set
            {
                if (_Driver != null)
                {
                    LogSimple.LogWarn($"{nameof(Driver)} was resetted.");
                }

                _Driver = value;
            }
        }

        #endregion

        #region Public

#if !RELEASE_NDD
        /// <summary>
        /// Initialize <see cref="Driver"/> with internal driver implementation.<br/>
        /// Use this if you don't want to implement your own driver.
        /// </summary>
        /// <param name="defaultDriver">Internal driver to use.</param>
        public static void InitDriver(InternalDriver defaultDriver)
        {
            switch (defaultDriver)
            {
                case InternalDriver.OLS:
                    Driver = new OLS();
                    break;
                case InternalDriver.Custom:
                default:
                    break;
            }

            DriverImplementation = defaultDriver;
        }
#endif

        #endregion
    }
}
