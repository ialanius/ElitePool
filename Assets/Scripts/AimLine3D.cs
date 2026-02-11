using System.Collections.Generic;
using UnityEngine;

public class AimLine3D : MonoBehaviour
{
    [Header("References")]
    public Transform cueBall;
    public LineRenderer lineRenderer;
    public GameStateManager gameState;

    [Header("Line Settings")]
    public float lineLength = 3f;
    public float lineWidth = 0.05f;
    public int segments = 50; // قللت العدد قليلاً لتحسين الأداء

    [Header("Line Color")]
    public Color lineColor = Color.white;

    [Header("Ball Size")]
    public float ballRadius = 0.25f;

    [Header("Ghost Ball (Custom Prefab)")]
    public GameObject ghostBallPrefab;
    private GameObject ghostBallInstance;

    [Header("Collision Point (Custom Prefab)")]
    public GameObject collisionPointPrefab;
    private GameObject collisionPointInstance;
    public bool showCollisionPoint = true;

    [Header("Target Ball Path")]
    public LineRenderer targetBallPathRenderer;
    public bool showTargetPath = true;
    public float targetPathLength = 2f;
    public Color targetPathColor = Color.white;

    [Header("Bounce Prediction")]
    public LineRenderer bounceLineRenderer;
    public bool showBounce = true;
    public int maxBounces = 2;
    public Color bounceColor = Color.white;

    [Header("Masks")]
    public LayerMask ballMask;
    public LayerMask wallMask;

    [Header("Performance")]
    public float checkDistance = 5f;

    private Vector3 aimDirection = Vector3.right;
    private Ball3D targetBall = null;

