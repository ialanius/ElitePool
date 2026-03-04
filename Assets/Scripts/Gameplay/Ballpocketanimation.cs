using UnityEngine;
using System.Collections;

/// <summary>
/// Ball Pocket Animation - Smooth animation when ball enters pocket
/// Add this component to each ball prefab
/// </summary>
public class BallPocketAnimation : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("Animation duration in seconds")]
    [Range(0.2f, 2f)]
    public float animationDuration = 0.8f;

    [Tooltip("Animation type")]
    public AnimationType animationType = AnimationType.SpiralSink;

    public enum AnimationType
    {
        SimpleSink,      // Just sink down
        ScaleDown,       // Scale down while sinking
        Spiral,          // Rotate while sinking
        SpiralSink,      // Spiral + scale (best!)
        Pop              // Pop and disappear
    }

    [Header("Sink Settings")]
    [Tooltip("How far to sink down")]
    public float sinkDistance = 1.5f;

    [Tooltip("Sink curve")]
    public AnimationCurve sinkCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Scale Settings")]
    [Tooltip("Scale down to this size (0 = disappear completely)")]
    [Range(0f, 1f)]
    public float targetScale = 0f;

    [Tooltip("Scale curve")]
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Rotation Settings")]
    [Tooltip("Rotation speed (degrees per second)")]
    public float rotationSpeed = 720f;

    [Tooltip("Rotation axis")]
    public Vector3 rotationAxis = Vector3.up;

    [Header("Fade Settings")]
    [Tooltip("Fade out during animation")]
    public bool fadeOut = false;

    [Header("Effects")]
    [Tooltip("Particle effect to spawn")]
    public GameObject pocketParticle;

    [Tooltip("Spawn particle at ball or pocket position")]
    public bool particleAtBall = true;

    [Header("Audio")]
    [Tooltip("Sound when animation starts")]
    public AudioClip animationSound;

    [Range(0f, 1f)]
    public float soundVolume = 1f;

    // Private
    private Vector3 originalScale;
    private Renderer ballRenderer;
    private AudioSource audioSource;
    private bool isAnimating = false;

    void Awake()
    {
        originalScale = transform.localScale;
        ballRenderer = GetComponent<Renderer>();

        audioSource = GetComponent<AudioSource>();
        if (!audioSource)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    /// <summary>
    /// Start pocket animation
    /// </summary>
    public void PlayPocketAnimation(Vector3 pocketPosition)
    {
        if (isAnimating) return;

        StartCoroutine(AnimateToPocket(pocketPosition));
    }

    IEnumerator AnimateToPocket(Vector3 pocketPosition)
    {
        isAnimating = true;

        // Play sound
        if (animationSound && audioSource)
        {
            audioSource.PlayOneShot(animationSound, soundVolume);
        }

        // Spawn particle
        if (pocketParticle)
        {
            Vector3 particlePos = particleAtBall ? transform.position : pocketPosition;

            // توليد البارتكل (المؤثر البصري) بالطريقة العادية
            GameObject particle = Instantiate(pocketParticle, particlePos, Quaternion.identity);
            Destroy(particle, 2f);
        }

        // Store starting values
        Vector3 startPos = transform.position;
        Vector3 endPos = pocketPosition - Vector3.up * sinkDistance;
        Vector3 startScale = transform.localScale;
        Quaternion startRotation = transform.rotation;

        float elapsed = 0f;

        // Get material for fade
        Material ballMaterial = null;
        Color originalColor = Color.white;
        if (fadeOut && ballRenderer && ballRenderer.material)
        {
            ballMaterial = ballRenderer.material;
            originalColor = ballMaterial.color;
        }

        // Animation loop
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;

            // Apply animation based on type
            switch (animationType)
            {
                case AnimationType.SimpleSink:
                    AnimateSimpleSink(startPos, endPos, t);
                    break;

                case AnimationType.ScaleDown:
                    AnimateScaleDown(startPos, endPos, startScale, t);
                    break;

                case AnimationType.Spiral:
                    AnimateSpiral(startPos, endPos, startRotation, t);
                    break;

                case AnimationType.SpiralSink:
                    AnimateSpiralSink(startPos, endPos, startScale, startRotation, t);
                    break;

                case AnimationType.Pop:
                    AnimatePop(startPos, startScale, t);
                    break;
            }

            // Apply fade
            if (fadeOut && ballMaterial)
            {
                Color newColor = originalColor;
                newColor.a = Mathf.Lerp(1f, 0f, t);
                ballMaterial.color = newColor;
            }

            yield return null;
        }

        // Animation complete - disable ball
        gameObject.SetActive(false);

        // Reset for next time
        transform.localScale = originalScale;
        if (ballMaterial)
        {
            ballMaterial.color = originalColor;
        }

        isAnimating = false;
    }

    void AnimateSimpleSink(Vector3 startPos, Vector3 endPos, float t)
    {
        float sinkT = sinkCurve.Evaluate(t);
        transform.position = Vector3.Lerp(startPos, endPos, sinkT);
    }

    void AnimateScaleDown(Vector3 startPos, Vector3 endPos, Vector3 startScale, float t)
    {
        // Sink
        float sinkT = sinkCurve.Evaluate(t);
        transform.position = Vector3.Lerp(startPos, endPos, sinkT);

        // Scale
        float scaleT = scaleCurve.Evaluate(t);
        transform.localScale = Vector3.Lerp(startScale, startScale * targetScale, scaleT);
    }

    void AnimateSpiral(Vector3 startPos, Vector3 endPos, Quaternion startRotation, float t)
    {
        // Sink
        float sinkT = sinkCurve.Evaluate(t);
        transform.position = Vector3.Lerp(startPos, endPos, sinkT);

        // Rotate
        float rotationAmount = rotationSpeed * t * animationDuration;
        transform.rotation = startRotation * Quaternion.AngleAxis(rotationAmount, rotationAxis);
    }

    void AnimateSpiralSink(Vector3 startPos, Vector3 endPos, Vector3 startScale, Quaternion startRotation, float t)
    {
        // Sink
        float sinkT = sinkCurve.Evaluate(t);
        transform.position = Vector3.Lerp(startPos, endPos, sinkT);

        // Scale
        float scaleT = scaleCurve.Evaluate(t);
        transform.localScale = Vector3.Lerp(startScale, startScale * targetScale, scaleT);

        // Rotate
        float rotationAmount = rotationSpeed * t * animationDuration;
        transform.rotation = startRotation * Quaternion.AngleAxis(rotationAmount, rotationAxis);
    }

    void AnimatePop(Vector3 startPos, Vector3 startScale, float t)
    {
        // Pop up then scale down
        float popHeight = 0.3f;
        float yOffset = Mathf.Sin(t * Mathf.PI) * popHeight;
        transform.position = startPos + Vector3.up * yOffset;

        // Scale
        float scaleT = scaleCurve.Evaluate(t);
        transform.localScale = Vector3.Lerp(startScale, startScale * targetScale, scaleT);

        // Spin fast
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Instant hide without animation (fallback)
    /// </summary>
    public void HideInstant()
    {
        StopAllCoroutines();
        gameObject.SetActive(false);
        transform.localScale = originalScale;
        isAnimating = false;
    }

    // Public getters
    public bool IsAnimating()
    {
        return isAnimating;
    }

    public float GetAnimationDuration()
    {
        return animationDuration;
    }
}