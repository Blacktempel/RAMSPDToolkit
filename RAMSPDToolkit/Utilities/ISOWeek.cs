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

namespace RAMSPDToolkit.Utilities
{
    public static class ISOWeek
    {
        #region Public

        /// <summary>
        /// Get a <see cref="DateTime"/> from specified year and week based on ISO 8601.
        /// </summary>
        /// <param name="year">Year.</param>
        /// <param name="week">Week of year.</param>
        /// <returns>New <see cref="DateTime"/> by using year and week parameters.</returns>
        public static DateTime ToDateTime(int year, int week)
        {
#if NET8_0_OR_GREATER
            if (week < 1 || week > 53)
            {
                return DateTime.MinValue;
            }

            return System.Globalization.ISOWeek.ToDateTime(year, week, DayOfWeek.Monday);
#else
            var firstJan = new DateTime(year, 1, 1);
            int daysOffset = DayOfWeek.Thursday - firstJan.DayOfWeek;

            //Use first Thursday in January to get first week of the year
            var firstThursday = firstJan.AddDays(daysOffset);

            var firstWeek = System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                firstThursday, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

            var weekNum = week;

            //As we're adding days to date in first week,
            //we need to subtract 1 in order to get the right date for week one
            if (firstWeek == 1)
            {
                weekNum -= 1;
            }

            //Using first Thursday as starting week ensures that we are starting in the right year
            //We then add number of weeks multiplied by days
            var result = firstThursday.AddDays(weekNum * 7);

            //Subtract 3 days from Thursday to get Monday, which is the first weekday in ISO8601
            return result.AddDays(-3);
#endif
        }

        #endregion
    }
}
