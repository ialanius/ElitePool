using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BallAudio : MonoBehaviour
{
    [Header("Ball vs Ball Sounds")]
    public AudioClip hardHitSound;
    public AudioClip mediumHitSound;
    public AudioClip softHitSound;

    [Header("Ball vs Wall Sound")]
    public AudioClip wallHitSound; // ✅ ضع ملف صوت الجدار هنا

    [Header("Settings")]
    public float minVelForSoft = 0.5f;
    public float minVelForMedium = 3.0f;
    public float minVelForHard = 10.0f;

    [Header("Volume & Pitch")]
    public float baseVolume = 1.0f;
    public bool randomizePitch = true;
    [Range(0.9f, 1.1f)] public float minPitch = 0.95f;
    [Range(0.9f, 1.1f)] public float maxPitch = 1.05f;

    private AudioSource audioSource;
    private float lastSoundTime;
    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1.0f;
    }

    void OnCollisionEnter(Collision collision)
    {
        float impactSpeed = collision.relativeVelocity.magnitude;
        if (impactSpeed < minVelForSoft) return;
        if (Time.time - lastSoundTime < 0.05f) return;

        // ✅ 1. فحص هل الاصطدام مع جدار؟
        if (collision.gameObject.CompareTag("Wall") ||
            collision.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            if (wallHitSound)
            {
                PlaySound(wallHitSound, impactSpeed, 0.5f); // 0.5f لأن صوت الجدار عادة أهدأ
            }
            return; // نخرج من الدالة عشان ما يشغل صوت الكرات
        }

        // ✅ 2. اصطدام مع كرة أخرى (نفس المنطق القديم)
        if (collision.gameObject.GetComponent<Ball3D>())
        {
            AudioClip clipToPlay = null;
            float vol = 1f;

            if (impactSpeed >= minVelForHard) { clipToPlay = hardHitSound; vol = 1.0f; }
            else if (impactSpeed >= minVelForMedium) { clipToPlay = mediumHitSound; vol = 0.8f; }
            else { clipToPlay = softHitSound; vol = 0.4f; }

            if (clipToPlay) PlaySound(clipToPlay, impactSpeed, vol);
        }
    }

    void PlaySound(AudioClip clip, float speed, float volScale)
    {
        // ✅✅ الإضافة هنا في البداية:
        // إذا كان مصدر الصوت غير موجود أو معطل، أو الكرة مخفية -> اخرج فوراً
        if (!audioSource || !audioSource.isActiveAndEnabled || !gameObject.activeInHierarchy)
            return;

        if (randomizePitch)
            audioSource.pitch = Random.Range(minPitch, maxPitch);

        // تعديل الصوت قليلاً بناءً على السرعة لواقعية أكثر
        float dynamicVol = Mathf.Clamp01(speed / 15f) * 0.5f + 0.5f;

        audioSource.PlayOneShot(clip, volScale * baseVolume * dynamicVol);
        lastSoundTime = Time.time;
    }
}