using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace DataFerret.Analytics.Tests.EditMode
{
    [TestFixture]
    [Category("Core")]
    public class EventBuilderTests
    {
        private const string AnonymousIdKey = "df_anonymous_id";
        private IdentityManager _identityManager;
        private EventBuilder _eventBuilder;

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey(AnonymousIdKey);
            _identityManager = new IdentityManager();
            _eventBuilder = new EventBuilder(_identityManager);
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(AnonymousIdKey);
        }

        /// <summary>
        /// TC-EB-001: BuildTrack sets Type="track" and Event=eventName.
        /// </summary>
        [Test]
        public void BuildTrack_SetsTypeTrackAndEventName()
        {
            var properties = new Dictionary<string, object>
            {
                { "button", "play" },
                { "level", 3 }
            };

            var envelope = _eventBuilder.BuildTrack("button_clicked", properties);

            Assert.That(envelope.Type, Is.EqualTo("track"),
                "Type should be 'track'");
            Assert.That(envelope.Event, Is.EqualTo("button_clicked"),
                "Event should match the provided event name");
            Assert.That(envelope.Properties, Is.Not.Null,
                "Properties should not be null when provided");
            Assert.That(envelope.Properties["button"], Is.EqualTo("play"),
                "Properties should contain the provided values");
            Assert.That(envelope.Properties["level"], Is.EqualTo(3),
                "Properties should contain the provided numeric values");
        }

        /// <summary>
        /// TC-EB-002: BuildIdentify sets Type="identify" and UserId.
        /// </summary>
        [Test]
        public void BuildIdentify_SetsTypeIdentifyAndUserId()
        {
            var traits = new Dictionary<string, object>
            {
                { "email", "test@example.com" },
                { "plan", "premium" }
            };

            var envelope = _eventBuilder.BuildIdentify("user_42", traits);

            Assert.That(envelope.Type, Is.EqualTo("identify"),
                "Type should be 'identify'");
            Assert.That(envelope.UserId, Is.EqualTo("user_42"),
                "UserId should match the provided user ID");
            Assert.That(envelope.Properties, Is.Not.Null,
                "Properties (traits) should not be null when provided");
            Assert.That(envelope.Properties["email"], Is.EqualTo("test@example.com"),
                "Properties should contain the provided traits");
        }

        /// <summary>
        /// TC-EB-003: BuildScreen sets Type="screen" and Event=screenName.
        /// </summary>
        [Test]
        public void BuildScreen_SetsTypeScreenAndScreenName()
        {
            var properties = new Dictionary<string, object>
            {
                { "previous_scene", "MainMenu" }
            };

            var envelope = _eventBuilder.BuildScreen("GameplayScene", properties);

            Assert.That(envelope.Type, Is.EqualTo("screen"),
                "Type should be 'screen'");
            Assert.That(envelope.Event, Is.EqualTo("GameplayScene"),
                "Event should match the provided screen name");
            Assert.That(envelope.Properties["previous_scene"], Is.EqualTo("MainMenu"),
                "Properties should contain the provided values");
        }

        /// <summary>
        /// TC-EB-004: BuildGroup sets Type="group" and Event=groupId.
        /// </summary>
        [Test]
        public void BuildGroup_SetsTypeGroupAndGroupId()
        {
            var traits = new Dictionary<string, object>
            {
                { "guild_name", "Dragon Slayers" },
                { "member_count", 25 }
            };

            var envelope = _eventBuilder.BuildGroup("guild_99", traits);

            Assert.That(envelope.Type, Is.EqualTo("group"),
                "Type should be 'group'");
            Assert.That(envelope.Event, Is.EqualTo("guild_99"),
                "Event should match the provided group ID");
            Assert.That(envelope.Properties["guild_name"], Is.EqualTo("Dragon Slayers"),
                "Properties should contain the provided traits");
        }

        /// <summary>
        /// TC-EB-005: All build methods set required fields (EventId, Type, Timestamp, AnonymousId, Context not null).
        /// </summary>
        [Test]
        public void AllBuildMethods_SetRequiredCommonFields()
        {
            var track = _eventBuilder.BuildTrack("evt", null);
            var identify = _eventBuilder.BuildIdentify("uid", null);
            var screen = _eventBuilder.BuildScreen("scr", null);
            var group = _eventBuilder.BuildGroup("gid", null);

            var envelopes = new[] { track, identify, screen, group };
            var labels = new[] { "Track", "Identify", "Screen", "Group" };

            for (int i = 0; i < envelopes.Length; i++)
            {
                var env = envelopes[i];
                var label = labels[i];

                Assert.That(env.EventId, Is.Not.Null.And.Not.Empty,
                    $"{label}: EventId should not be null or empty");
                Assert.That(env.Type, Is.Not.Null.And.Not.Empty,
                    $"{label}: Type should not be null or empty");
                Assert.That(env.Timestamp, Is.Not.Null.And.Not.Empty,
                    $"{label}: Timestamp should not be null or empty");
                Assert.That(env.AnonymousId, Is.Not.Null.And.Not.Empty,
                    $"{label}: AnonymousId should not be null or empty");
                Assert.That(env.Context, Is.Not.Null,
                    $"{label}: Context should not be null");
            }
        }

        /// <summary>
        /// TC-EB-006: Context.Library.Name = "com.dataferret.analytics".
        /// </summary>
        [Test]
        public void BuildTrack_ContextLibrary_HasCorrectNameAndVersion()
        {
            var envelope = _eventBuilder.BuildTrack("test_event", null);

            Assert.That(envelope.Context, Is.Not.Null,
                "Context should not be null");
            Assert.That(envelope.Context.Library, Is.Not.Null,
                "Context.Library should not be null");
            Assert.That(envelope.Context.Library.Name, Is.EqualTo("com.dataferret.analytics"),
                "Library name should be 'com.dataferret.analytics'");
            Assert.That(envelope.Context.Library.Version, Is.EqualTo("0.1.0"),
                "Library version should be '0.1.0'");
        }

        /// <summary>
        /// TC-EB-007: Properties with 201+ keys are truncated to 200.
        /// </summary>
        [Test]
        public void BuildTrack_WithOver200Properties_TruncatesTo200()
        {
            var properties = new Dictionary<string, object>();
            for (int i = 0; i < 210; i++)
            {
                properties[$"key_{i}"] = i;
            }

            var envelope = _eventBuilder.BuildTrack("big_event", properties);

            Assert.That(envelope.Properties, Is.Not.Null,
                "Properties should not be null");
            Assert.That(envelope.Properties.Count, Is.LessThanOrEqualTo(200),
                "Properties count should be truncated to 200 max");
        }

        /// <summary>
        /// TC-EB-008: EventId is a valid UUID format (Guid.TryParse).
        /// </summary>
        [Test]
        public void BuildTrack_EventId_IsValidUuidFormat()
        {
            var envelope = _eventBuilder.BuildTrack("uuid_test", null);

            Assert.That(envelope.EventId, Is.Not.Null.And.Not.Empty,
                "EventId should not be null or empty");
            Assert.That(Guid.TryParse(envelope.EventId, out _), Is.True,
                $"EventId '{envelope.EventId}' should be a valid UUID format");
        }

        /// <summary>
        /// TC-EB-009: Global properties are merged with event properties, event overrides globals.
        /// </summary>
        [Test]
        public void BuildTrack_WithGlobalProperties_MergesCorrectly()
        {
            var globalProps = new Dictionary<string, object>
            {
                { "app_version", "1.0.0" },
                { "shared_key", "global_value" }
            };
            _eventBuilder.SetGlobalProperties(globalProps);

            var eventProps = new Dictionary<string, object>
            {
                { "button", "play" },
                { "shared_key", "event_value" }
            };

            var envelope = _eventBuilder.BuildTrack("merge_test", eventProps);

            Assert.That(envelope.Properties["app_version"], Is.EqualTo("1.0.0"),
                "Global properties should be included");
            Assert.That(envelope.Properties["button"], Is.EqualTo("play"),
                "Event-specific properties should be included");
            Assert.That(envelope.Properties["shared_key"], Is.EqualTo("event_value"),
                "Event properties should override global properties with the same key");
        }

        /// <summary>
        /// TC-EB-010: Object pooling - ReturnEnvelope and subsequent BuildTrack reuse the envelope.
        /// </summary>
        [Test]
        public void ObjectPooling_ReturnedEnvelope_IsReused()
        {
            var envelope1 = _eventBuilder.BuildTrack("first", null);
            var reference = envelope1;
            _eventBuilder.ReturnEnvelope(envelope1);

            var envelope2 = _eventBuilder.BuildTrack("second", null);

            Assert.That(ReferenceEquals(reference, envelope2), Is.True,
                "Returned envelope should be reused from the pool");
            Assert.That(envelope2.Event, Is.EqualTo("second"),
                "Reused envelope should have new event name");
            Assert.That(envelope2.Type, Is.EqualTo("track"),
                "Reused envelope should have correct type");
        }
    }
}
