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
using RAMSPDToolkit.I2CSMBus.Interop.Shared;
using RAMSPDToolkit.Logging;
using RAMSPDToolkit.SPD.Interop;
using RAMSPDToolkit.SPD.Interop.Shared;
using RAMSPDToolkit.SPD.Timings;
using RAMSPDToolkit.Utilities;

namespace RAMSPDToolkit.SPD
{
    /// <summary>
    /// Accessor for DDR4 SPD.<br/>
    /// This works for Windows and Linux.
    /// </summary>
    /// <remarks>Please refer to JEDEC Standard for EE1004 and TSE2004av for property definitions.</remarks>
    public sealed class DDR4Accessor : DDR4AccessorBase
    {
        #region Constructor

        public DDR4Accessor(SMBusInterface bus, byte address)
            : base(bus, address)
        {
            //Set thermal sensor address
            _ThermalSensorAddress = (byte)(0x18 | (address & 0x07));

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

        readonly byte _ThermalSensorAddress;

        #endregion

        #region Properties

        /// <summary>
        /// The TS Capabilities Register indicates the supported features
        /// of the temperature sensor portion of the TSE2004av.
        /// </summary>
        public ushort ThermalSensorCapabilitiesRegister { get; private set; } = ushort.MaxValue;

        /// <summary>
        /// The TS Configuration Register holds the control and status bits
        /// of the EVENT_n pin as well as general hysteresis on all limits.
        /// </summary>
        public ushort ThermalSensorConfigurationRegister { get; private set; } = ushort.MaxValue;

        /// <summary>
        /// Thermal sensor critical limit.<br/>
        /// The standard does not specify if this is a critical low or high limit,
        /// so we should assume it is both.
        /// </summary>
        public float ThermalSensorCriticalLimit { get; private set; } = float.NaN;

        /// <summary>
        /// The Manufacturer ID Register holds the PCI SIG number assigned to the specific manufacturer.
        /// </summary>
        public ushort ThermalSensorManufacturerID { get; private set; } = ushort.MaxValue;

        /// <summary>
        /// The upper byte of the Device ID / Revision Register must be 0x22 for the TSE2004av.<br/>
        /// The lower byte holds the revision value which is vendor-specific.
        /// </summary>
        public ushort ThermalSensorDeviceID { get; private set; } = ushort.MaxValue;

        /// <inheritdoc cref="DDR4Timings"/>
        public DDR4Timings SDRAMTimings { get; private set; }

        #endregion

        #region Public

        /// <summary>
        /// Detects if a DDR4 RAM is available at specified address.
        /// </summary>
        /// <param name="bus">SMBus to check for RAM.</param>
        /// <param name="address">Address to check.</param>
        /// <returns>True if DDR4 is available at specified address; false otherwise.</returns>
        public static bool IsAvailable(SMBusInterface bus, byte address)
        {
            //Perform quick transfer to test if i2c address responds
            int value = bus.i2c_smbus_write_quick(DDR4Constants.SPD_DDR4_ADDRESS_PAGE, 0x00);

            int retries = SPDConstants.SPD_CFG_RETRIES;

            if (value < 0 && retries > 0)
            {
                var statusAbs = Math.Abs(value);

                //Try again specified number of times and give up
                if (statusAbs == SharedConstants.EBUSY ||
                    statusAbs == SharedConstants.ETIMEDOUT)
                {
                    int MAX_RETRIES = retries;
                    int retry = MAX_RETRIES;

                    while (value < 0 && retry-- > 0)
                    {
                        Thread.Sleep(SPDConstants.SPD_IO_DELAY);

                        value = bus.i2c_smbus_write_quick(DDR4Constants.SPD_DDR4_ADDRESS_PAGE, 0x00);
                    }
                }
            }

            if (value < 0)
            {
                return false;
            }

            //Select first page
            bus.i2c_smbus_write_byte_data(DDR4Constants.SPD_DDR4_ADDRESS_PAGE, 0x00, DDR4Constants.SPD_DDR4_EEPROM_PAGE_MASK);

            Thread.Sleep(SPDConstants.SPD_IO_DELAY);

            //Read value at address 0
            value = bus.i2c_smbus_read_byte_data(address, 0x00);

            //DDR4 is available if value is 0x23
            return value == 0x23;
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
            SetPage((byte)(address >> DDR4Constants.SPD_DDR4_EEPROM_PAGE_SHIFT));

            //Calculate offset
            byte offset = (byte)(address & DDR4Constants.SPD_DDR4_EEPROM_PAGE_MASK);

            //Read value at address
            RetryReadByteData(_Bus, _Address, offset, SPDConstants.SPD_DATA_RETRIES, out var value);

            Thread.Sleep(SPDConstants.SPD_IO_DELAY);

            //Return value
            return value;
        }

        public override bool UpdateTemperature()
        {
            //Set page to 0 to read volatile data
            SetPage(0);

            var status = RetryReadWordDataSwapped(_Bus, _ThermalSensorAddress, DDR4Constants.SPD_DDR4_TEMPERATURE_ADDRESS, SPDConstants.SPD_TS_RETRIES, out ushort temp);

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

        #endregion

        #region Private

        ushort RawTemperatureAdjust(ushort rawTemperature)
        {
            //Strip away not required bits
            rawTemperature = (ushort)(rawTemperature & 0xFFF);

            return rawTemperature;
        }

        bool ProbeThermalSensor()
        {
            //Set page to 0 in order to read
            SetPage(0);

            var status = _Bus.i2c_smbus_read_byte_data(_Address, DDR4Constants.SPD_DDR4_THERMAL_SENSOR_BYTE);

            if (status < 0)
            {
                LogSimple.LogTrace($"Failed to read {nameof(DDR4Constants.SPD_DDR4_THERMAL_SENSOR_BYTE)}.");
            }
            else
            {
                //Check if thermal sensor bit is set
                if (BitHandler.IsBitSet((byte)status, DDR4Constants.SPD_DDR4_THERMAL_SENSOR_BIT))
                {
                    LogSimple.LogTrace($"0x{_Address:X2} has {nameof(DDR4Constants.SPD_DDR4_THERMAL_SENSOR_BIT)} set.");
                    return true;
                }
                else
                {
                    LogSimple.LogTrace($"0x{_Address:X2} does not have {nameof(DDR4Constants.SPD_DDR4_THERMAL_SENSOR_BIT)} set.");
                    LogSimple.LogTrace($"Checking another way if thermal sensor is present.");

                    //Do a quick read to the thermal sensors address to check if it is available
                    status = _Bus.i2c_smbus_write_quick(_ThermalSensorAddress, 0x00);

                    if (status < 0)
                    {
                        LogSimple.LogTrace($"0x{_Address:X2} Thermal sensor not found.");
                    }
                    else
                    {
                        //If there is an ACK to the quick read, there is an "unregistered" thermal sensor
                        LogSimple.LogTrace($"0x{_Address:X2} Unregistered thermal sensor found.");

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
            var status = RetryReadWordDataSwapped(_Bus, _ThermalSensorAddress, DDR4Constants.SPD_DDR4_THERMAL_SENSOR_CAPABILITIES_REGISTER, retries, out wordTemp);
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
            status = RetryReadWordDataSwapped(_Bus, _ThermalSensorAddress, DDR4Constants.SPD_DDR4_THERMAL_SENSOR_CONFIGURATION_REGISTER, retries, out wordTemp);
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
            status = RetryReadWordDataSwapped(_Bus, _ThermalSensorAddress, DDR4Constants.SPD_DDR4_THERMAL_SENSOR_HIGH_LIMIT, retries, out wordTemp);
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
            status = RetryReadWordDataSwapped(_Bus, _ThermalSensorAddress, DDR4Constants.SPD_DDR4_THERMAL_SENSOR_LOW_LIMIT, retries, out wordTemp);
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
            status = RetryReadWordDataSwapped(_Bus, _ThermalSensorAddress, DDR4Constants.SPD_DDR4_THERMAL_SENSOR_CRIT_LIMIT, retries, out wordTemp);
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
            status = RetryReadWordDataSwapped(_Bus, _ThermalSensorAddress, DDR4Constants.SPD_DDR4_THERMAL_SENSOR_MANUFACTURER, retries, out wordTemp);
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
            status = RetryReadWordDataSwapped(_Bus, _ThermalSensorAddress, DDR4Constants.SPD_DDR4_THERMAL_SENSOR_DEVICEID, retries, out wordTemp);
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
