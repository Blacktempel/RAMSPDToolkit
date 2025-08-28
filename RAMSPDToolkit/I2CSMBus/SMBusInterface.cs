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

using RAMSPDToolkit.I2CSMBus.Interop;
using RAMSPDToolkit.PCI;
using System.Runtime.InteropServices;

namespace RAMSPDToolkit.I2CSMBus
{
    /// <summary>
    /// Base class for SMBus implementations.
    /// </summary>
    public abstract class SMBusInterface
    {
        #region Constructor

        protected SMBusInterface()
        {
            PortID             = -1;
            PCIDevice          = -1;
            PCIVendor          = -1;
            PCISubsystemDevice = -1;
            PCISubsystemVendor = -1;
        }

        #endregion

        #region Fields

        object _SMBusLock = new object();

        #endregion

        #region Properties

        /// <summary>
        /// Name of SMBus device.
        /// </summary>
        public string DeviceName      { get; protected set; }

        /// <summary>
        /// Port ID of SMBus.
        /// </summary>
        public int PortID             { get; protected set; }

        /// <summary>
        /// Device ID of SMBus.
        /// </summary>
        public int PCIDevice          { get; protected set; }

        /// <summary>
        /// Vendor ID of SMBus.
        /// </summary>
        public int PCIVendor          { get; protected set; }

        /// <summary>
        /// Subsystem device ID of SMBus.
        /// </summary>
        public int PCISubsystemDevice { get; protected set; }

        /// <summary>
        /// Subsystem vendor ID of SMBus.
        /// </summary>
        public int PCISubsystemVendor { get; protected set; }

        /// <summary>
        /// This property is to determine if SPD Write Protection is enabled.<br/>
        /// If it is enabled, a page change for DDR5 is not possible as a page change requires a write to SMBus at a write protected address.<br/>
        /// Currently it is known to be enabled on newer Intel CPU systems.<br/>
        /// Allowing SPD writes can usually be changed in BIOS settings by the PCs user.<br/>
        /// For DDR4 systems, page change is possible as the page address is not within the protected memory range.
        /// </summary>
        public bool HasSPDWriteProtection { get; protected set; } = false;

        #endregion

        #region Abstract

        protected abstract int I2CSMBusXfer(byte addr, byte read_write, byte command, int size, SMBusData data);
        protected abstract int I2CXfer(byte addr, byte read_write, int? size, byte[] data);

        #endregion

        #region Public

        /// <summary>
        /// Does a quick write with given value at provided address.
        /// </summary>
        /// <param name="addr">Address to do a quick write on.</param>
        /// <param name="value">Value to write.</param>
        /// <returns>When the return value is negative, it is an error code. If positive, the value returned by SMBus call.</returns>
        public int i2c_smbus_write_quick(byte addr, byte value)
        {
            return i2c_smbus_xfer_call(addr, value, 0, I2CConstants.I2C_SMBUS_QUICK, null);
        }

        /// <summary>
        /// Reads one byte at given address.
        /// </summary>
        /// <param name="addr">Address to read from.</param>
        /// <returns>When the return value is negative, it is an error code. If positive, the value returned by SMBus call.</returns>
        public int i2c_smbus_read_byte(byte addr)
        {
            var data = new SMBusData();

            int status = i2c_smbus_xfer_call(addr, I2CConstants.I2C_SMBUS_READ, 0, I2CConstants.I2C_SMBUS_BYTE, data);
            if (status != 0)
            {
                return status;
            }
            else
            {
                return data.ByteData;
            }
        }

        /// <summary>
        /// Does a write with given value at provided address.
        /// </summary>
        /// <param name="addr">Address to do a write on.</param>
        /// <param name="value">Value to write.</param>
        /// <returns>When the return value is negative, it is an error code. If positive, the value returned by SMBus call.</returns>
        public int i2c_smbus_write_byte(byte addr, byte value)
        {
            return i2c_smbus_xfer_call(addr, I2CConstants.I2C_SMBUS_WRITE, value, I2CConstants.I2C_SMBUS_BYTE, null);
        }

        /// <summary>
        /// Reads one byte at given address with specified command.
        /// </summary>
        /// <param name="addr">Address to read from.</param>
        /// <param name="command">Command for operation. This is usually an offset value.</param>
        /// <returns>When the return value is negative, it is an error code. If positive, the value returned by SMBus call.</returns>
        public int i2c_smbus_read_byte_data(byte addr, byte command)
        {
            var data = new SMBusData();

            int status = i2c_smbus_xfer_call(addr, I2CConstants.I2C_SMBUS_READ, command, I2CConstants.I2C_SMBUS_BYTE_DATA, data);
            if (status != 0)
            {
                return status;
            }
            else
            {
                return data.ByteData;
            }
        }

