using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.SceneManagement;

namespace DataFerret.Analytics.Tests.EditMode
{
    [TestFixture]
    [Category("Lifecycle")]
    public class SceneTrackerTests
    {
        private List<(string screenName, Dictionary<string, object> properties)> _trackedScreens;
        private SceneTracker _tracker;

        [SetUp]
        public void SetUp()
        {
            _trackedScreens = new List<(string, Dictionary<string, object>)>();
            _tracker = new SceneTracker(
                (screenName, props) => _trackedScreens.Add((screenName, props)),
                enabled: true
            );
        }

        /// <summary>
        /// TC-SCT-001: OnSceneLoaded invokes trackScreen with the scene name.
        /// </summary>
        [Test]
        public void OnSceneLoaded_InvokesTrackScreen_WithSceneName()
        {
            var scene = SceneManager.GetActiveScene();

            _tracker.OnSceneLoaded(scene, LoadSceneMode.Single);

            Assert.That(_trackedScreens.Count, Is.EqualTo(1));
            Assert.That(_trackedScreens[0].screenName, Is.EqualTo(scene.name));
            Assert.That(_trackedScreens[0].properties, Contains.Key("load_mode"));
            Assert.That(_trackedScreens[0].properties["load_mode"], Is.EqualTo("Single"));
        }

        /// <summary>
        /// TC-SCT-002: OnSceneLoaded includes previous_scene in properties.
        /// Calling twice should set previous_scene to the first scene's name on the second call.
        /// </summary>
        [Test]
        public void OnSceneLoaded_CalledTwice_SecondCallHasPreviousScene()
        {
            var scene = SceneManager.GetActiveScene();

            // First call: previous_scene should be empty string (initial state)
            _tracker.OnSceneLoaded(scene, LoadSceneMode.Single);

            Assert.That(_trackedScreens[0].properties["previous_scene"], Is.EqualTo(""));

            // Second call: previous_scene should be the scene name from the first call
            _tracker.OnSceneLoaded(scene, LoadSceneMode.Single);

            Assert.That(_trackedScreens.Count, Is.EqualTo(2));
            Assert.That(_trackedScreens[1].properties["previous_scene"], Is.EqualTo(scene.name));
        }

        /// <summary>
        /// TC-SCT-003: When Enabled=false, OnSceneLoaded does NOT invoke trackScreen.
        /// </summary>
        [Test]
        public void OnSceneLoaded_WhenDisabled_DoesNotInvokeTrackScreen()
        {
            _tracker.Enabled = false;
            var scene = SceneManager.GetActiveScene();

            _tracker.OnSceneLoaded(scene, LoadSceneMode.Single);

            Assert.That(_trackedScreens.Count, Is.EqualTo(0));
        }
    }
}
