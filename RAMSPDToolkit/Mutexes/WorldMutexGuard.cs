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

namespace RAMSPDToolkit.Mutexes
{
    /// <summary>
    /// Mutex guard for automatic lock and unlock of a <see cref="WorldMutex"/> via <see cref="IDisposable"/>.
    /// </summary>
    internal sealed class WorldMutexGuard : IDisposable
    {
        #region Constructor

        public WorldMutexGuard(WorldMutex worldMutex)
        {
            _WorldMutex = worldMutex;

            _WorldMutex.Lock();
        }

        #endregion

        #region Fields

        WorldMutex _WorldMutex;

        #endregion

        #region Public

        public void Dispose()
        {
            _WorldMutex.Unlock();
        }

        #endregion
    }
}
