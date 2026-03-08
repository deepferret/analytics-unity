using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace DataFerret.Analytics.Editor
{
    /// <summary>
    /// Build pre-processor that validates DataFerret SDK configuration before builds.
    /// Checks Write Key validity and disables Debug mode for release builds.
    /// </summary>
    public class BuildProcessor : IPreprocessBuildWithReport
    {
        /// <summary>
        /// Callback order. Runs early in the build pipeline.
        /// </summary>
        public int callbackOrder => 0;

        /// <summary>
        /// Called before a build starts. Validates Write Key and enforces release build settings.
        /// </summary>
        /// <param name="report">The build report containing build options and platform info.</param>
        public void OnPreprocessBuild(BuildReport report)
        {
            var settings = DataFerretSettings.GetOrCreate();

            // Write Key not set - warn but allow build
            if (settings == null || string.IsNullOrEmpty(settings.WriteKey))
            {
                if (!EditorUtility.DisplayDialog(
                    "DataFerret Analytics",
                    "Write Key is not configured. Analytics events will not be sent.\n\n" +
                    "Set it in Window > DataFerret Analytics.",
                    "Continue Build",
                    "Cancel"))
                {
                    throw new BuildFailedException("[DataFerret] Build cancelled: Write Key not configured.");
                }
                return;
            }

            // Write Key format validation - fail build
            if (!settings.WriteKey.StartsWith("wk_"))
            {
                throw new BuildFailedException(
                    "[DataFerret] Invalid Write Key format. Must start with 'wk_' prefix.\n" +
                    "Update it in Window > DataFerret Analytics.");
            }

            // Release build: force Debug = false
            bool isDevelopmentBuild = (report.summary.options & BuildOptions.Development) != 0;
            if (!isDevelopmentBuild && settings.Debug)
            {
                Debug.LogWarning("[DataFerret] Debug mode automatically disabled for release build.");
                settings.Debug = false;
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }
        }
    }
}
