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

namespace RAMSPDToolkit.I2CSMBus.Interop.Intel
{
    internal static class IMCConstants
    {
        //IMC SMBus Register Layout (offsets from PCI config space of IMC / SMBus device)
        public const int RegStep = 0x04;
        public const int CmdBase = 0x9C; //Command = CmdBase + idx * 4
        public const int StsBase = 0xA8; //Status  = StsBase + idx * 4
        public const int DatBase = 0xB4; //Data    = DatBase + idx * 4
        //public const int TSODBase = 0xC0; //TSOD   = TSODBase + idx * 4 //not used here

        //Bits in CMD
        public const uint WordBit           = 0x00020000;
        public const uint SelPtrBit         = 0x00040000;
        public const uint WriteOperation    = 0x00008000;
        public const uint GoBit             = 0x00080000;
        public const uint TsodActiveBit     = 0x00100000;
        public const uint CommandToggleBit  = 0x20000000;
        public const uint CommandKeepMask   = 0xFFEFFFFF;
        public const uint CommandPrefix     = 0x20080000;
        public const int AddrShift = 8;

        //Status bits
        public const uint StsBusy      = 0x1;
        public const uint StsError     = 0x2;
        public const uint StsReadDone  = 0x4;
        public const uint StsWriteDone = 0x8;
        public const uint StsAnyDone   = StsError | StsReadDone | StsWriteDone;
        public const uint StsStateMask = 0x7;

        public const int PageStatusRetries      = 9999;
        public const int PageCommandRetries     = 999;
        public const int TransferStatusRetries  = 99999;
        public const int TransferToggleRetries  = 9999;

        public const byte MaxSMBusControllers = 2;
    }
}
