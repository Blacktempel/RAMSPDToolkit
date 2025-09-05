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

using RAMSPDToolkit.I2CSMBus.Interop.PawnIO;

namespace RAMSPDToolkit.Windows.Driver.Interfaces
{
    /// <summary>
    /// Driver interface for PawnIO implementation.
    /// </summary>
    public interface IPawnIODriver : IDriver
    {
        int Execute(string name, long[] inBuffer, uint inSize, long[] outBuffer, uint outSize, out uint returnSize);
        bool LoadModule(PawnIOSMBusIdentifier pawnIOSMBusIdentifier);
    }
}
