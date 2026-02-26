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
using BlackSharp.Core.Interop.Windows.Mutexes;
using RAMSPDToolkit.I2CSMBus.Interfaces;
using RAMSPDToolkit.I2CSMBus.Interop;
using RAMSPDToolkit.I2CSMBus.Interop.PawnIO;
using RAMSPDToolkit.Logging;
using RAMSPDToolkit.Mutexes;
using RAMSPDToolkit.Windows.Driver;
using RAMSPDToolkit.Windows.Driver.Interfaces;
using System.Runtime.InteropServices;
using System.Text;
using OS = BlackSharp.Core.Platform.OperatingSystem;

namespace RAMSPDToolkit.I2CSMBus
{
    /// <summary>
    /// SMBus class for <see cref="IPawnIODriver"/> driver.<br/>
    /// Supports I801, Piix4, NCT6793 and Intel Skylake IMC.
    /// </summary>
    public sealed class SMBusPawnIO : SMBusInterface, IIntelSkylakeIMCSMBus
    {
        #region Constructor

        SMBusPawnIO(IPawnIOModule pawnIO, PawnIOSMBusIdentifier pawnIOSMBusIdentifier)
        {
            if (!OS.IsWindows())
            {
                throw new PlatformNotSupportedException();
            }

            PawnIO = pawnIO;
            PawnIOSMBusIdentifier = pawnIOSMBusIdentifier;

            if (pawnIO != null)
            {
                GetIdentity(pawnIO);
                CheckWriteProtection(pawnIO);
            }
        }

        #endregion

        #region Properties

        IPawnIOModule PawnIO { get; }

        /// <summary>
        /// Identifies SMBus type.
        /// </summary>
        public PawnIOSMBusIdentifier PawnIOSMBusIdentifier { get; private set; }

        /// <summary>
        /// Only valid if <see cref="PawnIOSMBusIdentifier"/> is set to <see cref="PawnIOSMBusIdentifier.IntelSkylakeIMC"/>.
        /// </summary>
        public byte SMBusIndex { get; internal set; } = byte.MaxValue;

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

            if (PawnIO == null)
            {
                throw new NullReferenceException($"{nameof(PawnIO)} was not initialized.");
            }

            if (PawnIOSMBusIdentifier == PawnIOSMBusIdentifier.IntelSkylakeIMC)
            {
                inSize = 4;
                outSize = 1;
            }

            int status;

            using (var guard = new WorldMutexGuard(WorldMutexManager.WorldSMBusMutex))
            {
                status = PawnIO.Execute("ioctl_smbus_xfer", inBuffer, inSize, outBuffer, outSize, out var retSize);
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
            var pawnIO = DriverManager.Driver as IPawnIODriver;

            if (pawnIO?.IsOpen == false)
            {
                return false;
            }

            bool any = false;

            //Lock SMBus mutex
            using (var smbm = new WorldMutexGuard(WorldMutexManager.WorldSMBusMutex))
            {
                //Lock PCI mutex
                using (var pci = new WorldMutexGuard(WorldMutexManager.WorldPCIMutex))
                {
                    //I801
                    var i801 = pawnIO.LoadModule(PawnIOSMBusIdentifier.I801);
                    if (i801 != null)
                    {
                        SMBusManager.AddSMBus(new SMBusPawnIO(i801, PawnIOSMBusIdentifier.I801));
                        any = true;
                    }

                    //Piix4
                    var piix4 = pawnIO.LoadModule(PawnIOSMBusIdentifier.Piix4);
                    if (piix4 != null)
                    {
                        if (Piix4PortSelect(piix4, 0))
                        {
                            SMBusManager.AddSMBus(new SMBusPawnIO(piix4, PawnIOSMBusIdentifier.Piix4));
                        }

                        piix4 = pawnIO.LoadModule(PawnIOSMBusIdentifier.Piix4);
                        if (piix4 != null)
                        {
                            if (Piix4PortSelect(piix4, 1))
                            {
                                SMBusManager.AddSMBus(new SMBusPawnIO(piix4, PawnIOSMBusIdentifier.Piix4));
                            }
                        }

                        any = true;
                    }

                    //NCT6793
                    var nct6793 = pawnIO.LoadModule(PawnIOSMBusIdentifier.NCT6793);
                    if (nct6793 != null)
                    {
                        SMBusManager.AddSMBus(new SMBusPawnIO(nct6793, PawnIOSMBusIdentifier.NCT6793));
                        any = true;
                    }

                    //Intel Skylake IMC
                    var intelSkylakeIMC = pawnIO.LoadModule(PawnIOSMBusIdentifier.IntelSkylakeIMC);
                    if (intelSkylakeIMC != null)
                    {
                        if (IMCSMBusIndexSelect(intelSkylakeIMC, 0))
                        {
                            var smbus = new SMBusPawnIO(intelSkylakeIMC, PawnIOSMBusIdentifier.IntelSkylakeIMC)
                            {
                                SMBusIndex = 0
                            };
                            SMBusManager.AddSMBus(smbus);
                        }

                        intelSkylakeIMC = pawnIO.LoadModule(PawnIOSMBusIdentifier.IntelSkylakeIMC);
                        if (intelSkylakeIMC != null)
                        {
                            if (IMCSMBusIndexSelect(intelSkylakeIMC, 1))
                            {
                                var smbus = new SMBusPawnIO(intelSkylakeIMC, PawnIOSMBusIdentifier.IntelSkylakeIMC)
                                {
                                    SMBusIndex = 1
                                };
                                SMBusManager.AddSMBus(smbus);
                            }
                        }

                        any = true;
                    }
                }
            }

            return any;
        }

        #endregion

        #region Internal

        internal bool SetBank(byte bankIndex)
        {
            uint inSize = 1;
            uint outSize = 1;

            var inBuffer = new long[inSize];
            var outBuffer = new long[outSize];

            inBuffer[0] = bankIndex;

            return PawnIO.Execute("ioctl_set_bank", inBuffer, inSize, outBuffer, outSize, out var returnSize) == 0;
        }

        #endregion

        #region Private

        static bool Piix4PortSelect(IPawnIOModule pawnIO, int port)
        {
            uint inSize = 1;
            uint outSize = 1;

            var inBuffer = new long[inSize];
            var outBuffer = new long[outSize];

            inBuffer[0] = port;

            return pawnIO.Execute("ioctl_piix4_port_sel", inBuffer, inSize, outBuffer, outSize, out var returnSize) == 0;
        }

        static bool IMCSMBusIndexSelect(IPawnIOModule pawnIO, byte smbusIndex)
        {
            uint inSize = 1;
            uint outSize = 1;

            var inBuffer = new long[inSize];
            var outBuffer = new long[outSize];

            inBuffer[0] = smbusIndex;

            return pawnIO.Execute("ioctl_smbus_index", inBuffer, inSize, outBuffer, outSize, out var returnSize) == 0;
        }

        void GetIdentity(IPawnIOModule pawnIO)
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

        void CheckWriteProtection(IPawnIOModule pawnIO)
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
