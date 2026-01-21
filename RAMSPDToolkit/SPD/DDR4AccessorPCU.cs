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
using BlackSharp.Core.Extensions;
using RAMSPDToolkit.I2CSMBus;
using RAMSPDToolkit.I2CSMBus.Interfaces;
using RAMSPDToolkit.I2CSMBus.Interop.Intel;
using RAMSPDToolkit.I2CSMBus.Interop.PawnIO;
using RAMSPDToolkit.Logging;
using RAMSPDToolkit.SPD.Interfaces;
using RAMSPDToolkit.SPD.Interop;
using RAMSPDToolkit.SPD.Interop.Shared;
using RAMSPDToolkit.SPD.Timings;
using RAMSPDToolkit.Utilities;

namespace RAMSPDToolkit.SPD
{
    /// <summary>
    /// Accessor for DDR4 SPD via PCU.<br/>
    /// This works for Windows and Linux.
    /// </summary>
    /// <remarks>Please refer to JEDEC Standard for EE1004 and TSE2004av for property definitions.</remarks>
    public sealed class DDR4AccessorPCU : DDR4AccessorBase, IThermalDataDDR4
    {
        #region Constructor

        public DDR4AccessorPCU(SMBusInterface bus, byte slot)
            : base(bus, (byte)(((bus as IIntelPCUSMBus).SMBusIndex * 4) + slot))
        {
            _Slot = slot;
            _SPDEncoded = PCUUtilities.Encode(SPDOpcode, _Slot);
            _ThermalEncoded = PCUUtilities.Encode(ThermalSensorOpcode, _Slot);

            //Check for thermal sensor
            HasThermalSensor = ProbeThermalSensor();

            if (HasThermalSensor)
            {
                //We want these values to be read, just wait until SMBus is not busy
                ReadThermalSensorConfiguration(SPDConstants.SPD_CFG_RETRIES);
            }

            SDRAMTimings = new DDR4Timings(this);
        }

        #endregion

        #region Fields

        const byte SPDOpcode = 0xA;
        const byte ThermalSensorOpcode = 0x3;

        readonly byte _Slot;

        readonly byte _SPDEncoded;
        readonly byte _ThermalEncoded;

        #endregion

        #region Properties

        public ushort ThermalSensorCapabilitiesRegister { get; private set; } = ushort.MaxValue;

        public ushort ThermalSensorConfigurationRegister { get; private set; } = ushort.MaxValue;

        public float ThermalSensorCriticalLimit { get; private set; } = float.NaN;

        public ushort ThermalSensorManufacturerID { get; private set; } = ushort.MaxValue;

        public ushort ThermalSensorDeviceID { get; private set; } = ushort.MaxValue;

        /// <inheritdoc cref="DDR4Timings"/>
        public DDR4Timings SDRAMTimings { get; private set; }

        #endregion

        #region Public

        public static bool IsAvailable(SMBusInterface bus, byte slot)
        {
            if (bus is SMBusPCU
             || (bus is SMBusPawnIO p && p.PawnIOSMBusIdentifier == PawnIOSMBusIdentifier.IntelPCU))
            { }
            else
            {
                return false;
            }

            var spdEncoded = PCUUtilities.Encode(SPDOpcode, slot);

            //Read memory type
            var status = bus.i2c_smbus_read_byte_data(DDR4Constants.SPD_DDR4_MODULE_MEMORY_TYPE, spdEncoded);

            if (status < 0)
            {
                LogSimple.LogTrace($"{nameof(DDR4AccessorPCU)}.{nameof(IsAvailable)} failed to read memory type due to error {status}.");

                return false;
            }

            //Check if memory type is DDR4
            return ((SPDMemoryType)status).AnyOf(SPDMemoryType.SPD_DDR4_SDRAM,
                                                 SPDMemoryType.SPD_DDR4E_SDRAM,
                                                 SPDMemoryType.SPD_LPDDR4_SDRAM,
                                                 SPDMemoryType.SPD_LPDDR4X_SDRAM);
        }

        #endregion

        #region DDR4Accessor

