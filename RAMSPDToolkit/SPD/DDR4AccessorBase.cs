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

using BlackSharp.Core.BitOperations;
using BlackSharp.Core.ByteOperations;
using BlackSharp.Core.Globalization;
using RAMSPDToolkit.I2CSMBus;
using RAMSPDToolkit.I2CSMBus.Interop.Shared;
using RAMSPDToolkit.SPD.Enums;
using RAMSPDToolkit.SPD.Interfaces;
using RAMSPDToolkit.SPD.Interop;
using RAMSPDToolkit.SPD.Interop.Shared;
using RAMSPDToolkit.SPD.Mappings;
using System.Text;

namespace RAMSPDToolkit.SPD
{
    /// <summary>
    /// Base class for DDR4 SPD accessors.
    /// </summary>
    public abstract class DDR4AccessorBase : SPDAccessor, IThermalSensor
    {
        #region Constructor

        protected DDR4AccessorBase(SMBusInterface bus, byte index)
            : base(bus, index)
        {
            var page = GetPage();

            if (PageMappingDDR4.PageDataMapping.TryGetValue(page, out var pageData))
            {
                _PageData = pageData;
            }
        }

        #endregion

        #region Properties

        public bool  HasThermalSensor       { get; protected set; }
        public float Temperature            { get; protected set; } = float.NaN;
        public float TemperatureResolution  { get; protected set; } = float.NaN;
        public float ThermalSensorLowLimit  { get; protected set; } = float.NaN;
        public float ThermalSensorHighLimit { get; protected set; } = float.NaN;

        #endregion

        #region SPDAccessor

        public override byte SPDRevision()
        {
            return At(DDR4Constants.SPD_DDR4_MODULE_SPD_REVISION);
        }

        public override SPDMemoryType MemoryType()
        {
            return (SPDMemoryType)At(DDR4Constants.SPD_DDR4_MODULE_MEMORY_TYPE);
        }

        public override float GetCapacity()
        {
            var densityAndBanks      = At(DDR4Constants.SPD_DDR4_MODULE_DENSITY_BANKS         );
            var primaryPackageType   = At(DDR4Constants.SPD_DDR4_MODULE_PRIMARY_PACKAGE_TYPE  );
            var secondaryPackageType = At(DDR4Constants.SPD_DDR4_MODULE_SECONDARY_PACKAGE_TYPE);
            var organization         = At(DDR4Constants.SPD_DDR4_MODULE_ORGANIZATION          );

            var capacityPerDie = BitHandler.GetBits(densityAndBanks, 0, 3);

            var primaryDies   = BitHandler.GetBits(primaryPackageType  , 4, 6);
            var secondaryDies = BitHandler.GetBits(secondaryPackageType, 4, 6);

            var rankMix      = BitHandler.GetBits(organization, 6, 6);
            var packageRanks = BitHandler.GetBits(organization, 3, 5);

            if (rankMix == DDR4Constants.SPD_DDR4_RANKMIX_SYMMETRICAL)
            {
                return GetDies(primaryDies)
                     * GetCapacityPerDie(capacityPerDie)
                     * GetRanks(packageRanks);
            }
            else if (rankMix == DDR4Constants.SPD_DDR4_RANKMIX_ASYMMETRICAL)
            {
                return
                (
                    GetDies(primaryDies  ) * GetCapacityPerDie(capacityPerDie)
                  + GetDies(secondaryDies) * GetCapacityPerDie(capacityPerDie)
                )
                * GetRanks(packageRanks);
            }

            throw new Exception($"{nameof(DDR4Constants.SPD_DDR4_MODULE_ORGANIZATION)} {nameof(rankMix)} is invalid.");
        }

        public override byte ModuleManufacturerContinuationCode()
        {
            return BitHandler.UnsetBit(At(DDR4Constants.SPD_DDR4_MODULE_MANUFACTURER_CONTINUATION_CODE), DDR4Constants.SPD_DDR4_MANUFACTURER_CONTINUATION_CODE_ODD_PARITY_BIT);
        }

