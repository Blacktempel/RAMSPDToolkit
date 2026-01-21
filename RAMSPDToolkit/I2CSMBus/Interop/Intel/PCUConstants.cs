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

using RAMSPDToolkit.SPD.Interop;

namespace RAMSPDToolkit.I2CSMBus.Interop.Intel
{
    internal static class PCUConstants
    {
        //PCU SMBus Register Layout (offsets from PCI config space of PCU / SMBus device)
        public const int RegStep = 0x04;
        public const int CmdBase = 0x9C; //Command = CmdBase + idx * 4
        public const int StsBase = 0xA8; //Status  = StsBase + idx * 4
        public const int DatBase = 0xB4; //Data    = DatBase + idx * 4
        //public const int TSODBase = 0xC0; //TSOD   = TSODBase + idx * 4 //not used here

        //Bits in CMD
        public const uint GoBit = 0x80000;
        public const uint WordBit = 0x20000;
        public const uint PecBit = 0x100000; //Unused here
        public const uint CmdMaskKeep = 0xFFE00000; //Keep upper bits from old CMD
        public const int SlotShift = 8;
        public const int OpShift = 11;

        public const uint BankMask = 0xB000;
        public const uint Bank0Mask = 0x600;
        public const uint Bank1Mask = 0x700;
        public const ushort PageSize = DDR4Constants.SPD_DDR4_EEPROM_LENGTH / 2;

        //Status bits
        public const uint StsBusy = 0x1;
        public const uint StsError = 0x2;

        public const int StartRetries = 5;
        //public const int ByteRetries = 10;
        public const int CmdDelayMs = 3; //= 5
        public const int PollSleepMs = 1;
        public const int PollTimeoutMs = 10; //= 250;

        public const byte MaxSMBusControllers = 2;
    }
}
