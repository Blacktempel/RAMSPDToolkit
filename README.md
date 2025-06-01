# RAMSPDToolkit
[![GitHub license](https://img.shields.io/github/license/blacktempel/ramspdtoolkit)](https://github.com/blacktempel/ramspdtoolkit/blob/master/license)
[![Build master](https://github.com/Blacktempel/RAMSPDToolkit/actions/workflows/master.yml/badge.svg)](https://github.com/Blacktempel/RAMSPDToolkit/actions/workflows/master.yml)
[![Nuget](https://img.shields.io/nuget/v/RAMSPDToolkit)](https://www.nuget.org/packages/RAMSPDToolkit/)

A toolkit for accessing RAMs SPD. Primarily used for reading temperature and data from SPD.<br/>
Reading from SPD is standardized by [JEDEC](https://www.jedec.org/), for the most part.<br/>

### Warning - writing via SMBus and to SPD is dangerous
It is possible and supported to write data via SMBus, but **NOT recommended** and **dangerous** for your hardware.<br/>
You can also write data to the SPD via SMBus, when write protection is disabled, which is also **NOT recommended.**<br/>
Doing so **COULD** brick whatever hardware you're writing to, rendering that hardware part useless.<br/>
Writing to something you should not write to means: this piece of hardware **WILL** be [a rock,](https://en.wikipedia.org/wiki/Rock_(geology)) once again.<br/>
Do this at your own risk and only if you know what you are doing.

## Project overview
| Project | .NET Version[s] |
| --- | --- |
| **[RAMSPDToolkit](https://github.com/Blacktempel/RAMSPDToolkit/tree/master/RAMSPDToolkit)** <br/> RAM SPD functionality for your software / application. <br/> Also includes various SMBus implementations for Windows and Linux. <br/> Depends on WinRing0Driver. | .NET Framework 4.7.2 & 4.8.1 <br/> .NET 8 & .NET 9 |
| **[WinRing0Driver](https://github.com/Blacktempel/RAMSPDToolkit/tree/master/WinRing0Driver)** <br/> WinRing0 driver implementation and binaries.<br/> This is required for RAMSPDToolkit. | .NET Framework 4.7.2 & 4.8.1 <br/> .NET 8 & .NET 9 |
| **[ConsoleOutputTest](https://github.com/Blacktempel/RAMSPDToolkit/tree/master/ConsoleOutputTest)** <br/> Example Application to show how some library functionality can be used. | .NET 8 |
| **[GZipSingleFile](https://github.com/Blacktempel/RAMSPDToolkit/tree/master/GZipSingleFile)** <br/> Small ("internal") console application used to zip driver files. | .NET 8 |
| **[Tools](https://github.com/Blacktempel/RAMSPDToolkit/tree/master/Tools)** <br/> Tools for the project. | --- |

## What platforms are supported ?
We currently support Windows and Linux.<br/>
There is currently no plan to support Mac / Apple Computers.

## Which RAM is supported ?
We support DDR4 & DDR5 RAM on Intel & AMD systems.

## Where can I download it ?
You can download the latest release [from here.](https://github.com/Blacktempel/RAMSPDToolkit/releases)

## How can I help improve the project ?
Feel free to give feedback and contribute to our project !<br/>
Pull requests are welcome. Please include as much information as possible.

## Developer information
**Integrate the library in your own application**

**Sample code**
```C#
static class Program
{
    static void Main(string[] args)
    {
        //You can enable logging and set level, if you need logging output
        Logger.Instance.IsEnabled = true;
        Logger.Instance.LogLevel = LogLevel.Trace;

        //Check for Windows before loading driver
        if (OperatingSystem.IsWindows())
        {
            //Load WinRing0 driver - this extracts driver files and loads them
            if (!DriverManager.LoadDriver())
            {
                Console.WriteLine($"***** DRIVER ERROR *****");
                Console.WriteLine($"-> OLS Status Code: {DriverManager.Ols.OLSStatus}");
                Console.WriteLine($"-> DLL Status Code: {(OLSDLLStatus)DriverManager.Ols.GetDllStatus()}");
                Environment.Exit(1);
            }
            else
            {
                Console.WriteLine("***** Driver is open *****");
            }
        }

        //First, go through available SMBuses
        foreach (var bus in SMBusManager.RegisteredSMBuses)
        {
            //Go through possible RAM slots
            for (byte i = SPDConstants.SPD_BEGIN; i <= SPDConstants.SPD_END; i++)
            {
                //Detect what kind of RAM we have
                var detector = new SPDDetector(bus, i);

                //We have a RAM stick here
                if (detector.IsValid)
                {
                    if (detector.Accessor is IThermalSensor ts)
                    {
                        //Get temperature, if possible
                        if (ts.HasThermalSensor && ts.UpdateTemperature())
                        {
                            //Output temperature for detected RAM sticks
                            Console.WriteLine($"DIMM #{i - SPDConstants.SPD_BEGIN}: {ts.Temperature}°C / {TemperatureConverter.CelsiusToFahrenheit(ts.Temperature)}°F.");
                        }
                    }
                }
            }
        }

        //Check for Windows before unloading driver
        if (OperatingSystem.IsWindows())
        {
            //Unload the driver - this removes extracted driver files and unloads them
            DriverManager.UnloadDriver();
            Console.WriteLine("***** Closed driver *****");
        }

        //Save log file to current directory, if you enabled logging output
        Logger.Instance.SaveToFile("Log.txt", false);

        //All done
        Console.WriteLine("Press enter to exit...");
        Console.ReadLine();
    }
}
```

**Administrator rights**

Some functionality requires administrator privileges to access the data.<br/>
Consider adding a manifest file to your application and set ``requestedExecutionLevel`` to ``requireAdministrator``.

## License
RAMSPDToolkit is free and open source software licensed under MPL 2.0.<br/>
You can use it in private and commercial projects.<br/>
Keep in mind that you must include a copy of the license in your project.
