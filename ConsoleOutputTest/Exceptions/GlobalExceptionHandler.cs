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

using BlackSharp.Core.Extensions;

namespace ConsoleOutputTest.Exceptions
{
    internal class GlobalExceptionHandler
    {
        #region Public

        public static void RegisterGlobalExceptionHandler()
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledExceptionAppDomain;
        }

        #endregion

        #region Private

        static void OnUnhandledExceptionAppDomain(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception)
            {
                Console.WriteLine((e.ExceptionObject as Exception).FullExceptionString());
            }
            else
            {
                Console.WriteLine($"{(e.IsTerminating ? "Fatal error: " : "Error: ")}" + "The application will terminate now.");
            }

            if (e.IsTerminating)
                Environment.Exit(1);
        }

        #endregion
    }
}
