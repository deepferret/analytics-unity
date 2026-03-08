using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DataFerret.Analytics.Tests.PlayMode
{
    /// <summary>
    /// PlayMode tests for <see cref="SceneTracker"/>.
    /// Validates screen event emission on scene load, previous_scene tracking,
    /// and opt-out via AutoCapture.SceneChanges.
    /// </summary>
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

        [TearDown]
        public void TearDown()
        {
            _tracker.Dispose();
        }

        /// <summary>
        /// TC-SCT-001: When a scene is loaded, a screen event is emitted with the scene name.
        /// Uses SceneManager.sceneLoaded event via the tracker's Initialize().
        /// </summary>
        [UnityTest]
        public IEnumerator SceneLoaded_EmitsScreenEvent_WithSceneName()
        {
            _tracker.Initialize();

            yield return null;

            // Trigger OnSceneLoaded by reloading the current active scene
            var activeScene = SceneManager.GetActiveScene();
            _tracker.OnSceneLoaded(activeScene, LoadSceneMode.Single);

            Assert.That(_trackedScreens.Count, Is.GreaterThanOrEqualTo(1),
                "At least one screen event should be emitted on scene load.");

            var lastScreen = _trackedScreens[_trackedScreens.Count - 1];
            Assert.That(lastScreen.screenName, Is.EqualTo(activeScene.name),
                "Screen event should carry the loaded scene's name.");
            Assert.That(lastScreen.properties, Contains.Key("load_mode"),
                "Screen event should include load_mode property.");
        }

        /// <summary>
        /// TC-SCT-002: The screen event includes a previous_scene property set to the
        /// previously loaded scene name.
        /// </summary>
        [UnityTest]
        public IEnumerator SceneLoaded_ScreenEvent_IncludesPreviousScene()
        {
            _tracker.Initialize();
            var activeScene = SceneManager.GetActiveScene();

            yield return null;

            // First scene load: previous_scene should be empty (initial state)
            _tracker.OnSceneLoaded(activeScene, LoadSceneMode.Single);

            Assert.That(_trackedScreens.Count, Is.GreaterThanOrEqualTo(1));
            var firstEvent = _trackedScreens[_trackedScreens.Count - 1];
            Assert.That(firstEvent.properties, Contains.Key("previous_scene"),
                "Screen event should include previous_scene property.");
            Assert.That(firstEvent.properties["previous_scene"], Is.EqualTo(""),
                "First scene load should have empty previous_scene.");

            // Second scene load: previous_scene should be the first scene's name
            _tracker.OnSceneLoaded(activeScene, LoadSceneMode.Single);

            var secondEvent = _trackedScreens[_trackedScreens.Count - 1];
            Assert.That(secondEvent.properties["previous_scene"], Is.EqualTo(activeScene.name),
                "Second scene load should have previous_scene equal to the first scene name.");
        }

        /// <summary>
        /// TC-SCT-003: When AutoCapture.SceneChanges is false (Enabled = false),
        /// scene loads do NOT emit screen events.
        /// </summary>
        [UnityTest]
        public IEnumerator SceneChangesDisabled_DoesNotEmitScreenEvent()
        {
            _tracker.Enabled = false;
            _tracker.Initialize();

            yield return null;

            var activeScene = SceneManager.GetActiveScene();
            _tracker.OnSceneLoaded(activeScene, LoadSceneMode.Single);

            Assert.That(_trackedScreens.Count, Is.EqualTo(0),
                "No screen events should be emitted when SceneChanges is disabled.");
        }
    }
}
