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

using RAMSPDToolkit.Extensions;
using RAMSPDToolkit.I2CSMBus;
using RAMSPDToolkit.I2CSMBusTools;
using RAMSPDToolkit.Logging;
using RAMSPDToolkit.SPD;
using RAMSPDToolkit.SPD.Interfaces;
using RAMSPDToolkit.SPD.Interop.Shared;
using RAMSPDToolkit.Utilities;
using RAMSPDToolkit.Windows.Driver;
using RAMSPDToolkit.Windows.Driver.Implementations;

using OS = RAMSPDToolkit.Software.OperatingSystem;

namespace ConsoleOutputTest
{
    public class TestClass
    {
        public TestClass()
        {
        }

        public static void Log()
        {
            //Console.WriteLine();
            Logger.Instance.Add(LogLevel.Debug, string.Empty, DateTime.Now);
        }

        public static void Log(string message)
        {
            //Console.WriteLine(message);
            Logger.Instance.Add(LogLevel.Debug, message, DateTime.Now);
        }

        public void DoTest()
        {
            try
            {
                Log($"Entering {nameof(DoTest)}.");

                //Check for Windows OS and load driver
                if (OperatingSystem.IsWindows())
                {
                    //Init driver
                    if (DriverManager.LoadDriver(DriverImplementation.WinRing0))
                    {
                        Log($"***** Driver {DriverManager.DriverImplementation} is open. *****");
                    }
                    else
                    {
                        Log($"***** DRIVER ERROR *****");
                        throw new Exception("Driver is not open.");
                    }
                }

                //Driver is open / loaded or we are on Linux, we can continue

                //Detect SMBuses
                SMBusManager.DetectSMBuses();

                Log("Outputting all detected SMBuses:");

                //SMBusManager does initialization and detection in static constructor
                //Here we iterate all detected SMBuses and output some information
                foreach (var bus in SMBusManager.RegisteredSMBuses)
                {
                    Log($"  Bus is of type {bus.GetType()}:");
                    Log($"    {nameof(bus.DeviceName        )} = {bus.DeviceName        }");
                    Log($"    {nameof(bus.PortID            )} = {bus.PortID            }");
                    Log($"    {nameof(bus.PCIDevice         )} = {bus.PCIDevice         }");
                    Log($"    {nameof(bus.PCIVendor         )} = {bus.PCIVendor         }");
                    Log($"    {nameof(bus.PCISubsystemDevice)} = {bus.PCISubsystemDevice}");
                    Log($"    {nameof(bus.PCISubsystemVendor)} = {bus.PCISubsystemVendor}");

                    if (bus is SMBusI801 intel)
                    {
                        Log($"    {nameof(SMBusI801.I801_SMBA)} = {intel.I801_SMBA}");
                    }
                    else if (bus is SMBusLinux linux)
                    {
                        linux.CheckFuncs();
                    }

                    //This outputs a table with detected devices for each "functionality"
                    Log();
                    Log("  Running detection for SMBus.");
                    Log();
                    Log($"    Mode {nameof(I2CSMBusDetect.MODE_AUTO )}:{Environment.NewLine}{I2CSMBusDetect.I2CDetect(bus, I2CSMBusDetect.MODE_AUTO )}");
                    Log($"    Mode {nameof(I2CSMBusDetect.MODE_QUICK)}:{Environment.NewLine}{I2CSMBusDetect.I2CDetect(bus, I2CSMBusDetect.MODE_QUICK)}");
                    Log($"    Mode {nameof(I2CSMBusDetect.MODE_READ )}:{Environment.NewLine}{I2CSMBusDetect.I2CDetect(bus, I2CSMBusDetect.MODE_READ )}");
                    Log();

                    //See if the SPDs are available
                    //SPDs are accessed not through a list, but are hard coded for a full stack of RAM modules.
                    //Some mainboards could have up to 8 modules.
                    for (byte i = SPDConstants.SPD_BEGIN; i <= SPDConstants.SPD_END; i++)
                    {
                        //Test for DDR4 RAM
                        ManuallyTestForDDR4(bus, i);

                        //Test for DDR5 RAM
                        ManuallyTestForDDR5(bus, i);

                        Log();
                    }

                    //Done with current SMBus
                    Log($"### Done with Bus {bus.GetType()}. ###");
                }

                Log();
                Log($"Leaving {nameof(DoTest)}.");
            }
            catch (Exception e)
            {
                //Log any exception we encounter
                Log(e.FullExceptionString());
            }
            finally
            {
                //For Windows, do cleanup and unload driver
                if (OperatingSystem.IsWindows())
                {
                    //Finished, must do cleanup
                    DriverManager.UnloadDriver();
                    Log("***** Closed driver. *****");
                }
            }
        }

