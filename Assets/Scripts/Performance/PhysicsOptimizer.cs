using UnityEngine;

/// <summary>
/// Physics Optimizer - Optimizes physics settings for better performance
/// Put this on a GameObject in your first scene
/// </summary>
public class PhysicsOptimizer : MonoBehaviour
{
    [Header("Physics Settings")]
    [Tooltip("Physics update rate (lower = better performance, higher = more accurate)")]
    [Range(30, 120)]
    public int physicsFrameRate = 50;

    [Tooltip("Sleep threshold - balls stop faster")]
    [Range(0.001f, 0.01f)]
    public float sleepThreshold = 0.005f;

    [Tooltip("Default contact offset")]
    [Range(0.001f, 0.02f)]
    public float contactOffset = 0.01f;

    [Tooltip("Maximum angular velocity for balls")]
    [Range(7f, 100f)]
    public float maxAngularVelocity = 50f;

    [Header("Ball Optimization")]
    [Tooltip("Find all balls automatically")]
    public bool autoFindBalls = true;

    [Tooltip("Manual ball references (if not auto)")]
    public Rigidbody[] ballRigidbodies;

    [Tooltip("Stop speed - balls below this speed will stop")]
    [Range(0.01f, 0.1f)]
    public float stopSpeed = 0.02f;

    [Header("Advanced Settings")]
    [Tooltip("Solver iterations (lower = faster, higher = more stable)")]
    [Range(4, 12)]
    public int solverIterations = 6;

    [Tooltip("Solver velocity iterations")]
    [Range(1, 8)]
    public int solverVelocityIterations = 1;

    [Tooltip("Continuous collision detection")]
    public bool useContinuousCollision = false;

    [Header("Debug")]
    public bool showDebugInfo = true;

    void Awake()
    {
        ApplyPhysicsSettings();
        OptimizeBalls();
    }

    void ApplyPhysicsSettings()
    {
        // Fixed timestep (physics frame rate)
        Time.fixedDeltaTime = 1f / physicsFrameRate;
        Time.maximumDeltaTime = 0.1f; // Cap to prevent spiral of death

        // Physics settings
        Physics.defaultContactOffset = contactOffset;
        Physics.sleepThreshold = sleepThreshold;

        // Solver iterations
        Physics.defaultSolverIterations = solverIterations;
        Physics.defaultSolverVelocityIterations = solverVelocityIterations;

        // Auto simulation
        Physics.simulationMode = SimulationMode.FixedUpdate;

        if (showDebugInfo)
        {
            Debug.Log("[Physics] Optimized settings applied:");
            Debug.Log("  Fixed Delta Time: " + Time.fixedDeltaTime);
            Debug.Log("  Sleep Threshold: " + sleepThreshold);
            Debug.Log("  Solver Iterations: " + solverIterations);
        }
    }

    void OptimizeBalls()
    {
        Rigidbody[] balls = ballRigidbodies;

        // Auto find balls if needed
        if (autoFindBalls || balls == null || balls.Length == 0)
        {
            Ball3D[] ballScripts = FindObjectsOfType<Ball3D>();
            balls = new Rigidbody[ballScripts.Length];

            for (int i = 0; i < ballScripts.Length; i++)
            {
                balls[i] = ballScripts[i].GetComponent<Rigidbody>();
            }

            if (showDebugInfo)
                Debug.Log("[Physics] Found " + balls.Length + " balls");
        }

        // Optimize each ball
        foreach (var rb in balls)
        {
            if (!rb) continue;

            // Sleep settings
            rb.sleepThreshold = sleepThreshold;

            // Max angular velocity
            rb.maxAngularVelocity = maxAngularVelocity;

            // Collision detection
            if (useContinuousCollision)
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            else
                rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

            // Interpolation for smooth movement
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        if (showDebugInfo)
            Debug.Log("[Physics] Ball optimization complete");
    }

    // Call this if you spawn new balls during gameplay
    public void RefreshBallOptimization()
    {
        OptimizeBalls();
    }
}