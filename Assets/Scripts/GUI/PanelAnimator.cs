using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Panel Animator - Smooth animations for UI panels
/// Supports multiple animation types
/// </summary>
public class PanelAnimator : MonoBehaviour
{
    [Header("Animation Type")]
    public AnimationType animationType = AnimationType.Scale;

    public enum AnimationType
    {
        Scale,          // Pop in/out
        Slide,          // Slide from direction
        Fade,           // Fade in/out
        ScaleAndFade,   // Combined
        SlideAndFade    // Combined
    }

    [Header("Animation Settings")]
    [Tooltip("Animation duration")]
    [Range(0.1f, 2f)]
    public float animationDuration = 0.3f;

    [Tooltip("Animation curve")]
    public AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("Delay before showing")]
    public float showDelay = 0f;

    [Tooltip("Delay before hiding")]
    public float hideDelay = 0f;

    [Header("Scale Settings")]
    [Tooltip("Start scale for show animation")]
    public Vector3 scaleFrom = Vector3.zero;

    [Tooltip("End scale for show animation")]
    public Vector3 scaleTo = Vector3.one;

    [Tooltip("Overshoot effect (bounce)")]
    public bool useOvershoot = false;

    [Tooltip("Overshoot amount")]
    [Range(0f, 0.5f)]
    public float overshootAmount = 0.1f;

    [Header("Slide Settings")]
    public SlideDirection slideDirection = SlideDirection.Bottom;

    public enum SlideDirection
    {
        Top,
        Bottom,
        Left,
        Right
    }

    [Tooltip("Slide distance (in pixels)")]
    public float slideDistance = 1000f;

    [Header("Fade Settings")]
    [Tooltip("Start alpha for show animation")]
    [Range(0f, 1f)]
    public float fadeFrom = 0f;

    [Tooltip("End alpha for show animation")]
    [Range(0f, 1f)]
    public float fadeTo = 1f;

    [Header("Audio")]
    public AudioClip showSound;
    public AudioClip hideSound;

    [Range(0f, 1f)]
    public float soundVolume = 1f;

    [Header("Events")]
    public UnityEngine.Events.UnityEvent onShowComplete;
    public UnityEngine.Events.UnityEvent onHideComplete;

    [Header("References")]
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private AudioSource audioSource;

    private Vector3 originalScale;
    private Vector2 originalPosition;
    private bool isAnimating = false;

    [Header("Debug")]
    public bool showDebugLogs = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        // Get or add CanvasGroup
        canvasGroup = GetComponent<CanvasGroup>();
        if (!canvasGroup && (animationType == AnimationType.Fade ||
                              animationType == AnimationType.ScaleAndFade ||
                              animationType == AnimationType.SlideAndFade))
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Get or add AudioSource
        audioSource = GetComponent<AudioSource>();
        if (!audioSource && (showSound || hideSound))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // Save original values
        originalScale = rectTransform.localScale;
        originalPosition = rectTransform.anchoredPosition;
    }

    /// <summary>
    /// Show panel with animation
    /// </summary>
    public void Show()
    {
        if (isAnimating) return;

        gameObject.SetActive(true);
        StartCoroutine(ShowAnimation());
    }

    /// <summary>
    /// Hide panel with animation
    /// </summary>
    public void Hide()
    {
        if (isAnimating) return;

        StartCoroutine(HideAnimation());
    }

    /// <summary>
    /// Toggle panel (show/hide)
    /// </summary>
    public void Toggle()
    {
        if (gameObject.activeSelf)
            Hide();
        else
            Show();
    }

    /// <summary>
    /// Show without animation
    /// </summary>
    public void ShowInstant()
    {
        gameObject.SetActive(true);
        ResetToEndState();
    }

    /// <summary>
    /// Hide without animation
    /// </summary>
    public void HideInstant()
    {
        gameObject.SetActive(false);
    }

    IEnumerator ShowAnimation()
    {
        isAnimating = true;

        // Delay
        if (showDelay > 0f)
            yield return new WaitForSecondsRealtime(showDelay);

        // Play sound
        PlaySound(showSound);

        // Set start state
        SetStartState();

        // Animate
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = animationCurve.Evaluate(elapsed / animationDuration);

            // Apply overshoot
            if (useOvershoot && t > 0.5f)
            {
                float overshoot = Mathf.Sin((t - 0.5f) * Mathf.PI * 2f) * overshootAmount;
                t += overshoot;
            }

            ApplyAnimation(t, true);

            yield return null;
        }

        // Set end state
        ResetToEndState();

        isAnimating = false;

        // Callback
        onShowComplete?.Invoke();

        if (showDebugLogs)
            Debug.Log("[PanelAnimator] Show complete: " + gameObject.name);
    }

    IEnumerator HideAnimation()
    {
        isAnimating = true;

        // Delay
        if (hideDelay > 0f)
            yield return new WaitForSecondsRealtime(hideDelay);

        // Play sound
        PlaySound(hideSound);

        // Animate
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = animationCurve.Evaluate(elapsed / animationDuration);

            ApplyAnimation(1f - t, true);

            yield return null;
        }

        // Deactivate
        gameObject.SetActive(false);

        isAnimating = false;

        // Callback
        onHideComplete?.Invoke();

        if (showDebugLogs)
            Debug.Log("[PanelAnimator] Hide complete: " + gameObject.name);
    }

    void SetStartState()
    {
        switch (animationType)
        {
            case AnimationType.Scale:
                rectTransform.localScale = scaleFrom;
                break;

            case AnimationType.Slide:
                rectTransform.anchoredPosition = GetSlideStartPosition();
                break;

            case AnimationType.Fade:
                if (canvasGroup) canvasGroup.alpha = fadeFrom;
                break;

            case AnimationType.ScaleAndFade:
                rectTransform.localScale = scaleFrom;
                if (canvasGroup) canvasGroup.alpha = fadeFrom;
                break;

            case AnimationType.SlideAndFade:
                rectTransform.anchoredPosition = GetSlideStartPosition();
                if (canvasGroup) canvasGroup.alpha = fadeFrom;
                break;
        }
    }

    void ApplyAnimation(float t, bool isShowing)
    {
        switch (animationType)
        {
            case AnimationType.Scale:
                rectTransform.localScale = Vector3.Lerp(scaleFrom, scaleTo, t);
                break;

            case AnimationType.Slide:
                Vector2 start = GetSlideStartPosition();
                Vector2 end = originalPosition;
                rectTransform.anchoredPosition = Vector2.Lerp(start, end, t);
                break;

            case AnimationType.Fade:
                if (canvasGroup)
                    canvasGroup.alpha = Mathf.Lerp(fadeFrom, fadeTo, t);
                break;

            case AnimationType.ScaleAndFade:
                rectTransform.localScale = Vector3.Lerp(scaleFrom, scaleTo, t);
                if (canvasGroup)
                    canvasGroup.alpha = Mathf.Lerp(fadeFrom, fadeTo, t);
                break;

            case AnimationType.SlideAndFade:
                Vector2 slideStart = GetSlideStartPosition();
                Vector2 slideEnd = originalPosition;
                rectTransform.anchoredPosition = Vector2.Lerp(slideStart, slideEnd, t);
                if (canvasGroup)
                    canvasGroup.alpha = Mathf.Lerp(fadeFrom, fadeTo, t);
                break;
        }
    }

    void ResetToEndState()
    {
        rectTransform.localScale = scaleTo;
        rectTransform.anchoredPosition = originalPosition;
        if (canvasGroup) canvasGroup.alpha = fadeTo;
    }

    Vector2 GetSlideStartPosition()
    {
        Vector2 offset = Vector2.zero;

        switch (slideDirection)
        {
            case SlideDirection.Top:
                offset = new Vector2(0, slideDistance);
                break;
            case SlideDirection.Bottom:
                offset = new Vector2(0, -slideDistance);
                break;
            case SlideDirection.Left:
                offset = new Vector2(-slideDistance, 0);
                break;
            case SlideDirection.Right:
                offset = new Vector2(slideDistance, 0);
                break;
        }

        return originalPosition + offset;
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource && clip)
        {
            audioSource.PlayOneShot(clip, soundVolume);
        }
    }

    // Public helper methods
    public void SetAnimationDuration(float duration)
    {
        animationDuration = duration;
    }

    public void SetAnimationType(AnimationType type)
    {
        animationType = type;
    }

    public bool IsAnimating()
    {
        return isAnimating;
    }
}