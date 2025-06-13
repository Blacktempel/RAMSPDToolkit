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

using RAMSPDToolkit.Windows.Driver.Implementations;
using RAMSPDToolkit.Windows.Driver.Implementations.WinRing0.Interop;

namespace RAMSPDToolkit.I2CSMBus.Interop.AMD
{
    internal sealed class AtiAdlModule : IDisposable
    {
        #region Constructor

        public AtiAdlModule()
        {
            IsModuleLoaded = LoadLibraryFunctions();
        }

        ~AtiAdlModule()
        {
            Dispose();
        }

        #endregion

        #region Fields

        bool _Disposed;

        IntPtr _Module;

        internal AtiAdl._ADL2_Main_Control_Create       ADL2_Main_Control_Create      ;
        internal AtiAdl._ADL2_Main_Control_Destroy      ADL2_Main_Control_Destroy     ;
        internal AtiAdl._ADL2_Adapter_Primary_Get       ADL2_Adapter_Primary_Get      ;
        internal AtiAdl._ADL2_Display_WriteAndReadI2C   ADL2_Display_WriteAndReadI2C  ;
        internal AtiAdl._ADL2_Adapter_AdapterInfoX4_Get ADL2_Adapter_AdapterInfoX4_Get;

        #endregion

        #region Properties

        public bool IsModuleLoaded { get; private set; }

        public IntPtr Context { get; set; }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (!_Disposed)
            {
                if (_Module != IntPtr.Zero)
                {
                    ADL2_Main_Control_Destroy(Context);

                    Kernel32.FreeLibrary(_Module);
                }

                _Disposed = true;
            }
        }

        #endregion

        #region Private

        bool LoadLibraryFunctions()
        {
            _Module = Kernel32.LoadLibrary("AtiAdlXX.dll");

            if (_Module == IntPtr.Zero)
            {
                /*---------------------------------------------------------------------*\
                | A 32 bit calling application on 64 bit OS will fail to LoadLibrary.   |
                | Try to load the 32 bit library (atiadlxy.dll) instead                 |
                \*---------------------------------------------------------------------*/
                _Module = Kernel32.LoadLibrary("AtiAdlXY.dll");

                if (_Module == IntPtr.Zero)
                {
                    return false;
                }
            }

            ADL2_Main_Control_Create       = DynamicLoader.GetDelegate<AtiAdl._ADL2_Main_Control_Create      >(_Module, "ADL2_Main_Control_Create"      );
            ADL2_Main_Control_Destroy      = DynamicLoader.GetDelegate<AtiAdl._ADL2_Main_Control_Destroy     >(_Module, "ADL2_Main_Control_Destroy"     );
            ADL2_Adapter_Primary_Get       = DynamicLoader.GetDelegate<AtiAdl._ADL2_Adapter_Primary_Get      >(_Module, "ADL2_Adapter_Primary_Get"      );
            ADL2_Display_WriteAndReadI2C   = DynamicLoader.GetDelegate<AtiAdl._ADL2_Display_WriteAndReadI2C  >(_Module, "ADL2_Display_WriteAndReadI2C"  );
            ADL2_Adapter_AdapterInfoX4_Get = DynamicLoader.GetDelegate<AtiAdl._ADL2_Adapter_AdapterInfoX4_Get>(_Module, "ADL2_Adapter_AdapterInfoX4_Get");

            return ADL2_Main_Control_Create       != null &&
                   ADL2_Main_Control_Destroy      != null &&
                   ADL2_Adapter_Primary_Get       != null &&
                   ADL2_Display_WriteAndReadI2C   != null &&
                   ADL2_Adapter_AdapterInfoX4_Get != null ;
        }

        #endregion
    }
}
