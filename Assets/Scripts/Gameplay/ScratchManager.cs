using System.Collections;
using UnityEngine;

public class ScratchManager : MonoBehaviour
{
    [Header("Refs")]
    public Transform cueBall;
    public Camera cam;
    public Rigidbody[] allBalls;

    [Header("Masks")]
    public LayerMask tableMask;
    public LayerMask pocketMask;

    [Header("Ball Size / Stop")]
    public float ballRadius = 0.25f;
    public float stopSpeed = 0.05f;

    [Header("Kitchen Rules")]
    [Tooltip("تفعيل قاعدة Kitchen (المنطقة الصفراء) بعد البريك؟")]
    public bool useKitchenAfterBreak = true;

    [Tooltip("هل البريك الأول؟ (يتغير تلقائياً بعد أول ضربة)")]
    public bool afterBreak = true;

    [Header("Foul Line Settings")]
    [Tooltip("خط الفاول (FoulLine)")]
    public Transform foulLine;

    [Tooltip("إحداثي X لخط الفاول (محسوب بناء على حجم الطاولة الجديد)")]
    public float foulLineX = -5.1235f; // ✅ القيمة الصحيحة الجديدة

    [Tooltip("إحداثي Z لمركز الطاولة")]
    public float tableZ = -0.88f;      // ✅ القيمة الصحيحة لسنتر الطاولة

    [Tooltip("Kitchen في الجهة السالبة X؟")]
    public bool kitchenIsNegativeX = true;

    [Header("Ball in Hand Mode")]
    [Tooltip("السماح بوضع الكرة في أي مكان بعد الفاول؟ (مثل 8 Ball Pool)")]
    public bool allowFullTableBallInHand = true;

    public bool IsPlacing { get; private set; }

    float fixedCueY;
    Coroutine co;
    bool prevDetect;

    void Awake()
    {
        if (!cam) cam = Camera.main;
    }

    void Start()
    {
        if (cueBall) fixedCueY = cueBall.position.y;

        // ✅✅ ضبط مكان خط الفاول تلقائياً ليكون دقيقاً 100%
        if (foulLine)
        {
            Vector3 flPos = foulLine.position;
            flPos.x = foulLineX;  // يضع الخط في مكانه الصحيح (-5.1235)
            flPos.z = tableZ;     // يسنتر الخط في الطاولة (-0.88)
            foulLine.position = flPos;
        }
    }

    public void OnScratch(Transform cueBallTransform)
    {
        Haptics.Error(); // ✅ اهتزاز مميز للفاول (Scratch)
        if (IsPlacing) return;

        cueBall = cueBallTransform;

        if (co != null) StopCoroutine(co);
        co = StartCoroutine(BeginBallInHand());
    }

    public void ResetScratchManager()
    {
        if (co != null) StopCoroutine(co);
        co = null;

        IsPlacing = false;
        afterBreak = true;

        if (cueBall)
        {
            var rb = cueBall.GetComponent<Rigidbody>();
            if (rb) rb.detectCollisions = true;
        }
    }

    IEnumerator BeginBallInHand()
    {
        while (!BallsStopped()) yield return null;

        if (!cueBall) yield break;

        cueBall.gameObject.SetActive(true);

        var rb = cueBall.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = false;
            rb.WakeUp();
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();

            rb.isKinematic = true;

            prevDetect = rb.detectCollisions;
            rb.detectCollisions = false;
        }

        var b = cueBall.GetComponent<Ball3D>();
        if (b) b.inPocket = true;

        if (fixedCueY == 0f) fixedCueY = cueBall.position.y;

        IsPlacing = true;
    }

    void Update()
    {
        if (!IsPlacing || !cueBall || !cam) return;

        if (GetPointerHeld(out Vector2 p))
        {
            Ray ray = cam.ScreenPointToRay(p);

            int mask = (tableMask.value == 0) ? ~0 : tableMask.value;

            if (Physics.Raycast(ray, out RaycastHit hit, 200f, mask, QueryTriggerInteraction.Ignore))
            {
                Vector3 pos = hit.point;
                pos.y = fixedCueY;

                // تطبيق قاعدة Kitchen
                bool applyKitchen = false;

                if (useKitchenAfterBreak && afterBreak)
                {
                    applyKitchen = true;
                }
                else if (useKitchenAfterBreak && !allowFullTableBallInHand)
                {
                    applyKitchen = true;
                }

                // ✅ التحقق من الحدود باستخدام القيمة المحدثة
                if (applyKitchen)
                {
                    // نستخدم القيمة الرقمية المباشرة لضمان الدقة
                    float limitX = foulLineX;

                    pos.x = kitchenIsNegativeX ? Mathf.Min(pos.x, limitX)
                                              : Mathf.Max(pos.x, limitX);
                }

                cueBall.position = pos;
            }
        }

        if (GetPointerUp(out _))
        {
            if (OverlapsOtherBall()) return;
            if (InPocketArea()) return;
            Haptics.Success(); // ✅ اهتزاز تأكيد وضع الكرة في المكان الصحيح
            var rb = cueBall.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = false;
                rb.detectCollisions = prevDetect;
                rb.WakeUp();
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            var b = cueBall.GetComponent<Ball3D>();
            if (b) b.inPocket = false;

            IsPlacing = false;
        }
    }

    bool OverlapsOtherBall()
    {
        if (allBalls == null) return false;

        float minDist = ballRadius * 2f * 0.98f;

        foreach (var rb in allBalls)
        {
            if (!rb || !rb.gameObject.activeInHierarchy) continue;
            if (rb.transform == cueBall) continue;

            if (Vector3.Distance(rb.position, cueBall.position) < minDist)
                return true;
        }
        return false;
    }

    bool InPocketArea()
    {
        if (pocketMask.value == 0) return false;

        return Physics.CheckSphere(
            cueBall.position,
            ballRadius * 1.05f,
            pocketMask,
            QueryTriggerInteraction.Collide
        );
    }

    bool BallsStopped()
    {
        if (allBalls == null || allBalls.Length == 0) return true;

        float s2 = stopSpeed * stopSpeed;

        foreach (var rb in allBalls)
        {
            if (!rb) continue;
            if (!rb.gameObject.activeInHierarchy) continue;
            if (rb.isKinematic) continue;

            if (rb.velocity.sqrMagnitude > s2) return false;
            if (rb.angularVelocity.sqrMagnitude > s2) return false;
        }
        return true;
    }

    // ---- Input (Mouse + Touch) ----
    bool GetPointerHeld(out Vector2 pos)
    {
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
            { pos = t.position; return true; }
        }

        if (Input.GetMouseButton(0))
        { pos = Input.mousePosition; return true; }

        pos = default;
        return false;
    }

    bool GetPointerUp(out Vector2 pos)
    {
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            { pos = t.position; return true; }
        }

        if (Input.GetMouseButtonUp(0))
        { pos = Input.mousePosition; return true; }

        pos = default;
        return false;
    }
}