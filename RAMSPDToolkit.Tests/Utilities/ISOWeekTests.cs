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

using RAMSPDToolkit.Utilities;

namespace RAMSPDToolkit.Tests.Utilities
{
    [TestClass]
    public class ISOWeekTests
    {
        [TestMethod]
        public void ToDateTime()
        {
            var one   = ISOWeek.ToDateTime(2025, 2);
            var two   = ISOWeek.ToDateTime(2025, 20);
            var three = ISOWeek.ToDateTime(2025, 52);

            Assert.AreEqual(new DateTime(2025, 01, 06), one);
            Assert.AreEqual(new DateTime(2025, 05, 12), two);
            Assert.AreEqual(new DateTime(2025, 12, 22), three);

            Assert.IsTrue(one   .DayOfWeek == DayOfWeek.Monday);
            Assert.IsTrue(two   .DayOfWeek == DayOfWeek.Monday);
            Assert.IsTrue(three .DayOfWeek == DayOfWeek.Monday);
        }
    }
}
