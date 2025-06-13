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
using RAMSPDToolkit.I2CSMBus.Interop.AMD;
using RAMSPDToolkit.Logging;
using RAMSPDToolkit.PCI;
using System.Runtime.InteropServices;
using System.Text;

namespace RAMSPDToolkit.I2CSMBus
{
    /// <summary>
    /// SMBus for AMD Display Library.
    /// </summary>
    /// <remarks>This class is not finalized yet, due to missing testers.</remarks>
    public sealed class SMBusAmdAdl : SMBusInterface
    {
        #region Constructor

        static SMBusAmdAdl()
        {
            _Module = new AtiAdlModule();
        }

        SMBusAmdAdl(IntPtr context, int adapterIndex)
        {
            if (_Module == null)
            {
                throw new InvalidOperationException($"{nameof(SMBusAmdAdl)}: Library not loaded.");
            }

            if (!_Module.IsModuleLoaded)
            {
                return;
            }

            _Module.Context = context;

            int numOfDevices;
            IntPtr info = IntPtr.Zero;

            if (_Module.ADL2_Adapter_AdapterInfoX4_Get(context, adapterIndex, out numOfDevices, out info) != AMDConstants.ADL_OK)
            {
                LogSimple.LogWarn($"{nameof(_Module.ADL2_Adapter_AdapterInfoX4_Get)} failed.");
            }
            else
            {
                var structSize = Marshal.SizeOf<AdapterInfoX2>();
                var device = Marshal.PtrToStructure<AdapterInfoX2>(new IntPtr(info.ToInt64()));

                var pnpStr = Encoding.Default.GetString(device.StrPNPString);

                var venLoc = pnpStr.IndexOf("VEN_");
                var devLoc = pnpStr.IndexOf("DEV_");
                var subLoc = pnpStr.IndexOf("SUBSYS_");

                if (venLoc != -1 && devLoc != -1 && subLoc != -1)
                {
                    var venStr = pnpStr.Substring(venLoc +  4, 4);
                    var devStr = pnpStr.Substring(devLoc +  4, 4);
                    var sbvStr = pnpStr.Substring(subLoc + 11, 4);
                    var sbdStr = pnpStr.Substring(subLoc +  7, 4);

                    int venID = Convert.ToInt32(venStr, 16);
                    int devID = Convert.ToInt32(devStr, 16);
                    int sbvID = Convert.ToInt32(sbvStr, 16);
                    int sbdID = Convert.ToInt32(sbdStr, 16);

                    PCIVendor = venID;
                    PCIDevice = devID;
                    PCISubsystemVendor = sbvID;
                    PCISubsystemDevice = sbdID;
                    PortID = 1;

                    DeviceName = "AMD ADL";
                }
            }
        }

        #endregion

        #region Fields

        static AtiAdlModule _Module;

        #endregion

        #region I2CSMBusInterface

