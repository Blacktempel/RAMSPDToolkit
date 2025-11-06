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

#if !RELEASE_NDD

#pragma warning disable CA1416 // Platform compatibility warning

using BlackSharp.Core.Interop.Windows.Utilities;
using Microsoft.Win32;
using RAMSPDToolkit.Logging;
using RAMSPDToolkit.Windows.Interop;
using System.Runtime.InteropServices;
using OS = BlackSharp.Core.Platform.OperatingSystem;

namespace RAMSPDToolkit.Windows.Driver.Implementations.PawnIO.Interop
{
    internal sealed class PawnIOLib : IDisposable
    {
        #region Constructor

        public PawnIOLib()
        {
            if (!OS.IsWindows())
            {
                throw new PlatformNotSupportedException();
            }

            if (!TryGetDriverPath())
            {
                LogSimple.LogError($"{nameof(PawnIOLib)} {nameof(TryGetDriverPath)} failed.");
                return;
            }

            IsModuleLoaded = LoadLibraryFunctions();

            if (!IsModuleLoaded)
            {
                LogSimple.LogError($"{nameof(PawnIOLib)} {nameof(LoadLibraryFunctions)} failed.");
                return;
            }
        }

        ~PawnIOLib()
        {
            Dispose();
        }

        #endregion

        #region Fields

        public const string PawnIOLibFilename = "PawnIOLib";

        bool _Disposed;

        IntPtr _Module;

        #region Delegates

        public delegate int _PawnIOVersion(out uint version);
        public delegate int _PawnIOOpen(out IntPtr handle);
        public unsafe delegate int _PawnIOLoad(IntPtr handle, byte* blob, uint size);
        public delegate int _PawnIOExecute(IntPtr handle, [MarshalAs(UnmanagedType.LPStr)] string name, long[] inBuffer, uint inSize, long[] outBuffer, uint outSize, out uint returnSize);
        public delegate int _PawnIOClose(IntPtr handle);

        public _PawnIOVersion PawnIOVersion;
        public _PawnIOOpen    PawnIOOpen   ;
        public _PawnIOLoad    PawnIOLoad   ;
        public _PawnIOExecute PawnIOExecute;
        public _PawnIOClose   PawnIOClose  ;

        #endregion

        #endregion

        #region Properties

        public bool IsModuleLoaded { get; private set; }

        /// <summary>
        /// File path of driver DLL file.
        /// </summary>
        public string FilePathDLL { get; private set; }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (!_Disposed)
            {
                if (_Module != IntPtr.Zero)
                {
                    Kernel32.FreeLibrary(_Module);
                    _Module = IntPtr.Zero;
                }

                _Disposed = true;
            }
        }

        #endregion

        #region Private

        bool TryGetDriverPath()
        {
            // Try getting path from registry
            if ((Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\PawnIO", "InstallLocation", null) ??
                 Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\PawnIO", "Install_Dir", null) ??
                 Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + Path.DirectorySeparatorChar + "PawnIO") is string
                {
                    Length: > 0
                } pawnIoPath)
            {
                var fullpath = Path.Combine(pawnIoPath, PawnIOLibFilename);

                FilePathDLL = fullpath;

                return true;
            }

            return false;
        }

        bool LoadLibraryFunctions()
        {
            if (_Module != IntPtr.Zero)
            {
                //Already loaded
                return true;
            }

            _Module = Kernel32.LoadLibrary(FilePathDLL);

            if (_Module == IntPtr.Zero)
            {
                return false;
            }

            PawnIOVersion = DynamicLoader.GetDelegate<_PawnIOVersion>(_Module, "pawnio_version");
            PawnIOOpen    = DynamicLoader.GetDelegate<_PawnIOOpen   >(_Module, "pawnio_open"   );
            PawnIOLoad    = DynamicLoader.GetDelegate<_PawnIOLoad   >(_Module, "pawnio_load"   );
            PawnIOExecute = DynamicLoader.GetDelegate<_PawnIOExecute>(_Module, "pawnio_execute");
            PawnIOClose   = DynamicLoader.GetDelegate<_PawnIOClose  >(_Module, "pawnio_close"  );

            return PawnIOVersion != null
                && PawnIOOpen    != null
                && PawnIOLoad    != null
                && PawnIOExecute != null
                && PawnIOClose   != null
                ;
        }

        #endregion
    }
}

#pragma warning restore CA1416 // Platform compatibility warning

#endif
