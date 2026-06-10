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
using RAMSPDToolkit.SPD.Interop.Shared;

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
        public const uint GoBit             = 0x00080000;
        public const uint TsodActiveBit     = 0x00100000;
        public const uint CommandToggleBit  = 0x20000000;
        public const uint CommandKeepMask   = 0xFFEFFFFF;
        public const uint CommandPrefix     = 0x20080000;
        public const uint PageCommand       = 0x2008B6;
        public const int SlotShift = 8;
        public const int OpShift = 11;

        public const ushort PageSize = DDR4Constants.SPD_DDR4_EEPROM_LENGTH / 2;

        public const byte DDR4PageAddress0 = DDR4Constants.SPD_DDR4_ADDRESS_PAGE;
        public const byte DDR4PageAddress1 = DDR4Constants.SPD_DDR4_ADDRESS_PAGE + 1;
        public const byte DDR4ThermalSensorBegin = 0x18;
        public const byte DDR4ThermalSensorEnd = 0x1F;

        public const byte SPDAddressBegin = SPDConstants.SPD_BEGIN;
        public const byte SPDAddressEnd = SPDConstants.SPD_END;
        public const byte SPDOpcode = 0x0A;
        public const byte ThermalSensorOpcode = 0x03;

        //Status bits
        public const uint StsBusy      = 0x1;
        public const uint StsError     = 0x2;
        public const uint StsDoneMask  = 0x6;
        public const uint StsDoneOk    = 0x4;
        public const uint StsStateMask = 0x7;

        public const int StartRetries           = 5;
        public const int PageStatusRetries      = 9999;
        public const int PageCommandRetries     = 999;
        public const int TransferStatusRetries  = 99999;
        public const int TransferToggleRetries  = 9999;

        public const int AddressSpace8BitSize = 0x100;

        public const byte MaxSMBusControllers = 2;
    }
}
