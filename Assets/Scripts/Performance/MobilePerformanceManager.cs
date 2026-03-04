using UnityEngine;

/// <summary>
/// Mobile Performance Manager - Optimizes game for mobile devices
/// Singleton - Add to first scene
/// </summary>
public class MobilePerformanceManager : MonoBehaviour
{
    public static MobilePerformanceManager Instance { get; private set; }

    [Header("Frame Rate")]
    [Tooltip("Target frame rate")]
    public int targetFrameRate = 60;

    [Tooltip("Enable VSync (may limit FPS)")]
    public bool enableVSync = false;

    [Header("Quality Settings")]
    [Tooltip("Auto-adjust quality based on device")]
    public bool autoQuality = true;

    [Tooltip("Quality level (0-5, 0=worst, 5=best)")]
    [Range(0, 5)]
    public int qualityLevel = 2;

    [Header("Device Thresholds")]
    [Tooltip("RAM threshold for low-end (MB)")]
    public int lowEndRAMThreshold = 2000;

    [Tooltip("RAM threshold for mid-end (MB)")]
    public int midEndRAMThreshold = 4000;

    [Header("Screen Settings")]
    [Tooltip("Prevent screen sleep")]
    public bool preventScreenSleep = true;

    [Tooltip("Screen brightness (0-1, -1 = no change)")]
    [Range(-1f, 1f)]
    public float screenBrightness = -1f;

    [Header("Battery Optimization")]
    [Tooltip("Reduce quality when battery is low")]
    public bool optimizeForBattery = true;

    [Tooltip("Battery level to trigger optimization (0-1)")]
    [Range(0f, 0.5f)]
    public float lowBatteryThreshold = 0.2f;

    [Header("Debug")]
    public bool showDebugInfo = true;
    public bool showFPS = false;

    private float fpsUpdateInterval = 0.5f;
    private float fpsTimer = 0f;
    private int frameCount = 0;
    private float currentFPS = 0f;

    // Device tier
    public enum DeviceTier { LowEnd, MidEnd, HighEnd }
    public DeviceTier currentDeviceTier { get; private set; }

    void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Initialize()
    {
        // Detect device tier
        DetectDeviceTier();

        // Apply settings
        ApplyFrameRateSettings();
        ApplyQualitySettings();
        ApplyScreenSettings();

        if (showDebugInfo)
        {
            LogDeviceInfo();
        }
    }

    void DetectDeviceTier()
    {
        int systemRAM = SystemInfo.systemMemorySize;

        if (systemRAM < lowEndRAMThreshold)
        {
            currentDeviceTier = DeviceTier.LowEnd;
        }
        else if (systemRAM < midEndRAMThreshold)
        {
            currentDeviceTier = DeviceTier.MidEnd;
        }
        else
        {
            currentDeviceTier = DeviceTier.HighEnd;
        }

        if (showDebugInfo)
        {
            Debug.Log("[Performance] Device Tier: " + currentDeviceTier);
            Debug.Log("[Performance] System RAM: " + systemRAM + " MB");
        }
    }

    void ApplyFrameRateSettings()
    {
        // Target frame rate
        Application.targetFrameRate = targetFrameRate;

        // VSync
        QualitySettings.vSyncCount = enableVSync ? 1 : 0;

        if (showDebugInfo)
        {
            Debug.Log("[Performance] Target FPS: " + targetFrameRate);
            Debug.Log("[Performance] VSync: " + (enableVSync ? "ON" : "OFF"));
        }
    }

    void ApplyQualitySettings()
    {
        int targetQuality = qualityLevel;

        // Auto quality based on device
        if (autoQuality)
        {
            switch (currentDeviceTier)
            {
                case DeviceTier.LowEnd:
                    targetQuality = 1; // Low
                    break;
                case DeviceTier.MidEnd:
                    targetQuality = 2; // Medium
                    break;
                case DeviceTier.HighEnd:
                    targetQuality = 3; // High
                    break;
            }
        }

        QualitySettings.SetQualityLevel(targetQuality, true);

        // Additional optimizations for low-end
        if (currentDeviceTier == DeviceTier.LowEnd)
        {
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.shadowResolution = ShadowResolution.Low;
            QualitySettings.shadowDistance = 10f;
            QualitySettings.realtimeReflectionProbes = false;
        }

        if (showDebugInfo)
        {
            Debug.Log("[Performance] Quality Level: " + targetQuality);
        }
    }

