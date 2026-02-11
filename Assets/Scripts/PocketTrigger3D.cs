using UnityEngine;

// ✅ إضافة هذا السطر لضمان وجود AudioSource
[RequireComponent(typeof(AudioSource))]
public class PocketTrigger3D : MonoBehaviour
{
    [Header("Pocket Size")]
    public float pocketRadius = 0.65f;
    public float enterDepth = 0.12f;

    [Header("Rules")]
    public float minSpeed = 0.02f;
    [Range(0f, 1f)] public float dotThreshold = 0.35f;

    [Header("Center (optional)")]
    public Transform pocketCenter;

    // ✅✅✅ متغيرات الصوت الجديدة
    [Header("Audio")]
    public AudioClip[] pocketSounds; // قائمة الأصوات (يمكنك وضع أكثر من واحد)
    private AudioSource audioSource;

    void Awake()
    {
        if (!pocketCenter) pocketCenter = transform;

        // ✅ جلب مكون الصوت
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1.0f; // لجعل الصوت 3D يصدر من مكان الحفرة
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
        // ✅✅✅ تشغيل الصوت قبل إخفاء الكرة
        PlayPocketSound();

        ball.inPocket = true;

        if (ball.rb)
        {
            ball.rb.velocity = Vector3.zero;
            ball.rb.angularVelocity = Vector3.zero;
            ball.rb.isKinematic = true;
        }

        ball.gameObject.SetActive(false);

        if (GameStateManager.Instance)
        {
            GameStateManager.Instance.OnBallPocketed(ball);
        }

        if (ball.type == BallType.Cue)
        {
            var scratch = FindObjectOfType<ScratchManager>();
            if (scratch) scratch.OnScratch(ball.transform);
        }
    }

    // ✅ دالة الصوت الجديدة
    void PlayPocketSound()
    {
        if (pocketSounds == null || pocketSounds.Length == 0 || !audioSource) return;

        // اختيار مقطع عشوائي
        AudioClip clip = pocketSounds[Random.Range(0, pocketSounds.Length)];

        // تغيير الحدة قليلاً للتنويع
        audioSource.pitch = Random.Range(0.9f, 1.1f);

        // تشغيل الصوت
        audioSource.PlayOneShot(clip);
    }
}