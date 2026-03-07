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
    [Tooltip("Enable true 3D physics (balls can fall in pockets)")]
    public bool enable3DPhysics = true;

    [Tooltip("Apply Y-lock only to balls on table (not falling)")]
    public bool smartYLock = true;

    [Tooltip("Table height - balls above this are 'on table'")]
    public float tableHeight = 0.5f;

    [Tooltip("Y threshold - balls below this are 'falling'")]
    public float fallingThreshold = 0.3f;

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
        var all = FindObjectsOfType<Ball3D>();
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
            foreach (var ball in balls)
                if (ball) ApplyEnhancedPhysics(ball);
        }

        if (cueBall && cueBall.gameObject.activeInHierarchy && !cueBall.inPocket)
            ApplyEnhancedPhysics(cueBall);

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
            foreach (var ball in balls)
            {
                if (ball && ball.rb)
                {
                    ConfigureBallPhysics(ball.rb);

                    // 💥 السر الاحترافي: "الفوضى المجهرية" (Micro-Chaos)
                    // نقوم بإزاحة كل كرة بمسافة عشوائية ضئيلة جداً (2 مليمتر) لكسر الترتيب المثالي
                    float randomX = UnityEngine.Random.Range(-0.002f, 0.002f);
                    float randomZ = UnityEngine.Random.Range(-0.002f, 0.002f);
                    ball.transform.position += new Vector3(randomX, 0f, randomZ);
                }
            }
        }

        if (cueBall && cueBall.rb)
        {
            ConfigureBallPhysics(cueBall.rb);
            // جعل الكرة البيضاء أثقل لتعمل كـ "مطرقة" تكسر المثلث
            cueBall.rb.mass = 0.25f;
        }
    }

    void ConfigureBallPhysics(Rigidbody rb)
    {
        // 🪶 وزن خفيف للكرات الملونة لتتطاير بسهولة
        rb.mass = 0.14f;
        rb.drag = 0.02f;
        rb.angularDrag = 0.05f;

        rb.useGravity = enable3DPhysics;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        PhysicMaterial ballMaterial = new PhysicMaterial("BallPhysics_Optimized");

        // 🧊 كرات زلقة جداً ومرنة للغاية
        ballMaterial.dynamicFriction = 0.01f;
        ballMaterial.staticFriction = 0.01f;
        ballMaterial.bounciness = 0.98f;

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

        bool isFalling = IsBallFalling(ball);
        if (isFalling)
        {
            rb.constraints = RigidbodyConstraints.None;
            return;
        }

        Vector3 velocity = rb.velocity;
        bool onSurface = smartYLock && IsOnTableSurface(ball);

        if (onSurface)
        {
            // 1. ✅ إطفاء الجاذبية تماماً لمنع ضغط الكرة على الأرضية (يقتل الاهتزاز)
            rb.useGravity = false;

            // 2. التجميد المطلق لمحرك الفيزياء
            rb.constraints = RigidbodyConstraints.FreezePositionY;

            velocity.y = 0f;
            rb.velocity = new Vector3(velocity.x, 0f, velocity.z);

            if (Mathf.Abs(ball.transform.position.y - tableHeight) > 0.001f)
            {
                Vector3 fixedPos = ball.transform.position;
                fixedPos.y = tableHeight;
                ball.transform.position = fixedPos;
            }
        }
        else
        {
            // ✅ إعادة تشغيل الجاذبية وفك القيود بمجرد أن تصبح الكرة فوق الجيب
            rb.useGravity = enable3DPhysics;
            rb.constraints = RigidbodyConstraints.None;
        }

        // تجاهل السرعة العمودية عند حساب سرعة التوقف لتجنب منع النوم بسبب الاهتزاز
        float speed = onSurface ? new Vector3(velocity.x, 0f, velocity.z).magnitude : velocity.magnitude;

        if (speed <= stopSpeed)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
            return;
        }

        bool isSliding = speed > slideToRollSpeed;
        float frictionFactor = isSliding ? slidingFriction : rollingFriction;
        velocity *= frictionFactor;

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

        if (onSurface)
        {
            rb.velocity = new Vector3(velocity.x, 0f, velocity.z);
        }
        else
        {
            rb.velocity = velocity;
        }

        if (enableRealisticRolling)
        {
            ApplyRealisticRolling(ball, speed, isSliding);
        }

        rb.angularVelocity *= angularDamping;

        // حساب السكون بناءً على المحاور الأفقية فقط إذا كانت الكرة على الطاولة
        float stopThreshold = stopSpeed * stopSpeed;
        float currentSqrVel = onSurface ? new Vector3(rb.velocity.x, 0f, rb.velocity.z).sqrMagnitude : rb.velocity.sqrMagnitude;

        bool isSlow = (currentSqrVel < stopThreshold) && (rb.angularVelocity.sqrMagnitude < stopThreshold);

        if (!stillFrames.ContainsKey(id)) stillFrames[id] = 0;
        stillFrames[id] = isSlow ? stillFrames[id] + 1 : 0;

        if (stillFrames[id] >= 2)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep(); // تنويم الكرة إجبارياً
        }
    }

    bool IsBallFalling(Ball3D ball)
    {
        if (!ball || !ball.rb) return false;
        if (ball.transform.position.y < fallingThreshold) return true;
        if (ball.rb.velocity.y < -0.2f && !IsOnTableSurface(ball)) return true;
        return false;
    }

    /// <summary>
    /// ✅ دالة الليزر الخفي: تكتشف هل الكرة فوق الطاولة أم فوق الجيب
    /// </summary>
    bool IsOnTableSurface(Ball3D ball)
    {
        if (!ball) return false;

        if (ball.transform.position.y < fallingThreshold) return false;

        // ✅ التعديل الأهم: نبدأ الشعاع من منطقة (أعلى) من مركز الكرة بقليل
        // لضمان أن الليزر يضرب الطاولة حتى لو غاصت الكرة نصفها تحت القماش!
        Vector3 rayOrigin = ball.transform.position + (Vector3.up * 0.15f);
        float checkDistance = 0.4f;

        bool hitSurface = Physics.Raycast(rayOrigin, Vector3.down, checkDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        if (hitSurface)
        {
            Debug.DrawRay(rayOrigin, Vector3.down * checkDistance, Color.green);
            return true;
        }
        else
        {
            Debug.DrawRay(rayOrigin, Vector3.down * checkDistance, Color.red);
            return false;
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

                if (IsBallFalling(ball)) continue;

                if (ball.rb.velocity.sqrMagnitude > s2) return false;
                if (ball.rb.angularVelocity.sqrMagnitude > s2) return false;
            }
        }

        if (cueBall && cueBall.gameObject.activeInHierarchy && !cueBall.inPocket)
        {
            if (cueBall.rb && !cueBall.rb.isKinematic)
            {
                if (!IsBallFalling(cueBall))
                {
                    if (cueBall.rb.velocity.sqrMagnitude > s2) return false;
                    if (cueBall.rb.angularVelocity.sqrMagnitude > s2) return false;
                }
            }
        }

        return true;
    }
}