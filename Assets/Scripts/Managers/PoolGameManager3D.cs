using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PoolGameManager3D : MonoBehaviour
{
    public static PoolGameManager3D Instance;

    [Header("Balls (No Cue)")]
    public Ball3D[] balls;
    public Ball3D cueBall;
    public ScratchManager scratch;
    public GameStateManager gameState;

    [Header("Physics Settings")]
    public float rollingFriction = 0.985f;
    public float slidingFriction = 0.970f;
    public float slideToRollSpeed = 1.2f;
    public float stopSpeed = 0.12f;
    public float spinInfluence = 0.15f;
    public float angularDamping = 0.970f;
    public bool enableRealisticRolling = true;

    [Header("3D Table Settings")]
    [Tooltip("Enable true 3D physics so balls can drop into pockets.")]
    public bool enable3DPhysics = true;

    [Tooltip("Keep balls pinned to the table plane unless they are clearly entering a pocket.")]
    public bool smartYLock = true;

    [Tooltip("Resting center Y for a ball on the cloth. In this project that is 0.25.")]
    public float tableHeight = 0.25f;

    [Tooltip("Absolute Y below which a ball is always considered to be falling.")]
    public float fallingThreshold = 0f;

    [Tooltip("Allowed drift around the resting Y before the manager snaps a ball back to tableHeight.")]
    public float surfaceSnapTolerance = 0.04f;

    [Tooltip("How far below tableHeight a ball must drop before Y-lock fully releases.")]
    public float surfaceReleaseDepth = 0.08f;

    [Tooltip("How far above the cloth a ball may rise before it is treated as an unwanted hop.")]
    public float surfaceHopTolerance = 0.015f;

    [Header("Pocket Release")]
    [Tooltip("Release Y-lock when a ball overlaps a pocket trigger.")]
    public bool releaseYLockNearPockets = true;

    [Tooltip("Extra probe radius used to detect nearby pocket openings.")]
    public float pocketReleasePadding = 0.05f;

    [Tooltip("Minimum vertical speed that disables Y-lock for a frame.")]
    public float pocketReleaseVerticalSpeed = 0.05f;

    [Tooltip("Release Y-lock earlier when a fast ball is clearly approaching a pocket mouth.")]
    public float highSpeedPocketBypassSpeed = 5.5f;

    [Tooltip("Extra reach beyond pocketRadius for fast pocket-approach detection.")]
    public float highSpeedPocketApproachPadding = 0.08f;

    [Tooltip("Minimum inward alignment to treat a fast shot as a real pocket approach.")]
    [Range(-1f, 1f)]
    public float highSpeedPocketApproachDot = 0.05f;

    [Header("3D Stability")]
    [Tooltip("Optional tiny rack jitter. Keep off for stable 3D tables.")]
    public bool addRackMicroChaos = false;

    [Tooltip("Clamp unexpected upward launch speed so balls stay on the table.")]
    public float maxBallRiseSpeed = 0.2f;

    private readonly Dictionary<int, int> ignoreFrames = new Dictionary<int, int>();
    private readonly Dictionary<int, int> stillFrames = new Dictionary<int, int>();
    private bool allBallsWereStopped = false;
    private int pocketLayerMask;
    private PocketTrigger3D[] pocketTriggers;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        CacheLayerMasks();
        CachePocketTriggers();
        RefreshRefs();
        if (!gameState) gameState = GameStateManager.Instance;
        SetupEnhancedPhysics();
    }

    void CacheLayerMasks()
    {
        int pocketsLayer = LayerMask.NameToLayer("Pockets");
        pocketLayerMask = pocketsLayer >= 0 ? (1 << pocketsLayer) : 0;
    }

    void CachePocketTriggers()
    {
        pocketTriggers = FindObjectsOfType<PocketTrigger3D>();
    }

    public void RefreshRefs()
    {
        Ball3D[] all = FindObjectsOfType<Ball3D>();
        cueBall = all.FirstOrDefault(b => b && b.type == BallType.Cue);
        balls = all.Where(b => b && b.type != BallType.Cue).ToArray();

        if (GameStateManager.Instance)
        {
            GameStateManager.Instance.RefreshBallReferences();
        }
    }

    void FixedUpdate()
    {
        if (balls != null)
        {
            foreach (Ball3D ball in balls)
            {
                if (ball) ApplyEnhancedPhysics(ball);
            }
        }

        if (cueBall && cueBall.gameObject.activeInHierarchy && !cueBall.inPocket)
        {
            ApplyEnhancedPhysics(cueBall);
        }

        CheckAllBallsStopped();
    }

    public void RegisterShot(Rigidbody rb)
    {
        if (!rb) return;
        ignoreFrames[rb.GetInstanceID()] = 3;
        rb.WakeUp();
    }

    void SetupEnhancedPhysics()
    {
        if (balls != null)
        {
            foreach (Ball3D ball in balls)
            {
                if (!ball || !ball.rb) continue;

                ConfigureBallPhysics(ball.rb);

                if (addRackMicroChaos)
                {
                    float randomX = Random.Range(-0.002f, 0.002f);
                    float randomZ = Random.Range(-0.002f, 0.002f);
                    ball.transform.position += new Vector3(randomX, 0f, randomZ);
                }
            }
        }

        if (cueBall && cueBall.rb)
        {
            ConfigureBallPhysics(cueBall.rb);
        }
    }

    void ConfigureBallPhysics(Rigidbody rb)
    {
        rb.useGravity = enable3DPhysics;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    void ApplyEnhancedPhysics(Ball3D ball)
    {
        if (!ball || ball.inPocket) return;
        if (!ball.rb || ball.rb.isKinematic) return;

        Rigidbody rb = ball.rb;
        int id = rb.GetInstanceID();
        ClampUnexpectedRise(rb);

        if (ignoreFrames.TryGetValue(id, out int left) && left > 0)
        {
            ignoreFrames[id] = left - 1;
            return;
        }

        bool nearPocketOpening = IsNearPocketOpening(ball);
        bool insidePocketReleaseZone = IsInsidePocketReleaseZone(ball);
        bool isFalling = IsBallFalling(ball);
        if (isFalling)
        {
            rb.useGravity = enable3DPhysics;
            rb.constraints = RigidbodyConstraints.None;
            return;
        }

        float restingY = GetRestingY(ball);
        bool nearRestingHeight = IsNearRestingHeight(ball);
        Vector3 velocity = rb.velocity;
        Vector3 planarVelocity = new Vector3(velocity.x, 0f, velocity.z);
        float planarSpeed = planarVelocity.magnitude;
        bool fastPocketApproach = IsFastPocketApproach(ball, planarSpeed);

        if (!insidePocketReleaseZone &&
            rb.position.y > restingY + surfaceHopTolerance &&
            rb.velocity.y > 0f)
        {
            rb.position = new Vector3(rb.position.x, restingY, rb.position.z);
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            velocity = rb.velocity;
            planarVelocity = new Vector3(velocity.x, 0f, velocity.z);
            planarSpeed = planarVelocity.magnitude;
        }

        bool allowYLock = smartYLock &&
                          !insidePocketReleaseZone &&
                          !fastPocketApproach &&
                          (nearRestingHeight || !nearPocketOpening || Mathf.Abs(velocity.y) < pocketReleaseVerticalSpeed);

        bool onSurface = allowYLock && (nearRestingHeight || IsOnTableSurface(ball));

        if (onSurface)
        {
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezePositionY;

            if (Mathf.Abs(rb.position.y - restingY) > 0.0005f)
            {
                rb.position = new Vector3(rb.position.x, restingY, rb.position.z);
            }

            velocity.y = 0f;
            rb.velocity = new Vector3(velocity.x, 0f, velocity.z);
        }
        else
        {
            rb.useGravity = enable3DPhysics;
            rb.constraints = RigidbodyConstraints.None;
        }

        float speed = onSurface
            ? new Vector3(rb.velocity.x, 0f, rb.velocity.z).magnitude
            : rb.velocity.magnitude;

        if (speed <= stopSpeed)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
            return;
        }

        bool isSliding = speed > slideToRollSpeed;
        float frictionFactor = isSliding ? slidingFriction : rollingFriction;
        Vector3 adjustedVelocity = rb.velocity * frictionFactor;

        if (spinInfluence > 0f && !isSliding)
        {
            Vector3 angularVel = rb.angularVelocity;
            angularVel.y = 0f;

            if (angularVel.magnitude > 0.1f)
            {
                Vector3 spinForce = Vector3.Cross(angularVel, Vector3.up) * spinInfluence;
                adjustedVelocity += spinForce * Time.fixedDeltaTime;
            }
        }

        if (onSurface)
        {
            adjustedVelocity.y = 0f;
        }

        rb.velocity = adjustedVelocity;

        if (enableRealisticRolling)
        {
            ApplyRealisticRolling(ball, speed, isSliding);
        }

        rb.angularVelocity *= angularDamping;

        float stopThreshold = stopSpeed * stopSpeed;
        float currentSqrVel = onSurface
            ? new Vector3(rb.velocity.x, 0f, rb.velocity.z).sqrMagnitude
            : rb.velocity.sqrMagnitude;

        bool isSlow = currentSqrVel < stopThreshold && rb.angularVelocity.sqrMagnitude < stopThreshold;

        if (!stillFrames.ContainsKey(id))
        {
            stillFrames[id] = 0;
        }

        stillFrames[id] = isSlow ? stillFrames[id] + 1 : 0;

        if (stillFrames[id] >= 2)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }
    }

    bool IsBallFalling(Ball3D ball)
    {
        if (!ball || !ball.rb) return false;

        float currentY = ball.rb.position.y;
        float releaseY = Mathf.Max(fallingThreshold, GetRestingY(ball) - surfaceReleaseDepth);

        if (currentY < releaseY) return true;

        bool movingDown = ball.rb.velocity.y < -Mathf.Max(0.1f, pocketReleaseVerticalSpeed);
        if (movingDown && !IsNearRestingHeight(ball) && !IsOnTableSurface(ball))
        {
            return true;
        }

        return false;
    }

    bool IsOnTableSurface(Ball3D ball)
    {
        if (!ball || !ball.rb) return false;

        float currentY = ball.rb.position.y;
        float releaseY = Mathf.Max(fallingThreshold, GetRestingY(ball) - surfaceReleaseDepth);
        if (currentY < releaseY) return false;

        float ballRadius = GetBallRadius(ball);
        float rayStartOffset = Mathf.Max(ballRadius + 0.06f, 0.2f);
        float checkDistance = Mathf.Max((ballRadius * 2f) + surfaceReleaseDepth + 0.05f, 0.55f);
        Vector3 rayOrigin = ball.rb.position + (Vector3.up * rayStartOffset);

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, checkDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            Debug.DrawRay(rayOrigin, Vector3.down * checkDistance, Color.green);
            return hit.normal.y > 0.2f;
        }

        Debug.DrawRay(rayOrigin, Vector3.down * checkDistance, Color.red);
        return false;
    }

    bool IsNearPocketOpening(Ball3D ball)
    {
        if (!releaseYLockNearPockets || !ball) return false;
        if (pocketLayerMask == 0) return false;

        float probeRadius = pocketReleasePadding + GetBallRadius(ball);
        Vector3 probeCenter = ball.transform.position + Vector3.up * 0.02f;
        return Physics.CheckSphere(probeCenter, probeRadius, pocketLayerMask, QueryTriggerInteraction.Collide);
    }

    bool IsInsidePocketReleaseZone(Ball3D ball)
    {
        if (!releaseYLockNearPockets || !ball) return false;

        if (pocketTriggers == null || pocketTriggers.Length == 0)
        {
            CachePocketTriggers();
        }

        if (pocketTriggers == null) return false;

        foreach (PocketTrigger3D pocketTrigger in pocketTriggers)
        {
            if (!pocketTrigger || !pocketTrigger.isActiveAndEnabled) continue;
            if (pocketTrigger.ShouldReleaseTableSupport(ball)) return true;
        }

        return false;
    }

    bool IsFastPocketApproach(Ball3D ball, float planarSpeed)
    {
        if (!releaseYLockNearPockets || !ball) return false;
        if (planarSpeed < highSpeedPocketBypassSpeed) return false;

        if (pocketTriggers == null || pocketTriggers.Length == 0)
        {
            CachePocketTriggers();
        }

        if (pocketTriggers == null) return false;

        foreach (PocketTrigger3D pocketTrigger in pocketTriggers)
        {
            if (!pocketTrigger || !pocketTrigger.isActiveAndEnabled) continue;
            if (pocketTrigger.IsApproachingPocketMouth(ball, highSpeedPocketApproachPadding, highSpeedPocketApproachDot))
            {
                return true;
            }
        }

        return false;
    }

    void ApplyRealisticRolling(Ball3D ball, float speed, bool isSliding)
    {
        if (!ball || !ball.rb) return;

        Vector3 velocity = ball.rb.velocity;
        velocity.y = 0f;

        if (velocity.magnitude < 0.01f) return;

        float radius = GetBallRadius(ball);
        if (radius < 0.01f) radius = 0.25f;

        Vector3 rollAxis = Vector3.Cross(Vector3.up, velocity.normalized);
        float rollSpeed = speed / radius;

        if (isSliding)
        {
            ball.rb.angularVelocity = Vector3.Lerp(
                ball.rb.angularVelocity,
                rollAxis * rollSpeed * 0.7f,
                Time.fixedDeltaTime * 2f
            );
        }
        else
        {
            Vector3 targetAngular = rollAxis * rollSpeed;
            ball.rb.angularVelocity = Vector3.Lerp(
                ball.rb.angularVelocity,
                targetAngular,
                Time.fixedDeltaTime * 15f
            );
        }
    }

    void ClampUnexpectedRise(Rigidbody rb)
    {
        if (!rb || maxBallRiseSpeed <= 0f) return;
        if (rb.velocity.y <= maxBallRiseSpeed) return;

        rb.velocity = new Vector3(rb.velocity.x, maxBallRiseSpeed, rb.velocity.z);
    }

    void CheckAllBallsStopped()
    {
        bool allStopped = BallsStopped();

        if (allStopped && !allBallsWereStopped)
        {
            allBallsWereStopped = true;
            if (gameState) gameState.OnAllBallsStopped();
        }
        else if (!allStopped)
        {
            allBallsWereStopped = false;
        }
    }

    bool BallsStopped()
    {
        float s2 = stopSpeed * stopSpeed;

        if (balls != null)
        {
            foreach (Ball3D ball in balls)
            {
                if (!ball || ball.inPocket) continue;
                if (!ball.rb || ball.rb.isKinematic) continue;
                if (IsBallFalling(ball)) continue;

                if (ball.rb.velocity.sqrMagnitude > s2) return false;
                if (ball.rb.angularVelocity.sqrMagnitude > s2) return false;
            }
        }

        if (cueBall && cueBall.gameObject.activeInHierarchy && !cueBall.inPocket)
        {
            if (cueBall.rb && !cueBall.rb.isKinematic && !IsBallFalling(cueBall))
            {
                if (cueBall.rb.velocity.sqrMagnitude > s2) return false;
                if (cueBall.rb.angularVelocity.sqrMagnitude > s2) return false;
            }
        }

        return true;
    }

    float GetRestingY(Ball3D ball)
    {
        return tableHeight;
    }

    bool IsNearRestingHeight(Ball3D ball)
    {
        if (!ball) return false;

        float currentY = ball.rb ? ball.rb.position.y : ball.transform.position.y;
        return Mathf.Abs(currentY - GetRestingY(ball)) <= surfaceSnapTolerance;
    }

    float GetBallRadius(Ball3D ball)
    {
        if (!ball) return 0.25f;

        SphereCollider sphereCollider = ball.GetComponent<SphereCollider>();
        if (!sphereCollider) return 0.25f;

        float maxScale = Mathf.Max(
            ball.transform.lossyScale.x,
            Mathf.Max(ball.transform.lossyScale.y, ball.transform.lossyScale.z)
        );

        return sphereCollider.radius * maxScale;
    }
}
