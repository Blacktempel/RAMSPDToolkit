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

using RAMSPDToolkit.I2CSMBus;
using RAMSPDToolkit.I2CSMBus.Interop;
using RAMSPDToolkit.Logging;
using RAMSPDToolkit.SPD.Enums;
using RAMSPDToolkit.SPD.Interfaces;
using RAMSPDToolkit.SPD.Interop;
using RAMSPDToolkit.SPD.Interop.Shared;
using RAMSPDToolkit.SPD.Mappings;
using RAMSPDToolkit.Utilities;
using System.Text;

using OS = RAMSPDToolkit.Software.OperatingSystem;

namespace RAMSPDToolkit.SPD
{
    /// <summary>
    /// Base class for DDR5 SPD accessors.
    /// </summary>
    public abstract class DDR5AccessorBase : SPDAccessor, IThermalSensor
    {
        #region Constructor

        protected DDR5AccessorBase(SMBusInterface bus, byte address)
            : base(bus, address)
        {
            ReadWriteRecoveryTime(SPDConstants.SPD_CFG_RETRIES);

            var page = GetPage();

            if (PageMappingDDR5.PageDataMapping.TryGetValue(page, out var pageData))
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

        /// <summary>
        /// Device write recovery time in milliseconds.
        /// </summary>
        public decimal WriteRecoveryTime { get; private set; } = decimal.Zero;

        #endregion

        #region SPDAccessor

        public override byte SPDRevision()
        {
            return At(DDR5Constants.SPD_DDR5_MODULE_SPD_REVISION);
        }

        public override SPDMemoryType MemoryType()
        {
            return (SPDMemoryType)At(DDR5Constants.SPD_DDR5_MODULE_MEMORY_TYPE);
        }

        public override byte ModuleManufacturerContinuationCode()
        {
            return BitHandler.UnsetBit(At(DDR5Constants.SPD_DDR5_MODULE_MANUFACTURER_CONTINUATION_CODE), DDR5Constants.SPD_DDR5_MANUFACTURER_CONTINUATION_CODE_ODD_PARITY_BIT);
        }

        public override byte ModuleManufacturerIDCode()
        {
            return At(DDR5Constants.SPD_DDR5_MODULE_MANUFACTURER_ID_CODE);
        }

        public override byte ModuleManufacturingLocation()
        {
            return At(DDR5Constants.SPD_DDR5_MODULE_MANUFACTURING_LOCATION);
        }

        public override DateTime? ModuleManufacturingDate()
        {
            var year = At(DDR5Constants.SPD_DDR5_MODULE_MANUFACTURING_DATE_BEGIN);
            var week = At(DDR5Constants.SPD_DDR5_MODULE_MANUFACTURING_DATE_END);

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

            for (ushort i = DDR5Constants.SPD_DDR5_MODULE_SERIAL_NUMBER_BEGIN; i < DDR5Constants.SPD_DDR5_MODULE_SERIAL_NUMBER_END; ++i)
            {
                byte c;

                if (i == 0)
                {
                    c = At(i, true);
                }
                else
                {
                    c = At(i, false);
                }

                sb.Append(c);
            }

            return sb.ToString();
        }

        public override string ModulePartNumber()
        {
            var sb = new StringBuilder();

            for (ushort i = DDR5Constants.SPD_DDR5_MODULE_PART_NUMBER_BEGIN; i < DDR5Constants.SPD_DDR5_MODULE_PART_NUMBER_END; ++i)
            {
                byte c;

                if (i == 0)
                {
                    c = At(i, true);
                }
                else
                {
                    c = At(i, false);
                }

                var s = Encoding.ASCII.GetString(new[] { c });

                if (c == DDR5Constants.SPD_DDR5_MODULE_PART_NUMBER_UNUSED)
                {
                    continue;
                }

                sb.Append(s);
            }

            //Some manufacturers include (multiple) zero terminators
            return sb.ToString().Trim('\0');
        }

        public override byte ModuleRevisionCode()
        {
            return At(DDR5Constants.SPD_DDR5_MODULE_REVISION_CODE);
        }

        public override byte DRAMManufacturerContinuationCode()
        {
            return BitHandler.UnsetBit(At(DDR5Constants.SPD_DDR5_DRAM_MANUFACTURER_CONTINUATION_CODE), DDR5Constants.SPD_DDR5_MANUFACTURER_CONTINUATION_CODE_ODD_PARITY_BIT);
        }

        public override byte DRAMManufacturerIDCode()
        {
            return At(DDR5Constants.SPD_DDR5_DRAM_MANUFACTURER_ID_CODE);
        }

        public override byte ManufacturerSpecificData(ushort index)
        {
            const int manufacturerSpecificDataMaxLength = DDR5Constants.SPD_DDR5_MANUFACTURER_SPECIFIC_DATA_END
                                                        - DDR5Constants.SPD_DDR5_MANUFACTURER_SPECIFIC_DATA_BEGIN;

            if (index > manufacturerSpecificDataMaxLength)
            {
                return 0;
            }

            return At((ushort)(DDR5Constants.SPD_DDR5_MANUFACTURER_SPECIFIC_DATA_BEGIN + index));
        }

        public override bool ChangePage(PageData pageData)
        {
            //Loop through page mapping and get proper page for requested data
            foreach (var item in PageMappingDDR5.PageDataMapping)
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
            var status = _Bus.i2c_smbus_read_byte_data(_Address, DDR5Constants.SPD_DDR5_MREG_VIRTUAL_PAGE);

            if (status < 0)
            {
                return byte.MaxValue;
            }

            //Return the first 3 bits of the MR11 register
            return (byte)(status & 0x07);
        }

        protected override void SetPage(byte page)
        {
            if (GetPage() == page)
            {
                return;
            }

            int status;

            //Linux Kernel (i2c-i801.c) supports PROC_CALL, but unfortunately only with I2C_SMBUS_WRITE
            //while we would require a I2C_SMBUS_READ instead.
            //So we currently cannot go around the write protection for page change on Linux.
            if (_Bus.HasSPDWriteProtection && OS.IsLinux())
            {
                //No page change for write protection + Linux system
                return;
            }
            //Write protection is active (Intel) - change page via PROC_CALL
            else if (_Bus.HasSPDWriteProtection)
            {
                status = _Bus.i2c_smbus_proc_call(_Address, I2CConstants.I2C_SMBUS_READ, DDR5Constants.SPD_DDR5_MREG_VIRTUAL_PAGE, page);
            }
            else //No write protection, change it normally
            {
                status = _Bus.i2c_smbus_write_byte_data(_Address, DDR5Constants.SPD_DDR5_MREG_VIRTUAL_PAGE, page);
            }

            //Page change OK
            if (status >= 0)
            {
                if (PageMappingDDR5.PageDataMapping.TryGetValue(page, out var pageData))
                {
                    _PageData = pageData;
                }
            }

            //Use write recovery time or default
            var sleepTime = WriteRecoveryTime > 0 ? WriteRecoveryTime : SPDConstants.SPD_IO_DELAY;

            Thread.Sleep(TimeSpan.FromMilliseconds((double)sleepTime));
        }

        #endregion

        #region IThermalSensor

        public abstract bool UpdateTemperature();

        #endregion

        #region Private

        void ReadWriteRecoveryTime(int retries)
        {
            //Write Recovery Time
            var status = RetryReadByteData(_Bus, _Address, DDR5Constants.SPD_DDR5_WRITE_RECOVERY_TIME, retries, out var byteTemp);
            if (status < 0)
            {
                LogSimple.LogTrace($"Reading {nameof(DDR5Constants.SPD_DDR5_WRITE_RECOVERY_TIME)} failed with status {status}.");
            }
            else
            {
                var timeUnit = BitHandler.GetBits(byteTemp, 0, 1);
                var recUnit  = BitHandler.GetBits(byteTemp, 4, 7);

                decimal recoveryTime = 0;

                switch (recUnit)
                {
                    case byte x when (x <= 10 && x >= 0):
                        recoveryTime = x;
                        break;
                    case 0b1011:
                        recoveryTime = 50;
                        break;
                    case 0b1100:
                        recoveryTime = 100;
                        break;
                    case 0b1101:
                        recoveryTime = 200;
                        break;
                    case 0b1110:
                        recoveryTime = 500;
                        break;
                }

                switch (timeUnit)
                {
                    case 0b00: //Nanoseconds
                        recoveryTime = recoveryTime == 0 ? 0 : recoveryTime / 1_000_000M;
                        break;
                    case 0b01: //Microseconds
                        recoveryTime = recoveryTime == 0 ? 0 : recoveryTime / 1000M;
                        break;
                    case 0b10: //Milliseconds
                        //recoveryTime = recoveryTime;
                        break;
                }

                WriteRecoveryTime = recoveryTime;

                LogSimple.LogTrace($"{nameof(WriteRecoveryTime)} = {WriteRecoveryTime} ms (Raw = {byteTemp}).");
            }
        }

        #endregion
    }
}
