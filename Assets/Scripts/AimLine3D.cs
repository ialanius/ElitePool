using System.Collections.Generic;
using UnityEngine;

public class AimLine3D : MonoBehaviour
{
    [Header("References")]
    public Transform cueBall;
    public LineRenderer lineRenderer;
    public GameStateManager gameState;

    [Header("Unified Settings")]
    public float lineLength = 4f;
    public float lineWidth = 0.03f;
    public Color lineColor = Color.white;

    [Header("Ball Size")]
    public float ballRadius = 0.25f;

    [Header("Visuals")]
    public GameObject ghostBallPrefab;
    private GameObject ghostBallInstance;
    public GameObject collisionPointPrefab;
    private GameObject collisionPointInstance;
    public bool showCollisionPoint = true;

    [Header("Bounce Settings")]
    public LineRenderer bounceLineRenderer;
    public bool showBounce = true;
    public float bounceLength = 2.0f;

    [Header("Target Ball Path")]
    public LineRenderer targetBallPathRenderer;
    public bool showTargetPath = true;
    public float targetPathLength = 2f;

    [Header("Masks")]
    public LayerMask ballMask;
    public LayerMask wallMask;
    public LayerMask pocketMask; // ✅ 1. ماسك جديد للجيوب
    public LayerMask boundsMask;

    private Vector3 aimDirection = Vector3.right;
    private Ball3D targetBall = null;

