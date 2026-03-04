using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// Button Animator - Makes buttons feel responsive and alive
/// Add to any Button for instant polish
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonAnimator : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Scale Animation")]
    [Tooltip("Enable scale animation on press")]
    public bool useScaleAnimation = true;

    [Tooltip("Scale when pressed (1 = normal, 0.95 = slightly smaller)")]
    [Range(0.5f, 1f)]
    public float pressedScale = 0.95f;

    [Tooltip("Scale when hovered (1 = normal, 1.05 = slightly bigger)")]
    [Range(1f, 1.2f)]
    public float hoverScale = 1.05f;

    [Tooltip("Animation speed")]
    [Range(5f, 30f)]
    public float animationSpeed = 15f;

    [Header("Color Animation")]
    [Tooltip("Enable color change on hover")]
    public bool useColorAnimation = false;

    [Tooltip("Color when hovered")]
    public Color hoverColor = new Color(1f, 1f, 0.8f, 1f);

    private Color originalColor;

    [Header("Rotation Animation")]
    [Tooltip("Enable rotation wobble on press")]
    public bool useRotationAnimation = false;

    [Tooltip("Rotation amount in degrees")]
    [Range(0f, 15f)]
    public float rotationAmount = 5f;

    [Header("Haptic Feedback")]
    [Tooltip("Vibrate on press")]
    public bool useHapticFeedback = true;

    public enum HapticType { Light, Medium, Selection }
    public HapticType hapticType = HapticType.Selection;

    [Header("Audio")]
    [Tooltip("Play sound on press")]
    public bool playSound = true;

    public AudioClip pressSound;
    public AudioClip hoverSound;

    [Range(0f, 1f)]
    public float soundVolume = 1f;

    [Header("Particle Effect")]
    [Tooltip("Spawn particle on press")]
    public GameObject particleEffect;

    [Tooltip("Particle spawn offset")]
    public Vector3 particleOffset = Vector3.zero;

    // Private
    private Vector3 originalScale;
    private Quaternion originalRotation;
    private Vector3 targetScale;
    private float targetRotation;
    private bool isPressed = false;
    private bool isHovered = false;

    private Button button;
    private Image buttonImage;
    private AudioSource audioSource;

    [Header("Debug")]
    public bool showDebugLogs = false;

    void Start()
    {
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();

        // Get or create AudioSource
        audioSource = GetComponent<AudioSource>();
        if (!audioSource && playSound)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // Save original values
        originalScale = transform.localScale;
        originalRotation = transform.localRotation;
        targetScale = originalScale;

        if (buttonImage)
        {
            originalColor = buttonImage.color;
        }
    }

    void Update()
    {
        // Smooth scale animation
        if (useScaleAnimation)
        {
            transform.localScale = Vector3.Lerp(
                transform.localScale,
                targetScale,
                Time.unscaledDeltaTime * animationSpeed
            );
        }

        // Smooth rotation animation
        if (useRotationAnimation && isPressed)
        {
            float wobble = Mathf.Sin(Time.time * 20f) * rotationAmount;
            transform.localRotation = Quaternion.Euler(0, 0, wobble);
        }
        else if (useRotationAnimation)
        {
            transform.localRotation = Quaternion.Lerp(
                transform.localRotation,
                originalRotation,
                Time.unscaledDeltaTime * animationSpeed
            );
        }

        // Smooth color animation
        if (useColorAnimation && buttonImage)
        {
            Color targetColor = isHovered ? hoverColor : originalColor;
            buttonImage.color = Color.Lerp(
                buttonImage.color,
                targetColor,
                Time.unscaledDeltaTime * animationSpeed
            );
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!button.interactable) return;

        isPressed = true;

        // Scale down
        if (useScaleAnimation)
        {
            targetScale = originalScale * pressedScale;
        }

        // Haptic feedback
        if (useHapticFeedback)
        {
            TriggerHaptic();
        }

        // Sound
        if (playSound && pressSound)
        {
            PlaySound(pressSound);
        }

        // Particle
        if (particleEffect)
        {
            SpawnParticle();
        }

        if (showDebugLogs)
            Debug.Log("[ButtonAnimator] Pressed: " + gameObject.name);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!button.interactable) return;

        isPressed = false;

        // Scale back
        if (useScaleAnimation)
        {
            targetScale = isHovered ? (originalScale * hoverScale) : originalScale;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!button.interactable) return;

        isHovered = true;

        // Scale up slightly
        if (useScaleAnimation && !isPressed)
        {
            targetScale = originalScale * hoverScale;
        }

        // Sound
        if (playSound && hoverSound)
        {
            PlaySound(hoverSound);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        isPressed = false;

        // Scale back to normal
        if (useScaleAnimation)
        {
            targetScale = originalScale;
        }
    }

    void TriggerHaptic()
    {
        if (HapticManager.Instance != null)
        {
            switch (hapticType)
            {
                case HapticType.Light:
                    Haptics.Light();
                    break;
                case HapticType.Medium:
                    Haptics.Medium();
                    break;
                case HapticType.Selection:
                    Haptics.Selection();
                    break;
            }
        }
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource && clip)
        {
            audioSource.PlayOneShot(clip, soundVolume);
        }
    }

    void SpawnParticle()
    {
        Vector3 spawnPos = transform.position + particleOffset;

        // Use object pool if available
        if (ObjectPool.Instance != null)
        {
            ObjectPool.Instance.SpawnFromPoolTimed("ButtonParticle", spawnPos, Quaternion.identity, 1f);
        }
        else
        {
            GameObject particle = Instantiate(particleEffect, spawnPos, Quaternion.identity);
            Destroy(particle, 1f);
        }
    }

    // Public methods
    public void ResetAnimation()
    {
        transform.localScale = originalScale;
        transform.localRotation = originalRotation;
        if (buttonImage) buttonImage.color = originalColor;

        isPressed = false;
        isHovered = false;
        targetScale = originalScale;
    }

    public void SetInteractable(bool interactable)
    {
        button.interactable = interactable;
        if (!interactable)
        {
            ResetAnimation();
        }
    }
}