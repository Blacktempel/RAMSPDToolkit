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
using RAMSPDToolkit.SPD.Enums;
using RAMSPDToolkit.SPD.Interop.Shared;
using RAMSPDToolkit.SPD.Mappings;

namespace RAMSPDToolkit.SPD
{
    /// <summary>
    /// Base class for SPD accessors.
    /// </summary>
    public abstract class SPDAccessor
    {
        #region Constructor

        protected SPDAccessor(SMBusInterface bus, byte address)
        {
            _Bus = bus;
            _Address = address;
        }

        ~SPDAccessor()
        {
            //Reset page
            SetPage(0);
        }

        #endregion

        #region Fields

        protected SMBusInterface _Bus;
        protected byte _Address;

        protected PageData _PageData = PageData.Nothing;

        #endregion

        #region Properties

        /// <summary>
        /// Index / slot of RAM stick, starting at zero (0).
        /// </summary>
        public byte Index => (byte)(_Address - SPDConstants.SPD_BEGIN);

        /// <summary>
        /// <inheritdoc cref="SMBusInterface.HasSPDWriteProtection"/>
        /// </summary>
        public bool HasSPDWriteProtection => _Bus.HasSPDWriteProtection;

        /// <summary>
        /// Gets page data (flag enumeration) which identifies which memory page is set and what data can be retrieved from it.<br/>
        /// This becomes a great functionality if having a DDR5 system with <see cref="SMBusInterface.HasSPDWriteProtection"/> enabled.
        /// </summary>
        public PageData PageData => _PageData;

        #endregion

        #region Protected

        protected static int RetryReadByteData(SMBusInterface bus, byte address, byte what, int retries, out byte byteData)
        {
            var status = bus.i2c_smbus_read_byte_data(address, what);

            if (status < 0 && retries > 0)
            {
                var statusAbs = Math.Abs(status);

                //Try again specified number of times and give up
                if (statusAbs == SharedConstants.EBUSY ||
                    statusAbs == SharedConstants.ETIMEDOUT)
                {
                    int MAX_RETRIES = retries;
                    int retry = MAX_RETRIES;

                    while (status < 0 && retry-- > 0)
                    {
                        Thread.Sleep(SPDConstants.SPD_IO_DELAY);

                        status = bus.i2c_smbus_read_byte_data(address, what);
                    }
                }
            }

            if (status >= 0)
            {
                byteData = (byte)status;
            }
            else
            {
                byteData = 0;
            }

            return status;
        }

        protected static int RetryReadWordData(SMBusInterface bus, byte address, byte what, int retries, out ushort word)
        {
            var status = bus.i2c_smbus_read_word_data(address, what);

            if (status < 0 && retries > 0)
            {
                var statusAbs = Math.Abs(status);

                //Try again specified number of times and give up
                if (statusAbs == SharedConstants.EBUSY ||
                    statusAbs == SharedConstants.ETIMEDOUT)
                {
                    int MAX_RETRIES = retries;
                    int retry = MAX_RETRIES;

                    while (status < 0 && retry-- > 0)
                    {
                        Thread.Sleep(SPDConstants.SPD_IO_DELAY);

                        status = bus.i2c_smbus_read_word_data(address, what);
                    }
                }
            }

            if (status >= 0)
            {
                word = (ushort)status;
            }
            else
            {
                word = 0;
            }

            return status;
        }

        protected static int RetryReadWordDataSwapped(SMBusInterface bus, byte address, byte what, int retries, out ushort word)
        {
            var status = bus.i2c_smbus_read_word_data_swapped(address, what);

            if (status < 0 && retries > 0)
            {
                var statusAbs = Math.Abs(status);

                //Try again specified number of times and give up
                if (statusAbs == SharedConstants.EBUSY ||
                    statusAbs == SharedConstants.ETIMEDOUT)
                {
                    int MAX_RETRIES = retries;
                    int retry = MAX_RETRIES;

                    while (status < 0 && retry-- > 0)
                    {
                        Thread.Sleep(SPDConstants.SPD_IO_DELAY);

                        status = bus.i2c_smbus_read_word_data_swapped(address, what);
                    }
                }
            }

            if (status >= 0)
            {
                word = (ushort)status;
            }
            else
            {
                word = 0;
            }

            return status;
        }

        #endregion

        #region Abstract

        /// <summary>
        /// Get SPD revision.
        /// </summary>
        /// <returns>Read SPD revision.</returns>
        public abstract byte SPDRevision();

        /// <summary>
        /// Get <see cref="SPDMemoryType"/>.
        /// </summary>
        /// <returns>Read memory type.</returns>
        public abstract SPDMemoryType MemoryType();

