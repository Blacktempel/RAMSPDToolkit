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

using BlackSharp.Core.Logging;

namespace RAMSPDToolkit.Logging
{
    /// <summary>
    /// Provides logging for RAMSPDToolkit diagnostics.
    /// </summary>
    public static class LogSimple
    {
        #region Properties

        /// <summary>
        /// Gets or sets the logger that receives toolkit diagnostics.
        /// Set to <see langword="null"/> to disable logging.
        /// </summary>
        public static ILogger Logger { get; set; }

        #endregion

        #region Public

        public static void LogTrace(string message)
        {
            Log(LogLevel.Trace, message);
        }

        public static void LogWarn(string message)
        {
            Log(LogLevel.Warn, message);
        }

        public static void LogError(string message)
        {
            Log(LogLevel.Error, message);
        }

        #endregion

        #region Private

        static void Log(LogLevel logLevel, string message)
        {
            Logger?.Log(logLevel, nameof(RAMSPDToolkit), message, null);
        }

        #endregion
    }
}
