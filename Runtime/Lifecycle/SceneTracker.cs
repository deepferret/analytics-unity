using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace DataFerret.Analytics
{
    /// <summary>
    /// Automatically tracks scene changes as screen events.
    /// Subscribes to <see cref="SceneManager.sceneLoaded"/> and fires a screen
    /// event each time a new scene finishes loading.
    /// </summary>
    public class SceneTracker
    {
        private string _previousSceneName = "";
        private readonly Action<string, Dictionary<string, object>> _trackScreen;
        private bool _enabled;

        /// <summary>
        /// Creates a new SceneTracker.
        /// </summary>
        /// <param name="trackScreen">
        /// Action invoked to track screen events. Receives the screen name and
        /// a properties dictionary containing <c>previous_scene</c> and <c>load_mode</c>.
        /// </param>
        /// <param name="enabled">Whether scene tracking is enabled.</param>
        public SceneTracker(Action<string, Dictionary<string, object>> trackScreen, bool enabled = true)
        {
            _trackScreen = trackScreen;
            _enabled = enabled;
        }

        /// <summary>
        /// Starts listening for scene load events via <see cref="SceneManager.sceneLoaded"/>.
        /// </summary>
        public void Initialize()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        /// <summary>
        /// Stops listening for scene load events and unsubscribes from
        /// <see cref="SceneManager.sceneLoaded"/>.
        /// </summary>
        public void Dispose()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        /// <summary>
        /// Whether scene tracking is enabled. When disabled, scene load events
        /// are silently ignored.
        /// </summary>
        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        /// <summary>
        /// Handles a scene loaded event. Invokes the trackScreen delegate with
        /// the scene name and properties including <c>previous_scene</c> and <c>load_mode</c>.
        /// </summary>
        /// <param name="scene">The scene that was loaded.</param>
        /// <param name="mode">The mode in which the scene was loaded.</param>
        internal void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!_enabled) return;

            _trackScreen?.Invoke(scene.name, new Dictionary<string, object>
            {
                { "previous_scene", _previousSceneName },
                { "load_mode", mode.ToString() }
            });
            _previousSceneName = scene.name;
        }
    }
}
