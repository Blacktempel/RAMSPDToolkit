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

using RAMSPDToolkit.Windows.Driver.Implementations;
using RAMSPDToolkit.Windows.Driver.Implementations.WinRing0;

namespace RAMSPDToolkit.Windows.Driver
{
    /// <summary>
    /// Manage driver implementation.
    /// </summary>
    public static class DriverManager
    {
        #region Properties

        /// <summary>
        /// Identifies current driver implementation.<br/>
        /// This is only valid after a driver has been set to <see cref="Driver"/> or after <see cref="InitDriver"/> was called.
        /// </summary>
        public static InternalDriver DriverImplementation { get; private set; }
            = InternalDriver.Custom;

        static IDriver _Driver;
        /// <summary>
        /// Current driver implementation instance.
        /// </summary>
        /// <remarks>Once it has been set, it cannot be changed.<br/>
        /// This includes setting it via <see cref="InitDriver"/>.</remarks>
        public static IDriver Driver
        {
            get { return _Driver; }
            set
            {
                //Disallow changing driver after it has been set
                if (_Driver == null)
                {
                    _Driver = value;
                }
            }
        }

        #endregion

        #region Public

        /// <summary>
        /// Initialize <see cref="Driver"/> with internal driver implementation.<br/>
        /// Use this if you don't want to implement your own driver.
        /// </summary>
        /// <param name="defaultDriver">Internal driver to use.</param>
        public static void InitDriver(InternalDriver defaultDriver)
        {
            if (_Driver == null)
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
        }

        #endregion
    }
}
