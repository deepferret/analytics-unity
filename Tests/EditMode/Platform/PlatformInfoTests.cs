using System.Text.RegularExpressions;
using NUnit.Framework;

namespace DataFerret.Analytics.Tests.EditMode
{
    [TestFixture]
    [Category("Platform")]
    public class PlatformInfoTests
    {
        /// <summary>
        /// TC-PI-001: DeviceType returns a non-null, non-empty string.
        /// </summary>
        [Test]
        public void DeviceType_WhenAccessed_IsNotNullOrEmpty()
        {
            var deviceType = PlatformInfo.DeviceType;

            Assert.That(deviceType, Is.Not.Null.And.Not.Empty,
                "DeviceType should not be null or empty");
        }

        /// <summary>
        /// TC-PI-002: UnityVersion contains a digit.digit pattern (e.g., "2021.3").
        /// </summary>
        [Test]
        public void UnityVersion_WhenAccessed_ContainsVersionPattern()
        {
            var unityVersion = PlatformInfo.UnityVersion;

            Assert.That(unityVersion, Is.Not.Null.And.Not.Empty,
                "UnityVersion should not be null or empty");
            Assert.That(Regex.IsMatch(unityVersion, @"\d+\.\d+"), Is.True,
                $"UnityVersion '{unityVersion}' should contain a digit.digit pattern");
        }

        /// <summary>
        /// TC-PI-003: Platform returns a non-null, non-empty string.
        /// </summary>
        [Test]
        public void Platform_WhenAccessed_IsNotNullOrEmpty()
        {
            var platform = PlatformInfo.Platform;

            Assert.That(platform, Is.Not.Null.And.Not.Empty,
                "Platform should not be null or empty");
        }

        /// <summary>
        /// TC-PI-004: Store returns one of the expected values.
        /// </summary>
        [Test]
        public void Store_WhenAccessed_ReturnsExpectedValue()
        {
            var store = PlatformInfo.Store;

            var expectedValues = new[] { "google_play", "app_store", "web", "steam", "unknown" };
            Assert.That(expectedValues, Does.Contain(store),
                $"Store '{store}' should be one of: {string.Join(", ", expectedValues)}");
        }
    }
}
