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
using RAMSPDToolkit.I2CSMBus;
using RAMSPDToolkit.I2CSMBus.Interop;
using RAMSPDToolkit.Logging;
using RAMSPDToolkit.SPD.Interop;
using RAMSPDToolkit.SPD.Interop.Shared;
using RAMSPDToolkit.SPD.Timings;
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

            SDRAMTimings = new DDR5Timings(this);
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

        /// <inheritdoc cref="DDR5Timings"/>
        public DDR5Timings SDRAMTimings { get; private set; }

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
            //Check device type before changing the page
            if (!HasHubDeviceType(bus, address))
            {
                return false;
            }

            //Read current page
            int status = RetryReadByteData(bus, address, DDR5Constants.SPD_DDR5_MREG_VIRTUAL_PAGE, SPDConstants.SPD_DATA_RETRIES, out byte page);

            if (status < 0)
            {
                LogSimple.LogTrace($"{nameof(DDR5Accessor)}.{nameof(IsAvailable)} failed to read current page due to error {status}.");
            }

            //First 3 bits of the MR11 register
            page &= 0x07;

            //Page is off
            if (page != 0)
            {
                if (!TrySelectPage(bus, address, 0))
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region Internal

        /// <summary>
        /// Detects if a DDR5 SPD Hub is available at specified address. A hidden hub identity
        /// may require restoring page zero before the device type can be read.
        /// </summary>
        /// <param name="bus">SMBus to check for RAM.</param>
        /// <param name="address">Address to check.</param>
        /// <returns>True if a DDR5 SPD Hub is available at specified address; false otherwise.</returns>
        internal static bool HasHubDeviceType(SMBusInterface bus, byte address)
        {
            //Try read most significant byte
            //Result should be 0x51
            var mostStatus = RetryReadByteData(bus, address, DDR5Constants.SPD_DDR5_DEVICE_TYPE_MOST, SPDConstants.SPD_DATA_RETRIES, out byte ddr5Magic);

            if (mostStatus < 0)
            {
                LogSimple.LogTrace($"{nameof(DDR5Accessor)}.{nameof(HasHubDeviceType)} failed to read {nameof(DDR5Constants.SPD_DDR5_DEVICE_TYPE_MOST)} due to error {mostStatus}.");
            }

            //Try read least significant byte
            //Result should be 0x18
            var leastStatus = RetryReadByteData(bus, address, DDR5Constants.SPD_DDR5_DEVICE_TYPE_LEAST, SPDConstants.SPD_DATA_RETRIES, out byte ddr5Sensor);

            if (leastStatus < 0)
            {
                LogSimple.LogTrace($"{nameof(DDR5Accessor)}.{nameof(HasHubDeviceType)} failed to read {nameof(DDR5Constants.SPD_DDR5_DEVICE_TYPE_LEAST)} due to error {leastStatus}.");
            }

            //Is it a DDR5 module ?
            if (mostStatus >= 0 && leastStatus >= 0
             && ddr5Magic  == DDR5Constants.SPD_DDR5_DEVICE_TYPE_MOST_EXPECTED
             && ddr5Sensor == DDR5Constants.SPD_DDR5_DEVICE_TYPE_LEAST_EXPECTED)
            {
                //We have a proper DDR5 + Thermal Sensor module
                return true;
            }

            //An ordinary SPD EEPROM exposes its data at these offsets. A hidden DDR5 hub
            //instead returns zero or an SMBus error for its volatile registers on some systems.
            if ((mostStatus >= 0 && ddr5Magic != 0)
             || (leastStatus >= 0 && ddr5Sensor != 0))
            {
                return false;
            }

            //Read the current page and retry the hub identity before changing page state.
            var pageStatus = RetryReadByteData(bus, address, DDR5Constants.SPD_DDR5_MREG_VIRTUAL_PAGE, SPDConstants.SPD_DATA_RETRIES, out byte selectedPage);

            if (pageStatus < 0)
            {
                return false;
            }

            mostStatus  = RetryReadByteData(bus, address, DDR5Constants.SPD_DDR5_DEVICE_TYPE_MOST , SPDConstants.SPD_DATA_RETRIES, out ddr5Magic );
            leastStatus = RetryReadByteData(bus, address, DDR5Constants.SPD_DDR5_DEVICE_TYPE_LEAST, SPDConstants.SPD_DATA_RETRIES, out ddr5Sensor);

            if (mostStatus >= 0 && leastStatus >= 0
             && ddr5Magic  == DDR5Constants.SPD_DDR5_DEVICE_TYPE_MOST_EXPECTED
             && ddr5Sensor == DDR5Constants.SPD_DDR5_DEVICE_TYPE_LEAST_EXPECTED)
            {
                return true;
            }

            if ((mostStatus  >= 0 && ddr5Magic  != 0)
             || (leastStatus >= 0 && ddr5Sensor != 0)
             || (selectedPage & ~0x07) != 0
             || (selectedPage &  0x07) == 0)
            {
                return false;
            }

            //Check more registers before writing MR11: offset 0x0B is EEPROM data on DDR4.
            var revisionStatus    = RetryReadByteData(bus, address, DDR5Constants.SPD_DDR5_DEVICE_REVISION, SPDConstants.SPD_DATA_RETRIES, out byte revision   );
            var vendorMostStatus  = RetryReadByteData(bus, address, DDR5Constants.SPD_DDR5_VENDOR_ID_MOST , SPDConstants.SPD_DATA_RETRIES, out byte vendorMost );
            var vendorLeastStatus = RetryReadByteData(bus, address, DDR5Constants.SPD_DDR5_VENDOR_ID_LEAST, SPDConstants.SPD_DATA_RETRIES, out byte vendorLeast);

            if ((revisionStatus    >= 0 && revision    != 0)
             || (vendorMostStatus  >= 0 && vendorMost  != 0)
             || (vendorLeastStatus >= 0 && vendorLeast != 0))
            {
                return false;
            }

            if (!TrySelectPage(bus, address, 0))
            {
                return false;
            }

            mostStatus  = RetryReadByteData(bus, address, DDR5Constants.SPD_DDR5_DEVICE_TYPE_MOST , SPDConstants.SPD_DATA_RETRIES, out ddr5Magic );
            leastStatus = RetryReadByteData(bus, address, DDR5Constants.SPD_DDR5_DEVICE_TYPE_LEAST, SPDConstants.SPD_DATA_RETRIES, out ddr5Sensor);

            if (mostStatus >= 0 && leastStatus >= 0
             && ddr5Magic  == DDR5Constants.SPD_DDR5_DEVICE_TYPE_MOST_EXPECTED
             && ddr5Sensor == DDR5Constants.SPD_DDR5_DEVICE_TYPE_LEAST_EXPECTED)
            {
                LogSimple.LogTrace($"{nameof(DDR5Accessor)}.{nameof(HasHubDeviceType)} recovered hub identity from page {selectedPage}.");
                return true;
            }

            //The identity did not confirm a hub; leave an unknown device on its original page.
            TrySelectPage(bus, address, selectedPage);

            return false;
        }

        #endregion

        #region DDR5Accessor

        public override byte At(ushort address)
        {
            return At(address, true);
        }

        protected override byte At(ushort address, bool changePage)
        {
            //Ensure address is valid
            if (address >= DDR5Constants.SPD_DDR5_EEPROM_LENGTH)
            {
                return 0xFF;
            }

            if (changePage)
            {
                //Switch to the page containing address
                SetPage((byte)(address >> DDR5Constants.SPD_DDR5_EEPROM_PAGE_SHIFT));
            }

            //Calculate offset
            byte offset = (byte)((address & DDR5Constants.SPD_DDR5_EEPROM_PAGE_MASK) | 0x80);

            //Read value at address
            RetryReadByteData(_Bus, _Address, offset, SPDConstants.SPD_DATA_RETRIES, out var value);

            Thread.Sleep(SPDConstants.SPD_IO_DELAY);

            //Return value
            return value;
        }

        public override byte[] At(ushort address, byte length)
        {
            //Ensure address is valid
            if (address >= DDR5Constants.SPD_DDR5_EEPROM_LENGTH)
            {
                return [];
            }

            //Switch to the page containing address
            SetPage((byte)(address >> DDR5Constants.SPD_DDR5_EEPROM_PAGE_SHIFT));

            //Calculate offset
            byte offset = (byte)((address & DDR5Constants.SPD_DDR5_EEPROM_PAGE_MASK) | 0x80);

            //Read values at address
            RetryReadI2CBlockData(_Bus, _Address, offset, length, SPDConstants.SPD_DATA_RETRIES, out var value);

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

        static bool TrySelectPage(SMBusInterface bus, byte address, byte page)
        {
            int status;

            if (bus.HasSPDWriteProtection)
            {
                status = bus.i2c_smbus_proc_call(address, I2CConstants.I2C_SMBUS_READ, DDR5Constants.SPD_DDR5_MREG_VIRTUAL_PAGE, page);
            }
            else
            {
                status = bus.i2c_smbus_write_byte_data(address, DDR5Constants.SPD_DDR5_MREG_VIRTUAL_PAGE, page);
            }

            if (status < 0)
            {
                LogSimple.LogTrace($"{nameof(DDR5Accessor)}.{nameof(TrySelectPage)} failed to select page {page} due to error {status}.");
                return false;
            }

            Thread.Sleep(SPDConstants.SPD_IO_DELAY);

            return true;
        }

        #endregion
    }
}
