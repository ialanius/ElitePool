using UnityEngine;

/// <summary>
/// Haptic Feedback Manager - Handles vibration for mobile devices
/// Singleton pattern with settings support
/// </summary>
public class HapticManager : MonoBehaviour
{
    public static HapticManager Instance { get; private set; }

    [Header("Settings")]
    [Tooltip("Enable/disable haptics globally")]
    public bool hapticsEnabled = true;

    [Header("Intensity Settings")]
    [Tooltip("Light haptic duration (ms)")]
    [Range(10, 100)]
    public int lightDuration = 25;

    [Tooltip("Medium haptic duration (ms)")]
    [Range(25, 150)]
    public int mediumDuration = 50;

    [Tooltip("Heavy haptic duration (ms)")]
    [Range(50, 200)]
    public int heavyDuration = 100;

    [Header("Debug")]
    public bool showDebugLogs = false;

    // Settings key for PlayerPrefs
    private const string HAPTICS_ENABLED_KEY = "HapticsEnabled";

    void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Load saved settings
            LoadSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void LoadSettings()
    {
        // Load from PlayerPrefs
        hapticsEnabled = PlayerPrefs.GetInt(HAPTICS_ENABLED_KEY, 1) == 1;

        if (showDebugLogs)
            Debug.Log("[Haptic] Loaded settings - Enabled: " + hapticsEnabled);
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetInt(HAPTICS_ENABLED_KEY, hapticsEnabled ? 1 : 0);
        PlayerPrefs.Save();

        if (showDebugLogs)
            Debug.Log("[Haptic] Saved settings - Enabled: " + hapticsEnabled);
    }

    /// <summary>
    /// Toggle haptics on/off
    /// </summary>
    public void ToggleHaptics()
    {
        hapticsEnabled = !hapticsEnabled;
        SaveSettings();
    }

    /// <summary>
    /// Set haptics enabled state
    /// </summary>
    public void SetHapticsEnabled(bool enabled)
    {
        hapticsEnabled = enabled;
        SaveSettings();
    }

    // ================================================================
    // HAPTIC FEEDBACK METHODS
    // ================================================================

    /// <summary>
    /// Light haptic feedback - for subtle interactions
    /// Use for: UI button presses, slider movement, light touches
    /// </summary>
    public void Light()
    {
        if (!hapticsEnabled) return;

        if (showDebugLogs)
            Debug.Log("[Haptic] Light feedback");

#if UNITY_IOS
        TriggerIOSHaptic(0); // Light impact
#elif UNITY_ANDROID
        AndroidVibrate(lightDuration);
#else
        // Fallback for other platforms
        Handheld.Vibrate();
#endif
    }

    /// <summary>
    /// Medium haptic feedback - for standard interactions
    /// Use for: Ball pocketed, collisions, moderate events
    /// </summary>
    public void Medium()
    {
        if (!hapticsEnabled) return;

        if (showDebugLogs)
            Debug.Log("[Haptic] Medium feedback");

#if UNITY_IOS
        TriggerIOSHaptic(1); // Medium impact
#elif UNITY_ANDROID
        AndroidVibrate(mediumDuration);
#else
        Handheld.Vibrate();
#endif
    }

    /// <summary>
    /// Heavy haptic feedback - for strong interactions
    /// Use for: Cue ball shot, win/lose, important events
    /// </summary>
    public void Heavy()
    {
        if (!hapticsEnabled) return;

        if (showDebugLogs)
            Debug.Log("[Haptic] Heavy feedback");

#if UNITY_IOS
        TriggerIOSHaptic(2); // Heavy impact
#elif UNITY_ANDROID
        AndroidVibrate(heavyDuration);
#else
        Handheld.Vibrate();
#endif
    }