    void ApplyScreenSettings()
    {
        // Prevent screen sleep
        if (preventScreenSleep)
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        // Screen brightness (if supported)
        if (screenBrightness >= 0f)
        {
#if UNITY_ANDROID
            try
            {
                AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaObject window = currentActivity.Call<AndroidJavaObject>("getWindow");
                AndroidJavaObject layoutParams = window.Call<AndroidJavaObject>("getAttributes");
                
                layoutParams.Set("screenBrightness", screenBrightness);
                window.Call("setAttributes", layoutParams);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Performance] Could not set brightness: " + e.Message);
            }
#endif
        }
    }

    void Update()
    {
        // FPS counter
        if (showFPS)
        {
            UpdateFPS();
        }

        // Battery optimization
        if (optimizeForBattery)
        {
            CheckBattery();
        }
    }

    void UpdateFPS()
    {
        frameCount++;
        fpsTimer += Time.unscaledDeltaTime;

        if (fpsTimer >= fpsUpdateInterval)
        {
            currentFPS = frameCount / fpsTimer;
            frameCount = 0;
            fpsTimer = 0f;
        }
    }

    void CheckBattery()
    {
        float batteryLevel = SystemInfo.batteryLevel;

        // If battery is low and not already optimized
        if (batteryLevel > 0 && batteryLevel < lowBatteryThreshold)
        {
            if (qualityLevel > 1)
            {
                // Reduce quality
                QualitySettings.SetQualityLevel(1, true);

                // Reduce frame rate
                Application.targetFrameRate = 30;

                if (showDebugInfo)
                {
                    Debug.Log("[Performance] Low battery detected - optimizing");
                }
            }
        }
    }

    void LogDeviceInfo()
    {
        Debug.Log("=== DEVICE INFO ===");
        Debug.Log("Device Model: " + SystemInfo.deviceModel);
        Debug.Log("Device Type: " + SystemInfo.deviceType);
        Debug.Log("OS: " + SystemInfo.operatingSystem);
        Debug.Log("Processor: " + SystemInfo.processorType);
        Debug.Log("Processor Count: " + SystemInfo.processorCount);
        Debug.Log("Graphics Device: " + SystemInfo.graphicsDeviceName);
        Debug.Log("Graphics Memory: " + SystemInfo.graphicsMemorySize + " MB");
        Debug.Log("System Memory: " + SystemInfo.systemMemorySize + " MB");
        Debug.Log("Screen Size: " + Screen.width + "x" + Screen.height);
        Debug.Log("Screen DPI: " + Screen.dpi);
        Debug.Log("===================");
    }

    void OnGUI()
    {
        if (showFPS)
        {
            // Display FPS counter
            int w = Screen.width;
            int h = Screen.height;

            GUIStyle style = new GUIStyle();
            style.alignment = TextAnchor.UpperLeft;
            style.fontSize = h / 40;
            style.normal.textColor = currentFPS >= 50 ? Color.green :
                                     currentFPS >= 30 ? Color.yellow : Color.red;

            Rect rect = new Rect(10, 10, w, h / 40);
            string text = string.Format("FPS: {0:0.}", currentFPS);
            GUI.Label(rect, text, style);
        }
    }

    // Public methods
    public void SetTargetFrameRate(int fps)
    {
        targetFrameRate = fps;
        Application.targetFrameRate = fps;
    }

    public void SetQualityLevel(int level)
    {
        qualityLevel = Mathf.Clamp(level, 0, 5);
        QualitySettings.SetQualityLevel(qualityLevel, true);
    }

    public float GetCurrentFPS()
    {
        return currentFPS;
    }
}