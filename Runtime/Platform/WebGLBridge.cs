using System.Runtime.InteropServices;

namespace DataFerret.Analytics
{
    /// <summary>
    /// C# wrapper for the WebGL JavaScript bridge (DataFerretBridge.jslib).
    /// Provides access to browser-native <c>fetch()</c> and <c>navigator.sendBeacon()</c>
    /// APIs for sending analytics data from WebGL builds.
    /// </summary>
    /// <remarks>
    /// On non-WebGL platforms, stub methods log a warning instead of calling
    /// into JavaScript, allowing the code to compile and run without errors.
    /// </remarks>
    public static class WebGLBridge
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        /// <summary>
        /// Sends a batch of events to the Collector API using the browser <c>fetch()</c> API.
        /// On completion, invokes <c>SendMessage(callbackObj, callbackMethod, "1"|"0")</c>
        /// to signal success or failure.
        /// </summary>
        /// <param name="url">The full Collector API endpoint URL.</param>
        /// <param name="body">JSON-serialized request body.</param>
        /// <param name="auth">Authorization header value (e.g. "Bearer wk_...").</param>
        /// <param name="callbackObj">Name of the GameObject to receive the SendMessage callback.</param>
        /// <param name="callbackMethod">Name of the method on the callback GameObject to invoke.</param>
        [DllImport("__Internal")]
        public static extern void DataFerret_SendBatch(string url, string body, string auth, string callbackObj, string callbackMethod);

        /// <summary>
        /// Sends events using <c>navigator.sendBeacon()</c> for fire-and-forget delivery
        /// during page unload or visibility change.
        /// </summary>
        /// <param name="url">The full Collector API endpoint URL.</param>
        /// <param name="body">JSON-serialized request body.</param>
        [DllImport("__Internal")]
        public static extern void DataFerret_SendBeacon(string url, string body);
#else
        /// <summary>
        /// Stub for non-WebGL platforms. Logs a warning and returns immediately.
        /// </summary>
        /// <param name="url">The full Collector API endpoint URL.</param>
        /// <param name="body">JSON-serialized request body.</param>
        /// <param name="auth">Authorization header value.</param>
        /// <param name="callbackObj">Name of the GameObject to receive the SendMessage callback.</param>
        /// <param name="callbackMethod">Name of the method on the callback GameObject to invoke.</param>
        public static void DataFerret_SendBatch(string url, string body, string auth, string callbackObj, string callbackMethod)
        {
            UnityEngine.Debug.LogWarning("[DataFerret] WebGLBridge.DataFerret_SendBatch called on non-WebGL platform.");
        }

        /// <summary>
        /// Stub for non-WebGL platforms. Logs a warning and returns immediately.
        /// </summary>
        /// <param name="url">The full Collector API endpoint URL.</param>
        /// <param name="body">JSON-serialized request body.</param>
        public static void DataFerret_SendBeacon(string url, string body)
        {
            UnityEngine.Debug.LogWarning("[DataFerret] WebGLBridge.DataFerret_SendBeacon called on non-WebGL platform.");
        }
#endif
    }
}
