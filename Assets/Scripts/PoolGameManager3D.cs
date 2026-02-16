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

    Dictionary<int, int> ignoreFrames = new Dictionary<int, int>();
    Dictionary<int, int> stillFrames = new Dictionary<int, int>();
    private bool allBallsWereStopped = false;

    void Awake() => Instance = this;

    void Start()
    {
        RefreshRefs();
        if (!gameState) gameState = GameStateManager.Instance;
        SetupEnhancedPhysics();
    }

    public void RefreshRefs()
    {
        // جلب الكرات من المشهد
        var all = FindObjectsOfType<Ball3D>();
        cueBall = all.FirstOrDefault(b => b && b.type == BallType.Cue);
        balls = all.Where(b => b && b.type != BallType.Cue).ToArray();

        // ✅ ربط مع GameState
        if (GameStateManager.Instance)
        {
            GameStateManager.Instance.RefreshBallReferences();
        }
    }

    void FixedUpdate()
    {
        // تطبيق الفيزياء على كل كرة موجودة
        if (balls != null)
        {
            foreach (var ball in balls)
                if (ball) ApplyEnhancedPhysics(ball);
        }

        if (cueBall && cueBall.gameObject.activeInHierarchy && !cueBall.inPocket)
            ApplyEnhancedPhysics(cueBall);

        CheckAllBallsStopped();
    }

    // ... (باقي دوال الفيزياء ApplyEnhancedPhysics و CheckAllBallsStopped اتركها كما هي في الكود السابق) ...
    // لقد قمت فقط بتعديل RefreshRefs و Start لضمان عدم التعارض

    // (انسخ باقي دوال الفيزياء من الكود السابق وضعها هنا)

    public void RegisterShot(Rigidbody rb)
    {
        if (!rb) return;
        ignoreFrames[rb.GetInstanceID()] = 3;
        rb.WakeUp();
    }

    void SetupEnhancedPhysics()
    {
        // نفس الكود السابق...
        if (balls != null)
        {
            foreach (var ball in balls)
            {
                if (ball && ball.rb)
                {
                    ConfigureBallPhysics(ball.rb);
                }
            }
        }
        if (cueBall && cueBall.rb) ConfigureBallPhysics(cueBall.rb);
    }

    void ConfigureBallPhysics(Rigidbody rb)
    {
        rb.mass = 0.17f;
        rb.drag = 0.05f;
        rb.angularDrag = 0.3f;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        PhysicMaterial ballMaterial = new PhysicMaterial("BallPhysics_Optimized");
        ballMaterial.dynamicFriction = 0.15f;
        ballMaterial.staticFriction = 0.15f;
        ballMaterial.bounciness = 0.85f;
        ballMaterial.frictionCombine = PhysicMaterialCombine.Minimum;
        ballMaterial.bounceCombine = PhysicMaterialCombine.Maximum;

        var collider = rb.GetComponent<Collider>();
        if (collider) collider.material = ballMaterial;
    }

    void ApplyEnhancedPhysics(Ball3D ball)
    {
        if (!ball || ball.inPocket) return;
        if (!ball.rb || ball.rb.isKinematic) return;

        Rigidbody rb = ball.rb;
        int id = rb.GetInstanceID();

        if (ignoreFrames.TryGetValue(id, out int left) && left > 0)
        {
            ignoreFrames[id] = left - 1;
            return;
        }

        Vector3 velocity = rb.velocity;
        velocity.y = 0f;
        float speed = velocity.magnitude;

        // ✅ توقف أسرع
        if (speed <= stopSpeed)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
            return;
        }

        bool isSliding = speed > slideToRollSpeed;

        // ✅ تطبيق الاحتكاك
        float frictionFactor = isSliding ? slidingFriction : rollingFriction;
        velocity *= frictionFactor;

        // Magnus effect (Spin)
        if (spinInfluence > 0f && !isSliding)
        {
            Vector3 angularVel = rb.angularVelocity;
            angularVel.y = 0f;
            if (angularVel.magnitude > 0.1f)
            {
                Vector3 spinForce = Vector3.Cross(angularVel, Vector3.up) * spinInfluence;
                velocity += spinForce * Time.fixedDeltaTime;
            }
        }

        rb.velocity = new Vector3(velocity.x, 0f, velocity.z);

        if (enableRealisticRolling)
        {
            ApplyRealisticRolling(ball, speed, isSliding);
        }

        // ✅ تخفيف الدوران الزاوي
        rb.angularVelocity *= angularDamping;

        // ✅ فحص التوقف
        float stopThreshold = stopSpeed * stopSpeed;
        bool isSlow = (velocity.sqrMagnitude < stopThreshold) &&
                      (rb.angularVelocity.sqrMagnitude < stopThreshold);

        if (!stillFrames.ContainsKey(id)) stillFrames[id] = 0;
        stillFrames[id] = isSlow ? stillFrames[id] + 1 : 0;

        // ✅ توقف بعد 2 فريمات (كان 3)
        if (stillFrames[id] >= 2)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }
    }

    void ApplyRealisticRolling(Ball3D ball, float speed, bool isSliding)
    {
        if (!ball || !ball.rb) return;

        Vector3 velocity = ball.rb.velocity;
        velocity.y = 0f;

        if (velocity.magnitude < 0.01f) return;

        float radius = 0.25f;
        var sphereCollider = ball.GetComponent<SphereCollider>();
        if (sphereCollider)
        {
            radius = sphereCollider.radius * Mathf.Max(
               ball.transform.lossyScale.x,
               Mathf.Max(ball.transform.lossyScale.y, ball.transform.lossyScale.z)
           );
        }

        if (radius < 0.01f) radius = 0.25f;

        if (isSliding)
        {
            Vector3 rollAxis = Vector3.Cross(Vector3.up, velocity.normalized);
            float rollSpeed = speed / radius;
            ball.rb.angularVelocity = Vector3.Lerp(ball.rb.angularVelocity, rollAxis * rollSpeed * 0.7f, Time.fixedDeltaTime * 2f);
        }
        else
        {
            Vector3 rollAxis = Vector3.Cross(Vector3.up, velocity.normalized);
            float rollSpeed = speed / radius;
            Vector3 targetAngular = rollAxis * rollSpeed;

            ball.rb.angularVelocity = Vector3.Lerp(
                ball.rb.angularVelocity,
                targetAngular,
                Time.fixedDeltaTime * 15f
            );
        }
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
            foreach (var ball in balls)
            {
                if (!ball || ball.inPocket) continue;
                if (!ball.rb || ball.rb.isKinematic) continue;

                if (ball.rb.velocity.sqrMagnitude > s2) return false;
                if (ball.rb.angularVelocity.sqrMagnitude > s2) return false;
            }
        }

        if (cueBall && cueBall.gameObject.activeInHierarchy && !cueBall.inPocket)
        {
            if (cueBall.rb && !cueBall.rb.isKinematic)
            {
                if (cueBall.rb.velocity.sqrMagnitude > s2) return false;
                if (cueBall.rb.angularVelocity.sqrMagnitude > s2) return false;
            }
        }

        return true;
    }
}