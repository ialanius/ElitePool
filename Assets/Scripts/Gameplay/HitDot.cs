using UnityEngine;

public class HitDotGlow : MonoBehaviour
{
    public float pulseSpeed = 6f;
    public float minScale = 0.9f;
    public float maxScale = 1.15f;

    Vector3 baseScale;

    void Awake()
    {
        baseScale = transform.localScale;
    }

    void Update()
    {
        float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
        transform.localScale = baseScale * Mathf.Lerp(minScale, maxScale, t);
    }
}