        /// <summary>
        /// Write one byte at given address with specified command.
        /// </summary>
        /// <param name="addr">Address to write to.</param>
        /// <param name="command">Command for operation. This is usually an offset value.</param>
        /// <param name="value">Value to write.</param>
        /// <returns>When the return value is negative, it is an error code. If positive, the value returned by SMBus call.</returns>
        public int i2c_smbus_write_byte_data(byte addr, byte command, byte value)
        {
            var data = new SMBusData();
            data.ByteData = value;

            return i2c_smbus_xfer_call(addr, I2CConstants.I2C_SMBUS_WRITE, command, I2CConstants.I2C_SMBUS_BYTE_DATA, data);
        }

        /// <summary>
        /// Reads a word (<see cref="ushort"/>) at given address with specified command.
        /// </summary>
        /// <param name="addr">Address to read from.</param>
        /// <param name="command">Command for operation. This is usually an offset value.</param>
        /// <returns>When the return value is negative, it is an error code. If positive, the value returned by SMBus call.</returns>
        public int i2c_smbus_read_word_data(byte addr, byte command)
        {
            var data = new SMBusData();

            int status = i2c_smbus_xfer_call(addr, I2CConstants.I2C_SMBUS_READ, command, I2CConstants.I2C_SMBUS_WORD_DATA, data);
            if (status != 0)
            {
                return status;
            }
            else
            {
                return data.Word;
            }
        }

        /// <summary>
        /// Reads a word (<see cref="ushort"/>) at given address with specified command and swaps its byte order.
        /// </summary>
        /// <param name="addr">Address to read from.</param>
        /// <param name="command">Command for operation. This is usually an offset value.</param>
        /// <returns>When the return value is negative, it is an error code. If positive, the value returned by SMBus call.</returns>
        public int i2c_smbus_read_word_data_swapped(byte addr, byte command)
        {
            var wordData = i2c_smbus_read_word_data(addr, command);

            if (wordData < 0)
            {
                return wordData;
            }
            else
            {
                return ((wordData & 0xFF00) >> 8) | ((wordData & 0x00FF) << 8);
            }
        }

        /// <summary>
        /// Writes a word (<see cref="ushort"/>) at given address with specified command.
        /// </summary>
        /// <param name="addr">Address to write to.</param>
        /// <param name="command">Command for operation. This is usually an offset value.</param>
        /// <param name="value">Value to write.</param>
        /// <returns>When the return value is negative, it is an error code. If positive, the value returned by SMBus call.</returns>
        public int i2c_smbus_write_word_data(byte addr, byte command, ushort value)
        {
            var data = new SMBusData();
            data.Word = value;

            return i2c_smbus_xfer_call(addr, I2CConstants.I2C_SMBUS_WRITE, command, I2CConstants.I2C_SMBUS_WORD_DATA, data);
        }

        /// <summary>
        /// Writes a word (<see cref="ushort"/>) at given address with specified command and reads a word from same address.
        /// </summary>
        /// <param name="addr">Address to write to and read from.</param>
        /// <param name="read_write">Specifies if read or write is executed.</param>
        /// <param name="command">Command for operation. This is usually an offset value.</param>
        /// <param name="value">Value to write.</param>
        /// <returns>When the return value is negative, it is an error code. If positive, the value returned by SMBus call.</returns>
        public int i2c_smbus_proc_call(byte addr, byte read_write, byte command, ushort value)
        {
            if (!HasSPDWriteProtection || PCIVendor != PCIConstants.PCI_VENDOR_INTEL)
            {
                return 0;
            }

            var data = new SMBusData();
            data.Word = value;

            var wordData = i2c_smbus_xfer_call(addr, read_write, command, I2CConstants.I2C_SMBUS_PROC_CALL, data);

            if (wordData < 0)
            {
                return wordData;
            }

            //Proc call returns a word read from the address after writing to its contents
            return data.Word;
        }

        /// <summary>
        /// Reads block data at given address with specified command.
        /// </summary>
        /// <param name="addr">Address to read from.</param>
        /// <param name="command">Command for operation. This is usually an offset value.</param>
        /// <param name="values">Input buffer in which the read data will be copied into.</param>
        /// <returns>When the return value is negative, it is an error code. If positive, the value returned by SMBus call.</returns>
        public int i2c_smbus_read_block_data(byte addr, byte command, byte[] values)
        {
            var data = new SMBusData();

            int status = i2c_smbus_xfer_call(addr, I2CConstants.I2C_SMBUS_READ, command, I2CConstants.I2C_SMBUS_BLOCK_DATA, data);
            if (status != 0)
            {
                return status;
            }
            else
            {
                var length = data[0] > values.Length ? values.Length : data[0];

                Array.Copy(data.Block, 1, values, 0, length);
                return data.Block[0];
            }
        }

