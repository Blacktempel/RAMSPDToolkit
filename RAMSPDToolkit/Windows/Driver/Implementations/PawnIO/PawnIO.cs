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
        }

        #endregion

        #region Fields

        static PawnIOLib _PawnIOLib;

        #endregion

        #region Properties

        public bool IsOpen => _PawnIOLib.IsModuleLoaded;

        #endregion

        #region Public

        public bool Load()
        {
            if (_PawnIOLib == null)
            {
                _PawnIOLib = new PawnIOLib();
            }

            return _PawnIOLib != null
                && _PawnIOLib.IsModuleLoaded;
        }

        public IPawnIOModule LoadModule(PawnIOSMBusIdentifier pawnIOSMBusIdentifier)
        {
            var module = new PawnIOModule();

            if (module.Open()
             && module.IsOpen
             && LoadModuleFromResource(module, pawnIOSMBusIdentifier))
            {
                return module;
            }

            return null;
        }

        public void Unload()
        {
            if (_PawnIOLib != null)
            {
                _PawnIOLib.Dispose();
                _PawnIOLib = null;
            }
        }

        #endregion

        #region Private

        unsafe bool LoadModuleFromResource(PawnIOModule module, PawnIOSMBusIdentifier pawnIOSMBusIdentifier)
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
                    var returnValue = _PawnIOLib.PawnIOLoad(module._Handle, moduleData, (uint)bytes.Length);

                    return returnValue == 0;
                }
            }
        }

        #endregion
    }
}

#endif
