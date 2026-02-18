using System.Collections.Generic;
using UnityEngine;

public class AimLine3D : MonoBehaviour
{
    [Header("References")]
    public Transform cueBall;
    public LineRenderer lineRenderer;
    public GameStateManager gameState;

    [Header("Settings")]
    public float lineLength = 5f;
    public float lineWidth = 0.02f;
    public Color mainLineColor = Color.white;
    public Color targetPathColor = Color.cyan;
    public float ballRadius = 0.14f; // نصف قطر الكرة (تأكد من هذا الرقم في لعبتك)

    [Header("Visuals")]
    public GameObject ghostBallPrefab;   // ضع بريفاب كرة شفافة هنا
    private GameObject ghostBallInstance;

    [Header("Secondary Lines")]
    public LineRenderer bounceLine;      // خط ارتداد الكرة البيضاء
    public LineRenderer targetBallPath;  // مسار الكرة الهدف

    [Header("Masks")]
    public LayerMask ballMask;
    public LayerMask wallMask;
    public LayerMask pocketMask;

    private Vector3 aimDirection = Vector3.right;

    void Awake()
    {
        SetupRenderers();
        if (ghostBallPrefab)
        {
            ghostBallInstance = Instantiate(ghostBallPrefab);
            ghostBallInstance.SetActive(false);
            // إزالة الكولايدر من كرة الشبح لتجنب المشاكل
            if (ghostBallInstance.GetComponent<Collider>()) Destroy(ghostBallInstance.GetComponent<Collider>());
        }
    }

    void SetupRenderers()
    {
        if (!lineRenderer) lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.startWidth = lineWidth; lineRenderer.endWidth = lineWidth;

        if (!bounceLine) bounceLine = CreateLine("BounceLine", mainLineColor);
        if (!targetBallPath) targetBallPath = CreateLine("TargetBallPath", targetPathColor);
    }

    LineRenderer CreateLine(string name, Color col)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform);
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.startWidth = lineWidth; lr.endWidth = lineWidth;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = col; lr.endColor = col;
        lr.enabled = false;
        return lr;
    }

    public void SetAimDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.001f) return;
        aimDirection = direction.normalized;
        aimDirection.y = 0f;
    }

    public void RenderLine()
    {
        if (!cueBall || !lineRenderer) return;

        HideAll();

        Vector3 startPos = cueBall.position;
        Vector3 endPos = startPos + aimDirection * lineLength;

        RaycastHit hit;
        int layerMask = ballMask | wallMask | pocketMask;

        // ✅ SphereCast بدلاً من Raycast لمحاكاة حجم الكرة الحقيقي
        if (Physics.SphereCast(startPos, ballRadius, aimDirection, out hit, lineLength, layerMask))
        {
            endPos = startPos + aimDirection * hit.distance;

            // 1. اصطدام بكرة
            if (((1 << hit.collider.gameObject.layer) & ballMask) != 0)
            {
                Ball3D targetBall = hit.collider.GetComponent<Ball3D>();
                if (targetBall && targetBall.type != BallType.Cue)
                {
                    HandleBallHit(hit, targetBall);
                }
            }
            // 2. اصطدام بحائط (ارتداد)
            else if (((1 << hit.collider.gameObject.layer) & wallMask) != 0)
            {
                HandleWallBounce(hit.point, hit.normal);
            }
        }

        // رسم الخط الرئيسي
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, endPos);
        lineRenderer.enabled = true;
    }

    void HandleBallHit(RaycastHit hit, Ball3D ball)
    {
        // حساب موقع "كرة الشبح" (المكان الذي ستكون فيه الكرة البيضاء لحظة الاصطدام)
        Vector3 impactDir = (hit.point - cueBall.position).normalized;
        // نرجع للخلف بمقدار نصف القطر لنحصل على مركز الكرة
        Vector3 ghostPos = hit.point + (hit.normal * ballRadius);
        // أو طريقة أدق: مركز الكرة الهدف + اتجاه الضرب * (2 * نصف القطر)
        Vector3 toBall = (ball.transform.position - hit.point).normalized;
        ghostPos = ball.transform.position - (toBall * (ballRadius * 2f));

        ghostPos.y = cueBall.position.y;

        // إظهار كرة الشبح
        if (ghostBallInstance)
        {
            ghostBallInstance.transform.position = ghostPos;
            ghostBallInstance.transform.rotation = Quaternion.LookRotation(toBall);
            ghostBallInstance.SetActive(true);
        }

        // رسم مسار الكرة الهدف (إلى أين ستذهب؟)
        // الكرة الملونة ستتحرك في خط مستقيم يربط بين مركز الكرة البيضاء ومركز الكرة الملونة لحظة التصادم
        Vector3 targetMoveDir = (ball.transform.position - ghostPos).normalized;

        if (targetBallPath)
        {
            targetBallPath.positionCount = 2;
            targetBallPath.SetPosition(0, ball.transform.position);
            targetBallPath.SetPosition(1, ball.transform.position + targetMoveDir * 3f); // طول الخط 3 متر
            targetBallPath.enabled = true;
        }

        // (اختياري) رسم ارتداد الكرة البيضاء بعد ضرب الكرة
        // الكرة البيضاء ترتد بزاوية 90 درجة تقريباً عن مسار الكرة الملونة (Tangent Line)
        Vector3 cueReflectDir = Vector3.Cross(targetMoveDir, Vector3.up); // اتجاه تقريبي
        // تحتاج حسابات فيزيائية معقدة للدقة التامة، لكن هذا يكفي للتوجيه
    }

    void HandleWallBounce(Vector3 hitPoint, Vector3 normal)
    {
        Vector3 incoming = aimDirection;
        Vector3 reflect = Vector3.Reflect(incoming, normal).normalized;

        if (bounceLine)
        {
            bounceLine.positionCount = 2;
            bounceLine.SetPosition(0, hitPoint);
            bounceLine.SetPosition(1, hitPoint + reflect * 2.0f); // طول خط الارتداد
            bounceLine.enabled = true;
        }
    }

    public void HideAll()
    {
        if (lineRenderer) lineRenderer.enabled = false;
        if (bounceLine) bounceLine.enabled = false;
        if (targetBallPath) targetBallPath.enabled = false;
        if (ghostBallInstance) ghostBallInstance.SetActive(false);
    }

    // لإخفاء الخط عند انتهاء الدور
    public void Hide()
    {
        HideAll();
    }

    // دالة لتحديث قوة الخط (اختياري لتغيير اللون حسب القوة)
    public void SetPower01(float p) { }
}