        /// <summary>
        /// Writes block data at given address with specified command.
        /// </summary>
        /// <param name="addr">Address to write to.</param>
        /// <param name="command">Command for operation. This is usually an offset value.</param>
        /// <param name="length">Length of data to be written.</param>
        /// <param name="values">Data to write.</param>
        /// <returns>When the return value is negative, it is an error code. If positive, the value returned by SMBus call.</returns>
        public int i2c_smbus_write_block_data(byte addr, byte command, byte length, byte[] values)
        {
            var data = new SMBusData();
            if (length > I2CConstants.I2C_SMBUS_BLOCK_MAX)
            {
                length = I2CConstants.I2C_SMBUS_BLOCK_MAX;
            }

            data[0] = length;

            Marshal.Copy(values, 0, data.Pointer + 1, length);

            return i2c_smbus_xfer_call(addr, I2CConstants.I2C_SMBUS_WRITE, command, I2CConstants.I2C_SMBUS_BLOCK_DATA, data);
        }

        /// <summary>
        /// Reads block data at given address with specified command.
        /// </summary>
        /// <param name="addr">Address to read from.</param>
        /// <param name="command">Command for operation. This is usually an offset value.</param>
        /// <param name="length">Length of data to be read.</param>
        /// <param name="values">Input buffer in which the read data will be copied into.</param>
        /// <returns>When the return value is negative, it is an error code. If positive, the value returned by SMBus call.</returns>
        public int i2c_smbus_read_i2c_block_data(byte addr, byte command, byte length, byte[] values)
        {
            var data = new SMBusData();
            if (length > I2CConstants.I2C_SMBUS_BLOCK_MAX)
            {
                length = I2CConstants.I2C_SMBUS_BLOCK_MAX;
            }

            data[0] = length;

            int status = i2c_smbus_xfer_call(addr, I2CConstants.I2C_SMBUS_READ, command, I2CConstants.I2C_SMBUS_I2C_BLOCK_DATA, data);
            if (status != 0)
            {
                return status;
            }
            else
            {
                Array.Copy(data.Block, 1, values, 0, data.Block[0]);
                return data.Block[0];
            }
        }

        /// <summary>
        /// Writes block data at given address with specified command.
        /// </summary>
        /// <param name="addr">Address to write to.</param>
        /// <param name="command">Command for operation. This is usually an offset value.</param>
        /// <param name="length">Length of data to be written.</param>
        /// <param name="values">Data to write.</param>
        /// <returns>When the return value is negative, it is an error code. If positive, the value returned by SMBus call.</returns>
        public int i2c_smbus_write_i2c_block_data(byte addr, byte command, byte length, byte[] values)
        {
            var data = new SMBusData();
            if (length > I2CConstants.I2C_SMBUS_BLOCK_MAX)
            {
                length = I2CConstants.I2C_SMBUS_BLOCK_MAX;
            }

            data[0] = length;

            Marshal.Copy(values, 0, data.Pointer + 1, length);

            return i2c_smbus_xfer_call(addr, I2CConstants.I2C_SMBUS_WRITE, command, I2CConstants.I2C_SMBUS_I2C_BLOCK_DATA, data);
        }

        //Additional Methods for pure I2C block operations
        //These may not be supported on all devices

        public int i2c_read_block(byte addr, int? size, byte[] data)
        {
            return i2c_xfer_call(addr, I2CConstants.I2C_SMBUS_READ, size, data);
        }

        public int i2c_write_block(byte addr, int size, byte[] data)
        {
            return i2c_xfer_call(addr, I2CConstants.I2C_SMBUS_WRITE, size, data);
        }

        #endregion

        #region Private

        int i2c_smbus_xfer_call(byte addr, byte read_write, byte command, int size, SMBusData data)
        {
            //Handle SMBus and I2C transfer calls in a single thread
            lock (_SMBusLock)
            {
                return I2CSMBusXfer(addr, read_write, command, size, data);
            }
        }

        int i2c_xfer_call(byte addr, byte read_write, int? size, byte[] data)
        {
            //Handle SMBus and I2C transfer calls in a single thread
            lock (_SMBusLock)
            {
                return I2CXfer(addr, read_write, size, data);
            }
        }

        #endregion
    }
}
