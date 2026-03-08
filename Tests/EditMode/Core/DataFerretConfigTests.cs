using NUnit.Framework;

namespace DataFerret.Analytics.Tests.EditMode
{
    [TestFixture]
    public class DataFerretConfigTests
    {
        /// <summary>
        /// TC-CF-001: Default values are set correctly on construction.
        /// </summary>
        [Test]
        public void DefaultValues_AreSetCorrectly()
        {
            var config = new DataFerretConfig();

            Assert.AreEqual("https://collect.dataferret.io", config.ApiHost);
            Assert.AreEqual(20, config.FlushSize);
            Assert.AreEqual(30f, config.FlushIntervalSeconds);
            Assert.AreEqual(1000, config.MaxQueueSize);
            Assert.AreEqual(3, config.MaxRetries);
            Assert.AreEqual(10, config.TimeoutSeconds);
            Assert.IsFalse(config.Debug);
        }

        /// <summary>
        /// TC-CF-002: WriteKey null or empty causes IsValid() to return false.
        /// </summary>
        [Test]
        public void IsValid_WriteKeyNullOrEmpty_ReturnsFalse()
        {
            var config = new DataFerretConfig();

            config.WriteKey = null;
            Assert.IsFalse(config.IsValid(), "null WriteKey should be invalid");

            config.WriteKey = "";
            Assert.IsFalse(config.IsValid(), "empty WriteKey should be invalid");
        }

        /// <summary>
        /// TC-CF-003: WriteKey without "wk_" prefix is invalid; with prefix is valid.
        /// </summary>
        [Test]
        public void IsValid_WriteKeyPrefix_ValidatesCorrectly()
        {
            var config = new DataFerretConfig();

            config.WriteKey = "invalid_key";
            Assert.IsFalse(config.IsValid(), "WriteKey without wk_ prefix should be invalid");

            config.WriteKey = "wk_test";
            Assert.IsTrue(config.IsValid(), "WriteKey with wk_ prefix should be valid");
        }

        /// <summary>
        /// TC-CF-004: AutoCapture defaults are set correctly.
        /// </summary>
        [Test]
        public void AutoCapture_DefaultValues_AreSetCorrectly()
        {
            var config = new DataFerretConfig();

            Assert.IsNotNull(config.AutoCapture);
            Assert.IsTrue(config.AutoCapture.Sessions);
            Assert.IsTrue(config.AutoCapture.SceneChanges);
            Assert.IsTrue(config.AutoCapture.Errors);
            Assert.IsTrue(config.AutoCapture.Lifecycle);
            Assert.IsFalse(config.AutoCapture.PerformanceSamples);
        }
    }
}
