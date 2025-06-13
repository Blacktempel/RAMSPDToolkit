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
using OS = RAMSPDToolkit.Software.OperatingSystem;

namespace RAMSPDToolkit.I2CSMBus
{
    /// <summary>
    /// Manager class for SMBuses.
    /// </summary>
    public static class SMBusManager
    {
        #region Fields

        static List<SMBusInterface> _RegisteredSMBuses = new List<SMBusInterface>();

        #endregion

        #region Properties

        /// <summary>
        /// Determine whether to use WMI for SMBus dectection or not (will use SetupAPI instead).
        /// </summary>
        public static bool UseWMI { get; set; } = false;

        /// <summary>
        /// Holds all registered SMBus instances.
        /// </summary>
        public static IReadOnlyList<SMBusInterface> RegisteredSMBuses
        {
            get { return _RegisteredSMBuses; }
        }

        #endregion

        #region Public

        /// <summary>
        /// Gets first SMBus in <see cref="RegisteredSMBuses"/> with specified type.
        /// </summary>
        /// <typeparam name="TSMBus">Type of SMBus to get.</typeparam>
        /// <returns>Found SMBus of given type or null.</returns>
        public static TSMBus GetSMBus<TSMBus>()
            where TSMBus : SMBusInterface
        {
            return _RegisteredSMBuses.Find(i => i is TSMBus) as TSMBus;
        }

        /// <summary>
        /// Uses available detection methods to detect all currently available SMBuses.<br/>
        /// This also clears current <see cref="RegisteredSMBuses"/>.
        /// </summary>
        public static void DetectSMBuses()
        {
            _RegisteredSMBuses.Clear();

            var smbusDetectMethods = new List<Func<bool>>();

            if (OS.IsWindows())
            {
                //smbusDetectMethods.Add(I2CSMBusAmdAdl.SMBusDetect);

                if (UseWMI)
                {
                    smbusDetectMethods.Add(SMBusI801 .SMBusDetect);
                    smbusDetectMethods.Add(SMBusPiix4.SMBusDetect);
                }
                else
                {
                    smbusDetectMethods.Add(WindowsSMBusDetector.DetectSMBuses);
                }

                smbusDetectMethods.Add(SMBusNVAPI  .SMBusDetect);
                smbusDetectMethods.Add(SMBusNCT6775.SMBusDetect);
            }
            else if (OS.IsLinux())
            {
                smbusDetectMethods.Add(SMBusLinux.SMBusDetect);
            }

            LogSimple.LogTrace($"Detecting SMBuses - amount of available detection methods are {smbusDetectMethods.Count}.");

            foreach (var detection in smbusDetectMethods)
            {
                detection?.Invoke();
            }
        }

        #endregion

        #region Internal

        internal static void AddSMBus(SMBusInterface smbus)
        {
            _RegisteredSMBuses.Add(smbus);
        }

        #endregion
    }
}
