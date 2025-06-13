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

namespace RAMSPDToolkit.Logging
{
    internal static class LogSimple
    {
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
            Logger.Instance.Add(logLevel, message, DateTime.Now);
        }

        #endregion
    }
}
