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

using RAMSPDToolkit.Windows.Driver.Implementations.PawnIO.Interop;
using RAMSPDToolkit.Windows.Driver.Interfaces;

namespace RAMSPDToolkit.Windows.Driver.Implementations.PawnIO
{
    internal sealed class PawnIOModule : IPawnIOModule
    {
        #region Constructor

        public PawnIOModule()
        {
            if (_PawnIOLib == null)
            {
                _PawnIOLib = new PawnIOLib();
            }
        }

        #endregion

        #region Fields

        internal IntPtr _Handle;

        static PawnIOLib _PawnIOLib;

        #endregion

        #region Properties

        public bool IsOpen => _PawnIOLib.IsModuleLoaded;

        #endregion

        #region Public

        public bool Open()
        {
            _PawnIOLib.PawnIOOpen(out _Handle);

            return _Handle != IntPtr.Zero;
        }

        public int Execute(string name, long[] inBuffer, uint inSize, long[] outBuffer, uint outSize, out uint returnSize)
        {
            return _PawnIOLib.PawnIOExecute(_Handle, name, inBuffer, inSize, outBuffer, outSize, out returnSize);
        }

        public void Unload()
        {
            _PawnIOLib?.PawnIOClose(_Handle);
        }

        #endregion
    }
}

#endif
