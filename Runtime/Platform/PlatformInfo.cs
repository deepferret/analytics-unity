using UnityEngine;
using UnityEngine.SceneManagement;

namespace DataFerret.Analytics
{
    /// <summary>
    /// Provides device, OS, platform, and store information by wrapping Unity SystemInfo.
    /// </summary>
    public static class PlatformInfo
    {
        /// <summary>Device type (Desktop, Handheld, Console, Unknown).</summary>
        public static string DeviceType => SystemInfo.deviceType.ToString();

        /// <summary>Device model name.</summary>
        public static string DeviceModel => SystemInfo.deviceModel;

        /// <summary>GPU name.</summary>
        public static string GPU => SystemInfo.graphicsDeviceName;

        /// <summary>Operating system name and version.</summary>
        public static string OS => SystemInfo.operatingSystem;

        /// <summary>Unity engine version.</summary>
        public static string UnityVersion => Application.unityVersion;

        /// <summary>Runtime platform string.</summary>
        public static string Platform => Application.platform.ToString();

        /// <summary>Detected store based on platform.</summary>
        public static string Store => DetectStore();

        /// <summary>Current active scene name.</summary>
        public static string CurrentScene => SceneManager.GetActiveScene().name;

        /// <summary>
        /// Detects the distribution store based on the current runtime platform.
        /// </summary>
        /// <returns>A string identifier for the detected store.</returns>
        private static string DetectStore()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.Android: return "google_play";
                case RuntimePlatform.IPhonePlayer: return "app_store";
                case RuntimePlatform.WebGLPlayer: return "web";
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.LinuxPlayer: return "steam";
                default: return "unknown";
            }
        }
    }
}
