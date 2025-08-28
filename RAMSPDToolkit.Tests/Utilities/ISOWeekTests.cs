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
