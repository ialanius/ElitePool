using UnityEngine;

/// <summary>
/// Pocket Trigger 3D - Enhanced with smooth ball animation
/// Replace your current PocketTrigger3D.cs with this version
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class PocketTrigger3D : MonoBehaviour
{
    [Header("Pocket Size")]
    public float pocketRadius = 0.65f;
    public float enterDepth = 0f;

    [Header("Rules")]
    public float minSpeed = 0.02f;
    [Range(0f, 1f)] public float dotThreshold = 0.35f;

    [Header("Center (optional)")]
    public Transform pocketCenter;

    [Header("Audio")]
    public AudioClip[] pocketSounds;
    private AudioSource audioSource;

    [Header("Animation")]
    [Tooltip("Use smooth animation when ball enters pocket")]
    public bool useAnimation = true;

    [Tooltip("Delay before notifying GameState (allows animation to play)")]
    public float notificationDelay = 0.15f;

    [Header("Particles")]
    [Tooltip("Particle effect when ball enters")]
    public GameObject pocketParticle;

    // Private
    private Ball3D lastPocketedBall;

    void Awake()
    {
        if (!pocketCenter) pocketCenter = transform;

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1.0f;
    }

    void OnTriggerStay(Collider other)
    {
        var ball = other.GetComponent<Ball3D>();
        if (!ball || ball.inPocket) return;

        Rigidbody rb = ball.rb;
        if (!rb || rb.isKinematic) return;

        Vector3 toCenter = pocketCenter.position - rb.position;
        toCenter.y = 0f;

        float dist = toCenter.magnitude;

        if (dist > pocketRadius - enterDepth) return;

        Vector3 vel = rb.velocity;
        vel.y = 0f;
        float speed = vel.magnitude;

        if (speed < minSpeed && dist > pocketRadius * 0.5f)
            return;

        if (vel.sqrMagnitude > 0.0001f && toCenter.sqrMagnitude > 0.0001f)
        {
            float dirDot = Vector3.Dot(vel.normalized, toCenter.normalized);
            if (dirDot < dotThreshold) return;
        }
        else
        {
            if (dist > pocketRadius * 0.4f) return;
        }

        PocketBall(ball);
    }

    void PocketBall(Ball3D ball)
    {
        // Mark as pocketed immediately
        ball.inPocket = true;

        // Stop physics
        if (ball.rb)
        {
            ball.rb.velocity = Vector3.zero;
            ball.rb.angularVelocity = Vector3.zero;
            ball.rb.isKinematic = true;
        }

        // Haptic feedback
        Haptics.Medium();

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

                // Store ball reference
                lastPocketedBall = ball;

                // Notify GameState after delay
                Invoke(nameof(NotifyGameStateDelayed), notificationDelay);
            }
            else
            {
                // No animation component - instant hide
                ball.gameObject.SetActive(false);
                NotifyGameState(ball);
            }
        }
        else
        {
            // Instant hide (old behavior)
            ball.gameObject.SetActive(false);
            NotifyGameState(ball);
        }
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

        // توليد البارتكل بالطريقة العادية
        GameObject particle = Instantiate(pocketParticle, position, Quaternion.identity);
        Destroy(particle, 2f);
    }

    // Gizmos for visualization in editor
    void OnDrawGizmos()
    {
        if (!pocketCenter) pocketCenter = transform;

        Gizmos.color = Color.yellow;
        Vector3 center = pocketCenter.position;

        // Draw outer radius
        for (int i = 0; i < 32; i++)
        {
            float angle1 = (i / 32f) * 360f * Mathf.Deg2Rad;
            float angle2 = ((i + 1) / 32f) * 360f * Mathf.Deg2Rad;

            Vector3 p1 = center + new Vector3(Mathf.Cos(angle1), 0, Mathf.Sin(angle1)) * pocketRadius;
            Vector3 p2 = center + new Vector3(Mathf.Cos(angle2), 0, Mathf.Sin(angle2)) * pocketRadius;

            Gizmos.DrawLine(p1, p2);
        }

        // Draw enter depth
        Gizmos.color = Color.green;
        float enterRadius = pocketRadius - enterDepth;
        for (int i = 0; i < 32; i++)
        {
            float angle1 = (i / 32f) * 360f * Mathf.Deg2Rad;
            float angle2 = ((i + 1) / 32f) * 360f * Mathf.Deg2Rad;

            Vector3 p1 = center + new Vector3(Mathf.Cos(angle1), 0, Mathf.Sin(angle1)) * enterRadius;
            Vector3 p2 = center + new Vector3(Mathf.Cos(angle2), 0, Mathf.Sin(angle2)) * enterRadius;

            Gizmos.DrawLine(p1, p2);
        }
    }
}