    /// <summary>
    /// Success haptic feedback - for positive events
    /// Use for: Win, achievement unlock, correct action
    /// </summary>
    public void Success()
    {
        if (!hapticsEnabled) return;

        if (showDebugLogs)
            Debug.Log("[Haptic] Success feedback");

#if UNITY_IOS
        TriggerIOSHaptic(3); // Success notification
#elif UNITY_ANDROID
        // Double pulse for success feeling
        AndroidVibrate(40);
        StartCoroutine(DelayedVibrate(100, 40));
#else
        Handheld.Vibrate();
#endif
    }

    /// <summary>
    /// Error haptic feedback - for negative events
    /// Use for: Foul, error, wrong action
    /// </summary>
    public void Error()
    {
        if (!hapticsEnabled) return;

        if (showDebugLogs)
            Debug.Log("[Haptic] Error feedback");

#if UNITY_IOS
        TriggerIOSHaptic(4); // Error notification
#elif UNITY_ANDROID
        // Triple short pulse for error feeling
        AndroidVibrate(30);
        StartCoroutine(DelayedVibrate(60, 30));
        StartCoroutine(DelayedVibrate(120, 30));
#else
        Handheld.Vibrate();
#endif
    }

    /// <summary>
    /// Selection haptic feedback - for selections/highlights
    /// Use for: Menu navigation, option selection
    /// </summary>
    public void Selection()
    {
        if (!hapticsEnabled) return;

        if (showDebugLogs)
            Debug.Log("[Haptic] Selection feedback");

#if UNITY_IOS
        TriggerIOSHaptic(5); // Selection
#elif UNITY_ANDROID
        AndroidVibrate(15);
#else
        Handheld.Vibrate();
#endif
    }

    /// <summary>
    /// Custom duration haptic
    /// </summary>
    public void Custom(int milliseconds)
    {
        if (!hapticsEnabled) return;

        if (showDebugLogs)
            Debug.Log("[Haptic] Custom feedback: " + milliseconds + "ms");

#if UNITY_ANDROID
        AndroidVibrate(milliseconds);
#else
        Handheld.Vibrate();
#endif
    }

    // ================================================================
    // PLATFORM-SPECIFIC IMPLEMENTATIONS
    // ================================================================

#if UNITY_IOS
    private void TriggerIOSHaptic(int type)
    {
        // iOS uses Handheld.Vibrate() but we can add more sophisticated
        // haptics using native plugins if needed
        // Types: 0=Light, 1=Medium, 2=Heavy, 3=Success, 4=Error, 5=Selection
        
        switch (type)
        {
            case 0: // Light
            case 5: // Selection
                Handheld.Vibrate(); // Short vibration
                break;
            case 1: // Medium
            case 3: // Success
                Handheld.Vibrate();
                break;
            case 2: // Heavy
            case 4: // Error
                Handheld.Vibrate();
                break;
        }
    }
#endif

#if UNITY_ANDROID
    private void AndroidVibrate(long milliseconds)
    {
        try
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            
            // Check if device has vibrator
            bool hasVibrator = vibrator.Call<bool>("hasVibrator");
            if (hasVibrator)
            {
                vibrator.Call("vibrate", milliseconds);
            }
        }
        catch (System.Exception e)
        {
            if (showDebugLogs)
                Debug.LogWarning("[Haptic] Android vibration failed: " + e.Message);
        }
    }

    private System.Collections.IEnumerator DelayedVibrate(int delayMs, int durationMs)
    {
        yield return new WaitForSeconds(delayMs / 1000f);
        AndroidVibrate(durationMs);
    }
#endif
}

// ================================================================
// EXTENSION CLASS - Easy access from any script
// ================================================================
public static class Haptics
{
    public static void Light() => HapticManager.Instance?.Light();
    public static void Medium() => HapticManager.Instance?.Medium();
    public static void Heavy() => HapticManager.Instance?.Heavy();
    public static void Success() => HapticManager.Instance?.Success();
    public static void Error() => HapticManager.Instance?.Error();
    public static void Selection() => HapticManager.Instance?.Selection();
    public static void Custom(int ms) => HapticManager.Instance?.Custom(ms);
}