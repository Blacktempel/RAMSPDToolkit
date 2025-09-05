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

using System.Collections;

namespace RAMSPDToolkit.Extensions
{
    /// <summary>
    /// Extension class for <see cref="object"/>.
    /// </summary>
    public static class ObjectExtensions
    {
        /// <summary>
        /// Checks if the current object matches (Equals) any of the specified objects.
        /// </summary>
        /// <param name="obj">The current object.</param>
        /// <param name="value1">An object to check for equality.</param>
        /// <param name="value2">An object to check for equality.</param>
        /// <returns>If any specified object is equal to the current object.</returns>
        public static bool Any<T>(this T obj, T value1, T value2)
        {
            return obj.Equals(value1) || obj.Equals(value2);
        }

        /// <summary>
        /// Checks if the current object matches (Equals) any of the specified objects.
        /// </summary>
        /// <param name="obj">The current object.</param>
        /// <param name="value1">An object to check for equality.</param>
        /// <param name="value2">An object to check for equality.</param>
        /// <param name="values">A variable list of objects to check for equality.</param>
        /// <returns>If any specified object is equal to the current object.</returns>
        public static bool Any<T>(this T obj, T value1, T value2, params T[] values)
        {
            if (Any(obj, value1, value2))
                return true;
            foreach (var o in values)
                if (obj.Equals(o))
                    return true;
            return false;
        }

        /// <summary>
        /// Checks if the current object matches (Equals) any of the objects in specified <see cref="IEnumerable"/>.
        /// </summary>
        /// <param name="obj">The current object.</param>
        /// <param name="collection">A collection of objects to check for equality.</param>
        /// <returns>If any object in <see cref="IEnumerable"/> is equal to the current object.</returns>
        public static bool Any<T>(this T obj, IEnumerable<T> collection)
        {
            foreach (var o in collection)
                if (obj.Equals(o))
                    return true;
            return false;
        }
    }
}
