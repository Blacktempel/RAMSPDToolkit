/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * Copyright (c) 2026 Florian K.
 *
 * Code inspiration, improvements and fixes are from, but not limited to, following projects:
 * LibreHardwareMonitor; Linux Kernel; OpenRGB; WinRing0 (QCute)
 */

using BlackSharp.Core.BitOperations;
using BlackSharp.Core.ByteOperations;
using BlackSharp.Core.Globalization;
using RAMSPDToolkit.I2CSMBus;
using RAMSPDToolkit.SPD.Enums;
using RAMSPDToolkit.SPD.Interfaces;
using RAMSPDToolkit.SPD.Interop;
using RAMSPDToolkit.SPD.Interop.Shared;
using RAMSPDToolkit.SPD.Mappings;
using System.Text;

namespace RAMSPDToolkit.SPD
{
    /// <summary>
    /// Base class for DDR3 SPD accessors.
    /// </summary>
    public abstract class DDR3AccessorBase : SPDAccessor, IThermalSensor
    {
        #region Constructor

        protected DDR3AccessorBase(SMBusInterface bus, byte index)
            : base(bus, index)
        {
            _PageData = PageMappingDDR3.PageDataMapping[DDR3Constants.SPD_DDR3_PAGE];
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
            return At(DDR3Constants.SPD_DDR3_MODULE_SPD_REVISION);
        }

        public override SPDMemoryType MemoryType()
        {
            return (SPDMemoryType)At(DDR3Constants.SPD_DDR3_MODULE_MEMORY_TYPE);
        }

        public override float GetCapacity()
        {
            byte densityAndBanks = At(DDR3Constants.SPD_DDR3_MODULE_DENSITY_BANKS);
            byte organization    = At(DDR3Constants.SPD_DDR3_MODULE_ORGANIZATION);
            byte busWidth        = At(DDR3Constants.SPD_DDR3_MODULE_BUS_WIDTH);

            int densityCode         = densityAndBanks & DDR3Constants.SPD_DDR3_SDRAM_DENSITY_MASK;
            int deviceWidthCode     = organization & DDR3Constants.SPD_DDR3_SDRAM_DEVICE_WIDTH_MASK;
            int rankCode            = (organization >> DDR3Constants.SPD_DDR3_MODULE_RANKS_SHIFT) & DDR3Constants.SPD_DDR3_MODULE_RANKS_MASK;
            int primaryBusWidthCode = busWidth & DDR3Constants.SPD_DDR3_PRIMARY_BUS_WIDTH_MASK;

            if (densityCode         > DDR3Constants.SPD_DDR3_MAX_SDRAM_DENSITY_CODE
             || deviceWidthCode     > DDR3Constants.SPD_DDR3_MAX_SDRAM_DEVICE_WIDTH_CODE
             || primaryBusWidthCode > DDR3Constants.SPD_DDR3_MAX_PRIMARY_BUS_WIDTH_CODE)
            {
                return 0f;
            }

            int sdramCapacityMbit = 256 << densityCode;
            int sdramDeviceWidth  = 4 << deviceWidthCode;
            int primaryBusWidth   = 8 << primaryBusWidthCode;
            int ranks             = rankCode + 1;

            float moduleCapacityMByte = (sdramCapacityMbit / 8f)
                                      * (primaryBusWidth / (float)sdramDeviceWidth)
                                      * ranks;

            return moduleCapacityMByte / 1024f;
        }

        public override byte ModuleManufacturerContinuationCode()
        {
            return BitHandler.UnsetBit(At(DDR3Constants.SPD_DDR3_MODULE_MANUFACTURER_CONTINUATION_CODE), DDR3Constants.SPD_DDR3_MANUFACTURER_CONTINUATION_CODE_ODD_PARITY_BIT);
        }

        public override byte ModuleManufacturerIDCode()
        {
            return At(DDR3Constants.SPD_DDR3_MODULE_MANUFACTURER_ID_CODE);
        }

        public override byte ModuleManufacturingLocation()
        {
            return At(DDR3Constants.SPD_DDR3_MODULE_MANUFACTURING_LOCATION);
        }

        public override DateTime? ModuleManufacturingDate()
        {
            var year = At(DDR3Constants.SPD_DDR3_MODULE_MANUFACTURING_DATE_BEGIN);
            var week = At(DDR3Constants.SPD_DDR3_MODULE_MANUFACTURING_DATE_END);

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

            var serialNumberBytes = At(DDR3Constants.SPD_DDR3_MODULE_SERIAL_NUMBER_BEGIN, DDR3Constants.SPD_DDR3_MODULE_SERIAL_NUMBER_LENGTH);

            foreach (var b in serialNumberBytes)
            {
                sb.Append($"{b:X2}");
            }

            return sb.ToString();
        }

        public override string ModulePartNumber()
        {
            var partNumberBytes = At(DDR3Constants.SPD_DDR3_MODULE_PART_NUMBER_BEGIN, DDR3Constants.SPD_DDR3_MODULE_PART_NUMBER_LENGTH);

            return Encoding.ASCII.GetString(partNumberBytes).Trim('\0', ' ');
        }

        public override byte ModuleRevisionCode()
        {
            return At(DDR3Constants.SPD_DDR3_MODULE_REVISION_CODE);
        }

        public override byte DRAMManufacturerContinuationCode()
        {
            return BitHandler.UnsetBit(At(DDR3Constants.SPD_DDR3_DRAM_MANUFACTURER_CONTINUATION_CODE), DDR3Constants.SPD_DDR3_MANUFACTURER_CONTINUATION_CODE_ODD_PARITY_BIT);
        }

        public override byte DRAMManufacturerIDCode()
        {
            return At(DDR3Constants.SPD_DDR3_DRAM_MANUFACTURER_ID_CODE);
        }

        public override byte ManufacturerSpecificData(ushort index)
        {
            if (index >= DDR3Constants.SPD_DDR3_MANUFACTURER_SPECIFIC_DATA_LENGTH)
            {
                return 0;
            }

            return At((ushort)(DDR3Constants.SPD_DDR3_MANUFACTURER_SPECIFIC_DATA_BEGIN + index));
        }

        public override bool ChangePage(PageData pageData)
        {
            if (PageMappingDDR3.PageDataMapping[DDR3Constants.SPD_DDR3_PAGE].HasFlag(pageData))
            {
                SetPage(DDR3Constants.SPD_DDR3_PAGE);
                return PageData.HasFlag(pageData);
            }

            return false;
        }

        protected override byte GetPage()
        {
            return DDR3Constants.SPD_DDR3_PAGE;
        }

        protected override void SetPage(byte page)
        {
            if (page == DDR3Constants.SPD_DDR3_PAGE)
            {
                _PageData = PageMappingDDR3.PageDataMapping[DDR3Constants.SPD_DDR3_PAGE];
            }
        }

        #endregion

        #region IThermalSensor

        public abstract bool UpdateTemperature();

        #endregion

        #region Protected

        protected ushort RawTemperatureAdjust(ushort rawTemperature)
        {
            //Strip status and reserved bits while retaining the sign bit
            return (ushort)(rawTemperature & 0x1FFE);
        }

        #endregion
    }
}
