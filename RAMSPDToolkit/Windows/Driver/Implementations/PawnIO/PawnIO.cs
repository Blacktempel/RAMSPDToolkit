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

using RAMSPDToolkit.I2CSMBus.Interop.PawnIO;
using RAMSPDToolkit.Windows.Driver.Implementations.PawnIO.Interop;
using RAMSPDToolkit.Windows.Driver.Interfaces;
using RAMSPDToolkit.Windows.Utilities;

namespace RAMSPDToolkit.Windows.Driver.Implementations.PawnIO
{
    /// <summary>
    /// PawnIO driver implementation.
    /// </summary>
    public sealed class PawnIO : IPawnIODriver
    {
        #region Constructor

        public PawnIO()
        {
            _PawnIOLib = new PawnIOLib();
        }

        #endregion

        #region Fields

        IntPtr _Handle;

        PawnIOLib _PawnIOLib;

        #endregion

        #region Properties

        public bool IsOpen => _PawnIOLib.IsModuleLoaded;

        #endregion

        #region Public

        public bool Load()
        {
            /*var status = */_PawnIOLib.PawnIOOpen(out _Handle);

            //_PawnIOLib.PawnIOVersion(out var version);

            return _Handle != IntPtr.Zero;
        }

        public unsafe bool LoadModule(PawnIOSMBusIdentifier pawnIOSMBusIdentifier)
        {
            string moduleResourceFilename = null;

            switch (pawnIOSMBusIdentifier)
            {
                case PawnIOSMBusIdentifier.I801:
                    moduleResourceFilename = PawnIOConstants.I801ModuleFilename;
                    break;
                case PawnIOSMBusIdentifier.Piix4:
                    moduleResourceFilename = PawnIOConstants.Piix4ModuleFilename;
                    break;
                case PawnIOSMBusIdentifier.NCT6793:
                    moduleResourceFilename = PawnIOConstants.NCT6793ModuleFilename;
                    break;
                default:
                    break;
            }

            if (moduleResourceFilename == null)
            {
                return false;
            }

            byte[] bytes = null;

            try
            {
                using MemoryStream ms = new();

                using var gzipStream = ResourceFileExtractor.GetResourceFileGZipStream(moduleResourceFilename);

                gzipStream.CopyTo(ms);

                bytes = ms.ToArray();
            }
            catch (Exception)
            {
                return false;
            }

            unsafe
            {
                fixed (byte* moduleData = bytes)
                {
                    var returnValue = _PawnIOLib.PawnIOLoad(_Handle, moduleData, (uint)bytes.Length);

                    return returnValue == 0;
                }
            }
        }

        public int Execute(string name, long[] inBuffer, uint inSize, long[] outBuffer, uint outSize, out uint returnSize)
        {
            return _PawnIOLib.PawnIOExecute(_Handle, name, inBuffer, inSize, outBuffer, outSize, out returnSize);
        }

        public void Unload()
        {
            if (_PawnIOLib != null)
            {
                _PawnIOLib.PawnIOClose(_Handle);

                _PawnIOLib.Dispose();
                _PawnIOLib = null;
            }
        }

        #endregion
    }
}

#endif
