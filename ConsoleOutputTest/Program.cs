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
using BlackSharp.Core.Logging;
using ConsoleOutputTest;
using ConsoleOutputTest.Exceptions;

static class Program
{
    static void Main(string[] args)
    {
        //Add a global exception handler
        GlobalExceptionHandler.RegisterGlobalExceptionHandler();

        //Enable logging and set level
        Logger.Instance.IsEnabled = true;
        Logger.Instance.LogLevel = LogLevel.Trace;

        var tc = new TestClass();

        try
        {
            //Start our test
            tc.DoTest();
        }
        catch (Exception e)
        {
            //On exception, we log it to console first and also to our log file
            var exceptionString = e.FullExceptionString();
            Console.WriteLine(exceptionString);

            Logger.Instance.Add(LogLevel.Error, exceptionString, DateTime.Now);
        }

        //Save log file to current directory
        Logger.Instance.SaveToFile("Log.txt", false);

        //All done
        Console.WriteLine("Press enter to exit...");
        Console.ReadLine();
    }
}