        /// <summary>
        /// Get module manufacturer continuation code.
        /// </summary>
        /// <returns>Read module manufacturer continuation code.</returns>
        public abstract byte ModuleManufacturerContinuationCode();

        /// <summary>
        /// Get module manufacturer ID code.
        /// </summary>
        /// <returns>Read module manufacturer ID code.</returns>
        public abstract byte ModuleManufacturerIDCode();

        /// <summary>
        /// Get module manufacturing location.
        /// </summary>
        /// <returns>Read module manufacturing location.</returns>
        public abstract byte ModuleManufacturingLocation();

        /// <summary>
        /// Get module manufacturing date.
        /// </summary>
        /// <returns>Read module manufacturing date.</returns>
        public abstract DateTime? ModuleManufacturingDate();

        /// <summary>
        /// Get module serial number.
        /// </summary>
        /// <returns>Read module serial number.</returns>
        public abstract string ModuleSerialNumber();

        /// <summary>
        /// Get module part number.
        /// </summary>
        /// <returns>Read module part number.</returns>
        public abstract string ModulePartNumber();

        /// <summary>
        /// Get module revision code.
        /// </summary>
        /// <returns>Read module revision code.</returns>
        public abstract byte ModuleRevisionCode();

        /// <summary>
        /// Get DRAM manufacturer continuation code.
        /// </summary>
        /// <returns>Read DRAM manufacturer continuation code.</returns>
        public abstract byte DRAMManufacturerContinuationCode();

        /// <summary>
        /// Get DRAM manufacturer ID code.
        /// </summary>
        /// <returns>Read DRAM manufacturer ID code.</returns>
        public abstract byte DRAMManufacturerIDCode();

        /// <summary>
        /// Get manufacturer specific data from specified index.
        /// </summary>
        /// <param name="index">Index to read manufacturer specific data from.</param>
        /// <returns>Read manufacturer specific data.</returns>
        public abstract byte ManufacturerSpecificData(ushort index);

        /// <summary>
        /// Get one byte from specified address.
        /// </summary>
        /// <param name="address">Address to read from.</param>
        /// <returns>Read byte.</returns>
        public abstract byte At(ushort address);

        /// <summary>
        /// Changes current page to page containing given <see cref="PageData"/>.
        /// </summary>
        /// <param name="pageData">Data the caller of this method want to read.</param>
        /// <returns>Boolean value to indicate whether page change, based on given parameter, was successful.</returns>
        public abstract bool ChangePage(PageData pageData);

        /// <summary>
        /// Gets current page.
        /// </summary>
        /// <returns>Current page.</returns>
        protected abstract byte GetPage();

        /// <summary>
        /// Sets current page.
        /// </summary>
        /// <param name="page">New page to set.</param>
        protected abstract void SetPage(byte page);

        #endregion

        #region Public

        /// <summary>
        /// Updates all volatile data which is available for read, in current implementation of <see cref="SPDAccessor"/>.
        /// </summary>
        /// <param name="retries">Amount of times to retry reading data if SMBus is busy.</param>
        public virtual void Update(int retries = SPDConstants.SPD_DATA_RETRIES)
        {
            //Nothing in base class
        }

        /// <summary>
        /// Uses <see cref="ModuleManufacturerContinuationCode"/> and <see cref="ModuleManufacturerIDCode"/> to get mapped manufacturer string from <see cref="ManufacturerMapping.ManufacturerBanks"/>.
        /// </summary>
        /// <returns>Manufacturer string or null.</returns>
        public virtual string GetModuleManufacturerString()
        {
            var mfgContinuationCode = ModuleManufacturerContinuationCode();
            var mfgIDCode           = ModuleManufacturerIDCode();

            if (ManufacturerMapping.ManufacturerBanks.TryGetValue((byte)(mfgContinuationCode + 1), out var dict))
            {
                if (dict.TryGetValue(mfgIDCode, out var manufacturer))
                {
                    return manufacturer;
                }
            }

            return null;
        }

        /// <summary>
        /// Uses <see cref="DRAMManufacturerContinuationCode"/> and <see cref="DRAMManufacturerIDCode"/> to get mapped manufacturer string from <see cref="ManufacturerMapping.ManufacturerBanks"/>.
        /// </summary>
        /// <returns>Manufacturer string or null.</returns>
        public virtual string GetDRAMManufacturerString()
        {
            var mfgContinuationCode = DRAMManufacturerContinuationCode();
            var mfgIDCode           = DRAMManufacturerIDCode();

            if (ManufacturerMapping.ManufacturerBanks.TryGetValue((byte)(mfgContinuationCode + 1), out var dict))
            {
                if (dict.TryGetValue(mfgIDCode, out var manufacturer))
                {
                    return manufacturer;
                }
            }

            return null;
        }

        #endregion
    }
}
