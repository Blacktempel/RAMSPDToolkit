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

namespace RAMSPDToolkit.SPD.Interfaces
{
    internal interface IThermalDataDDR3
    {
        /// <summary>
        /// The capability register indicates the supported features of the TSE2002av-compatible temperature sensor.
        /// </summary>
        ushort ThermalSensorCapabilitiesRegister { get; }

        /// <summary>
        /// The configuration register contains the control and status bits of the temperature sensor.
        /// </summary>
        ushort ThermalSensorConfigurationRegister { get; }

        /// <summary>
        /// Thermal sensor critical high limit.
        /// </summary>
        float ThermalSensorCriticalLimit { get; }

        /// <summary>
        /// The manufacturer ID register identifies the temperature sensor manufacturer.
        /// </summary>
        ushort ThermalSensorManufacturerID { get; }

        /// <summary>
        /// The device ID and revision register identifies the temperature sensor device and revision.
        /// </summary>
        ushort ThermalSensorDeviceID { get; }
    }
}
