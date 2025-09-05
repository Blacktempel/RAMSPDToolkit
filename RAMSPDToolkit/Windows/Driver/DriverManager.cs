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
using RAMSPDToolkit.Windows.Driver.Implementations;
using RAMSPDToolkit.Windows.Driver.Interfaces;

#if !RELEASE_NDD

using RAMSPDToolkit.Windows.Driver.Implementations.PawnIO;
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

        /// <summary>
        /// Identifies current driver implementation.<br/>
        /// This is only valid after <see cref="LoadDriver"/> was called.
        /// </summary>
        public static DriverImplementation DriverImplementation { get; private set; }
            = DriverImplementation.Custom;

        static IDriver _Driver;
        /// <summary>
        /// Current driver implementation instance.
        /// </summary>
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

#if RELEASE_NDD
        /// <summary>
        /// Loads specified <see cref="Driver"/>.
        /// </summary>
        /// <returns>Returns boolean value whether loading driver was successful.</returns>
        public static bool LoadDriver()
        {
#else
        /// <summary>
        /// Loads specified <see cref="Driver"/>.
        /// </summary>
        /// <param name="driverImplementation">If this parameter is null: it will determine implementation based on assigned instance to <see cref="Driver"/> and its used interface (<see cref="IWinRing0Driver"/>; <see cref="IPawnIODriver"/>; <see cref="IGenericDriver"/>).<br/>
        /// If this parameter is NOT null: it will load an internal implementation based on which you have specified. <see cref="DriverImplementation.Custom"/> is invalid here.
        /// </param>
        /// <returns>Returns boolean value whether loading driver was successful.</returns>
        public static bool LoadDriver(DriverImplementation? driverImplementation = null)
        {
            //Internal implementation
            if (driverImplementation != null)
            {
                switch (driverImplementation.Value)
                {
                    case DriverImplementation.WinRing0:
                        Driver = new OLS();
                        break;
                    case DriverImplementation.PawnIO:
                        Driver = new PawnIO();
                        break;
                    default:
                        return false;
                }

                DriverImplementation = driverImplementation.Value;
            }
            else
#endif
            {
                //External implementation
                if (Driver is IWinRing0Driver)
                {
                    DriverImplementation = DriverImplementation.WinRing0;
                }
                else if (Driver is IPawnIODriver)
                {
                    DriverImplementation = DriverImplementation.PawnIO;
                }
                else if (Driver is IGenericDriver)
                {
                    DriverImplementation = DriverImplementation.Custom;
                }
                else
                {
                    return false;
                }
            }

            return Driver.Load();
        }

        /// <summary>
        /// Unloads current instance of <see cref="Driver"/>.
        /// </summary>
        public static void UnloadDriver()
        {
            Driver?.Unload();
            Driver = null;
        }

        #endregion

        #region Private

#if !RELEASE_NDD
        static void Dummy()
        {
            //Used as static assert
            Dummy_Check_WinRing0<OLS   >();
            Dummy_Check_PawnIO  <PawnIO>();
        }

        static void Dummy_Check_WinRing0<TWinRing0>()
            where TWinRing0 : IWinRing0Driver
        {
            //Empty
        }

        static void Dummy_Check_PawnIO<TPawnIO>()
            where TPawnIO : IPawnIODriver
        {
            //Empty
        }
#endif

        #endregion
    }
}
