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
using RAMSPDToolkit.I2CSMBus.Interop.PawnIO;
using RAMSPDToolkit.Logging;
using RAMSPDToolkit.Mutexes;
using RAMSPDToolkit.Utilities;
using RAMSPDToolkit.Windows.Driver;
using RAMSPDToolkit.Windows.Driver.Interfaces;
using System.Runtime.InteropServices;
using System.Text;
using OS = RAMSPDToolkit.Software.OperatingSystem;

namespace RAMSPDToolkit.I2CSMBus
{
    /// <summary>
    /// SMBus class for <see cref="IPawnIODriver"/> driver.<br/>
    /// Supports I801, Piix4 and NCT6793.
    /// </summary>
    public sealed class SMBusPawnIO : SMBusInterface
    {
        #region Constructor

        SMBusPawnIO(PawnIOSMBusIdentifier pawnIOSMBusIdentifier)
        {
            if (!OS.IsWindows())
            {
                throw new PlatformNotSupportedException();
            }

            PawnIOSMBusIdentifier = pawnIOSMBusIdentifier;

            var pawnIO = TryGetPawnIO();

            if (pawnIO != null)
            {
                GetIdentity(pawnIO);
                CheckWriteProtection(pawnIO);
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Identifies SMBus type.
        /// </summary>
        public PawnIOSMBusIdentifier PawnIOSMBusIdentifier { get; private set; }

        #endregion

        #region I2CSMBusInterface

        protected override int I2CSMBusXfer(byte addr, byte read_write, byte command, int size, SMBusData data)
        {
            uint inSize = 9;
            uint outSize = 5;

            var inBuffer = new long[inSize];

            inBuffer[0] = addr;
            inBuffer[1] = read_write;
            inBuffer[2] = command;
            inBuffer[3] = size;

            switch (size)
            {
                case I2CConstants.I2C_SMBUS_BYTE:
                case I2CConstants.I2C_SMBUS_BYTE_DATA:
                    outSize = 1;

                    if (data != null)
                    {
                        inBuffer[4] = data.ByteData;
                    }
                    break;
                case I2CConstants.I2C_SMBUS_WORD_DATA:
                case I2CConstants.I2C_SMBUS_PROC_CALL:
                    outSize = 2;

                    if (data != null)
                    {
                        inBuffer[4] = data.Word;
                    }
                    break;
            }

            var outBuffer = new long[outSize];

            var pawnIO = TryGetPawnIO();

            if (pawnIO == null)
            {
                throw new NullReferenceException($"{nameof(IPawnIODriver)} was not initialized.");
            }

            int status;

            using (var guard = new WorldMutexGuard(WorldMutexManager.WorldSMBusMutex))
            {
                status = pawnIO.Execute("ioctl_smbus_xfer", inBuffer, inSize, outBuffer, outSize, out var retSize);
            }

            if (data != null)
            {
                Marshal.Copy(outBuffer, 0, data.Pointer, (int)outSize);
            }

            return status;
        }

        protected override int I2CXfer(byte addr, byte read_write, int? size, byte[] data)
        {
            return -1;
        }

        #endregion

        #region Public

        /// <summary>
        /// Detects if this SMBus is available for usage.<br/>
        /// If it is, an instance of this SMBus will be created and added into <see cref="SMBusManager.RegisteredSMBuses"/>.
        /// </summary>
        /// <returns>True if SMBus is available and false if not.</returns>
        public static bool SMBusDetect()
        {
            var pawnIO = TryGetPawnIO();

            if (pawnIO?.IsOpen == false)
            {
                return false;
            }
            
            //Lock SMBus mutex
            using (var smbm = new WorldMutexGuard(WorldMutexManager.WorldSMBusMutex))
            {
                //Lock PCI mutex
                using (var pci = new WorldMutexGuard(WorldMutexManager.WorldPCIMutex))
                {
                    //I801
                    if (pawnIO.LoadModule(PawnIOSMBusIdentifier.I801))
                    {
                        SMBusManager.AddSMBus(new SMBusPawnIO(PawnIOSMBusIdentifier.I801));
                        return true;
                    }

                    //Piix4
                    if (pawnIO.LoadModule(PawnIOSMBusIdentifier.Piix4))
                    {
                        if (Piix4PortSelect(pawnIO, 0))
                        {
                            SMBusManager.AddSMBus(new SMBusPawnIO(PawnIOSMBusIdentifier.Piix4));
                            return true;
                        }
                    }

                    //NCT6793
                    if (pawnIO.LoadModule(PawnIOSMBusIdentifier.NCT6793))
                    {
                        SMBusManager.AddSMBus(new SMBusPawnIO(PawnIOSMBusIdentifier.NCT6793));
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion

        #region Private

        static IPawnIODriver TryGetPawnIO()
        {
            return DriverManager.Driver as IPawnIODriver;
        }

        static bool Piix4PortSelect(IPawnIODriver pawnIO, int port)
        {
            uint inSize = 1;
            uint outSize = 1;

            var inBuffer = new long[inSize];
            var outBuffer = new long[outSize];

            inBuffer[0] = port;

            return pawnIO.Execute("ioctl_piix4_port_sel", inBuffer, inSize, outBuffer, outSize, out var returnSize) == 0;
        }

        void GetIdentity(IPawnIODriver pawnIO)
        {
            if (pawnIO != null)
            {
                uint inSize = 1;
                uint outSize = 3;

                var inBuffer = new long[inSize];
                var outBuffer = new long[outSize];

                if (pawnIO.Execute("ioctl_identity", inBuffer, inSize, outBuffer, outSize, out var returnSize) == 0)
                {
                    PortID = SMBusManager.RegisteredSMBuses.Count; // Assign next available port ID

                    PCIVendor          = (int)BitHandler.GetBits(outBuffer[2],  0, 15);
                    PCIDevice          = (int)BitHandler.GetBits(outBuffer[2], 16, 31);
                    PCISubsystemVendor = (int)BitHandler.GetBits(outBuffer[2], 32, 47);
                    PCISubsystemDevice = (int)BitHandler.GetBits(outBuffer[2], 48, 63);

                    var sb = new StringBuilder(8);

                    sb.Append((char)BitHandler.GetBits(outBuffer[0],  0,  7));
                    sb.Append((char)BitHandler.GetBits(outBuffer[0],  8, 15));
                    sb.Append((char)BitHandler.GetBits(outBuffer[0], 16, 23));
                    sb.Append((char)BitHandler.GetBits(outBuffer[0], 24, 31));
                    sb.Append((char)BitHandler.GetBits(outBuffer[0], 32, 39));
                    sb.Append((char)BitHandler.GetBits(outBuffer[0], 40, 47));
                    sb.Append((char)BitHandler.GetBits(outBuffer[0], 48, 55));
                    sb.Append((char)BitHandler.GetBits(outBuffer[0], 56, 63));

                    DeviceName = sb.ToString().Trim('\0');
                }
            }
        }

        void CheckWriteProtection(IPawnIODriver pawnIO)
        {
            if (pawnIO != null && PawnIOSMBusIdentifier == PawnIOSMBusIdentifier.I801)
            {
                uint inSize = 1;
                uint outSize = 1;

                var inBuffer = new long[inSize];
                var outBuffer = new long[outSize];

                bool writeProtectionEnabled = false;

                if (pawnIO.Execute("ioctl_write_protection", inBuffer, inSize, outBuffer, outSize, out var returnSize) == 0)
                {
                    writeProtectionEnabled = outBuffer[0] == 1;
                }

                HasSPDWriteProtection = writeProtectionEnabled;

                LogSimple.LogTrace($"{nameof(HasSPDWriteProtection)} = {HasSPDWriteProtection}");
            }
        }

        #endregion
    }
}
