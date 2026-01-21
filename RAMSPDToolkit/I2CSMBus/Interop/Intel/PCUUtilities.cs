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

using RAMSPDToolkit.Logging;

namespace RAMSPDToolkit.I2CSMBus.Interop.Intel
{
    internal static class PCUUtilities
    {
        #region Public

        public static byte Encode(byte opcode, byte slot)
        {
            if (opcode >= 0xF)
            {
                LogSimple.LogWarn($"{nameof(PCUUtilities)}: {nameof(opcode)} value too high ({opcode}).");
                return 0;
            }

            if (slot >= 0xF)
            {
                LogSimple.LogWarn($"{nameof(PCUUtilities)}: {nameof(slot)} value too high ({slot}).");
                return 0;
            }

            var encoded = (byte)((opcode << 4) | slot);

            return encoded;
        }

        public static (byte Opcode, byte Slot) Decode(byte encoded)
        {
            var opcode = (byte)(encoded >> 4);
            var slot = (byte)(encoded & 0x0F);

            return new(opcode, slot);
        }

        #endregion
    }
}
