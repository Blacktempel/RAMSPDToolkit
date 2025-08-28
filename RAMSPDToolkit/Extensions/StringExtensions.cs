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

namespace RAMSPDToolkit.Extensions
{
    public static class StringExtensions
    {
        #region Public

        /// <summary>
        /// Concatenates all given elements, using the specified separator for each element, but removing the last one.
        /// </summary>
        /// <param name="separator">Separator to use to separate elements.</param>
        /// <param name="values">Values to concatenate.</param>
        /// <returns>Concatenated string.</returns>
        public static string Join<T>(string separator, IEnumerable<T> values)
        {
            string str = string.Empty;
            foreach (var value in values)
            {
                str += value + separator;
            }
            return str?.Substring(0, str.Length - separator.Length);
        }

        /// <inheritdoc cref="Join{T}(string, IEnumerable{T})"/>
        public static string Join(string separator, params object[] values)
        {
            return Join<object>(separator, values);
        }

        #endregion
    }
}