        void ManuallyTestForDDR4(SMBusInterface bus, byte i)
        {
            //Check if the SPD is available
            if (DDR4Accessor.IsAvailable(bus, i))
            {
                Log($"{nameof(DDR4Accessor)} available at {i:X2}.");

                //DDR4 set page address is not within the write protected range, so we can safely ignore that
                TryReadSPDData<DDR4Accessor>(bus, i);
            }
        }

        void ManuallyTestForDDR5(SMBusInterface bus, byte i)
        {
            //Check if the SPD is available
            if (DDR5Accessor.IsAvailable(bus, i))
            {
                Log($"{nameof(DDR5Accessor)} available at {i:X2}.");

                //DDR5 set page is within write protection, so we cannot read everything
                TryReadSPDData<DDR5Accessor>(bus, i);
            }
        }

        void TryReadSPDData<TAccessor>(SMBusInterface bus, byte i)
            where TAccessor : SPDAccessor, IThermalSensor
        {
            //Create an instance of our accessor
            var spd = Activator.CreateInstance(typeof(TAccessor), bus, i) as TAccessor;

            if (spd == null)
            {
                throw new Exception($"{nameof(TryReadSPDData)} {nameof(spd)} is null. Provided generic type was '{typeof(TAccessor).FullName}'.");
            }

            /*

            //We can manually set the page to get what data we want
            //For example, if we want to read the module part number, we can do this:
            if (spd.ChangePage(PageData.ModulePartNumber))
            {
                Log($"Module Serial Number: {spd.ModulePartNumber()}");
            }

            //For DDR4 on both Windows and Linux the write protection does not include the address for page change,
            //so pages can be changed normally. You can use ChangePage, but don't have to.

            //For DDR5 systems the page address is write protected, if enabled.
            //A workaround for Intel Write Protection, for page change only, is PROC_CALL.
            //This has been implemented and currently works on Windows.
            //The Linux Kernel (i2c-i801.c) does currently not support it the way it is required.

            //You can always check "HasSPDWriteProtection" in SMBus if write protection is enabled.

            */

            Log($"Page Data                            : {spd.PageData}");
            Log($"SPD Revision                         : 0x{spd.SPDRevision():X2}");
            Log($"Memory Type                          : {spd.MemoryType()}");
            Log($"Capacity (GB)                        : {spd.GetCapacity()}");

            if (OS.IsWindows() || (OS.IsLinux() && !bus.HasSPDWriteProtection))
            {
                Log($"Module Manufacturer Continuation Code: 0x{spd.ModuleManufacturerContinuationCode():X2}");
                Log($"Module Manufacturer ID Code          : 0x{spd.ModuleManufacturerIDCode():X2}");
                Log($"Module Manufacturer Mapped           : {spd.GetModuleManufacturerString()}");
                Log($"Module Manufacturing Location        : {spd.ModuleManufacturingLocation()}");

                var manufacturingDate = spd.ModuleManufacturingDate();
                Log($"Module Manufacturing Date            : {(manufacturingDate != null ? manufacturingDate : "+++ No data +++")}");
                Log($"Module Serial Number                 : {spd.ModuleSerialNumber()}");
                Log($"Module Part Number                   : {spd.ModulePartNumber()}");
                Log($"Module Revision Code                 : {spd.ModuleRevisionCode():X2}");
                Log($"DRAM Manufacturer Continuation Code  : 0x{spd.DRAMManufacturerContinuationCode():X2}");
                Log($"DRAM Manufacturer ID Code            : 0x{spd.DRAMManufacturerIDCode():X2}");
                Log($"DRAM Manufacturer Mapped             : {spd.GetDRAMManufacturerString()}");

                Log($"");
            }
            else if (OS.IsLinux() && bus.HasSPDWriteProtection)
            {
                Log($"+++++ SPD Write Protection is active. SPD-Page cannot be set and some data can therefore not be read. +++++");
                Log($"+++++ Please disable SPD Write Protection in BIOS and try again. +++++");
                Log($"+++++ Unfortunately Linux does currently not support page change via PROC_CALL while SPD Write Protection is active. +++++");
            }

            if (spd.UpdateTemperature())
            {
                Log($"Temperature read success.");

                var fahrenheit = TemperatureConverter.CelsiusToFahrenheit(spd.Temperature);

                Log($"Temperature for Module at address 0x{i:X2} is: {spd.Temperature}°C / {fahrenheit}°F.");
                Log($"Temperature resolution is {spd.TemperatureResolution}.");
            }
            else
            {
                Log($"Temperature read failed.");
            }
        }
    }
}