    void Awake()
    {
        if (!lineRenderer) lineRenderer = GetComponent<LineRenderer>();

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

    public void SetAimDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.00001f) return;
        aimDirection = direction.normalized;
        aimDirection.y = 0f;
    }

    public void SetPower01(float power) { }

    public void RenderLine()
    {
        if (!cueBall || !lineRenderer) return;

        // إخفاء العناصر
        HideBounce();
        HideGhostBall();
        HideCollisionPoint();
        HideTargetPath();

        Vector3 startPos = cueBall.position;
        List<Vector3> mainPoints = new List<Vector3>();
        mainPoints.Add(startPos);

        RaycastHit hit;
        // ✅ 2. دمج الماسكات (كرات + جدران + جيوب)
        int combinedMask = ballMask.value | wallMask.value | pocketMask.value | boundsMask.value;

        // ✅ 3. تفعيل Collide لاكتشاف الجيوب (لأنها غالباً Triggers)
        bool hitSomething = Physics.Raycast(startPos, aimDirection, out hit, lineLength, combinedMask, QueryTriggerInteraction.Collide);

        Vector3 endPoint;

        if (hitSomething)
        {
            endPoint = hit.point;
            mainPoints.Add(endPoint);

            // التحقق مما اصطدمنا به

            // أ) اصطدام بالجيب (الأولوية للتوقف)
            if (((1 << hit.collider.gameObject.layer) & pocketMask.value & boundsMask.value) != 0)
            {
                // ✅ توقف هنا! لا ترسم ارتداد ولا كرة شبح
                // الخط ينتهي عند فوهة الجيب فقط
            }
            // ب) اصطدام بكرة
            else if (((1 << hit.collider.gameObject.layer) & ballMask.value) != 0)
            {
                Ball3D ball = hit.collider.GetComponent<Ball3D>();
                if (ball && ball.type != BallType.Cue)
                {
                    HandleBallHit(hit, ball);
                }
            }
            // ج) اصطدام بجدار (ارتداد)
            else if (((1 << hit.collider.gameObject.layer) & wallMask.value) != 0)
            {
                if (showBounce)
                {
                    HandleWallBounce(hit.point, hit.normal, aimDirection);
                }
            }
        }
        else
        {
            // لا يوجد اصطدام
            endPoint = startPos + aimDirection * lineLength;
            mainPoints.Add(endPoint);
        }

        // رسم الخط
        lineRenderer.positionCount = mainPoints.Count;
        lineRenderer.SetPositions(mainPoints.ToArray());
        lineRenderer.enabled = true;
    }

    void HandleWallBounce(Vector3 hitPoint, Vector3 hitNormal, Vector3 incomingDir)
    {
        Vector3 reflectDir = Vector3.Reflect(incomingDir, hitNormal);
        reflectDir.y = 0;
        reflectDir.Normalize();

        List<Vector3> bouncePoints = new List<Vector3>();
        bouncePoints.Add(hitPoint);

        Vector3 startBounceRay = hitPoint + (reflectDir * 0.05f);
        RaycastHit hit2;

        // في الارتداد نفحص الكرات والجدران والجيوب أيضاً
        int combinedMask = ballMask.value | wallMask.value | pocketMask.value | boundsMask.value;

        if (Physics.Raycast(startBounceRay, reflectDir, out hit2, bounceLength, combinedMask, QueryTriggerInteraction.Collide))
        {
            bouncePoints.Add(hit2.point);
            // لو الارتداد دخل في جيب، يوقف عنده ولا يكمل
        }
        else
        {
            bouncePoints.Add(hitPoint + reflectDir * bounceLength);
        }

        if (bounceLineRenderer)
        {
            bounceLineRenderer.positionCount = bouncePoints.Count;
            bounceLineRenderer.SetPositions(bouncePoints.ToArray());
            bounceLineRenderer.enabled = true;
        }
    }

    void HandleBallHit(RaycastHit hit, Ball3D ball)
    {
        targetBall = ball;
        Vector3 impactNormal = (hit.point - ball.transform.position).normalized;
        Vector3 ghostPos = ball.transform.position + impactNormal * (ballRadius * 2f);
        ghostPos.y = cueBall.position.y;

        ShowGhostBall(ghostPos);
        if (showCollisionPoint) ShowCollisionPoint(hit.point);
        if (showTargetPath) ShowTargetBallPath(ball, hit.point);
    }

    // ================= HELPER SETUP FUNCTIONS =================

    void SetupLineRenderer()
    {
        if (!lineRenderer) return;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.useWorldSpace = true;
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
        bounceLineRenderer.startWidth = lineWidth;
        bounceLineRenderer.endWidth = lineWidth;
        bounceLineRenderer.useWorldSpace = true;
        bounceLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        bounceLineRenderer.startColor = lineColor;
        bounceLineRenderer.endColor = lineColor;
        bounceLineRenderer.enabled = false;
    }

    void SetupTargetPathRenderer()
    {
        if (!targetBallPathRenderer)
        {
            GameObject pathObj = new GameObject("TargetBallPath");
            pathObj.transform.SetParent(transform);
            targetBallPathRenderer = pathObj.AddComponent<LineRenderer>();
        }
        targetBallPathRenderer.startWidth = lineWidth;
        targetBallPathRenderer.endWidth = lineWidth;
        targetBallPathRenderer.useWorldSpace = true;
        targetBallPathRenderer.material = new Material(Shader.Find("Sprites/Default"));
        targetBallPathRenderer.startColor = lineColor;
        targetBallPathRenderer.endColor = lineColor;
        targetBallPathRenderer.enabled = false;
    }

    void SetupGhostBall()
    {
        if (ghostBallInstance) return;
        if (ghostBallPrefab != null)
        {
            ghostBallInstance = Instantiate(ghostBallPrefab);
            ghostBallInstance.transform.SetParent(transform);
            Destroy(ghostBallInstance.GetComponent<Collider>());
            ghostBallInstance.SetActive(false);
        }
    }

    void SetupCollisionPoint()
    {
        if (collisionPointInstance) return;
        if (collisionPointPrefab != null)
        {
            collisionPointInstance = Instantiate(collisionPointPrefab);
            collisionPointInstance.transform.SetParent(transform);
            Destroy(collisionPointInstance.GetComponent<Collider>());
            collisionPointInstance.SetActive(false);
        }
    }

    // ================= VISUAL HELPERS =================

    void ShowGhostBall(Vector3 position)
    {
        if (ghostBallInstance) { ghostBallInstance.transform.position = position; ghostBallInstance.SetActive(true); }
    }
    void HideGhostBall() { if (ghostBallInstance) ghostBallInstance.SetActive(false); }

    void ShowCollisionPoint(Vector3 point)
    {
        if (collisionPointInstance) { collisionPointInstance.transform.position = point; collisionPointInstance.SetActive(true); }
    }
    void HideCollisionPoint() { if (collisionPointInstance) collisionPointInstance.SetActive(false); }

    void HideBounce() { if (bounceLineRenderer) bounceLineRenderer.positionCount = 0; }

    void ShowTargetBallPath(Ball3D ball, Vector3 hitPoint)
    {
        if (!targetBallPathRenderer || !ball) return;
        Vector3 moveDir = (ball.transform.position - hitPoint).normalized;
        moveDir.y = 0;

        targetBallPathRenderer.positionCount = 2;
        targetBallPathRenderer.SetPosition(0, ball.transform.position);
        targetBallPathRenderer.SetPosition(1, ball.transform.position + moveDir * targetPathLength);
        targetBallPathRenderer.enabled = true;
    }
    void HideTargetPath() { if (targetBallPathRenderer) targetBallPathRenderer.enabled = false; }

    public void Hide()
    {
        if (lineRenderer) lineRenderer.enabled = false;
        HideBounce();
        HideGhostBall();
        HideCollisionPoint();
        HideTargetPath();
    }
}