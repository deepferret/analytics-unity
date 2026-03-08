using System;
using NUnit.Framework;

namespace DataFerret.Analytics.Tests.EditMode
{
    [TestFixture]
    [Category("Core")]
    public class EventTypeTests
    {
        [Test]
        public void EventType_HasExpectedValues()
        {
            Assert.That(Enum.IsDefined(typeof(EventType), EventType.Identify), Is.True);
            Assert.That(Enum.IsDefined(typeof(EventType), EventType.Track), Is.True);
            Assert.That(Enum.IsDefined(typeof(EventType), EventType.Page), Is.True);
            Assert.That(Enum.IsDefined(typeof(EventType), EventType.Screen), Is.True);
            Assert.That(Enum.IsDefined(typeof(EventType), EventType.Group), Is.True);
        }

        [Test]
        public void EventType_Count_IsFive()
        {
            var values = Enum.GetValues(typeof(EventType));
            Assert.That(values, Has.Length.EqualTo(5));
        }

        [TestCase(EventType.Identify, "Identify")]
        [TestCase(EventType.Track, "Track")]
        [TestCase(EventType.Page, "Page")]
        [TestCase(EventType.Screen, "Screen")]
        [TestCase(EventType.Group, "Group")]
        public void EventType_ToString_ReturnsExpectedNames(
            EventType type, string expectedName)
        {
            Assert.That(type.ToString(), Is.EqualTo(expectedName));
        }

        [TestCase(EventType.Track, "track")]
        [TestCase(EventType.Identify, "identify")]
        [TestCase(EventType.Screen, "screen")]
        [TestCase(EventType.Group, "group")]
        [TestCase(EventType.Page, "page")]
        public void EventType_ToLowerInvariant_MatchesApiFormat(
            EventType type, string expectedLowercase)
        {
            var typeString = type.ToString().ToLowerInvariant();
            Assert.That(typeString, Is.EqualTo(expectedLowercase));
        }
    }
}
