using UnityEngine;

/// <summary>
/// Ball Safety - Prevents balls from glitching, falling, or moving infinitely
/// Add this to each ball prefab or to the Ball3D script
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BallSafety : MonoBehaviour
{
    [Header("Velocity Limits")]
    [Tooltip("Maximum velocity (prevents balls from going too fast)")]
    public float maxVelocity = 14f;

    [Tooltip("Maximum angular velocity (prevents infinite spinning)")]
    public float maxAngularVelocity = 50f;

    [Tooltip("Maximum upward speed for normal table play")]
    public float maxUpwardVelocity = 0.12f;

    [Header("Auto Stop")]
    [Tooltip("Stop ball if below this speed")]
    [Range(0.01f, 0.1f)]
    public float stopThreshold = 0.02f;

    [Tooltip("Time to wait before auto-stopping slow balls")]
    public float stopDelay = 0.5f;

    private float slowTimer = 0f;

    [Header("Out of Bounds")]
    [Tooltip("Y position considered out of bounds")]
    public float outOfBoundsY = -5f;

    [Tooltip("Reset position if out of bounds")]
    public Vector3 resetPosition = new Vector3(0, 1, 0);

    [Tooltip("Auto-find reset position from initial position")]
    public bool useInitialPosition = true;

    [Header("Wall Collision")]
    [Tooltip("Maximum number of wall collisions before force stop")]
    public int maxWallCollisions = 50;

    private int wallCollisionCount = 0;
    private float wallCollisionResetTime = 5f;
    private float wallCollisionTimer = 0f;

    [Header("References")]
    private Rigidbody rb;
    private Vector3 initialPosition;
    private Ball3D ballScript;

    [Header("Debug")]
    public bool showDebugLogs = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        ballScript = GetComponent<Ball3D>();

        // Save initial position
        initialPosition = transform.position;

        if (useInitialPosition)
        {
            resetPosition = initialPosition;
        }

        // Apply limits
        if (rb)
        {
            rb.maxAngularVelocity = maxAngularVelocity;
        }
    }

    void FixedUpdate()
    {
        // ✅ التعديل الأهم: الخروج فوراً إذا كانت الكرة Kinematic
        if (!rb || rb.isKinematic) return;
        if (ballScript && ballScript.inPocket) return;

        // Limit velocity
        LimitVelocity();

        // Auto stop slow balls
        AutoStopSlowBalls();

        // Check out of bounds
        CheckOutOfBounds();

        // Reset wall collision counter
        UpdateWallCollisionTimer();
    }

    void LimitVelocity()
    {
        // Limit linear velocity
        if (rb.velocity.magnitude > maxVelocity)
        {
            rb.velocity = rb.velocity.normalized * maxVelocity;

            if (showDebugLogs)
                Debug.LogWarning("[BallSafety] Velocity clamped on " + gameObject.name);
        }

        // Limit angular velocity
        if (rb.angularVelocity.magnitude > maxAngularVelocity)
        {
            rb.angularVelocity = rb.angularVelocity.normalized * maxAngularVelocity;
        }

        if (rb.velocity.y > maxUpwardVelocity)
        {
            rb.velocity = new Vector3(rb.velocity.x, maxUpwardVelocity, rb.velocity.z);

            if (showDebugLogs)
                Debug.LogWarning("[BallSafety] Upward velocity clamped on " + gameObject.name);
        }
    }

    void AutoStopSlowBalls()
    {
        float speed = rb.velocity.magnitude;
        float angularSpeed = rb.angularVelocity.magnitude;

        // If moving very slowly
        if (speed < stopThreshold && angularSpeed < stopThreshold)
        {
            slowTimer += Time.fixedDeltaTime;

            // Stop after delay
            if (slowTimer >= stopDelay)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.Sleep(); // Put to sleep for performance

                slowTimer = 0f;

                if (showDebugLogs)
                    Debug.Log("[BallSafety] Ball auto-stopped: " + gameObject.name);
            }
        }
        else
        {
            slowTimer = 0f;
        }
    }

    void CheckOutOfBounds()
    {
        if (ballScript && ballScript.inPocket) return;

        // Check if ball fell below table
        if (transform.position.y < outOfBoundsY)
        {
            ResetBall();

            if (showDebugLogs)
                Debug.LogWarning("[BallSafety] Ball out of bounds! Resetting: " + gameObject.name);
        }

        // Check if ball is too far from table (X or Z)
        float maxDistance = 20f; // Adjust based on your table size
        if (Mathf.Abs(transform.position.x) > maxDistance ||
            Mathf.Abs(transform.position.z) > maxDistance)
        {
            ResetBall();

            if (showDebugLogs)
                Debug.LogWarning("[BallSafety] Ball too far! Resetting: " + gameObject.name);
        }
    }

    void UpdateWallCollisionTimer()
    {
        if (wallCollisionCount > 0)
        {
            wallCollisionTimer += Time.fixedDeltaTime;

            if (wallCollisionTimer >= wallCollisionResetTime)
            {
                wallCollisionCount = 0;
                wallCollisionTimer = 0f;
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (ballScript && ballScript.inPocket) return;

        // Count wall collisions
        if (collision.gameObject.CompareTag("Wall") ||
            collision.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            wallCollisionCount++;
            wallCollisionTimer = 0f;

            // Too many wall bounces - force stop
            if (wallCollisionCount >= maxWallCollisions)
            {
                ForceStop();

                if (showDebugLogs)
                    Debug.LogWarning("[BallSafety] Too many wall collisions! Force stop: " + gameObject.name);
            }
        }
    }

    public void ResetBall()
    {
        if (ballScript && ballScript.inPocket) return;

        // ✅ التأكد أنها ليست Kinematic قبل التصفير
        if (rb) rb.isKinematic = false;

        // Stop movement
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Reset position
        transform.position = resetPosition;

        // Reset rotation
        transform.rotation = Quaternion.identity;

        // Wake up rigidbody
        rb.WakeUp();

        // Reset counters
        wallCollisionCount = 0;
        wallCollisionTimer = 0f;
        slowTimer = 0f;

        // Reset ball script state if exists
        if (ballScript)
        {
            ballScript.inPocket = false;
        }
    }

    public void ForceStop()
    {
        // ✅ التحقق من الـ Kinematic
        if (rb && !rb.isKinematic)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }

        wallCollisionCount = 0;
        wallCollisionTimer = 0f;
        slowTimer = 0f;
    }

    // Public method to set new reset position
    public void SetResetPosition(Vector3 position)
    {
        resetPosition = position;
    }
}
