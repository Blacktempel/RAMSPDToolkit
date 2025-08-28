using RAMSPDToolkit.Logging;

namespace RAMSPDToolkit.Tests.Logging
{
    [TestClass]
    public class LoggerTests
    {
        static object _Lock = new object();

        LogLevel _LogLevel = LogLevel.Debug;

        [TestMethod]
        public void Add()
        {
            var message0 = "Add Message 0";
            var message1 = "Add Message 1";
            var message2 = "Add Message 2";

            string str;

            lock (_Lock)
            {
                Logger.Instance.IsEnabled = true;
                Logger.Instance.LogLevel = _LogLevel;

                Assert.AreEqual(_LogLevel, Logger.Instance.LogLevel);

                Logger.Instance.Add(_LogLevel, message0, DateTime.Now);
                Logger.Instance.Add(_LogLevel, message1);
                Logger.Instance.Add(new LogItem(_LogLevel, message2, DateTime.Now));

                str = Logger.Instance.ToString();
            }

            Assert.IsTrue(str.Contains(message0));
            Assert.IsTrue(str.Contains(message1));
            Assert.IsTrue(str.Contains(message2));
        }

        [TestMethod]
        public void Remove()
        {
            var message = "Remove Message";

            string str;

            lock (_Lock)
            {
                Logger.Instance.IsEnabled = true;
                Logger.Instance.LogLevel = _LogLevel;

                Logger.Instance.Add(_LogLevel, message, DateTime.Now);
                Logger.Instance.Remove(_LogLevel);

                str = Logger.Instance.ToString();
            }

            Assert.IsTrue(!str.Contains(message));
        }

        [TestMethod]
        public void GetStringForLogLevel()
        {
            string str;

            lock (_Lock)
            {
                str = Logger.GetStringForLogLevel(_LogLevel);
            }

            Assert.AreEqual("[DEBUG]", str);
        }
    }
}
