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
using RAMSPDToolkit.I2CSMBus;
using RAMSPDToolkit.Logging;
using RAMSPDToolkit.SPD.Interfaces;
using RAMSPDToolkit.SPD.Interop;
using RAMSPDToolkit.SPD.Interop.Shared;
using RAMSPDToolkit.SPD.Timings;
using RAMSPDToolkit.Utilities;

namespace RAMSPDToolkit.SPD
{
    /// <summary>
    /// Accessor for DDR3 SPD.<br/>
    /// This works for Windows and Linux.
    /// </summary>
    /// <remarks>Please refer to the JEDEC DDR3 SPD and TSE2002av standards for property definitions.</remarks>
    public sealed class DDR3Accessor : DDR3AccessorBase, IThermalDataDDR3
    {
        #region Constructor

        public DDR3Accessor(SMBusInterface bus, byte address)
            : base(bus, (byte)(address - SPDConstants.SPD_BEGIN))
        {
            _Address = address;

            //Set thermal sensor address
            _ThermalSensorAddress = (byte)(0x18 | (address & 0x07));

            //Check for thermal sensor
            HasThermalSensor = ProbeThermalSensor();

            if (HasThermalSensor)
            {
                //We want these values to be read, just wait until SMBus is not busy
                ReadThermalSensorConfiguration(SPDConstants.SPD_CFG_RETRIES);
            }

            SDRAMTimings = new DDR3Timings(this);
        }

        #endregion

        #region Fields

        readonly byte _Address;
        readonly byte _ThermalSensorAddress;

        #endregion

        #region Properties

        public ushort ThermalSensorCapabilitiesRegister { get; private set; } = ushort.MaxValue;

        public ushort ThermalSensorConfigurationRegister { get; private set; } = ushort.MaxValue;

        public float ThermalSensorCriticalLimit { get; private set; } = float.NaN;

        public ushort ThermalSensorManufacturerID { get; private set; } = ushort.MaxValue;

        public ushort ThermalSensorDeviceID { get; private set; } = ushort.MaxValue;

        /// <inheritdoc cref="DDR3Timings"/>
        public DDR3Timings SDRAMTimings { get; private set; }

        #endregion

        #region Public

        /// <summary>
        /// Detects if DDR3 or LPDDR3 RAM is available at the specified address.
        /// </summary>
        /// <param name="bus">SMBus to check for RAM.</param>
        /// <param name="address">Address to check.</param>
        /// <returns>True if DDR3 or LPDDR3 is available at the specified address; false otherwise.</returns>
        public static bool IsAvailable(SMBusInterface bus, byte address)
        {
            int value = bus.i2c_smbus_read_byte_data(address, (byte)DDR3Constants.SPD_DDR3_MODULE_MEMORY_TYPE);

            return value >= 0 && ((SPDMemoryType)value == SPDMemoryType.SPD_DDR3_SDRAM ||
                                  (SPDMemoryType)value == SPDMemoryType.SPD_LPDDR3_SDRAM);
        }

        #endregion

        #region DDR3Accessor

        public override byte At(ushort address)
        {
            if (address >= DDR3Constants.SPD_DDR3_EEPROM_LENGTH)
            {
                return 0xFF;
            }

            RetryReadByteData(_Bus, _Address, (byte)address, SPDConstants.SPD_DATA_RETRIES, out var value);

            Thread.Sleep(SPDConstants.SPD_IO_DELAY);

            return value;
        }

        public override byte[] At(ushort address, byte length)
        {
            if (length == 0 || address >= DDR3Constants.SPD_DDR3_EEPROM_LENGTH || address + length > DDR3Constants.SPD_DDR3_EEPROM_LENGTH)
            {
                return [];
            }

            RetryReadI2CBlockData(_Bus, _Address, (byte)address, length, SPDConstants.SPD_DATA_RETRIES, out var value);

            Thread.Sleep(SPDConstants.SPD_IO_DELAY);

            return value;
        }

        public override bool UpdateTemperature()
        {
            var status = RetryReadWordDataSwapped(_Bus, _ThermalSensorAddress, DDR3Constants.SPD_DDR3_TEMPERATURE_ADDRESS, SPDConstants.SPD_TS_RETRIES, out ushort temp);

            if (status >= 0)
            {
                Temperature = SPDTemperatureConverter.CheckAndConvertTemperature(RawTemperatureAdjust(temp));
            }
            else
            {
                LogSimple.LogTrace($"Temperature read failed with status {status}.");
            }

            return status >= 0;
        }

        #endregion

        #region Private

        bool ProbeThermalSensor()
        {
            var status = _Bus.i2c_smbus_read_byte_data(_Address, DDR3Constants.SPD_DDR3_THERMAL_SENSOR_BYTE);

            if (status < 0)
            {
                LogSimple.LogTrace($"Failed to read {nameof(DDR3Constants.SPD_DDR3_THERMAL_SENSOR_BYTE)}.");
            }
            else if (BitHandler.IsBitSet((byte)status, DDR3Constants.SPD_DDR3_THERMAL_SENSOR_BIT))
            {
                LogSimple.LogTrace($"0x{_Address:X2} has {nameof(DDR3Constants.SPD_DDR3_THERMAL_SENSOR_BIT)} set.");
                return true;
            }
            else
            {
                LogSimple.LogTrace($"0x{_Address:X2} does not have {nameof(DDR3Constants.SPD_DDR3_THERMAL_SENSOR_BIT)} set.");
                LogSimple.LogTrace("Checking another way if thermal sensor is present.");

                //Fallback: do a quick read to the thermal sensor address to check if it is available
                status = _Bus.i2c_smbus_write_quick(_ThermalSensorAddress, 0x00);

                if (status < 0)
                {
                    LogSimple.LogTrace($"0x{_Address:X2} Thermal sensor address did not respond or quick was unsupported.");

                    //Fallback: probe a mandatory temperature sensor register
                    status = _Bus.i2c_smbus_read_word_data(_ThermalSensorAddress, DDR3Constants.SPD_DDR3_THERMAL_SENSOR_CAPABILITIES_REGISTER);

                    if (status < 0)
                    {
                        LogSimple.LogTrace($"0x{_Address:X2} Thermal sensor not found.");
                        return false;
                    }
                }

                LogSimple.LogTrace($"0x{_Address:X2} Unregistered thermal sensor found.");
                return true;
            }

            return false;
        }

