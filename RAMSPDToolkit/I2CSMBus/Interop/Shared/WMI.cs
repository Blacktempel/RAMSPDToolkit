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

#pragma warning disable CA1416 // Platform compatibility warning

using System.Management;
using System.Text.RegularExpressions;

using OS = BlackSharp.Core.Platform.OperatingSystem;

namespace RAMSPDToolkit.I2CSMBus.Interop.Shared
{
    /// <summary>
    /// Provides access to Windows Management Instrumentation (WMI).
    /// </summary>
    internal sealed class WMI
    {
        #region Constructor

        public WMI()
        {
            if (!OS.IsWindows())
            {
                throw new PlatformNotSupportedException();
            }
        }

        #endregion

        #region Public

        public List<Dictionary<string, string>> Query(string query, Dictionary<string, Regex> filters = null)
        {
            if (!OS.IsWindows())
            {
                throw new PlatformNotSupportedException();
            }

            var searcher = new ManagementObjectSearcher(new ManagementScope("ROOT\\CIMV2"), new ObjectQuery("WQL", query));
            var collection = searcher.Get();

            var list = new List<Dictionary<string, string>>();

            foreach (var item in collection)
            {
                if (filters != null)
                {
                    bool filterNotMatched = false;
                    foreach (var filter in filters)
                    {
                        var value = item.GetPropertyValue(filter.Key);

                        if (value != null)
                        {
                            //Check for filter regex match
                            if (!filter.Value.Match(value.ToString()).Success)
                            {
                                //No match for filter, skip this element
                                filterNotMatched = true;
                                break;
                            }
                        }
                    }

                    //Filter not matched, skip to next element
                    if (filterNotMatched)
                    {
                        continue;
                    }
                }

                var dict = new Dictionary<string, string>();

                foreach (var prop in item.Properties)
                {
                    if (prop.Value == null)
                    {
                        continue;
                    }

                    if ((prop.Type == CimType.String || prop.Type == CimType.Reference) && prop.Type != CimType.None)
                    {
                        dict.Add(prop.Name, prop.Value.ToString());
                    }

                    if (prop.Name == "Dependent" && prop.Type != CimType.None)
                    {
                        if (!dict.ContainsKey(prop.Name))
                        {
                            dict.Add(prop.Name, prop.Value.ToString());
                        }
                    }
                }

                list.Add(dict);
            }

            return list;
        }

        #endregion
    }
}

#pragma warning restore CA1416 // Platform compatibility warning
