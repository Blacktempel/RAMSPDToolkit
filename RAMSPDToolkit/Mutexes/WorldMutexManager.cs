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

using BlackSharp.Core.Interop.Windows.Mutexes;
using RAMSPDToolkit.I2CSMBus.Interop.Shared;
using RAMSPDToolkit.PCI;

namespace RAMSPDToolkit.Mutexes
{
    /// <summary>
    /// Holds instances to all <see cref="WorldMutex"/>es.
    /// </summary>
    internal class WorldMutexManager
    {
        #region Constructor

        static WorldMutexManager()
        {
            //Initialize world mutex for SMBus access
            WorldSMBusMutex = new WorldMutex(SharedConstants.SMBusMutexName);

            //Initialize world mutex for PCI access
            WorldPCIMutex = new WorldMutex(PCIConstants.PCIMutexName);
        }

        #endregion

        #region Properties

        internal static WorldMutex WorldSMBusMutex { get; }
        internal static WorldMutex WorldPCIMutex   { get; }

        #endregion
    }
}
