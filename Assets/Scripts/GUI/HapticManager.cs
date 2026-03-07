using UnityEngine;

/// <summary>
/// Haptic Feedback Manager - Handles vibration for mobile devices
/// </summary>
public class HapticManager : MonoBehaviour
{
    public static HapticManager Instance { get; private set; }

    [Header("Settings")]
    public bool hapticsEnabled = true;

    [Header("Intensity Settings")]
    [Range(10, 100)] public int lightDuration = 25;
    [Range(25, 150)] public int mediumDuration = 50;
    [Range(50, 200)] public int heavyDuration = 100;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private const string HAPTICS_ENABLED_KEY = "HapticsEnabled";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void LoadSettings()
    {
        hapticsEnabled = PlayerPrefs.GetInt(HAPTICS_ENABLED_KEY, 1) == 1;
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetInt(HAPTICS_ENABLED_KEY, hapticsEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void ToggleHaptics()
    {
        hapticsEnabled = !hapticsEnabled;
        SaveSettings();
    }

    public void SetHapticsEnabled(bool enabled)
    {
        hapticsEnabled = enabled;
        SaveSettings();
    }

    // ================================================================
    // HAPTIC FEEDBACK METHODS
    // ================================================================

    public void Light()
    {
        if (!hapticsEnabled) return;

#if UNITY_IOS
        TriggerIOSHaptic(0); 
#elif UNITY_ANDROID
        AndroidVibrate(lightDuration);
#else
        // تم تعطيل Handheld لتوافق اللعبة مع WebGL والكمبيوتر
#endif
    }

    public void Medium()
    {
        if (!hapticsEnabled) return;

#if UNITY_IOS
        TriggerIOSHaptic(1); 
#elif UNITY_ANDROID
        AndroidVibrate(mediumDuration);
#else
        // تم تعطيل Handheld 
#endif
    }

    public void Heavy()
    {
        if (!hapticsEnabled) return;

#if UNITY_IOS
        TriggerIOSHaptic(2); 
#elif UNITY_ANDROID
        AndroidVibrate(heavyDuration);
#else
        // تم تعطيل Handheld 
#endif
    }

    public void Success()
    {
        if (!hapticsEnabled) return;

#if UNITY_IOS
        TriggerIOSHaptic(3); 
#elif UNITY_ANDROID
        AndroidVibrate(40);
        StartCoroutine(DelayedVibrate(100, 40));
#else
        // تم تعطيل Handheld
#endif
    }

    public void Error()
    {
        if (!hapticsEnabled) return;

#if UNITY_IOS
        TriggerIOSHaptic(4); 
#elif UNITY_ANDROID
        AndroidVibrate(30);
        StartCoroutine(DelayedVibrate(60, 30));
        StartCoroutine(DelayedVibrate(120, 30));
#else
        // تم تعطيل Handheld
#endif
    }

    public void Selection()
    {
        if (!hapticsEnabled) return;

#if UNITY_IOS
        TriggerIOSHaptic(5); 
#elif UNITY_ANDROID
        AndroidVibrate(15);
#else
        // تم تعطيل Handheld 
#endif
    }

    public void Custom(int milliseconds)
    {
        if (!hapticsEnabled) return;

#if UNITY_ANDROID
        AndroidVibrate(milliseconds);
#else
        // تم تعطيل Handheld 
#endif
    }

    // ================================================================
    // PLATFORM-SPECIFIC IMPLEMENTATIONS
    // ================================================================

#if UNITY_IOS
    private void TriggerIOSHaptic(int type)
    {
        switch (type)
        {
            case 0: 
            case 5: 
                Handheld.Vibrate(); 
                break;
            case 1: 
            case 3: 
                Handheld.Vibrate();
                break;
            case 2: 
            case 4: 
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
            
            bool hasVibrator = vibrator.Call<bool>("hasVibrator");
            if (hasVibrator)
            {
                vibrator.Call("vibrate", milliseconds);
            }
        }
        catch (System.Exception e)
        {
            if (showDebugLogs) Debug.LogWarning("[Haptic] Android vibration failed: " + e.Message);
        }
    }

    private System.Collections.IEnumerator DelayedVibrate(int delayMs, int durationMs)
    {
        yield return new WaitForSeconds(delayMs / 1000f);
        AndroidVibrate(durationMs);
    }
#endif
}

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