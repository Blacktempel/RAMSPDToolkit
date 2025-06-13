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
using RAMSPDToolkit.Logging;
using RAMSPDToolkit.SPD.Interop;
using RAMSPDToolkit.SPD.Interop.Shared;
using RAMSPDToolkit.Utilities;

namespace RAMSPDToolkit.SPD
{
    /// <summary>
    /// Accessor for DDR5 SPD.<br/>
    /// This works for Windows and Linux.
    /// </summary>
    public sealed class DDR5Accessor : DDR5AccessorBase
    {
        #region Constructor

        public DDR5Accessor(SMBusInterface bus, byte address)
            : base(bus, address)
        {
            //We want these values to be read, just wait until SMBus is not busy
            ReadThermalSensorConfiguration(SPDConstants.SPD_CFG_RETRIES);

            if (HasThermalSensor)
            {
                //Initial read for volatile data
                Update();

                //For DDR5 the value is fixed
                TemperatureResolution = DDR5Constants.SPD_DDR5_TEMPERATURE_RESOLUTION;
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Determines if thermal sensor is currently enabled.
        /// </summary>
        public bool ThermalSensorEnabled { get; private set; }

        /// <summary>
        /// Thermal sensor critical high limit temperature.
        /// </summary>
        public float ThermalSensorCriticalHighLimit { get; private set; }

        /// <summary>
        /// Thermal sensor critical low limit temperature.
        /// </summary>
        public float ThermalSensorCriticalLowLimit { get; private set; }

        /// <summary>
        /// Thermal sensor temperature status.
        /// </summary>
        public ThermalSensorStatus ThermalSensorStatus { get; private set; }

        #endregion

        #region Public

        /// <summary>
        /// Detects if a DDR5 RAM is available at specified address.
        /// </summary>
        /// <param name="bus">SMBus to check for RAM.</param>
        /// <param name="address">Address to check.</param>
        /// <returns>True if DDR5 is available at specified address; false otherwise.</returns>
        public static bool IsAvailable(SMBusInterface bus, byte address)
        {
            //Read current page
            int status = RetryReadByteData(bus, address, DDR5Constants.SPD_DDR5_MREG_VIRTUAL_PAGE, SPDConstants.SPD_DATA_RETRIES, out byte page);

            if (status < 0)
            {
                LogSimple.LogWarn($"{nameof(DDR5Accessor)}.{nameof(IsAvailable)} failed to read current page due to error {status}.");
            }

            //First 3 bits of the MR11 register
            page &= 0x07;

            //Page is off
            if (page != 0)
            {
                //We can change the page
                if (!bus.HasSPDWriteProtection)
                {
                    //Change page to 0
                    status = bus.i2c_smbus_write_byte_data(address, DDR5Constants.SPD_DDR5_MREG_VIRTUAL_PAGE, 0);

                    if (status < 0)
                    {
                        LogSimple.LogWarn($"{nameof(DDR5Accessor)}.{nameof(IsAvailable)} failed to change current page due to error {page}.");
                        return false;
                    }

                    //Page change OK, continue
                }
                else //Write protection is active
                {
                    LogSimple.LogWarn($"{nameof(DDR5Accessor)}.{nameof(IsAvailable)} page is off. Cannot change due to {nameof(bus.HasSPDWriteProtection)} being active.");
                    return false;
                }
            }

            //Try read most significant byte
            //Result should be 0x51
            var result = RetryReadByteData(bus, address, DDR5Constants.SPD_DDR5_DEVICE_TYPE_MOST, SPDConstants.SPD_DATA_RETRIES, out byte ddr5Magic);

            if (result < 0)
            {
                LogSimple.LogWarn($"{nameof(DDR5Accessor)}.{nameof(IsAvailable)} failed to read {nameof(DDR5Constants.SPD_DDR5_DEVICE_TYPE_MOST)} due to error {result}.");
            }

            //Try read least significant byte
            //Result should be 0x18
            result = RetryReadByteData(bus, address, DDR5Constants.SPD_DDR5_DEVICE_TYPE_LEAST, SPDConstants.SPD_DATA_RETRIES, out byte ddr5Sensor);

            if (result < 0)
            {
                LogSimple.LogWarn($"{nameof(DDR5Accessor)}.{nameof(IsAvailable)} failed to read {nameof(DDR5Constants.SPD_DDR5_DEVICE_TYPE_LEAST)} due to error {result}.");
            }

            //Is it a DDR5 module ?
            if (ddr5Magic  == DDR5Constants.SPD_DDR5_DEVICE_TYPE_MOST_EXPECTED
             && ddr5Sensor == DDR5Constants.SPD_DDR5_DEVICE_TYPE_LEAST_EXPECTED)
            {
                //We have a proper DDR5 + Thermal Sensor module
                return true;
            }

            //No proper DDR5 module
            return false;
        }

        #endregion

        #region DDR5Accessor

        public override byte At(ushort address)
        {
            //Ensure address is valid
            if (address >= DDR5Constants.SPD_DDR5_EEPROM_LENGTH)
            {
                return 0xFF;
            }

            //Switch to the page containing address
            SetPage((byte)(address >> DDR5Constants.SPD_DDR5_EEPROM_PAGE_SHIFT));

            //Calculate offset
            byte offset = (byte)((address & DDR5Constants.SPD_DDR5_EEPROM_PAGE_MASK) | 0x80);

            //Read value at address
            RetryReadByteData(_Bus, _Address, offset, SPDConstants.SPD_DATA_RETRIES, out var value);

            Thread.Sleep(SPDConstants.SPD_IO_DELAY);

            //Return value
            return value;
        }

        public override bool UpdateTemperature()
        {
            //Cannot read data if sensor is disabled
            if (!ThermalSensorEnabled)
            {
                return false;
            }

            //Set page to 0 to read volatile data
            SetPage(0);

            var status = RetryReadWordData(_Bus, _Address, DDR5Constants.SPD_DDR5_TEMPERATURE_ADDRESS, SPDConstants.SPD_TS_RETRIES, out ushort temp);

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

        public override void Update(int retries = SPDConstants.SPD_DATA_RETRIES)
        {
            if (HasThermalSensor)
            {
                //Set page to 0
                SetPage(0);

                byte byteTemp;

                //Thermal sensor enabled
                var status = RetryReadByteData(_Bus, _Address, DDR5Constants.SPD_DDR5_THERMAL_SENSOR_ENABLED, retries, out byteTemp);
                if (status < 0)
                {
                    LogSimple.LogTrace($"Reading {nameof(DDR5Constants.SPD_DDR5_THERMAL_SENSOR_ENABLED)} failed with status {status}.");
                }
                else
                {
                    //0 = Enabled; 1 = Disabled
                    ThermalSensorEnabled = byteTemp == 0;
                    LogSimple.LogTrace($"{nameof(ThermalSensorEnabled)} = {ThermalSensorEnabled}.");
                }

                //Thermal sensor status
                status = RetryReadByteData(_Bus, _Address, DDR5Constants.SPD_DDR5_THERMAL_SENSOR_STATUS, retries, out byteTemp);
                if (status < 0)
                {
                    LogSimple.LogTrace($"Reading {nameof(DDR5Constants.SPD_DDR5_THERMAL_SENSOR_STATUS)} failed with status {status}.");
                }
                else
                {
                    //Check critical limits first
                    if (BitHandler.IsBitSet(byteTemp, 2))
                    {
                        ThermalSensorStatus = ThermalSensorStatus.AboveCriticalHighLimit;
                    }
                    else if (BitHandler.IsBitSet(byteTemp, 3))
                    {
                        ThermalSensorStatus = ThermalSensorStatus.BelowCriticalLowLimit;
                    }
                    else if (BitHandler.IsBitSet(byteTemp, 0))
                    {
                        ThermalSensorStatus = ThermalSensorStatus.AboveHighLimit;
                    }
                    else if (BitHandler.IsBitSet(byteTemp, 1))
                    {
                        ThermalSensorStatus = ThermalSensorStatus.BelowLowLimit;
                    }
                    else
                    {
                        ThermalSensorStatus = ThermalSensorStatus.Good;
                    }

                    LogSimple.LogTrace($"{nameof(ThermalSensorStatus)} = {ThermalSensorStatus}.");
                }
            }
        }

        #endregion

        #region Private

        void ReadThermalSensorConfiguration(int retries)
        {
            //Set page to 0
            SetPage(0);

            byte byteTemp;
            ushort wordTemp;

            //Device capability
            var status = RetryReadByteData(_Bus, _Address, DDR5Constants.SPD_DDR5_DEVICE_CAPABILITY, retries, out byteTemp);
            if (status < 0)
            {
                LogSimple.LogTrace($"Reading {nameof(DDR5Constants.SPD_DDR5_DEVICE_CAPABILITY)} failed with status {status}.");
            }
            else
            {
                //Bit 1 => 1 = Supports Temperature Sensor
                HasThermalSensor = BitHandler.IsBitSet(byteTemp, 1);

                LogSimple.LogTrace($"{nameof(HasThermalSensor)} = {HasThermalSensor}.");
            }

            if (!HasThermalSensor)
            {
                return;
            }

            //Sensor high limit
            status = RetryReadWordData(_Bus, _Address, DDR5Constants.SPD_DDR5_THERMAL_SENSOR_HIGH_LIMIT_CONFIGURATION, retries, out wordTemp);
            if (status < 0)
            {
                LogSimple.LogTrace($"Reading {nameof(DDR5Constants.SPD_DDR5_THERMAL_SENSOR_HIGH_LIMIT_CONFIGURATION)} failed with status {status}.");
            }
            else
            {
                ThermalSensorHighLimit = SPDTemperatureConverter.CheckAndConvertTemperature(wordTemp);
                LogSimple.LogTrace($"{nameof(ThermalSensorHighLimit)} = {ThermalSensorHighLimit}.");
            }

            //Sensor low limit
            status = RetryReadWordData(_Bus, _Address, DDR5Constants.SPD_DDR5_THERMAL_SENSOR_LOW_LIMIT_CONFIGURATION, retries, out wordTemp);
            if (status < 0)
            {
                LogSimple.LogTrace($"Reading {nameof(DDR5Constants.SPD_DDR5_THERMAL_SENSOR_LOW_LIMIT_CONFIGURATION)} failed with status {status}.");
            }
            else
            {
                ThermalSensorLowLimit = SPDTemperatureConverter.CheckAndConvertTemperature(wordTemp);
                LogSimple.LogTrace($"{nameof(ThermalSensorLowLimit)} = {ThermalSensorLowLimit}.");
            }

            //Sensor critical high limit
            status = RetryReadWordData(_Bus, _Address, DDR5Constants.SPD_DDR5_THERMAL_SENSOR_CRITICAL_HIGH_LIMIT_CONFIGURATION, retries, out wordTemp);
            if (status < 0)
            {
                LogSimple.LogTrace($"Reading {nameof(DDR5Constants.SPD_DDR5_THERMAL_SENSOR_CRITICAL_HIGH_LIMIT_CONFIGURATION)} failed with status {status}.");
            }
            else
            {
                ThermalSensorCriticalHighLimit = SPDTemperatureConverter.CheckAndConvertTemperature(wordTemp);
                LogSimple.LogTrace($"{nameof(ThermalSensorCriticalHighLimit)} = {ThermalSensorCriticalHighLimit}.");
            }

            //Sensor critical low limit
            status = RetryReadWordData(_Bus, _Address, DDR5Constants.SPD_DDR5_THERMAL_SENSOR_CRITICAL_LOW_LIMIT_CONFIGURATION, retries, out wordTemp);
            if (status < 0)
            {
                LogSimple.LogTrace($"Reading {nameof(DDR5Constants.SPD_DDR5_THERMAL_SENSOR_CRITICAL_LOW_LIMIT_CONFIGURATION)} failed with status {status}.");
            }
            else
            {
                ThermalSensorCriticalLowLimit = SPDTemperatureConverter.CheckAndConvertTemperature(wordTemp);
                LogSimple.LogTrace($"{nameof(ThermalSensorCriticalLowLimit)} = {ThermalSensorCriticalLowLimit}.");
            }
        }

        #endregion
    }
}
