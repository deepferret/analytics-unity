using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DataFerret.Analytics.Tests.EditMode
{
    [TestFixture]
    [Category("Core")]
    public class EventContextTests
    {
        private JsonSerializerSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
        }

        // --- LibraryContext ---

        [Test]
        public void LibraryContext_Serialize_ContainsNameAndVersion()
        {
            var lib = EventTestFactory.CreateLibraryContext();
            var json = JsonConvert.SerializeObject(lib, _settings);
            var jObj = JObject.Parse(json);

            Assert.That(jObj.ContainsKey("name"), Is.True);
            Assert.That(jObj.ContainsKey("version"), Is.True);
        }

        [Test]
        public void LibraryContext_DefaultValues_NameIsPackageName()
        {
            var lib = EventTestFactory.CreateLibraryContext();
            Assert.That(lib.Name, Is.EqualTo("com.dataferret.analytics"));
        }

        // --- DeviceContext ---

        [Test]
        public void DeviceContext_Serialize_ContainsAllFields()
        {
            var device = EventTestFactory.CreateDeviceContext();
            var json = JsonConvert.SerializeObject(device, _settings);
            var jObj = JObject.Parse(json);

            Assert.That(jObj.ContainsKey("type"), Is.True);
            Assert.That(jObj.ContainsKey("model"), Is.True);
            Assert.That(jObj.ContainsKey("gpu"), Is.True);
            Assert.That(jObj.ContainsKey("os"), Is.True);
        }

        [Test]
        public void DeviceContext_WithNullFields_OmitsNullValues()
        {
            var device = new DeviceContext { Type = "Desktop" };
            var json = JsonConvert.SerializeObject(device, _settings);
            var jObj = JObject.Parse(json);

            Assert.That(jObj.ContainsKey("type"), Is.True);
            Assert.That(jObj.ContainsKey("model"), Is.False);
            Assert.That(jObj.ContainsKey("gpu"), Is.False);
            Assert.That(jObj.ContainsKey("os"), Is.False);
        }

        // --- GameContext ---

        [Test]
        public void GameContext_Serialize_ContainsAllFields()
        {
            var game = EventTestFactory.CreateGameContext();
            var json = JsonConvert.SerializeObject(game, _settings);
            var jObj = JObject.Parse(json);

            Assert.That(jObj.ContainsKey("engine"), Is.True);
            Assert.That(jObj.ContainsKey("engineVersion"), Is.True);
            Assert.That(jObj.ContainsKey("appVersion"), Is.True);
            Assert.That(jObj.ContainsKey("platform"), Is.True);
            Assert.That(jObj.ContainsKey("scene"), Is.True);
            Assert.That(jObj.ContainsKey("store"), Is.True);
        }

        [Test]
        public void GameContext_WithNullOptionalFields_OmitsNulls()
        {
            var game = new GameContext
            {
                Engine = "Unity",
                EngineVersion = "2021.3.0f1"
            };
            var json = JsonConvert.SerializeObject(game, _settings);
            var jObj = JObject.Parse(json);

            Assert.That(jObj.ContainsKey("engine"), Is.True);
            Assert.That(jObj.ContainsKey("scene"), Is.False);
            Assert.That(jObj.ContainsKey("store"), Is.False);
        }

        // --- EventContext Integration ---

        [Test]
        public void EventContext_Serialize_ContainsLibraryDeviceGame()
        {
            var ctx = EventTestFactory.CreateFullContext();
            var json = JsonConvert.SerializeObject(ctx, _settings);
            var jObj = JObject.Parse(json);

            Assert.That(jObj.ContainsKey("library"), Is.True);
            Assert.That(jObj.ContainsKey("device"), Is.True);
            Assert.That(jObj.ContainsKey("game"), Is.True);
        }

        [Test]
        public void EventContext_Deserialize_ReconstructsNestedContexts()
        {
            var original = EventTestFactory.CreateFullContext();
            var json = JsonConvert.SerializeObject(original, _settings);
            var restored = JsonConvert.DeserializeObject<EventContext>(json);

            Assert.That(restored.Library, Is.Not.Null);
            Assert.That(restored.Library.Name,
                Is.EqualTo(original.Library.Name));
            Assert.That(restored.Device, Is.Not.Null);
            Assert.That(restored.Device.Model,
                Is.EqualTo(original.Device.Model));
            Assert.That(restored.Game, Is.Not.Null);
            Assert.That(restored.Game.Engine,
                Is.EqualTo(original.Game.Engine));
        }

        [Test]
        public void EventContext_WithNullDevice_OmitsDeviceField()
        {
            var ctx = EventTestFactory.CreateMinimalContext();
            var json = JsonConvert.SerializeObject(ctx, _settings);
            var jObj = JObject.Parse(json);

            Assert.That(jObj.ContainsKey("library"), Is.True);
            Assert.That(jObj.ContainsKey("device"), Is.False);
        }

        [Test]
        public void EventContext_WithNullGame_OmitsGameField()
        {
            var ctx = EventTestFactory.CreateMinimalContext();
            var json = JsonConvert.SerializeObject(ctx, _settings);
            var jObj = JObject.Parse(json);

            Assert.That(jObj.ContainsKey("game"), Is.False);
        }
    }
}
