using System.Collections.Generic;

namespace DataFerret.Analytics.Tests.EditMode
{
    /// <summary>
    /// 테스트용 EventEnvelope, EventContext 객체를 생성하는 팩토리.
    /// </summary>
    internal static class EventTestFactory
    {
        public const string ValidEventId = "0190a6e0-7b3a-7000-8000-000000000001";
        public const string ValidTimestamp = "2026-03-07T12:00:00.0000000Z";
        public const string ValidAnonymousId = "550e8400-e29b-41d4-a716-446655440000";
        public const string ValidUserId = "user-123";
        public const string ValidEventName = "button_clicked";

        public static EventEnvelope CreateFullEnvelope()
        {
            return new EventEnvelope
            {
                EventId = ValidEventId,
                Type = "track",
                Timestamp = ValidTimestamp,
                AnonymousId = ValidAnonymousId,
                UserId = ValidUserId,
                Event = ValidEventName,
                Properties = CreateSampleProperties(),
                Context = CreateFullContext()
            };
        }

        public static EventEnvelope CreateMinimalEnvelope()
        {
            return new EventEnvelope
            {
                EventId = ValidEventId,
                Type = "track",
                Timestamp = ValidTimestamp,
                AnonymousId = ValidAnonymousId,
                Context = CreateMinimalContext()
            };
        }

        public static EventEnvelope CreateEnvelopeWithEmptyProperties()
        {
            var envelope = CreateMinimalEnvelope();
            envelope.Properties = new Dictionary<string, object>();
            return envelope;
        }

        public static EventEnvelope CreateEnvelopeWithNestedProperties()
        {
            var envelope = CreateMinimalEnvelope();
            envelope.Properties = new Dictionary<string, object>
            {
                { "simple", "value" },
                { "number", 42 },
                { "nested", new Dictionary<string, object>
                    {
                        { "inner_key", "inner_value" },
                        { "inner_list", new List<object> { 1, 2, 3 } }
                    }
                }
            };
            return envelope;
        }

        public static Dictionary<string, object> CreateSampleProperties()
        {
            return new Dictionary<string, object>
            {
                { "button_name", "start_game" },
                { "level", 5 },
                { "is_premium", true }
            };
        }

        public static EventContext CreateFullContext()
        {
            return new EventContext
            {
                Library = CreateLibraryContext(),
                Device = CreateDeviceContext(),
                Game = CreateGameContext()
            };
        }

        public static EventContext CreateMinimalContext()
        {
            return new EventContext
            {
                Library = CreateLibraryContext()
            };
        }

        public static LibraryContext CreateLibraryContext()
        {
            return new LibraryContext
            {
                Name = "com.dataferret.analytics",
                Version = "0.1.0"
            };
        }

        public static DeviceContext CreateDeviceContext()
        {
            return new DeviceContext
            {
                Type = "Desktop",
                Model = "MacBookPro18,1",
                Gpu = "Apple M1 Pro",
                Os = "macOS 14.5"
            };
        }

        public static GameContext CreateGameContext()
        {
            return new GameContext
            {
                Engine = "Unity",
                EngineVersion = "2021.3.0f1",
                AppVersion = "1.0.0",
                Platform = "OSXEditor",
                Scene = "MainMenu",
                Store = "steam"
            };
        }
    }
}
