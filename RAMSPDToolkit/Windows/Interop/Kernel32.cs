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

using System.Runtime.InteropServices;

namespace RAMSPDToolkit.Windows.Interop
{
    /// <summary>
    /// Imports of native functions via <see cref="DllImportAttribute"/>.
    /// </summary>
    internal static class Kernel32
    {
        const string DLL_NAME = "kernel32.dll";

        [DllImport(DLL_NAME, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport(DLL_NAME, ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern IntPtr GetProcAddress(IntPtr module, string methodName);

        [DllImport(DLL_NAME)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FreeLibrary(IntPtr module);
    }
}
