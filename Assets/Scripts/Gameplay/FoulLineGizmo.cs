using UnityEngine;

[ExecuteAlways]
public class FoulLineGizmo : MonoBehaviour
{
    [Header("Refs")]
    public Renderer tableRenderer;     // Renderer للطاولة (Table)
    public bool startSideIsNegativeX = true; // جهة البداية عندك -X غالباً

    [Header("Settings")]
    [Range(0.05f, 0.45f)]
    public float foulFractionFromStart = 0.25f; // 1/4 الطاولة

    public float yOffset = 0.02f; // ارتفاع بسيط للرسم

    void OnEnable() => SnapToFoulLine();
    void OnValidate() => SnapToFoulLine();

    public void SnapToFoulLine()
    {
        if (!tableRenderer) return;

        Bounds b = tableRenderer.bounds;
        float length = b.size.x;                 // طول الطاولة على X
        float minX = b.min.x;
        float maxX = b.max.x;

        float foulX = startSideIsNegativeX
            ? minX + length * foulFractionFromStart
            : maxX - length * foulFractionFromStart;

        Vector3 p = transform.position;
        p.x = foulX;
        p.y = b.center.y + yOffset;
        p.z = b.center.z;
        transform.position = p;
    }

    void OnDrawGizmos()
    {
        if (!tableRenderer) return;

        Bounds b = tableRenderer.bounds;
        float halfZ = b.size.z * 0.5f;

        Vector3 a = new Vector3(transform.position.x, b.center.y + yOffset, b.center.z - halfZ);
        Vector3 c = new Vector3(transform.position.x, b.center.y + yOffset, b.center.z + halfZ);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(a, c);
    }
}