        void ReadThermalSensorConfiguration(int retries)
        {
            ushort wordTemp;

            //Sensor Capabilities
            var status = RetryReadWordDataSwapped(_Bus, _ThermalSensorAddress, DDR3Constants.SPD_DDR3_THERMAL_SENSOR_CAPABILITIES_REGISTER, retries, out wordTemp);
            if (status < 0)
            {
                LogSimple.LogTrace($"Reading {nameof(DDR3Constants.SPD_DDR3_THERMAL_SENSOR_CAPABILITIES_REGISTER)} failed with status {status}.");
            }
            else
            {
                ThermalSensorCapabilitiesRegister = wordTemp;
                TemperatureResolution = 0.5f / (1 << ((ThermalSensorCapabilitiesRegister & 0x18) >> 3));

                LogSimple.LogTrace($"{nameof(ThermalSensorCapabilitiesRegister)} = {ThermalSensorCapabilitiesRegister}.");
            }

            //Sensor Configuration
            status = RetryReadWordDataSwapped(_Bus, _ThermalSensorAddress, DDR3Constants.SPD_DDR3_THERMAL_SENSOR_CONFIGURATION_REGISTER, retries, out wordTemp);
            if (status < 0)
            {
                LogSimple.LogTrace($"Reading {nameof(DDR3Constants.SPD_DDR3_THERMAL_SENSOR_CONFIGURATION_REGISTER)} failed with status {status}.");
            }
            else
            {
                ThermalSensorConfigurationRegister = wordTemp;

                LogSimple.LogTrace($"{nameof(ThermalSensorConfigurationRegister)} = {ThermalSensorConfigurationRegister}.");
            }

            //Sensor High Limit
            status = RetryReadWordDataSwapped(_Bus, _ThermalSensorAddress, DDR3Constants.SPD_DDR3_THERMAL_SENSOR_HIGH_LIMIT, retries, out wordTemp);
            if (status < 0)
            {
                LogSimple.LogTrace($"Reading {nameof(DDR3Constants.SPD_DDR3_THERMAL_SENSOR_HIGH_LIMIT)} failed with status {status}.");
            }
            else
            {
                ThermalSensorHighLimit = SPDTemperatureConverter.CheckAndConvertTemperature(RawTemperatureAdjust(wordTemp));

                LogSimple.LogTrace($"{nameof(ThermalSensorHighLimit)} = {ThermalSensorHighLimit}.");
            }

            //Sensor Low Limit
            status = RetryReadWordDataSwapped(_Bus, _ThermalSensorAddress, DDR3Constants.SPD_DDR3_THERMAL_SENSOR_LOW_LIMIT, retries, out wordTemp);
            if (status < 0)
            {
                LogSimple.LogTrace($"Reading {nameof(DDR3Constants.SPD_DDR3_THERMAL_SENSOR_LOW_LIMIT)} failed with status {status}.");
            }
            else
            {
                ThermalSensorLowLimit = SPDTemperatureConverter.CheckAndConvertTemperature(RawTemperatureAdjust(wordTemp));

                LogSimple.LogTrace($"{nameof(ThermalSensorLowLimit)} = {ThermalSensorLowLimit}.");
            }

            //Sensor Critical Limit
            status = RetryReadWordDataSwapped(_Bus, _ThermalSensorAddress, DDR3Constants.SPD_DDR3_THERMAL_SENSOR_CRITICAL_LIMIT, retries, out wordTemp);
            if (status < 0)
            {
                LogSimple.LogTrace($"Reading {nameof(DDR3Constants.SPD_DDR3_THERMAL_SENSOR_CRITICAL_LIMIT)} failed with status {status}.");
            }
            else
            {
                ThermalSensorCriticalLimit = SPDTemperatureConverter.CheckAndConvertTemperature(RawTemperatureAdjust(wordTemp));

                LogSimple.LogTrace($"{nameof(ThermalSensorCriticalLimit)} = {ThermalSensorCriticalLimit}.");
            }

            //Sensor Manufacturer
            status = RetryReadWordDataSwapped(_Bus, _ThermalSensorAddress, DDR3Constants.SPD_DDR3_THERMAL_SENSOR_MANUFACTURER, retries, out wordTemp);
            if (status < 0)
            {
                LogSimple.LogTrace($"Reading {nameof(DDR3Constants.SPD_DDR3_THERMAL_SENSOR_MANUFACTURER)} failed with status {status}.");
            }
            else
            {
                ThermalSensorManufacturerID = wordTemp;

                LogSimple.LogTrace($"{nameof(ThermalSensorManufacturerID)} = {ThermalSensorManufacturerID}.");
            }

            //Sensor Device ID / Revision Register
            status = RetryReadWordDataSwapped(_Bus, _ThermalSensorAddress, DDR3Constants.SPD_DDR3_THERMAL_SENSOR_DEVICE_ID, retries, out wordTemp);
            if (status < 0)
            {
                LogSimple.LogTrace($"Reading {nameof(DDR3Constants.SPD_DDR3_THERMAL_SENSOR_DEVICE_ID)} failed with status {status}.");
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
