using UnityEngine;
using DataFerret.Analytics;
using System.Collections.Generic;

/// <summary>
/// Example MonoBehaviour demonstrating DataFerret Analytics SDK usage.
/// Attach to a GameObject in your scene and configure the Write Key in the Inspector.
/// </summary>
public class SampleTracker : MonoBehaviour
{
    [Header("DataFerret Settings")]
    [SerializeField] private string writeKey = "wk_proj_YOUR_KEY_HERE";

    void Awake()
    {
        DataFerretAnalytics.Init(new DataFerretConfig
        {
            WriteKey = writeKey,
            Debug = true,
            AutoCapture = new DataFerretConfig.AutoCaptureConfig
            {
                Sessions = true,
                SceneChanges = true,
                Errors = true,
                Lifecycle = true,
            },
        });

        DataFerretAnalytics.SetGlobalProperties(new Dictionary<string, object>
        {
            { "app_version", Application.version },
        });

        Debug.Log("[Sample] DataFerret Analytics initialized");
    }

    /// <summary>
    /// Call this from a UI Button's OnClick event to track play button clicks.
    /// </summary>
    public void OnPlayButtonClicked()
    {
        DataFerretAnalytics.Track("play_button_clicked", new Dictionary<string, object>
        {
            { "source", "main_menu" },
        });
    }

    /// <summary>
    /// Track stage completion with score and time.
    /// </summary>
    public void OnStageClear(int stageId, int stars, float timeSpent)
    {
        DataFerretAnalytics.Track("stage_cleared", new Dictionary<string, object>
        {
            { "stage_id", stageId },
            { "stars", stars },
            { "time_spent", timeSpent },
        });
    }

    /// <summary>
    /// Identify the user after login.
    /// </summary>
    public void OnLoginSuccess(string userId, int playerLevel)
    {
        DataFerretAnalytics.Identify(userId, new Dictionary<string, object>
        {
            { "level", playerLevel },
        });
    }

    /// <summary>
    /// Reset identity on logout.
    /// </summary>
    public void OnLogout()
    {
        DataFerretAnalytics.Reset();
    }
}
