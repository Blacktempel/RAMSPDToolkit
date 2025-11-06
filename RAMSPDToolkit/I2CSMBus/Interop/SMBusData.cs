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

using BlackSharp.Core.Extensions;
using System.Runtime.InteropServices;

namespace RAMSPDToolkit.I2CSMBus.Interop
{
    /// <summary>
    /// Data object for SMBus transactions.
    /// </summary>
    public sealed class SMBusData : IDisposable
    {
        #region Constructor

        public SMBusData()
        {
            _Pointer = Marshal.AllocHGlobal(MAX_BLOCK_SIZE);
            Block = new byte[MAX_BLOCK_SIZE];  
        }

        ~SMBusData()
        {
            Dispose();
        }

        #endregion

        #region Fields

        bool _Disposed;

        IntPtr _Pointer;

        const int MIN_BLOCK_SIZE = 4;
        const int MAX_BLOCK_SIZE = I2CConstants.I2C_SMBUS_BLOCK_MAX + 2;

        #endregion

        #region Properties

        /// <summary>
        /// Byte data.
        /// </summary>
        public byte ByteData
        {
            get { return Marshal.ReadByte(_Pointer, 0); }
            set { Marshal.WriteByte(_Pointer, value); }
        }

        /// <summary>
        /// Word data.
        /// </summary>
        public ushort Word
        {
            get { return MarshalExtensions.ReadUInt16(_Pointer, 0); }
            set { MarshalExtensions.WriteUInt16(_Pointer, value); }
        }

        /// <summary>
        /// Gets or sets <see cref="Block"/> property. Please note that get produces a copy.<br/>
        /// When modifying contents of this array, please use indexer <see cref="this[int]"/>.
        /// </summary>
        public byte[] Block
        {
            get
            {
                var blockData = new byte[MAX_BLOCK_SIZE];

                Marshal.Copy(_Pointer, blockData, 0, MAX_BLOCK_SIZE);

                return blockData;
            }
            set
            {
                if (value.Length < MIN_BLOCK_SIZE || value.Length > MAX_BLOCK_SIZE)
                {
                    throw new ArgumentException("Unsupported block size.");
                }

                Marshal.Copy(value, 0, _Pointer, value.Length);
            }
        }

        internal IntPtr Pointer => _Pointer;

        #endregion

        #region Operators

        public byte this[int index]
        {
            get
            {
                if (index < 0 || index >= MAX_BLOCK_SIZE)
                {
                    throw new IndexOutOfRangeException("Block index out of range.");
                }

                return Marshal.ReadByte(_Pointer, index);
            }
            set
            {
                if (index < 0 || index >= MAX_BLOCK_SIZE)
                {
                    throw new IndexOutOfRangeException("Block index out of range.");
                }

                Marshal.WriteByte(_Pointer, index, value);
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (!_Disposed)
            {
                Marshal.FreeHGlobal(_Pointer);

                _Disposed = true;
            }
        }

        #endregion
    }
}