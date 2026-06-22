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
using RAMSPDToolkit.SPD.Interop.Shared;

namespace RAMSPDToolkit.SPD
{
    /// <summary>
    /// Automatically detects <see cref="Interop.Shared.SPDMemoryType"/>.
    /// </summary>
    public sealed class SPDDetector
    {
        #region Constructor

        public SPDDetector(SMBusInterface bus, byte address, SPDMemoryType memoryType = SPDMemoryType.SPD_RESERVED)
        {
            _Bus = bus;
            _Address = address;
            _MemoryType = memoryType;

            DetectMemoryType();
        }

        #endregion

        #region Fields

        SMBusInterface _Bus;
        byte _Address;
        SPDMemoryType _MemoryType;
        bool _Valid;
        SPDAccessor _Accessor;

        #endregion

        #region Properties

        /// <summary>
        /// Get address.
        /// </summary>
        public byte Address
        {
            get { return _Address; }
        }

        /// <summary>
        /// Get <see cref="Interop.Shared.SPDMemoryType"/>.
        /// </summary>
        public SPDMemoryType SPDMemoryType
        {
            get { return _MemoryType; }
        }

        /// <summary>
        /// Checks if current <see cref="SPDDetector"/> instance is valid.
        /// </summary>
        public bool IsValid
        {
            get { return _Valid; }
        }

        /// <summary>
        /// Get detected <see cref="SPDAccessor"/>.
        /// </summary>
        public SPDAccessor Accessor
        {
            get { return _Accessor; }
        }

        #endregion

        #region Private

        void DetectMemoryType()
        {
            SPDAccessor accessor = null;

            if (  ( _MemoryType == SPDMemoryType.SPD_RESERVED
                 || _MemoryType == SPDMemoryType.SPD_DDR4_SDRAM
                 || _MemoryType == SPDMemoryType.SPD_DDR4E_SDRAM
                 || _MemoryType == SPDMemoryType.SPD_LPDDR4_SDRAM
                 || _MemoryType == SPDMemoryType.SPD_LPDDR4X_SDRAM )
             && DDR4Accessor.IsAvailable(_Bus, _Address))
            {
                accessor = new DDR4Accessor(_Bus, _Address);
            }
            else if (  (_MemoryType == SPDMemoryType.SPD_RESERVED
                     || _MemoryType == SPDMemoryType.SPD_DDR5_SDRAM
                     || _MemoryType == SPDMemoryType.SPD_LPDDR5_SDRAM )
             && DDR5Accessor.IsAvailable(_Bus, _Address))
            {
                accessor = new DDR5Accessor(_Bus, _Address);
            }
            else if (  (_MemoryType == SPDMemoryType.SPD_RESERVED
                     || _MemoryType == SPDMemoryType.SPD_DDR3_SDRAM
                     || _MemoryType == SPDMemoryType.SPD_LPDDR3_SDRAM )
             && DDR3Accessor.IsAvailable(_Bus, _Address))
            {
                accessor = new DDR3Accessor(_Bus, _Address);
            }
            else if (_MemoryType == SPDMemoryType.SPD_RESERVED)
            {
                //Read the memory type for diagnostics even if no supported accessor was detected
                int value = _Bus.i2c_smbus_read_byte_data(_Address, 0x02);

                if (value >= 0)
                {
                    _MemoryType = (SPDMemoryType)value;
                }

                _Valid = false;
                return;
            }

            if (accessor != null)
            {
                _Valid = true;
                _MemoryType = accessor.MemoryType();
                _Accessor = accessor;
            }
        }

        #endregion
    }
}
