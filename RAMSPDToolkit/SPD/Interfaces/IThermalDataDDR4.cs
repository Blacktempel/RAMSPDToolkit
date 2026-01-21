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

namespace RAMSPDToolkit.SPD.Interfaces
{
    internal interface IThermalDataDDR4
    {
        /// <summary>
        /// The TS Capabilities Register indicates the supported features
        /// of the temperature sensor portion of the TSE2004av.
        /// </summary>
        ushort ThermalSensorCapabilitiesRegister { get; }

        /// <summary>
        /// The TS Configuration Register holds the control and status bits
        /// of the EVENT_n pin as well as general hysteresis on all limits.
        /// </summary>
        ushort ThermalSensorConfigurationRegister { get; }

        /// <summary>
        /// Thermal sensor critical limit.<br/>
        /// The standard does not specify if this is a critical low or high limit,
        /// so we should assume it is both.
        /// </summary>
        float ThermalSensorCriticalLimit { get; }

        /// <summary>
        /// The Manufacturer ID Register holds the PCI SIG number assigned to the specific manufacturer.
        /// </summary>
        ushort ThermalSensorManufacturerID { get; }

        /// <summary>
        /// The upper byte of the Device ID / Revision Register must be 0x22 for the TSE2004av.<br/>
        /// The lower byte holds the revision value which is vendor-specific.
        /// </summary>
        ushort ThermalSensorDeviceID { get; }
    }
}