    void Awake()
    {
        if (!lineRenderer)
        {
            lineRenderer = GetComponent<LineRenderer>();
            if (!lineRenderer) lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        SetupLineRenderer();
        SetupBounceRenderer();
        SetupTargetPathRenderer();
        SetupGhostBall();
        SetupCollisionPoint();
    }

    void Start()
    {
        if (!gameState) gameState = GameStateManager.Instance;
    }

    // ... (Setup functions remain the same, skipped for brevity but keep them inside) ...
    // تأكد من نسخ دوال SetupLineRenderer, SetupBounceRenderer, إلخ من الكود السابق
    // سأكتب التعديلات المهمة في RenderLine فقط

    void SetupLineRenderer()
    {
        if (!lineRenderer) return;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.positionCount = 0;
        lineRenderer.useWorldSpace = true;
        lineRenderer.enabled = true;
        if (!lineRenderer.material || lineRenderer.material.name == "Default-Material")
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
    }

    void SetupBounceRenderer()
    {
        if (!bounceLineRenderer)
        {
            GameObject bounceObj = new GameObject("BounceLineRenderer");
            bounceObj.transform.SetParent(transform);
            bounceLineRenderer = bounceObj.AddComponent<LineRenderer>();
        }
        bounceLineRenderer.startWidth = lineWidth * 0.8f;
        bounceLineRenderer.endWidth = lineWidth * 0.8f;
        bounceLineRenderer.positionCount = 0;
        bounceLineRenderer.useWorldSpace = true;
        bounceLineRenderer.enabled = true;
        bounceLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        bounceLineRenderer.startColor = bounceColor;
        bounceLineRenderer.endColor = bounceColor;
    }

    void SetupTargetPathRenderer()
    {
        if (!targetBallPathRenderer)
        {
            GameObject pathObj = new GameObject("TargetBallPath");
            pathObj.transform.SetParent(transform);
            targetBallPathRenderer = pathObj.AddComponent<LineRenderer>();
        }
        targetBallPathRenderer.startWidth = lineWidth * 0.5f;
        targetBallPathRenderer.endWidth = lineWidth * 0.5f;
        targetBallPathRenderer.positionCount = 0;
        targetBallPathRenderer.useWorldSpace = true;
        targetBallPathRenderer.enabled = true;
        targetBallPathRenderer.material = new Material(Shader.Find("Sprites/Default"));
        targetBallPathRenderer.startColor = targetPathColor;
        targetBallPathRenderer.endColor = targetPathColor;
    }

    void SetupGhostBall()
    {
        if (ghostBallInstance) return;
        if (ghostBallPrefab != null)
        {
            ghostBallInstance = Instantiate(ghostBallPrefab);
            ghostBallInstance.name = "GhostBall_Visual";
            ghostBallInstance.transform.SetParent(transform);
            Collider col = ghostBallInstance.GetComponent<Collider>();
            if (col) Destroy(col);
            ghostBallInstance.SetActive(false);
        }
    }

    void SetupCollisionPoint()
    {
        if (collisionPointInstance) return;
        if (collisionPointPrefab != null)
        {
            collisionPointInstance = Instantiate(collisionPointPrefab);
            collisionPointInstance.name = "CollisionPoint_Visual";
            collisionPointInstance.transform.SetParent(transform);
            Collider col = collisionPointInstance.GetComponent<Collider>();
            if (col) Destroy(col);
            collisionPointInstance.SetActive(false);
        }
    }

    public void SetAimDirection(Vector3 direction)
    {
        // ✅ حماية من القيم الصفرية أو غير الصالحة
        if (direction.sqrMagnitude < 0.00001f || float.IsNaN(direction.x))
            return;

        aimDirection = direction.normalized;
        aimDirection.y = 0f;
    }

    public void SetPower01(float power) { }

    public void RenderLine()
    {
        if (!cueBall || !lineRenderer) return;

        // ✅ حماية إضافية: إذا كانت الكرة في مكان خطأ (NaN) لا ترسم شيئاً
        if (float.IsNaN(cueBall.position.x) || float.IsInfinity(cueBall.position.x))
        {
            Hide();
            return;
        }

        Vector3 startPos = cueBall.position;
        List<Vector3> mainLinePoints = new List<Vector3>();
        List<Vector3> bouncePoints = new List<Vector3>();

        RaycastHit firstHit;
        bool hitSomething = Physics.Raycast(startPos, aimDirection, out firstHit, checkDistance,
            ballMask.value | wallMask.value, QueryTriggerInteraction.Ignore);

        float distToHit = hitSomething ? firstHit.distance : lineLength;
        float visualLength = hitSomething ? Mathf.Max(0, distToHit - (ballRadius * 2f)) : lineLength;

        // ✅ حماية من الأرقام السالبة أو غير المنطقية
        if (visualLength < 0.01f) visualLength = 0.01f;

        int currentSegments = Mathf.Clamp(Mathf.CeilToInt(visualLength * 20), 2, 100);

        for (int i = 0; i <= currentSegments; i++)
        {
            float t = (float)i / currentSegments;
            Vector3 point = startPos + aimDirection * (visualLength * t);

            // ✅ فحص أخير قبل الإضافة للقائمة
            if (!float.IsNaN(point.x))
                mainLinePoints.Add(point);
        }

        lineRenderer.positionCount = mainLinePoints.Count;
        lineRenderer.SetPositions(mainLinePoints.ToArray());
        lineRenderer.enabled = true;
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;

        if (hitSomething && firstHit.collider)
        {
            Ball3D ball = firstHit.collider.GetComponent<Ball3D>();
            if (ball && ball.type != BallType.Cue)
            {
                targetBall = ball;

                Vector3 impactNormal = (firstHit.point - ball.transform.position).normalized;
                // ✅ حماية Normalization
                if (impactNormal == Vector3.zero) impactNormal = -aimDirection;

                Vector3 ghostPos = ball.transform.position + impactNormal * (ballRadius * 2f);
                ghostPos.y = cueBall.position.y;

                ShowGhostBall(ghostPos);
                if (showCollisionPoint) ShowCollisionPoint(firstHit.point);
                if (showTargetPath) ShowTargetBallPath(ball, firstHit.point);
            }
            else
            {
                HideGhostBall();
                HideCollisionPoint();
                HideTargetPath();
            }
        }
        else
        {
            HideGhostBall();
            HideCollisionPoint();
            HideTargetPath();
        }

        if (showBounce && hitSomething)
        {
            var wallCollider = firstHit.collider;
            if (wallCollider && (wallCollider.gameObject.CompareTag("Wall") ||
                wallCollider.gameObject.layer == LayerMask.NameToLayer("Wall")))
            {
                CalculateBounce(firstHit.point, aimDirection, bouncePoints, 0);
            }
        }

        if (bouncePoints.Count > 0)
        {
            bounceLineRenderer.positionCount = bouncePoints.Count;
            bounceLineRenderer.SetPositions(bouncePoints.ToArray());
            bounceLineRenderer.enabled = true;
            bounceLineRenderer.startColor = bounceColor;
            bounceLineRenderer.endColor = bounceColor;
        }
        else
        {
            bounceLineRenderer.positionCount = 0;
        }
    }

    void CalculateBounce(Vector3 hitPoint, Vector3 incomingDir, List<Vector3> points, int bounceCount)
    {
        if (bounceCount >= maxBounces) return;

        // إضافة إزاحة بسيطة جداً لمنع Raycast من داخل الجدار
        Vector3 startRay = hitPoint + incomingDir * -0.01f;

        RaycastHit hit;
        // نطلق الشعاع عكس الاتجاه قليلاً للتأكد من الحصول على الـ Normal الصحيح
        if (!Physics.Raycast(startRay, incomingDir, out hit, checkDistance, wallMask.value, QueryTriggerInteraction.Ignore))
            return;

        Vector3 reflectDir = Vector3.Reflect(incomingDir, hit.normal);
        reflectDir.y = 0f;
        reflectDir.Normalize();

        float bounceLength = lineLength * 0.6f;
        int bounceSegments = 20; // تقليل العدد للأداء

        for (int i = 0; i <= bounceSegments; i++)
        {
            float t = (float)i / bounceSegments;
            Vector3 p = hit.point + reflectDir * (bounceLength * t);
            if (!float.IsNaN(p.x)) points.Add(p);
        }

        CalculateBounce(hit.point, reflectDir, points, bounceCount + 1);
    }

    void ShowGhostBall(Vector3 position)
    {
        if (!ghostBallInstance) return;
        if (float.IsNaN(position.x)) return; // ✅ حماية
        ghostBallInstance.transform.position = position;
        ghostBallInstance.SetActive(true);
    }

    void HideGhostBall()
    {
        if (ghostBallInstance) ghostBallInstance.SetActive(false);
    }

    void ShowCollisionPoint(Vector3 point)
    {
        if (!collisionPointInstance) return;
        if (float.IsNaN(point.x)) return; // ✅ حماية
        collisionPointInstance.transform.position = point;
        collisionPointInstance.SetActive(true);
    }

    void HideCollisionPoint()
    {
        if (collisionPointInstance) collisionPointInstance.SetActive(false);
    }

    void ShowTargetBallPath(Ball3D ball, Vector3 hitPoint)
    {
        if (!targetBallPathRenderer || !ball) return;

        Vector3 diff = (ball.transform.position - hitPoint);
        if (diff.sqrMagnitude < 0.0001f) return; // ✅ حماية القسمة على صفر

        Vector3 moveDir = diff.normalized;
        moveDir.y = 0f;

        List<Vector3> pathPoints = new List<Vector3>();
        int pathSegments = 30;
        Vector3 startPath = ball.transform.position;

        for (int i = 0; i <= pathSegments; i++)
        {
            float t = (float)i / pathSegments;
            Vector3 p = startPath + moveDir * (targetPathLength * t);
            if (!float.IsNaN(p.x)) pathPoints.Add(p);
        }

        targetBallPathRenderer.positionCount = pathPoints.Count;
        targetBallPathRenderer.SetPositions(pathPoints.ToArray());
        targetBallPathRenderer.enabled = true;
        targetBallPathRenderer.startColor = targetPathColor;
        targetBallPathRenderer.endColor = targetPathColor;
    }

    void HideTargetPath()
    {
        if (targetBallPathRenderer) targetBallPathRenderer.positionCount = 0;
    }

    public void Hide()
    {
        if (lineRenderer) lineRenderer.positionCount = 0;
        if (bounceLineRenderer) bounceLineRenderer.positionCount = 0;
        HideGhostBall();
        HideCollisionPoint();
        HideTargetPath();
    }
}