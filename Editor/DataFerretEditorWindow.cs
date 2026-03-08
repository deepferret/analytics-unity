using UnityEngine;
using UnityEditor;

namespace DataFerret.Analytics.Editor
{
    /// <summary>
    /// Unity Editor window for configuring the DataFerret Analytics SDK.
    /// Accessible via Window > DataFerret Analytics.
    /// Provides UI for Write Key configuration, auto-capture toggles,
    /// advanced transport settings, and runtime actions.
    /// </summary>
    public class DataFerretEditorWindow : EditorWindow
    {
        private DataFerretSettings _settings;
        private SerializedObject _serializedSettings;
        private Vector2 _scrollPosition;
        private bool _writeKeyValid;
        private string _connectionStatus = "";

        /// <summary>
        /// Opens the DataFerret Analytics settings window from the Unity menu.
        /// </summary>
        [MenuItem("Window/DataFerret Analytics")]
        public static void ShowWindow()
        {
            var window = GetWindow<DataFerretEditorWindow>("DataFerret Analytics");
            window.minSize = new Vector2(400, 500);
        }

        private void OnEnable()
        {
            _settings = DataFerretSettings.GetOrCreate();
            if (_settings != null)
            {
                _serializedSettings = new SerializedObject(_settings);
                ValidateWriteKey();
            }
        }

        private void OnGUI()
        {
            if (_settings == null)
            {
                EditorGUILayout.HelpBox("Failed to load DataFerret settings.", MessageType.Error);
                return;
            }

            _serializedSettings.Update();
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            // Header
            GUILayout.Label("DataFerret Analytics SDK v0.1.0", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            // Connection Section
            DrawConnectionSection();

            EditorGUILayout.Space(10);

            // Auto Capture Section
            DrawAutoCaptureSection();

            EditorGUILayout.Space(10);

            // Advanced Section
            DrawAdvancedSection();

            EditorGUILayout.Space(10);

            // Actions Section
            DrawActionsSection();

            EditorGUILayout.EndScrollView();

            if (_serializedSettings.hasModifiedProperties)
            {
                _serializedSettings.ApplyModifiedProperties();
                EditorUtility.SetDirty(_settings);
            }
        }

        private void DrawConnectionSection()
        {
            EditorGUILayout.LabelField("Connection", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            var writeKeyProp = _serializedSettings.FindProperty("WriteKey");
            EditorGUILayout.PropertyField(writeKeyProp, new GUIContent("Write Key"));
            if (EditorGUI.EndChangeCheck())
            {
                _serializedSettings.ApplyModifiedProperties();
                ValidateWriteKey();
            }

            if (GUILayout.Button("Verify", GUILayout.Width(60)))
            {
                ValidateWriteKey();
            }
            EditorGUILayout.EndHorizontal();

            // Write Key validation indicator
            if (string.IsNullOrEmpty(_settings.WriteKey))
            {
                EditorGUILayout.HelpBox("Write Key is required. Get it from the DataFerret dashboard.", MessageType.Warning);
            }
            else if (!_writeKeyValid)
            {
                EditorGUILayout.HelpBox("Write Key must start with 'wk_' prefix.", MessageType.Error);
            }
            else
            {
                EditorGUILayout.HelpBox("Write Key is valid.", MessageType.Info);
            }

            EditorGUILayout.PropertyField(
                _serializedSettings.FindProperty("ApiHost"),
                new GUIContent("API Host"));

            // HTTPS warning
            if (!string.IsNullOrEmpty(_settings.ApiHost) &&
                _settings.ApiHost.StartsWith("http://"))
            {
                EditorGUILayout.HelpBox(
                    "WARNING: Using HTTP instead of HTTPS. Data will not be encrypted in transit.",
                    MessageType.Warning);
            }
        }

        private void DrawAutoCaptureSection()
        {
            EditorGUILayout.LabelField("Auto Capture", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _serializedSettings.FindProperty("AutoCaptureSessions"),
                new GUIContent("Sessions"));
            EditorGUILayout.PropertyField(
                _serializedSettings.FindProperty("AutoCaptureSceneChanges"),
                new GUIContent("Scene Changes"));
            EditorGUILayout.PropertyField(
                _serializedSettings.FindProperty("AutoCaptureErrors"),
                new GUIContent("Errors"));
            EditorGUILayout.PropertyField(
                _serializedSettings.FindProperty("AutoCaptureLifecycle"),
                new GUIContent("Lifecycle"));
            EditorGUILayout.PropertyField(
                _serializedSettings.FindProperty("AutoCapturePerformanceSamples"),
                new GUIContent("Performance Samples (opt-in)"));
        }

        private void DrawAdvancedSection()
        {
            EditorGUILayout.LabelField("Advanced", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _serializedSettings.FindProperty("FlushSize"),
                new GUIContent("Flush Size"));
            EditorGUILayout.PropertyField(
                _serializedSettings.FindProperty("FlushIntervalSeconds"),
                new GUIContent("Flush Interval (seconds)"));
            EditorGUILayout.PropertyField(
                _serializedSettings.FindProperty("MaxQueueSize"),
                new GUIContent("Max Queue Size"));
            EditorGUILayout.PropertyField(
                _serializedSettings.FindProperty("MaxRetries"),
                new GUIContent("Max Retries"));
            EditorGUILayout.PropertyField(
                _serializedSettings.FindProperty("TimeoutSeconds"),
                new GUIContent("Timeout (seconds)"));
            EditorGUILayout.PropertyField(
                _serializedSettings.FindProperty("Debug"),
                new GUIContent("Debug Mode"));
        }

        private void DrawActionsSection()
        {
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Test Connection"))
            {
                TestConnection();
            }

            GUI.enabled = Application.isPlaying && DataFerretAnalytics.IsInitialized;
            if (GUILayout.Button("Flush Now"))
            {
                DataFerretAnalytics.Flush();
                _connectionStatus = "Flush triggered.";
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(_connectionStatus))
            {
                EditorGUILayout.HelpBox(_connectionStatus, MessageType.Info);
            }
        }

        private void ValidateWriteKey()
        {
            _writeKeyValid = !string.IsNullOrEmpty(_settings.WriteKey) &&
                             _settings.WriteKey.StartsWith("wk_");
        }

        private void TestConnection()
        {
            if (!_writeKeyValid)
            {
                _connectionStatus = "Cannot test: Write Key is invalid.";
                return;
            }
            _connectionStatus = "Connection test not available in Editor. Write Key format is valid.";
            Repaint();
        }
    }
}
