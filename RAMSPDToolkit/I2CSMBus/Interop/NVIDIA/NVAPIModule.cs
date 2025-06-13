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
using System.Runtime.InteropServices;

namespace RAMSPDToolkit.I2CSMBus.Interop.NVIDIA
{
    internal sealed class NVAPIModule : IDisposable
    {
        #region Constructor

        public NVAPIModule()
        {
            IsModuleLoaded = LoadLibraryFunctions();
        }

        ~NVAPIModule()
        {
            Dispose();
        }

        #endregion

        #region Fields

        bool _Disposed;

        IntPtr _Module;

        internal NVAPI._NvAPI_QueryInterface NvAPI_QueryInterface;

        internal NVAPI._NvAPI_EnumPhysicalGPUs      NvAPI_EnumPhysicalGPUs     ;
        internal NVAPI._NvAPI_GPU_GetFullName       NvAPI_GPU_GetFullName      ;
        internal NVAPI._NvAPI_GPU_GetPCIIdentifiers NvAPI_GPU_GetPCIIdentifiers;
        internal NVAPI._NvAPI_I2CReadEx             NvAPI_I2CReadEx            ;
        internal NVAPI._NvAPI_I2CWriteEx            NvAPI_I2CWriteEx           ;

        #endregion

        #region Properties

        public bool IsModuleLoaded { get; private set; }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (!_Disposed)
            {
                if (_Module != IntPtr.Zero)
                {
                    Kernel32.FreeLibrary(_Module);
                }

                _Disposed = true;
            }
        }

        #endregion

        #region Private

        bool LoadLibraryFunctions()
        {
            if (_Module != IntPtr.Zero)
            {
                //Already loaded
                return true;
            }

            if (IntPtr.Size == 4)
            {
                //32-bit
                _Module = Kernel32.LoadLibrary("nvapi.dll");
            }
            else
            {
                //64-bit
                _Module = Kernel32.LoadLibrary("nvapi64.dll");
            }

            if (_Module == IntPtr.Zero)
            {
                return false;
            }

            NvAPI_QueryInterface = DynamicLoader.GetDelegate<NVAPI._NvAPI_QueryInterface>(_Module, "nvapi_QueryInterface");

            if (NvAPI_QueryInterface == null)
            {
                return false;
            }

            NvAPI_EnumPhysicalGPUs      = QueryDelegate<NVAPI._NvAPI_EnumPhysicalGPUs     >(0xE5AC921F );
            NvAPI_GPU_GetFullName       = QueryDelegate<NVAPI._NvAPI_GPU_GetFullName      >(0x0CEEE8E9F);
            NvAPI_GPU_GetPCIIdentifiers = QueryDelegate<NVAPI._NvAPI_GPU_GetPCIIdentifiers>(0x2DDFB66E );
            NvAPI_I2CWriteEx            = QueryDelegate<NVAPI._NvAPI_I2CWriteEx           >(0x283AC65A );
            NvAPI_I2CReadEx             = QueryDelegate<NVAPI._NvAPI_I2CReadEx            >(0x4D7B0709 );

            return NvAPI_QueryInterface != null

                && NvAPI_EnumPhysicalGPUs      != null
                && NvAPI_GPU_GetFullName       != null
                && NvAPI_GPU_GetPCIIdentifiers != null
                && NvAPI_I2CReadEx             != null
                && NvAPI_I2CWriteEx            != null
                ;
        }

        T QueryDelegate<T>(uint interfaceID)
            where T : Delegate
        {
            if (NvAPI_QueryInterface == null)
            {
                return null;
            }

            var funcPtr = NvAPI_QueryInterface(interfaceID);

            if (funcPtr == IntPtr.Zero)
            {
                return null;
            }

            return Marshal.GetDelegateForFunctionPointer<T>(funcPtr);
        }

        #endregion
    }
}