        public override byte ModuleManufacturerIDCode()
        {
            return At(DDR4Constants.SPD_DDR4_MODULE_MANUFACTURER_ID_CODE);
        }

        public override byte ModuleManufacturingLocation()
        {
            return At(DDR4Constants.SPD_DDR4_MODULE_MANUFACTURING_LOCATION);
        }

        public override DateTime? ModuleManufacturingDate()
        {
            var year = At(DDR4Constants.SPD_DDR4_MODULE_MANUFACTURING_DATE_BEGIN);
            var week = At(DDR4Constants.SPD_DDR4_MODULE_MANUFACTURING_DATE_END);

            //Sometimes there is no data
            if (year == 0 && week == 0)
            {
                return null;
            }

            return ISOWeek.ToDateTime(BinaryHandler.NormalizeBcd(year) + 2000, BinaryHandler.NormalizeBcd(week));
        }

        public override string ModuleSerialNumber()
        {
            var sb = new StringBuilder();

            for (ushort i = DDR4Constants.SPD_DDR4_MODULE_SERIAL_NUMBER_BEGIN; i < DDR4Constants.SPD_DDR4_MODULE_SERIAL_NUMBER_END; ++i)
            {
                var c = At(i);

                sb.Append(c);
            }

            return sb.ToString();
        }

        public override string ModulePartNumber()
        {
            var sb = new StringBuilder();

            for (ushort i = DDR4Constants.SPD_DDR4_MODULE_PART_NUMBER_BEGIN; i < DDR4Constants.SPD_DDR4_MODULE_PART_NUMBER_END; ++i)
            {
                var c = At(i);
                var s = Encoding.ASCII.GetString(new[] { c });

                if (c == DDR4Constants.SPD_DDR4_MODULE_PART_NUMBER_UNUSED)
                {
                    continue;
                }

                sb.Append(s);
            }

            //Some manufacturers include characters like '\0'
            return sb.ToString().Trim('\0');
        }

        public override byte ModuleRevisionCode()
        {
            return At(DDR4Constants.SPD_DDR4_MODULE_REVISION_CODE);
        }

        public override byte DRAMManufacturerContinuationCode()
        {
            return BitHandler.UnsetBit(At(DDR4Constants.SPD_DDR4_DRAM_MANUFACTURER_CONTINUATION_CODE), DDR4Constants.SPD_DDR4_MANUFACTURER_CONTINUATION_CODE_ODD_PARITY_BIT);
        }

        public override byte DRAMManufacturerIDCode()
        {
            return At(DDR4Constants.SPD_DDR4_DRAM_MANUFACTURER_ID_CODE);
        }

        public override byte ManufacturerSpecificData(ushort index)
        {
            const int manufacturerSpecificDataMaxLength = DDR4Constants.SPD_DDR4_MANUFACTURER_SPECIFIC_DATA_END
                                                        - DDR4Constants.SPD_DDR4_MANUFACTURER_SPECIFIC_DATA_BEGIN;

            if (index > manufacturerSpecificDataMaxLength)
            {
                return 0;
            }

            return At((ushort)(DDR4Constants.SPD_DDR4_MANUFACTURER_SPECIFIC_DATA_BEGIN + index));
        }

        public override bool ChangePage(PageData pageData)
        {
            foreach (var item in PageMappingDDR4.PageDataMapping)
            {
                if (item.Value.HasFlag(pageData))
                {
                    SetPage(item.Key);

                    return PageData.HasFlag(pageData);
                }
            }

            return false;
        }

