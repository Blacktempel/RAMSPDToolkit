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

using BlackSharp.Core.Interop.Windows.Utilities;
using System.Runtime.InteropServices;

namespace RAMSPDToolkit.I2CSMBus.Interop.NVIDIA
{
    internal sealed class NVAPIModule : ModuleBase
    {
        #region Fields

        internal NVAPI._NvAPI_QueryInterface NvAPI_QueryInterface;

        internal NVAPI._NvAPI_EnumPhysicalGPUs      NvAPI_EnumPhysicalGPUs     ;
        internal NVAPI._NvAPI_GPU_GetFullName       NvAPI_GPU_GetFullName      ;
        internal NVAPI._NvAPI_GPU_GetPCIIdentifiers NvAPI_GPU_GetPCIIdentifiers;
        internal NVAPI._NvAPI_I2CReadEx             NvAPI_I2CReadEx            ;
        internal NVAPI._NvAPI_I2CWriteEx            NvAPI_I2CWriteEx           ;

        #endregion

        #region Protected

        protected override string GetModuleFilename()
        {
            if (IntPtr.Size == 4)
            {
                //32-bit
                return "nvapi.dll";
            }
            else
            {
                //64-bit
                return "nvapi64.dll";
            }
        }

        protected override bool LoadLibraryFunctions()
        {
            if (IsModuleLoaded)
            {
                return true;
            }

            NvAPI_QueryInterface = GetDelegate<NVAPI._NvAPI_QueryInterface>("nvapi_QueryInterface");

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

        #endregion

        #region Private

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
