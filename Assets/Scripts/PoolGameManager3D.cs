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

    [Header("Enhanced Physics")]
    [Tooltip("الاحتكاك - أقل = الكرات تتحرك أطول")]
    [Range(0.95f, 0.995f)]
    public float rollingFriction = 0.985f;          // ✅ أقل من قبل

    [Tooltip("الاحتكاك وقت الانزلاق")]
    [Range(0.90f, 0.995f)]
    public float slidingFriction = 0.975f;          // ✅ محسّن

    [Tooltip("سرعة الانتقال من انزلاق لدوران")]
    public float slideToRollSpeed = 1.8f;           // ✅ أقل

    [Tooltip("سرعة توقف الكرة")]
    public float stopSpeed = 0.025f;                // ✅ أقل

    [Tooltip("تأثير الدوران على الحركة")]
    [Range(0f, 0.5f)]
    public float spinInfluence = 0.15f;             // ✅ جديد

    [Tooltip("استمرارية الدوران")]
    [Range(0.95f, 0.999f)]
    public float angularDamping = 0.992f;           // ✅ جديد

    [Header("Ball Rolling Visual")]
    [Tooltip("تفعيل الدوران المرئي الواقعي")]
    public bool enableRealisticRolling = true;      // ✅ جديد

    Dictionary<int, int> stillFrames = new Dictionary<int, int>();
    Dictionary<int, int> ignoreFrames = new Dictionary<int, int>();

    public void RegisterShot(Rigidbody rb)
    {
        if (!rb) return;
        ignoreFrames[rb.GetInstanceID()] = 3;
        rb.WakeUp();
    }

    void Awake() => Instance = this;

    void Start()
    {
        RefreshRefs();
        if (!gameState) gameState = GameStateManager.Instance;

        // ✅ إعداد الفيزياء المحسنة
        SetupEnhancedPhysics();
    }

    public void RefreshRefs()
    {
        var all = FindObjectsOfType<Ball3D>();
        cueBall = all.FirstOrDefault(b => b && b.type == BallType.Cue);
        balls = all.Where(b => b && b.type != BallType.Cue).ToArray();

        if (gameState) gameState.RefreshBallReferences();
    }

    void SetupEnhancedPhysics()
    {
        // ✅ إعداد كل الكرات
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

        if (cueBall && cueBall.rb)
        {
            ConfigureBallPhysics(cueBall.rb);
        }
    }

    void ConfigureBallPhysics(Rigidbody rb)
    {
        // ✅ إعدادات محسنة للفيزياء
        rb.mass = 0.17f;                                    // وزن كرة البلياردو الحقيقي (كغم)
        rb.drag = 0f;                                       // نستخدم friction يدوي
        rb.angularDrag = 0f;                                // نستخدم damping يدوي
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;  // ✅ حركة سلسة
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // ✅ PhysicMaterial محسّن
        PhysicMaterial ballMaterial = new PhysicMaterial("BallPhysics");
        ballMaterial.dynamicFriction = 0.1f;                // احتكاك ديناميكي منخفض
        ballMaterial.staticFriction = 0.1f;                 // احتكاك ثابت منخفض
        ballMaterial.bounciness = 0.9f;                     // ارتداد قوي
        ballMaterial.frictionCombine = PhysicMaterialCombine.Minimum;
        ballMaterial.bounceCombine = PhysicMaterialCombine.Maximum;

        var collider = rb.GetComponent<Collider>();
        if (collider) collider.material = ballMaterial;
    }

    void FixedUpdate()
    {
        // طبّق على الكرات العادية
        if (balls != null)
            for (int i = 0; i < balls.Length; i++)
                ApplyEnhancedPhysics(balls[i]);

        // طبّق على البيضاء
        if (cueBall && cueBall.gameObject.activeInHierarchy)
        {
            if (!(scratch && scratch.IsPlacing) && !cueBall.inPocket)
                ApplyEnhancedPhysics(cueBall);
        }

        CheckAllBallsStopped();
    }

    void ApplyEnhancedPhysics(Ball3D ball)
    {
        if (!ball || ball.inPocket) return;
        if (!ball.rb || ball.rb.isKinematic) return;

        Rigidbody rb = ball.rb;
        int id = rb.GetInstanceID();

        // ✅ تجاهل أول فريمين بعد الضربة
        if (ignoreFrames.TryGetValue(id, out int left) && left > 0)
        {
            ignoreFrames[id] = left - 1;
            return;
        }

        Vector3 velocity = rb.velocity;
        velocity.y = 0f;
        float speed = velocity.magnitude;

        // ✅ Hard stop للكرات البطيئة جداً
        if (speed <= stopSpeed)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
            return;
        }

        // ✅ تحديد نوع الحركة (انزلاق أو دوران)
        bool isSliding = speed > slideToRollSpeed;
        float frictionFactor = isSliding ? slidingFriction : rollingFriction;

        // ✅ تطبيق الاحتكاك
        velocity *= frictionFactor;

        // ✅ تأثير الدوران على الحركة (Spin)
        if (spinInfluence > 0f && !isSliding)
        {
            Vector3 angularVel = rb.angularVelocity;
            angularVel.y = 0f; // فقط الدوران الأفقي

            // Magnus effect - الدوران يأثر على المسار
            if (angularVel.magnitude > 0.1f)
            {
                Vector3 spinForce = Vector3.Cross(angularVel, Vector3.up) * spinInfluence;
                velocity += spinForce * Time.fixedDeltaTime;
            }
        }

        rb.velocity = new Vector3(velocity.x, 0f, velocity.z);

        // ✅ دوران واقعي
        if (enableRealisticRolling)
        {
            ApplyRealisticRolling(ball, speed, isSliding);
        }

        // ✅ Angular damping
        rb.angularVelocity *= angularDamping;

        // ✅ Smart sleep
        float stopThreshold = stopSpeed * stopSpeed;
        bool isSlow = (velocity.sqrMagnitude < stopThreshold) &&
                      (rb.angularVelocity.sqrMagnitude < stopThreshold);

        if (!stillFrames.ContainsKey(id)) stillFrames[id] = 0;
        stillFrames[id] = isSlow ? stillFrames[id] + 1 : 0;

        if (stillFrames[id] >= 3)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }
    }

    // ✅ دوران واقعي للكرة
    void ApplyRealisticRolling(Ball3D ball, float speed, bool isSliding)
    {
        if (!ball || !ball.rb) return;

        Vector3 velocity = ball.rb.velocity;
        velocity.y = 0f;

        if (velocity.magnitude < 0.01f) return;

        // نصف قطر الكرة
        float radius = 0.028575f; // نصف قطر كرة البلياردو الحقيقي (متر)

        var sphereCollider = ball.GetComponent<SphereCollider>();
        if (sphereCollider)
        {
            radius = sphereCollider.radius * Mathf.Max(
                ball.transform.lossyScale.x,
                Mathf.Max(ball.transform.lossyScale.y, ball.transform.lossyScale.z)
            );
        }

        if (isSliding)
        {
            // وقت الانزلاق: دوران جزئي
            Vector3 rollAxis = Vector3.Cross(Vector3.up, velocity.normalized);
            float rollSpeed = speed / radius;
            ball.rb.angularVelocity = rollAxis * rollSpeed * 0.7f; // 70% فقط
        }
        else
        {
            // وقت الدوران: دوران كامل
            Vector3 rollAxis = Vector3.Cross(Vector3.up, velocity.normalized);
            float rollSpeed = speed / radius;

            // دوران يطابق السرعة تماماً (pure rolling)
            Vector3 targetAngular = rollAxis * rollSpeed;
            ball.rb.angularVelocity = Vector3.Lerp(
                ball.rb.angularVelocity,
                targetAngular,
                Time.fixedDeltaTime * 10f
            );
        }
    }

    private bool allBallsWereStopped = false;

    void CheckAllBallsStopped()
    {
        bool allStopped = BallsStopped();

        if (allStopped && !allBallsWereStopped)
        {
            allBallsWereStopped = true;

            if (gameState)
            {
                gameState.OnAllBallsStopped();
            }
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