        protected override byte GetPage()
        {
            //Read the current page
            var status = _Bus.i2c_smbus_read_byte(DDR4Constants.SPD_DDR4_ADDRESS_PAGE);

            if (status >= 0)
            {
                //If the read operation on 0x36 is successful, we're on page 0
                return 0;
            }
            else if (status == -SharedConstants.ENXIO)
            {
                //If the read operation fails due to a NACK, we're on page 1
                return 1;
            }
            else
            {
                //Any other error is an actual error
                return byte.MaxValue;
            }
        }

        protected override void SetPage(byte page)
        {
            //Switch page
            var status = _Bus.i2c_smbus_write_byte_data((byte)(DDR4Constants.SPD_DDR4_ADDRESS_PAGE + page), 0x00, DDR4Constants.SPD_DDR4_EEPROM_PAGE_MASK);

            //Page change OK
            if (status >= 0)
            {
                if (PageMappingDDR4.PageDataMapping.TryGetValue(page, out var pageData))
                {
                    _PageData = pageData;
                }
            }

            Thread.Sleep(SPDConstants.SPD_IO_DELAY);
        }

        #endregion

        #region IThermalSensor

        public abstract bool UpdateTemperature();

        #endregion

        #region Protected

        protected ushort RawTemperatureAdjust(ushort rawTemperature)
        {
            //Strip away not required bits
            rawTemperature = (ushort)(rawTemperature & 0xFFF);

            return rawTemperature;
        }

        #endregion

        #region Private

        int GetDies(int rawValue)
        {
            int dies = 1;

            switch (rawValue)
            {
                case 0b000:
                    dies = 1;
                    break;
                case 0b001:
                    dies = 2;
                    break;
                case 0b010:
                    dies = 3;
                    break;
                case 0b011:
                    dies = 4;
                    break;
                case 0b100:
                    dies = 5;
                    break;
                case 0b101:
                    dies = 6;
                    break;
                case 0b110:
                    dies = 7;
                    break;
                case 0b111:
                    dies = 8;
                    break;
            }

            return dies;
        }

        float GetCapacityPerDie(int rawValue)
        {
            float gb = 0;

            switch (rawValue)
            {
                case 0b0000:
                    gb = 0.256f;
                    break;
                case 0b0001:
                    gb = 0.512f;
                    break;
                case 0b0010:
                    gb = 1;
                    break;
                case 0b0011:
                    gb = 2;
                    break;
                case 0b0100:
                    gb = 4;
                    break;
                case 0b0101:
                    gb = 8;
                    break;
                case 0b0110:
                    gb = 16;
                    break;
                case 0b0111:
                    gb = 32;
                    break;
                case 0b1000:
                    gb = 12;
                    break;
                case 0b1001:
                    gb = 24;
                    break;
            }

            return gb;
        }

        int GetRanks(int rawValue)
        {
            byte ranks = 0;

            switch (rawValue)
            {
                case 0b000:
                    ranks = 1;
                    break;
                case 0b001:
                    ranks = 2;
                    break;
                case 0b010:
                    ranks = 3;
                    break;
                case 0b011:
                    ranks = 4;
                    break;
                case 0b100:
                    ranks = 5;
                    break;
                case 0b101:
                    ranks = 6;
                    break;
                case 0b110:
                    ranks = 7;
                    break;
                case 0b111:
                    ranks = 8;
                    break;
            }

            return ranks;
        }

        int GetBusWidth(int rawValue)
        {
            byte width = 0;

            switch (rawValue)
            {
                case 0b000:
                    width = 8;
                    break;
                case 0b001:
                    width = 16;
                    break;
                case 0b010:
                    width = 32;
                    break;
                case 0b011:
                    width = 64;
                    break;
            }

            return width;
        }

        int GetDeviceWidth(int rawValue)
        {
            byte width = 0;

            switch (rawValue)
            {
                case 0b000:
                    width = 4;
                    break;
                case 0b001:
                    width = 8;
                    break;
                case 0b010:
                    width = 16;
                    break;
                case 0b011:
                    width = 32;
                    break;
            }

            return width;
        }

        #endregion
    }
}