        public override byte At(ushort address)
        {
            //Ensure address is valid
            if (address >= DDR4Constants.SPD_DDR4_EEPROM_LENGTH)
            {
                return 0xFF;
            }

            //Switch to the page containing address
            if (address < PCUConstants.PageSize)
            {
                SetPage(0);
            }
            else
            {
                SetPage(1);

                //Adjust address to be within page
                address -= PCUConstants.PageSize;
            }

            //Read value
            RetryReadByteData(_Bus, (byte)address, _SPDEncoded, SPDConstants.SPD_DATA_RETRIES, out var value);

            //Return value
            return value;
        }

        public override bool UpdateTemperature()
        {
            //Set page to 0 to read volatile data
            SetPage(0);

            var status = RetryReadWordDataSwapped(_Bus, DDR4Constants.SPD_DDR4_TEMPERATURE_ADDRESS, _ThermalEncoded, SPDConstants.SPD_TS_RETRIES, out ushort temp);

            temp = RawTemperatureAdjust(temp);

            if (status >= 0)
            {
                Temperature = SPDTemperatureConverter.CheckAndConvertTemperature(temp);
            }
            else
            {
                LogSimple.LogTrace($"Temperature read failed with status {status}.");
            }

            return status >= 0;
        }

        protected override void SetPage(byte page)
        {
            if (_Bus is SMBusPCU pcu)
            {
                pcu.SetBank(page);
            }
            else if (_Bus is SMBusPawnIO pcuPawnIO
                  && pcuPawnIO.PawnIOSMBusIdentifier == PawnIOSMBusIdentifier.IntelPCU)
            {
                pcuPawnIO.SetBank(page);
            }
        }

        #endregion

        #region Private

        bool ProbeThermalSensor()
        {
            var status = _Bus.i2c_smbus_read_byte_data(DDR4Constants.SPD_DDR4_THERMAL_SENSOR_BYTE, _SPDEncoded);

            if (status < 0)
            {
                LogSimple.LogTrace($"Failed to read {nameof(DDR4Constants.SPD_DDR4_THERMAL_SENSOR_BYTE)}.");
            }
            else
            {
                //Check if thermal sensor bit is set
                if (BitHandler.IsBitSet((byte)status, DDR4Constants.SPD_DDR4_THERMAL_SENSOR_BIT))
                {
                    LogSimple.LogTrace($"{nameof(DDR4AccessorPCU)} ({Index}) has {nameof(DDR4Constants.SPD_DDR4_THERMAL_SENSOR_BIT)} set.");
                    return true;
                }
                else
                {
                    LogSimple.LogTrace($"{nameof(DDR4AccessorPCU)} ({Index}) does not have {nameof(DDR4Constants.SPD_DDR4_THERMAL_SENSOR_BIT)} set.");
                    LogSimple.LogTrace($"Checking another way if thermal sensor is present.");

                    //Do a read to the thermal sensors address to check if it is available
                    status = _Bus.i2c_smbus_read_byte_data(0x00, _ThermalEncoded);

                    if (status < 0)
                    {
                        LogSimple.LogTrace($"{nameof(DDR4AccessorPCU)} ({Index}) Thermal sensor not found.");
                    }
                    else
                    {
                        //If there is an ACK to the quick read, there is an "unregistered" thermal sensor
                        LogSimple.LogTrace($"{nameof(DDR4AccessorPCU)} ({Index}) Unregistered thermal sensor found.");

                        return true;
                    }
                }
            }

            return false;
        }

