using UnityEngine;

/// <summary>
/// Pocket Trigger 3D - Optimized for 3D table with actual falling
/// Balls fall naturally into pockets with gravity
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class PocketTrigger3D : MonoBehaviour
{
    [Header("Pocket Detection")]
    [Tooltip("Pocket radius for entry detection")]
    public float pocketRadius = 0.65f;

    [Tooltip("Use simplified detection (recommended for 3D)")]
    public bool useSimplifiedDetection = true;

    [Header("Rules")]
    [Tooltip("Minimum speed to enter pocket")]
    public float minSpeed = 0.02f;

    [Tooltip("Direction alignment threshold (0-1)")]
    [Range(0f, 1f)]
    public float dotThreshold = 0.2f; // Lower = more forgiving

    [Header("Center")]
    public Transform pocketCenter;

    [Header("Audio")]
    public AudioClip[] pocketSounds;
    private AudioSource audioSource;

    [Header("Animation")]
    [Tooltip("Use smooth animation when ball enters pocket")]
    public bool useAnimation = true;

    [Tooltip("Delay before notifying GameState")]
    public float notificationDelay = 0.15f;

    [Header("Particles")]
    public GameObject pocketParticle;

    [Header("3D Table Settings")]
    [Tooltip("Falling detection height threshold")]
    public float fallingHeightThreshold = 0.3f;

    [Tooltip("Auto-pocket balls that fall below this Y")]
    public float autoPocketY = -0.5f;

    [Tooltip("Disable collider when pocketed")]
    public bool disableColliderOnPocket = true;

    // Private
    private Ball3D lastPocketedBall;
    private bool processingPocket = false;

    void Awake()
    {
        if (!pocketCenter) pocketCenter = transform;

        audioSource = GetComponent<AudioSource>();
        if (audioSource)
        {
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1.0f;
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (processingPocket) return;

        var ball = other.GetComponent<Ball3D>();
        if (!ball || ball.inPocket) return;

        Rigidbody rb = ball.rb;
        if (!rb || rb.isKinematic) return;

        // ✅ Simplified detection for 3D table
        if (useSimplifiedDetection)
        {
            if (ShouldPocketBall_Simplified(ball, rb))
            {
                PocketBall(ball);
            }
        }
        else
        {
            if (ShouldPocketBall_Advanced(ball, rb))
            {
                PocketBall(ball);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Auto-pocket if ball falls deep enough
        var ball = other.GetComponent<Ball3D>();
        if (!ball || ball.inPocket) return;

        if (ball.transform.position.y < fallingHeightThreshold)
        {
            PocketBall(ball);
        }
    }

    /// <summary>
    /// Simplified detection - good for 3D tables with physical pockets
    /// </summary>
    bool ShouldPocketBall_Simplified(Ball3D ball, Rigidbody rb)
    {
        // Check distance to pocket center
        Vector3 toCenter = pocketCenter.position - rb.position;
        toCenter.y = 0f;
        float dist = toCenter.magnitude;

        // Must be within pocket radius
        if (dist > pocketRadius) return false;

        // Check velocity
        Vector3 vel = rb.velocity;
        float speed = vel.magnitude;

        // If moving fast, pocket it
        if (speed > minSpeed) return true;

        // If very slow but very close to center, pocket it
        if (dist < pocketRadius * 0.3f) return true;

        // If falling (Y velocity negative), pocket it
        if (vel.y < -0.3f) return true;

        return false;
    }

    /// <summary>
    /// Advanced detection - more strict rules
    /// </summary>
    bool ShouldPocketBall_Advanced(Ball3D ball, Rigidbody rb)
    {
        Vector3 toCenter = pocketCenter.position - rb.position;
        toCenter.y = 0f;
        float dist = toCenter.magnitude;

        if (dist > pocketRadius) return false;

        Vector3 vel = rb.velocity;
        vel.y = 0f;
        float speed = vel.magnitude;

        // Very slow ball far from center
        if (speed < minSpeed && dist > pocketRadius * 0.5f)
            return false;

        // Check direction alignment
        if (vel.sqrMagnitude > 0.0001f && toCenter.sqrMagnitude > 0.0001f)
        {
            float dirDot = Vector3.Dot(vel.normalized, toCenter.normalized);
            if (dirDot < dotThreshold) return false;
        }
        else
        {
            // Stationary ball must be very close
            if (dist > pocketRadius * 0.4f) return false;
        }

        return true;
    }

    void Update()
    {
        // Auto-pocket balls that fell too low
        CheckAutoPocket();
    }

    void CheckAutoPocket()
    {
        // Find all balls in trigger that are too low
        Collider[] colliders = Physics.OverlapSphere(pocketCenter.position, pocketRadius * 2f);

        foreach (var col in colliders)
        {
            Ball3D ball = col.GetComponent<Ball3D>();
            if (!ball || ball.inPocket) continue;

            if (ball.transform.position.y < autoPocketY)
            {
                PocketBall(ball);
            }
        }
    }

    void PocketBall(Ball3D ball)
    {
        if (processingPocket) return;
        processingPocket = true;

        // Mark as pocketed
        ball.inPocket = true;

        // Disable collider to prevent further collisions
        if (disableColliderOnPocket)
        {
            var collider = ball.GetComponent<Collider>();
            if (collider) collider.enabled = false;
        }

        // Stop physics OR let it fall
        if (ball.rb)
        {
            if (!useAnimation)
            {
                // Instant - stop physics
                ball.rb.velocity = Vector3.zero;
                ball.rb.angularVelocity = Vector3.zero;
                ball.rb.isKinematic = true;
            }
            else
            {
                // With animation - let it fall a bit
                ball.rb.velocity = new Vector3(0, ball.rb.velocity.y * 0.5f, 0);
                ball.rb.angularVelocity *= 0.5f;
            }
        }

        // Haptic feedback
        if (HapticManager.Instance != null)
        {
            Haptics.Medium();
        }

        // Play sound
        PlayPocketSound();

        // Spawn particle
        if (pocketParticle)
        {
            SpawnParticle(ball.transform.position);
        }

        // Animation or instant hide
        if (useAnimation)
        {
            BallPocketAnimation anim = ball.GetComponent<BallPocketAnimation>();
            if (anim != null)
            {
                // Play animation
                anim.PlayPocketAnimation(pocketCenter.position);

                // Store reference
                lastPocketedBall = ball;

                // Notify after delay
                Invoke(nameof(NotifyGameStateDelayed), notificationDelay);
            }
            else
            {
                // No animation - instant hide
                HideBallInstant(ball);
                NotifyGameState(ball);
            }
        }
        else
        {
            // Instant hide
            HideBallInstant(ball);
            NotifyGameState(ball);
        }

        // Reset processing flag
        Invoke(nameof(ResetProcessingFlag), 0.1f);
    }

    void HideBallInstant(Ball3D ball)
    {
        if (ball.rb)
        {
            ball.rb.velocity = Vector3.zero;
            ball.rb.angularVelocity = Vector3.zero;
            ball.rb.isKinematic = true;
        }
        ball.gameObject.SetActive(false);
    }

    void ResetProcessingFlag()
    {
        processingPocket = false;
    }

    void NotifyGameStateDelayed()
    {
        if (lastPocketedBall != null)
        {
            NotifyGameState(lastPocketedBall);
            lastPocketedBall = null;
        }
    }

    void NotifyGameState(Ball3D ball)
    {
        // Notify GameStateManager
        if (GameStateManager.Instance)
        {
            GameStateManager.Instance.OnBallPocketed(ball);
        }

        // Handle scratch
        if (ball.type == BallType.Cue)
        {
            var scratch = FindObjectOfType<ScratchManager>();
            if (scratch) scratch.OnScratch(ball.transform);
        }
    }

    void PlayPocketSound()
    {
        if (pocketSounds == null || pocketSounds.Length == 0 || !audioSource) return;

        AudioClip clip = pocketSounds[Random.Range(0, pocketSounds.Length)];
        audioSource.pitch = Random.Range(0.9f, 1.1f);
        audioSource.PlayOneShot(clip);
    }

    void SpawnParticle(Vector3 position)
    {
        if (!pocketParticle) return;

        GameObject particle = Instantiate(pocketParticle, position, Quaternion.identity);
        Destroy(particle, 2f);
    }

    // Gizmos
    void OnDrawGizmos()
    {
        if (!pocketCenter) pocketCenter = transform;

        // Draw pocket radius
        Gizmos.color = Color.yellow;
        Vector3 center = pocketCenter.position;

        for (int i = 0; i < 32; i++)
        {
            float angle1 = (i / 32f) * 360f * Mathf.Deg2Rad;
            float angle2 = ((i + 1) / 32f) * 360f * Mathf.Deg2Rad;

            Vector3 p1 = center + new Vector3(Mathf.Cos(angle1), 0, Mathf.Sin(angle1)) * pocketRadius;
            Vector3 p2 = center + new Vector3(Mathf.Cos(angle2), 0, Mathf.Sin(angle2)) * pocketRadius;

            Gizmos.DrawLine(p1, p2);
        }

        // Draw falling threshold
        Gizmos.color = Color.red;
        Vector3 thresholdCenter = center;
        thresholdCenter.y = fallingHeightThreshold;

        for (int i = 0; i < 32; i++)
        {
            float angle1 = (i / 32f) * 360f * Mathf.Deg2Rad;
            float angle2 = ((i + 1) / 32f) * 360f * Mathf.Deg2Rad;

            Vector3 p1 = thresholdCenter + new Vector3(Mathf.Cos(angle1), 0, Mathf.Sin(angle1)) * pocketRadius;
            Vector3 p2 = thresholdCenter + new Vector3(Mathf.Cos(angle2), 0, Mathf.Sin(angle2)) * pocketRadius;

            Gizmos.DrawLine(p1, p2);
        }

        // Draw auto-pocket line
        Gizmos.color = Color.cyan;
        Vector3 autoPocketCenter = center;
        autoPocketCenter.y = autoPocketY;
        Gizmos.DrawWireSphere(autoPocketCenter, pocketRadius);
    }
}