        protected override int I2CSMBusXfer(byte addr, byte read_write, byte command, int size, SMBusData data)
        {
            int primaryDisplay;
            int ret;
            int dataSize = 0;

            byte[] dataBlock = null;

            var i2c = new ADLI2C();

            i2c.Structure.Size = Marshal.SizeOf<ADLI2C>();
            i2c.Structure.Speed = 100;
            i2c.Structure.Line = 1;  //location of the Aura chip
            i2c.Structure.Address = addr << 1;
            i2c.Structure.Offset = 0;

            if (data != null)
            {
                i2c.Buffer = data.Block;
            }

            if (_Module.ADL2_Adapter_Primary_Get(_Module.Context, out primaryDisplay) != AMDConstants.ADL_OK)
            {
                return AMDConstants.ADL_ERR;
            }

            switch (size)
            {
                case I2CConstants.I2C_SMBUS_QUICK:
                    return -1;
                case I2CConstants.I2C_SMBUS_BYTE:
                case I2CConstants.I2C_SMBUS_BYTE_DATA:
                    dataSize = 1;
                    dataBlock = data.Block;
                    break;
                case I2CConstants.I2C_SMBUS_WORD_DATA:
                    dataSize = 2;
                    dataBlock = data.Block;
                    break;
                case I2CConstants.I2C_SMBUS_BLOCK_DATA:
                    dataSize = data.Block[0] + 1;
                    dataBlock = data.Block;
                    break;
                default:
                    return -1;
            }

            var handle = Marshal.AllocHGlobal(Marshal.SizeOf<ADLI2C>());

            if (read_write == I2CConstants.I2C_SMBUS_READ)
            {
                /* An SMBus read can be achieved by setting the offset to the command (register address) */
                i2c.Structure.Offset = command;
                i2c.Structure.Action = AMDConstants.ADL_DL_I2C_ACTIONREAD;
                i2c.Structure.DataSize = dataSize;
                i2c.Buffer = dataBlock;

                Marshal.StructureToPtr(i2c, handle, true);

                //TODO: array needs to be copied back

                ret = _Module.ADL2_Display_WriteAndReadI2C(_Module.Context, primaryDisplay, handle);
            }
            else
            {
                byte[] i2cBuffer = new byte[I2CConstants.I2C_SMBUS_BLOCK_MAX + 8];

                /* An SMBus write has one extra byte, the register address, before the data */
                i2c.Structure.Action = AMDConstants.ADL_DL_I2C_ACTIONWRITE;
                i2c.Structure.DataSize = dataSize + 1;
                i2c.Buffer = i2cBuffer;

                i2cBuffer[0] = command;
                Array.Copy(dataBlock, 0, i2cBuffer, 1, dataSize);
                //TODO: array needs to be copied back properly, this ^ will not work

                Marshal.StructureToPtr(i2c, handle, true);

                ret = _Module.ADL2_Display_WriteAndReadI2C(_Module.Context, primaryDisplay, handle);
            }

            Marshal.FreeHGlobal(handle);

            return ret;
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
            if (_Module == null || !_Module.IsModuleLoaded)
            {
                return false;
            }

            IntPtr context = IntPtr.Zero;

            if (_Module.ADL2_Main_Control_Create(ADL_Main_Memory_Alloc, 1, out context) != AMDConstants.ADL_OK)
            {
                return false;
            }
            else
            {
                int numOfDevices;

                IntPtr info = IntPtr.Zero;

                if (_Module.ADL2_Adapter_AdapterInfoX4_Get(context, -1, out numOfDevices, out info) == AMDConstants.ADL_OK)
                {
                    int lastBusNumber = -1;

                    var structSize = Marshal.SizeOf<AdapterInfoX2>();

                    for (int i = 0; i < numOfDevices; ++i)
                    {
                        var device = Marshal.PtrToStructure<AdapterInfoX2>(new IntPtr(info.ToInt64() + structSize * i));

                        if (lastBusNumber == device.BusNumber)
                        {
                            continue;
                        }

                        lastBusNumber = device.BusNumber;

                        var amdSMBus = new SMBusAmdAdl(context, device.AdapterIndex);

                        if (amdSMBus.PCIVendor != PCIConstants.PCI_VENDOR_AMD_GPU)
                        {
                            continue;
                        }

                        LogSimple.LogTrace(
                            $"ADL GPU Device {amdSMBus.PCIVendor:04X}:{amdSMBus.PCIDevice:04X} Subsystem: {amdSMBus.PCISubsystemVendor:04X}:{amdSMBus.PCISubsystemDevice:04X}");

                        SMBusManager.AddSMBus(amdSMBus);
                    }
                }
            }

            return true;
        }

        #endregion

        #region Private

        static IntPtr ADL_Main_Memory_Alloc(int size)
        {
            return Marshal.AllocHGlobal(size);
        }

        static void ADL_Main_Memory_Free(IntPtr buffer)
        {
            Marshal.FreeHGlobal(buffer);
        }

        #endregion
    }
}
