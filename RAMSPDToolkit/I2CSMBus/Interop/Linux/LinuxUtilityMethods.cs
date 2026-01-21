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

using System.Globalization;

namespace RAMSPDToolkit.I2CSMBus.Interop.Linux
{
    internal static class LinuxUtilityMethods
    {
        #region Public

        public static bool TryParseBusDeviceFunction(string name, out int bus, out int device, out int function)
        {
            bus = device = function = 0;

            //Format: "0000:bb:dd.f" or "bb:dd.f"
            //We expect at least "bb:dd.f"
            string[] parts = name.Split([':'], StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 2)
            {
                return false;
            }

            string busPart, devFuncPart;

            if (parts.Length == 3)
            {
                busPart = parts[1];
                devFuncPart = parts[2];
            }
            else
            {
                busPart = parts[0];
                devFuncPart = parts[1];
            }

            var df = devFuncPart.Split(['.'], StringSplitOptions.RemoveEmptyEntries);

            if (df.Length != 2)
            {
                return false;
            }

            if (!int.TryParse(busPart, NumberStyles.HexNumber, null, out bus     )
             || !int.TryParse(df[0]  , NumberStyles.HexNumber, null, out device  )
             || !int.TryParse(df[1]  , NumberStyles.Integer  , null, out function))
            {
                return false;
            }

            return true;
        }

        public static bool TryReadHex16(string path, out ushort value)
        {
            value = 0;

            try
            {
                var txt = File.ReadAllText(path).Trim();

                //Example: "0x8086"
                if (txt.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    txt = txt.Substring(2);
                }

                if (ushort.TryParse(txt, NumberStyles.HexNumber, null, out var v))
                {
                    value = v;
                    return true;
                }
            }
            catch { }

            return false;
        }

        #endregion
    }
}
