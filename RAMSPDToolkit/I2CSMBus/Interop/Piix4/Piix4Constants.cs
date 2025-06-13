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

namespace RAMSPDToolkit.I2CSMBus.Interop.Piix4
{
    internal sealed class Piix4Constants
    {
        public const int PIIX4_QUICK      = 0x00;
        public const int PIIX4_BYTE       = 0x04;
        public const int PIIX4_BYTE_DATA  = 0x08;
        public const int PIIX4_WORD_DATA  = 0x0C;
        public const int PIIX4_BLOCK_DATA = 0x14;

        public const int PIIX4_SMBA_0 = 0x0B00;
        public const int PIIX4_SMBA_1 = 0x0B20;

        public const int MAX_TIMEOUT = 5000;
    }
}
