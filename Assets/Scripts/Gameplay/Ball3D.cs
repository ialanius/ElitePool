using UnityEngine;

public enum BallType { Cue, Solid, Stripe, Eight }

[RequireComponent(typeof(Rigidbody))]
public class Ball3D : MonoBehaviour
{
    public int id;
    public BallType type;
    public int number;
    public bool inPocket;

    public Rigidbody rb { get; private set; }

    private bool hasReportedFirstHit = false;

    // متغيرات الصوت
    public AudioSource rollingSource;
    public float maxRollSpeed = 10f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // ❌ تم حذف الأسطر التي تلغي الجاذبية وتجمد السقوط.
        // ✅ الآن الكرة ستأخذ إعداداتها الفيزيائية من الـ Inspector مباشرة!
    }

    void Update()
    {
        if (rollingSource)
        {
            float speed = rb.velocity.magnitude;

            // إذا كانت السرعة قليلة جداً، نوقف الصوت
            if (speed < 0.1f)
            {
                rollingSource.volume = 0;
                if (rollingSource.isPlaying) rollingSource.Pause();
            }
            else
            {
                if (!rollingSource.isPlaying) rollingSource.Play();

                // جعل الصوت يعلو وينخفض حسب السرعة
                rollingSource.volume = Mathf.Clamp01(speed / maxRollSpeed);

                // تغيير الـ Pitch قليلاً ليعطي واقعية (كلما أسرعت، زادت حدة الصوت)
                rollingSource.pitch = 0.8f + (speed / maxRollSpeed) * 0.4f;
            }
        }
    }

    public bool IsMoving(float t = 0.03f)
    {
        if (!rb) return false;
        Vector3 v = rb.velocity; v.y = 0f;
        return (v.magnitude > t) || (rb.angularVelocity.magnitude > t);
    }

    public void Shoot(Vector3 dir, float power)
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        if (!rb) return;

        rb.isKinematic = false;
        rb.WakeUp();

        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        rb.AddForce(dir.normalized * power, ForceMode.VelocityChange);

        // صفّر التتبع لأول ضربة
        hasReportedFirstHit = false;
    }

    // تتبع الاصطدامات
    void OnCollisionEnter(Collision collision)
    {
        // 1) لو الكرة البيضاء ضربت كرة ثانية
        if (type == BallType.Cue && !hasReportedFirstHit)
        {
            Ball3D otherBall = collision.gameObject.GetComponent<Ball3D>();
            if (otherBall && otherBall.type != BallType.Cue)
            {
                hasReportedFirstHit = true;

                if (GameStateManager.Instance)
                {
                    GameStateManager.Instance.OnCueBallHitBall(otherBall);
                }
            }
        }

        // 2) لو أي كرة ضربت جدار (Cushion/Wall)
        if (collision.gameObject.CompareTag("Wall") ||
            collision.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            if (GameStateManager.Instance)
            {
                GameStateManager.Instance.OnBallHitCushion(this);
            }
        }
    }
}