        void ReadThermalSensorConfiguration(int retries)
        {
            //Set page to 0
            SetPage(0);

            ushort wordTemp;

            //Sensor Capabilities
            var status = RetryReadWordDataSwapped(_Bus, DDR4Constants.SPD_DDR4_THERMAL_SENSOR_CAPABILITIES_REGISTER, _ThermalEncoded, retries, out wordTemp);
            if (status < 0)
            {
                LogSimple.LogTrace($"Reading {nameof(DDR4Constants.SPD_DDR4_THERMAL_SENSOR_CAPABILITIES_REGISTER)} failed with status {status}.");
            }
            else
            {
                ThermalSensorCapabilitiesRegister = wordTemp;

                TemperatureResolution = 0.5f / (1 << ((ThermalSensorCapabilitiesRegister & 0x18) >> 3));

                LogSimple.LogTrace($"{nameof(ThermalSensorCapabilitiesRegister)} = {ThermalSensorCapabilitiesRegister}.");
            }

            //Sensor Configuration
            status = RetryReadWordDataSwapped(_Bus, DDR4Constants.SPD_DDR4_THERMAL_SENSOR_CONFIGURATION_REGISTER, _ThermalEncoded, retries, out wordTemp);
            if (status < 0)
            {
                LogSimple.LogTrace($"Reading {nameof(DDR4Constants.SPD_DDR4_THERMAL_SENSOR_CONFIGURATION_REGISTER)} failed with status {status}.");
            }
            else
            {
                ThermalSensorConfigurationRegister = wordTemp;

                LogSimple.LogTrace($"{nameof(ThermalSensorConfigurationRegister)} = {ThermalSensorConfigurationRegister}.");
            }

            //Sensor High Limit
            status = RetryReadWordDataSwapped(_Bus, DDR4Constants.SPD_DDR4_THERMAL_SENSOR_HIGH_LIMIT, _ThermalEncoded, retries, out wordTemp);
            if (status < 0)
            {
                LogSimple.LogTrace($"Reading {nameof(DDR4Constants.SPD_DDR4_THERMAL_SENSOR_HIGH_LIMIT)} failed with status {status}.");
            }
            else
            {
                wordTemp = RawTemperatureAdjust(wordTemp);

                ThermalSensorHighLimit = SPDTemperatureConverter.CheckAndConvertTemperature(wordTemp);

                LogSimple.LogTrace($"{nameof(ThermalSensorHighLimit)} = {ThermalSensorHighLimit}.");
            }

            //Sensor Low Limit
            status = RetryReadWordDataSwapped(_Bus, DDR4Constants.SPD_DDR4_THERMAL_SENSOR_LOW_LIMIT, _ThermalEncoded, retries, out wordTemp);
            if (status < 0)
            {
                LogSimple.LogTrace($"Reading {nameof(DDR4Constants.SPD_DDR4_THERMAL_SENSOR_LOW_LIMIT)} failed with status {status}.");
            }
            else
            {
                wordTemp = RawTemperatureAdjust(wordTemp);

                ThermalSensorLowLimit = SPDTemperatureConverter.CheckAndConvertTemperature(wordTemp);

                LogSimple.LogTrace($"{nameof(ThermalSensorLowLimit)} = {ThermalSensorLowLimit}.");
            }

            //Sensor Critical Limit
            status = RetryReadWordDataSwapped(_Bus, DDR4Constants.SPD_DDR4_THERMAL_SENSOR_CRIT_LIMIT, _ThermalEncoded, retries, out wordTemp);
            if (status < 0)
            {
                LogSimple.LogTrace($"Reading {nameof(DDR4Constants.SPD_DDR4_THERMAL_SENSOR_CRIT_LIMIT)} failed with status {status}.");
            }
            else
            {
                wordTemp = RawTemperatureAdjust(wordTemp);

                ThermalSensorCriticalLimit = SPDTemperatureConverter.CheckAndConvertTemperature(wordTemp);

                LogSimple.LogTrace($"{nameof(ThermalSensorCriticalLimit)} = {ThermalSensorCriticalLimit}.");
            }

            //Sensor Manufacturer
            status = RetryReadWordDataSwapped(_Bus, DDR4Constants.SPD_DDR4_THERMAL_SENSOR_MANUFACTURER, _ThermalEncoded, retries, out wordTemp);
            if (status < 0)
            {
                LogSimple.LogTrace($"Reading {nameof(DDR4Constants.SPD_DDR4_THERMAL_SENSOR_MANUFACTURER)} failed with status {status}.");
            }
            else
            {
                ThermalSensorManufacturerID = wordTemp;

                LogSimple.LogTrace($"{nameof(ThermalSensorManufacturerID)} = {ThermalSensorManufacturerID}.");
            }

            //Sensor DeviceID / Revision Register
            status = RetryReadWordDataSwapped(_Bus, DDR4Constants.SPD_DDR4_THERMAL_SENSOR_DEVICEID, _ThermalEncoded, retries, out wordTemp);
            if (status < 0)
            {
                LogSimple.LogTrace($"Reading {nameof(DDR4Constants.SPD_DDR4_THERMAL_SENSOR_DEVICEID)} failed with status {status}.");
            }
            else
            {
                ThermalSensorDeviceID = wordTemp;

                LogSimple.LogTrace($"{nameof(ThermalSensorDeviceID)} = {ThermalSensorDeviceID}.");
            }
        }

        #endregion
    }
}
