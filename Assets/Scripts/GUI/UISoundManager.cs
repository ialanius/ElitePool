using UnityEngine;

/// <summary>
/// UI Sound Manager - Centralized audio for UI
/// Singleton - plays UI sounds from anywhere
/// </summary>
public class UISoundManager : MonoBehaviour
{
    public static UISoundManager Instance { get; private set; }

    [Header("Button Sounds")]
    public AudioClip buttonClick;
    public AudioClip buttonHover;
    public AudioClip buttonDisabled;

    [Header("Panel Sounds")]
    public AudioClip panelOpen;
    public AudioClip panelClose;
    public AudioClip panelSlide;

    [Header("Notification Sounds")]
    public AudioClip success;
    public AudioClip error;
    public AudioClip warning;
    public AudioClip info;

    [Header("Toggle Sounds")]
    public AudioClip toggleOn;
    public AudioClip toggleOff;

    [Header("Slider Sounds")]
    public AudioClip sliderMove;
    public AudioClip sliderRelease;

    [Header("Other UI Sounds")]
    public AudioClip tabSwitch;
    public AudioClip dropdownOpen;
    public AudioClip dropdownClose;
    public AudioClip checkboxCheck;
    public AudioClip checkboxUncheck;

    [Header("Settings")]
    [Range(0f, 1f)]
    public float masterVolume = 1f;

    [Range(0f, 1f)]
    public float buttonVolume = 0.8f;

    [Range(0f, 1f)]
    public float panelVolume = 0.6f;

    [Range(0f, 1f)]
    public float notificationVolume = 1f;

    [Tooltip("Limit rapid fire sounds")]
    public bool preventSpam = true;

    [Tooltip("Minimum time between same sound")]
    public float spamPreventionTime = 0.1f;

    [Header("Audio Source")]
    private AudioSource audioSource;

    private float lastPlayTime = 0f;
    private AudioClip lastPlayedClip = null;

    [Header("Debug")]
    public bool showDebugLogs = false;

    void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Get or create AudioSource
            audioSource = GetComponent<AudioSource>();
            if (!audioSource)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.playOnAwake = false;
            audioSource.loop = false;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ================================================================
    // BUTTON SOUNDS
    // ================================================================

    public void PlayButtonClick()
    {
        PlaySound(buttonClick, buttonVolume, "Button Click");
    }

    public void PlayButtonHover()
    {
        PlaySound(buttonHover, buttonVolume * 0.5f, "Button Hover");
    }

    public void PlayButtonDisabled()
    {
        PlaySound(buttonDisabled, buttonVolume, "Button Disabled");
    }

    // ================================================================
    // PANEL SOUNDS
    // ================================================================

    public void PlayPanelOpen()
    {
        PlaySound(panelOpen, panelVolume, "Panel Open");
    }

    public void PlayPanelClose()
    {
        PlaySound(panelClose, panelVolume, "Panel Close");
    }

    public void PlayPanelSlide()
    {
        PlaySound(panelSlide, panelVolume, "Panel Slide");
    }

    // ================================================================
    // NOTIFICATION SOUNDS
    // ================================================================

    public void PlaySuccess()
    {
        PlaySound(success, notificationVolume, "Success");
    }

    public void PlayError()
    {
        PlaySound(error, notificationVolume, "Error");
    }

    public void PlayWarning()
    {
        PlaySound(warning, notificationVolume, "Warning");
    }

    public void PlayInfo()
    {
        PlaySound(info, notificationVolume, "Info");
    }

    // ================================================================
    // TOGGLE SOUNDS
    // ================================================================

    public void PlayToggleOn()
    {
        PlaySound(toggleOn, buttonVolume, "Toggle On");
    }

    public void PlayToggleOff()
    {
        PlaySound(toggleOff, buttonVolume, "Toggle Off");
    }

    // ================================================================
    // SLIDER SOUNDS
    // ================================================================

    public void PlaySliderMove()
    {
        PlaySound(sliderMove, buttonVolume * 0.3f, "Slider Move");
    }

    public void PlaySliderRelease()
    {
        PlaySound(sliderRelease, buttonVolume, "Slider Release");
    }

    // ================================================================
    // OTHER SOUNDS
    // ================================================================

    public void PlayTabSwitch()
    {
        PlaySound(tabSwitch, buttonVolume, "Tab Switch");
    }

    public void PlayDropdownOpen()
    {
        PlaySound(dropdownOpen, buttonVolume, "Dropdown Open");
    }

    public void PlayDropdownClose()
    {
        PlaySound(dropdownClose, buttonVolume, "Dropdown Close");
    }

    public void PlayCheckboxCheck()
    {
        PlaySound(checkboxCheck, buttonVolume, "Checkbox Check");
    }

    public void PlayCheckboxUncheck()
    {
        PlaySound(checkboxUncheck, buttonVolume, "Checkbox Uncheck");
    }

    // ================================================================
    // CORE PLAY FUNCTION
    // ================================================================

    void PlaySound(AudioClip clip, float volumeMultiplier, string soundName)
    {
        if (!clip)
        {
            if (showDebugLogs)
                Debug.LogWarning("[UISound] No clip assigned for: " + soundName);
            return;
        }

        // Spam prevention
        if (preventSpam && lastPlayedClip == clip)
        {
            if (Time.unscaledTime - lastPlayTime < spamPreventionTime)
            {
                if (showDebugLogs)
                    Debug.Log("[UISound] Prevented spam: " + soundName);
                return;
            }
        }

        // Play sound
        float finalVolume = masterVolume * volumeMultiplier;
        audioSource.PlayOneShot(clip, finalVolume);

        // Track for spam prevention
        lastPlayedClip = clip;
        lastPlayTime = Time.unscaledTime;

        if (showDebugLogs)
            Debug.Log("[UISound] Played: " + soundName + " (Volume: " + finalVolume + ")");
    }

    // ================================================================
    // SETTINGS
    // ================================================================

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
    }

    public void SetButtonVolume(float volume)
    {
        buttonVolume = Mathf.Clamp01(volume);
    }

    public void SetPanelVolume(float volume)
    {
        panelVolume = Mathf.Clamp01(volume);
    }

    public void SetNotificationVolume(float volume)
    {
        notificationVolume = Mathf.Clamp01(volume);
    }
}

// ================================================================
// EXTENSION CLASS - Easy access
// ================================================================

public static class UISound
{
    // Buttons
    public static void Click() => UISoundManager.Instance?.PlayButtonClick();
    public static void Hover() => UISoundManager.Instance?.PlayButtonHover();
    public static void Disabled() => UISoundManager.Instance?.PlayButtonDisabled();

    // Panels
    public static void PanelOpen() => UISoundManager.Instance?.PlayPanelOpen();
    public static void PanelClose() => UISoundManager.Instance?.PlayPanelClose();

    // Notifications
    public static void Success() => UISoundManager.Instance?.PlaySuccess();
    public static void Error() => UISoundManager.Instance?.PlayError();
    public static void Warning() => UISoundManager.Instance?.PlayWarning();

    // Toggles
    public static void ToggleOn() => UISoundManager.Instance?.PlayToggleOn();
    public static void ToggleOff() => UISoundManager.Instance?.PlayToggleOff();
}