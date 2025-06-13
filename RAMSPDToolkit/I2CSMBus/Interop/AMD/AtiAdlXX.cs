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

namespace RAMSPDToolkit.I2CSMBus.Interop.AMD
{
    /// <summary>
    /// Contains delegates for AMD Display Library.
    /// </summary>
    internal sealed class AtiAdl
    {
        public delegate IntPtr ADL_MAIN_MALLOC_CALLBACK(int x);

        /// <summary>
        /// Function to initialize the ADL2 interface and to obtain client's context handle.<br/>
        /// Clients can use ADL2 versions of ADL APIs to assure that there is no interference with other
        /// ADL clients that are running in the same process.
        /// Such clients have to call ADL2_Main_Control_Create first to obtain ADL_CONTEXT_HANDLE instance
        /// that has to be passed to each subsequent ADL2 call and finally destroyed using <see cref="_ADL2_Main_Control_Destroy"/>.
        /// ADL initialized using ADL2_Main_Control_Create will not enforce serialization of ADL API executions by multiple threads.
        /// Multiple threads will be allowed to enter to ADL at the same time. Note that ADL library is not guaranteed to be thread-safe.
        /// Client that calls ADL2_Main_Control_Create have to provide its own mechanism for ADL calls serialization.
        /// </summary>
        /// <param name="callback">The memory allocation function for memory buffer allocation. This must be provided by the user.
        /// ADL internally uses this callback to allocate memory for the output parameters returned to the user and user is responsible
        /// to free the memory once used for these parameters. ADL internal takes care of allocating and de allocating the memory
        /// for its local variables.</param>
        /// <param name="iEnumConnectedAdapters">Specify a value of 0 to retrieve adapter information for all adapters that have
        /// ever been present in the system. Specify a value of 1 to retrieve adapter information only for adapters that are
        /// physically present and enabled in the system.</param>
        /// <param name="context">ADL_CONTEXT_HANDLE instance that has to be passed to each subsequent ADL2 call and
        /// finally destroyed using ADL2_Main_Control_Destroy.</param>
        /// <returns>If the function succeeds, the return value is <see cref="AMDConstants.ADL_OK"/>.
        /// Otherwise the return value is an ADL error code.</returns>
        public delegate int _ADL2_Main_Control_Create(ADL_MAIN_MALLOC_CALLBACK callback, int iEnumConnectedAdapters, out IntPtr context);

        /// <summary>
        /// Destroy client's ADL context.<br/>
        /// Clients can use ADL2 versions of ADL APIs to assure that there is no interference with other ADL clients that
        /// are running in the same process and to assure that ADL APIs are thread-safe.
        /// Such clients have to call <see cref="_ADL2_Main_Control_Create"/> first to obtain ADL_CONTEXT_HANDLE instance
        /// that has to be passed to each subsequent ADL2 call and finally destroyed using ADL2_Main_Control_Destroy.
        /// </summary>
        /// <param name="context">ADL_CONTEXT_HANDLE instance to destroy.</param>
        /// <returns>If the function succeeds, the return value is <see cref="AMDConstants.ADL_OK"/>.
        /// Otherwise the return value is an ADL error code.</returns>
        public delegate int _ADL2_Main_Control_Destroy(IntPtr context);

        /// <summary>
        /// Function to retrieve the primary display adapter index.<br/>
        /// This function retrieves the adapter index for the primary display adapter.
        /// </summary>
        /// <param name="context">Client's ADL context handle ADL_CONTEXT_HANDLE obtained from <see cref="_ADL2_Main_Control_Create"/>.</param>
        /// <param name="lpPrimaryAdapterIndex">The pointer to the ADL index handle of the primary adapter.</param>
        /// <returns>If the function succeeds, the return value is <see cref="AMDConstants.ADL_OK"/>.
        /// Otherwise the return value is an ADL error code.</returns>
        public delegate int _ADL2_Adapter_Primary_Get(IntPtr context, out int lpPrimaryAdapterIndex);

        /// <summary>
        /// Function to write and read I2C.<br/>
        /// This function writes and reads I2C.
        /// </summary>
        /// <param name="context">Client's ADL context handle ADL_CONTEXT_HANDLE obtained from <see cref="_ADL2_Main_Control_Create"/>.</param>
        /// <param name="iAdapterIndex">The ADL index handle of the desired adapter.</param>
        /// <param name="pI2C">A pointer to the <see cref="ADLI2C"/> structure.</param>
        /// <returns>If the function succeeds, the return value is <see cref="AMDConstants.ADL_OK"/>.
        /// Otherwise the return value is an ADL error code.</returns>
        public delegate int _ADL2_Display_WriteAndReadI2C(IntPtr context, int iAdapterIndex, IntPtr pI2C);

        /// <summary>
        /// ADL local interface. Retrieves extended adapter information for given adapter or all OS-known adapters.<br/>
        /// This function retrieves information for passed adapter or if pass -1, information of all OS-known adapters
        /// in the system. OS-known adapters can include adapters that are physically present in the
        /// system (logical adapters) as well as ones that are no longer present in the system but are still recognized by the OS.
        /// </summary>
        /// <param name="context">Client's ADL context handle ADL_CONTEXT_HANDLE obtained from <see cref="_ADL2_Main_Control_Create"/>.</param>
        /// <param name="iAdapterIndex">The ADL index handle of the desired adapter or -1 if all adapters are desired.</param>
        /// <param name="numAdapters">Number of items in the lppAdapterInfo array.
        /// Can pass NULL pointer if passing an adapter index (in which case only one AdapterInfo is returned).</param>
        /// <param name="lppAdapterInfoX2">A pointer to the pointer of <see cref="AdapterInfoX2"/> array.
        /// Initialize to NULL before calling this API.
        /// ADL will allocate the necessary memory, using the user provided callback function.</param>
        /// <returns>If the function valid, the return value is 1. Otherwise it is 0.</returns>
        public delegate int _ADL2_Adapter_AdapterInfoX4_Get(IntPtr context, int iAdapterIndex, out int numAdapters, out IntPtr lppAdapterInfoX2);
    }
}
