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

namespace RAMSPDToolkit.Software
{
    /// <summary>
    /// Represents information about an operating system.
    /// </summary>
    public static class OperatingSystem
    {
#if !NET8_0_OR_GREATER

        #region Constructor

        static OperatingSystem()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.Win32NT:
                case PlatformID.WinCE:
                    _IsWindows = true;
                    break;
                case PlatformID.Unix:
                    _IsLinux = true;
                    break;
            }
        }

        #endregion

        #region Fields

        static readonly bool _IsWindows;
        static readonly bool _IsLinux;

        #endregion

#endif

        #region Public

        /// <summary>
        /// Indicates whether the current application is running on Windows.
        /// </summary>
        /// <returns>True if current application is running on Windows, false otherwise.</returns>
        public static bool IsWindows()
        {
#if NET8_0_OR_GREATER
            return System.OperatingSystem.IsWindows();
#else
            return _IsWindows;
#endif
        }

        /// <summary>
        /// Indicates whether the current application is running on Linux.
        /// </summary>
        /// <returns>True if current application is running on Linux, false otherwise.</returns>
        public static bool IsLinux()
        {
#if NET8_0_OR_GREATER
            return System.OperatingSystem.IsLinux();
#else
            return _IsLinux;
#endif
        }

        #endregion
    }
}
