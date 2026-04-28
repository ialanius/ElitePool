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

    [Tooltip("Clamp oversized prefab values back to this radius at runtime")]
    public bool syncTriggerRadiusWithDetection = true;

    [Tooltip("Extra trigger space beyond the detection radius")]
    public float triggerRadiusPadding = 0.04f;

    [Tooltip("Use simplified detection (recommended for 3D)")]
    public bool useSimplifiedDetection = true;

    [Header("Rules")]
    [Tooltip("Minimum speed to enter pocket")]
    public float minSpeed = 0.02f;

    [Tooltip("Direction alignment threshold (0-1)")]
    [Range(0f, 1f)]
    public float dotThreshold = 0.2f; // Lower = more forgiving

    [Tooltip("Ball must be this far below the pocket center before it is auto-accepted")]
    public float entryDepth = 0.12f;

    [Tooltip("Absolute inset from the pocket rim before capture is allowed")]
    public float rimInset = 0.08f;

    [Tooltip("How much of the ball must cross the pocket rim before capture")]
    [Range(0f, 2f)]
    public float edgeCaptureFactor = 1f;

    [Tooltip("Minimum inward movement required for an above-rim capture")]
    [Range(0f, 1f)]
    public float minInwardDotForCapture = 0.45f;

    [Header("Fast Entry")]
    [Tooltip("Minimum planar speed for swept high-speed pocket capture.")]
    public float fastEntrySpeed = 4.5f;

    [Tooltip("Extra inner-mouth radius used for swept high-speed capture.")]
    public float fastEntryRadiusPadding = 0.12f;

    [Tooltip("Minimum inward travel during one physics step for a fast-entry capture.")]
    public float fastEntryMinInwardTravel = 0.01f;

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
    private SphereCollider triggerCollider;

    void Awake()
    {
        if (!pocketCenter) pocketCenter = transform;
        triggerCollider = GetComponent<SphereCollider>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource)
        {
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1.0f;
        }

        ApplyRuntimePocketSettings();
    }

    void OnValidate()
    {
        if (!pocketCenter) pocketCenter = transform;
        ApplyRuntimePocketSettings();
    }

    void OnTriggerStay(Collider other)
    {
        TryPocketBall(other);
    }

    void OnTriggerEnter(Collider other)
    {
        TryPocketBall(other);
    }

    /// <summary>
    /// Simplified detection - good for 3D tables with physical pockets
    /// </summary>
    bool ShouldPocketBall_Simplified(Ball3D ball, Rigidbody rb)
    {
        float ballRadius = GetBallWorldRadius(ball);
        float captureRadius = GetCaptureRadius(ball);

        // Check distance to pocket center
        Vector3 toCenter = pocketCenter.position - rb.position;
        toCenter.y = 0f;
        float dist = toCenter.magnitude;
        float verticalDrop = pocketCenter.position.y - rb.position.y;

        if (ShouldPocketBall_FastEntry(ball, rb, ballRadius)) return true;

        // Must be within pocket radius
        if (dist > pocketRadius) return false;

        // Check velocity
        Vector3 vel = rb.velocity;
        Vector3 planarVel = new Vector3(vel.x, 0f, vel.z);
        float speed = planarVel.magnitude;
        float inwardDot = 0f;
        float inwardSpeed = 0f;
        bool hasPlanarMotion = planarVel.sqrMagnitude > 0.0001f && toCenter.sqrMagnitude > 0.0001f;
        if (hasPlanarMotion)
        {
            Vector3 toCenterDir = toCenter.normalized;
            inwardDot = Vector3.Dot(planarVel.normalized, toCenterDir);
            inwardSpeed = Vector3.Dot(planarVel, toCenterDir);
        }

        bool isBelowRim = verticalDrop > (ballRadius * 0.12f) || vel.y < -0.15f;

        // Once the ball drops below the rim, accept it quickly.
        if (isBelowRim && dist <= pocketRadius - (ballRadius * 0.1f)) return true;

        // Do not capture while the ball is still only brushing the jaw.
        if (dist > captureRadius) return false;

        if (speed <= minSpeed)
        {
            return dist < captureRadius * 0.7f && verticalDrop > entryDepth * 0.2f;
        }

        // A rolling ball near the rim must be clearly moving toward the pocket center.
        float inwardThreshold = Mathf.Max(dotThreshold, minInwardDotForCapture);
        bool clearlyEntering = hasPlanarMotion && inwardDot >= inwardThreshold && inwardSpeed > minSpeed;
        if (!clearlyEntering) return false;

        return isBelowRim || dist <= captureRadius * 0.82f;
    }

    /// <summary>
    /// Advanced detection - more strict rules
    /// </summary>
    bool ShouldPocketBall_Advanced(Ball3D ball, Rigidbody rb)
    {
        float ballRadius = GetBallWorldRadius(ball);
        float captureRadius = GetCaptureRadius(ball);

        Vector3 toCenter = pocketCenter.position - rb.position;
        toCenter.y = 0f;
        float dist = toCenter.magnitude;
        float verticalDrop = pocketCenter.position.y - rb.position.y;

        if (ShouldPocketBall_FastEntry(ball, rb, ballRadius)) return true;

        if (dist > pocketRadius) return false;

        Vector3 velocity = rb.velocity;
        Vector3 planarVel = new Vector3(velocity.x, 0f, velocity.z);
        float speed = planarVel.magnitude;
        bool isBelowRim = verticalDrop > (ballRadius * 0.12f) || velocity.y < -0.15f;

        // Very slow ball far from center
        if (speed < minSpeed && (!isBelowRim || dist > captureRadius * 0.7f))
            return false;

        if (isBelowRim && dist <= pocketRadius - (ballRadius * 0.1f))
            return true;

        if (dist > captureRadius)
            return false;

        // Check direction alignment
        if (planarVel.sqrMagnitude > 0.0001f && toCenter.sqrMagnitude > 0.0001f)
        {
            Vector3 toCenterDir = toCenter.normalized;
            float dirDot = Vector3.Dot(planarVel.normalized, toCenterDir);
            float inwardSpeed = Vector3.Dot(planarVel, toCenterDir);
            if (dirDot < Mathf.Max(dotThreshold, minInwardDotForCapture) || inwardSpeed <= minSpeed) return false;
            return isBelowRim || dist <= captureRadius * 0.8f;
        }

        // Stationary ball must already be below the rim and clearly inside the mouth.
        return isBelowRim && dist <= captureRadius * 0.7f;
    }

    public bool ShouldReleaseTableSupport(Ball3D ball)
    {
        if (!ball || !ball.rb || ball.inPocket) return false;

        float ballRadius = GetBallWorldRadius(ball);
        Vector3 toCenter = pocketCenter.position - ball.rb.position;
        toCenter.y = 0f;

        float dist = toCenter.magnitude;
        if (ShouldPocketBall_FastEntry(ball, ball.rb, ballRadius)) return true;
        if (dist > pocketRadius) return false;

        float releaseRadius = GetReleaseRadius(ballRadius);
        if (dist <= releaseRadius * 0.82f) return true;

        Vector3 planarVel = new Vector3(ball.rb.velocity.x, 0f, ball.rb.velocity.z);
        if (planarVel.sqrMagnitude < 0.0001f || toCenter.sqrMagnitude < 0.0001f) return false;

        float inwardDot = Vector3.Dot(planarVel.normalized, toCenter.normalized);
        return dist <= releaseRadius && inwardDot >= 0.15f;
    }

    public bool IsApproachingPocketMouth(Ball3D ball, float extraRadius, float minInwardDot)
    {
        if (!ball || !ball.rb || ball.inPocket) return false;

        Vector3 toCenter = pocketCenter.position - ball.rb.position;
        toCenter.y = 0f;

        float dist = toCenter.magnitude;
        float ballRadius = GetBallWorldRadius(ball);
        float approachRadius = pocketRadius + Mathf.Max(0f, extraRadius) + (ballRadius * 0.35f);
        if (dist > approachRadius) return false;

        Vector3 planarVel = new Vector3(ball.rb.velocity.x, 0f, ball.rb.velocity.z);
        if (planarVel.sqrMagnitude < 0.0001f || toCenter.sqrMagnitude < 0.0001f) return false;

        float inwardDot = Vector3.Dot(planarVel.normalized, toCenter.normalized);
        return inwardDot >= Mathf.Clamp(minInwardDot, -1f, 1f);
    }

    void Update()
    {
        // Auto-pocket balls that fell too low
        CheckAutoPocket();
    }

    bool TryPocketBall(Collider other)
    {
        if (processingPocket) return false;

        Ball3D ball = other.GetComponent<Ball3D>();
        if (!ball || ball.inPocket) return false;

        Rigidbody rb = ball.rb;
        if (!rb || rb.isKinematic) return false;

        if (ball.transform.position.y < fallingHeightThreshold)
        {
            PocketBall(ball);
            return true;
        }

        bool shouldPocket = useSimplifiedDetection
            ? ShouldPocketBall_Simplified(ball, rb)
            : ShouldPocketBall_Advanced(ball, rb);

        if (!shouldPocket) return false;

        PocketBall(ball);
        return true;
    }

    void CheckAutoPocket()
    {
        // Find all balls in trigger that are too low
        float overlapRadius = triggerCollider ? triggerCollider.radius * MaxAxis(transform.lossyScale) : pocketRadius + triggerRadiusPadding;
        Collider[] colliders = Physics.OverlapSphere(pocketCenter.position, overlapRadius);

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

        // Stop physics and hand off to the pocket animation immediately so
        // safety/reset systems do not treat the falling ball as a glitch.
        if (ball.rb)
        {
            ball.rb.velocity = Vector3.zero;
            ball.rb.angularVelocity = Vector3.zero;
            ball.rb.useGravity = false;
            ball.rb.isKinematic = true;
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

    void ApplyRuntimePocketSettings()
    {
        pocketRadius = Mathf.Max(0.05f, pocketRadius);
        triggerRadiusPadding = Mathf.Max(0f, triggerRadiusPadding);
        entryDepth = Mathf.Max(0f, entryDepth);
        rimInset = Mathf.Max(0f, rimInset);
        edgeCaptureFactor = Mathf.Clamp(edgeCaptureFactor, 0f, 2f);
        minInwardDotForCapture = Mathf.Clamp01(minInwardDotForCapture);
        fastEntrySpeed = Mathf.Max(0f, fastEntrySpeed);
        fastEntryRadiusPadding = Mathf.Max(0f, fastEntryRadiusPadding);
        fastEntryMinInwardTravel = Mathf.Max(0f, fastEntryMinInwardTravel);

        if (!syncTriggerRadiusWithDetection) return;

        if (!triggerCollider) triggerCollider = GetComponent<SphereCollider>();
        if (triggerCollider)
        {
            triggerCollider.radius = Mathf.Max(0.05f, pocketRadius + triggerRadiusPadding);
            triggerCollider.isTrigger = true;
        }
    }

    float MaxAxis(Vector3 value)
    {
        return Mathf.Max(value.x, Mathf.Max(value.y, value.z));
    }

    float GetBallWorldRadius(Ball3D ball)
    {
        if (!ball) return 0.25f;

        SphereCollider sphere = ball.GetComponent<SphereCollider>();
        if (!sphere) return 0.25f;

        return sphere.radius * MaxAxis(ball.transform.lossyScale);
    }

    float GetCaptureRadius(Ball3D ball)
    {
        float captureInset = Mathf.Max(rimInset, GetBallWorldRadius(ball) * edgeCaptureFactor);
        float captureRadius = pocketRadius - captureInset;
        return Mathf.Clamp(captureRadius, pocketRadius * 0.22f, pocketRadius - 0.01f);
    }

    bool ShouldPocketBall_FastEntry(Ball3D ball, Rigidbody rb, float ballRadius)
    {
        if (!ball || !rb) return false;

        Vector3 planarVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        float speed = planarVelocity.magnitude;
        if (speed < fastEntrySpeed) return false;

        Vector3 previousPosition = GetPreviousPhysicsPosition(ball, rb);
        Vector3 previousPlanar = new Vector3(previousPosition.x, 0f, previousPosition.z);
        Vector3 currentPlanar = new Vector3(rb.position.x, 0f, rb.position.z);
        Vector3 centerPlanar = new Vector3(pocketCenter.position.x, 0f, pocketCenter.position.z);

        float previousDist = Vector3.Distance(previousPlanar, centerPlanar);
        float currentDist = Vector3.Distance(currentPlanar, centerPlanar);
        float outerRadius = pocketRadius + triggerRadiusPadding + (ballRadius * 0.15f);

        float closestDist = DistancePointToSegmentXZ(centerPlanar, previousPlanar, currentPlanar);
        if (Mathf.Min(previousDist, currentDist) > outerRadius && closestDist > outerRadius)
        {
            return false;
        }

        Vector3 step = currentPlanar - previousPlanar;
        if (step.sqrMagnitude < 0.0001f) return false;

        float inwardTravel = previousDist - currentDist;
        if (inwardTravel < fastEntryMinInwardTravel) return false;

        Vector3 toCenterFromPrevious = centerPlanar - previousPlanar;
        if (toCenterFromPrevious.sqrMagnitude > 0.0001f)
        {
            float stepDot = Vector3.Dot(step.normalized, toCenterFromPrevious.normalized);
            if (stepDot < -0.05f) return false;
        }

        float sweptCaptureRadius = GetSweptCaptureRadius(ballRadius);
        bool crossedInnerMouth = previousDist > sweptCaptureRadius && currentDist <= sweptCaptureRadius;
        bool sweptIntoPocket = closestDist <= sweptCaptureRadius && inwardTravel > fastEntryMinInwardTravel;
        return crossedInnerMouth || sweptIntoPocket;
    }

    Vector3 GetPreviousPhysicsPosition(Ball3D ball, Rigidbody rb)
    {
        if (ball)
        {
            Vector3 previous = ball.PreviousPhysicsPosition;
            if ((previous - rb.position).sqrMagnitude > 0.0000001f)
            {
                return previous;
            }
        }

        return rb.position - (rb.velocity * Time.fixedDeltaTime);
    }

    float GetSweptCaptureRadius(float ballRadius)
    {
        float fastCaptureInset = Mathf.Max(rimInset * 0.5f, ballRadius * 0.55f);
        float sweptRadius = pocketRadius - fastCaptureInset + fastEntryRadiusPadding;
        return Mathf.Clamp(sweptRadius, pocketRadius * 0.38f, pocketRadius + (ballRadius * 0.2f));
    }

    float DistancePointToSegmentXZ(Vector3 point, Vector3 start, Vector3 end)
    {
        Vector3 segment = end - start;
        float segmentSqrMagnitude = segment.sqrMagnitude;
        if (segmentSqrMagnitude < 0.000001f)
        {
            return Vector3.Distance(point, start);
        }

        float t = Mathf.Clamp01(Vector3.Dot(point - start, segment) / segmentSqrMagnitude);
        Vector3 closestPoint = start + (segment * t);
        return Vector3.Distance(point, closestPoint);
    }

    float GetReleaseRadius(float ballRadius)
    {
        float releaseInset = Mathf.Max(rimInset * 0.35f, ballRadius * 0.75f);
        float releaseRadius = pocketRadius - releaseInset;
        return Mathf.Clamp(releaseRadius, pocketRadius * 0.4f, pocketRadius - 0.01f);
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
