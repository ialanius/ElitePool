using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SpinController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("Spin UI")]
    public RectTransform spinCircle;        // الدائرة الخارجية
    public RectTransform spinDot;           // النقطة المتحركة
    public Image spinCircleImage;
    public Image spinDotImage;
    public float maxRadius = 50f;           // أقصى مسافة للنقطة

    [Header("Spin Values")]
    [Range(-1f, 1f)]
    public float verticalSpin = 0f;         // Top/Back spin (-1 = Back, +1 = Top)
    [Range(-1f, 1f)]
    public float horizontalSpin = 0f;       // Left/Right spin (-1 = Left, +1 = Right)

    [Header("Spin Force")]
    public float spinMultiplier = 5f;       // قوة تأثير الـSpin

    [Header("Visual Feedback")]
    public bool showSpinOnBall = true;
    public GameObject spinIndicatorPrefab;
    private GameObject spinIndicatorInstance;
    public Transform cueBall;

    [Header("Colors")]
    public Color topSpinColor = Color.green;
    public Color backSpinColor = Color.red;
    public Color sideSpinColor = Color.yellow;
    public Color centerColor = Color.white;

    [Header("Auto Reset")]
    public bool resetAfterShot = true;

    private Vector2 spinOffset = Vector2.zero;
    private bool isDragging = false;

    void Start()
    {
        if (!spinCircle || !spinDot)
        {
            Debug.LogError("⚠Spin UI elements not assigned!");
            enabled = false;
            return;
        }

        ResetSpin();
        SetupSpinIndicator();
    }

    void Update()
    {
        UpdateSpinVisual();

        if (showSpinOnBall && cueBall)
        {
            UpdateBallIndicator();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isDragging = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
        // اختياري: إعادة للمركز عند الإفلات
        // ResetSpin();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        // حساب الموقع النسبي
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            spinCircle,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint
        );

        // تقييد المسافة
        float distance = localPoint.magnitude;
        if (distance > maxRadius)
        {
            localPoint = localPoint.normalized * maxRadius;
        }

        spinOffset = localPoint;

        // حساب قيم الـSpin (-1 إلى 1)
        horizontalSpin = Mathf.Clamp(localPoint.x / maxRadius, -1f, 1f);
        verticalSpin = Mathf.Clamp(localPoint.y / maxRadius, -1f, 1f);

        // تحديث موقع النقطة
        spinDot.anchoredPosition = spinOffset;
    }

    void UpdateSpinVisual()
    {
        if (!spinDot || !spinDotImage) return;

        // تغيير لون النقطة حسب نوع الـSpin
        if (Mathf.Abs(verticalSpin) > Mathf.Abs(horizontalSpin))
        {
            // Top or Back spin
            spinDotImage.color = verticalSpin > 0 ? topSpinColor : backSpinColor;
        }
        else if (Mathf.Abs(horizontalSpin) > 0.1f)
        {
            // Side spin
            spinDotImage.color = sideSpinColor;
        }
        else
        {
            // Center
            spinDotImage.color = centerColor;
        }
    }

    void SetupSpinIndicator()
    {
        if (!showSpinOnBall || !cueBall) return;

        if (!spinIndicatorPrefab)
        {
            // إنشاء indicator بسيط
            spinIndicatorInstance = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            spinIndicatorInstance.name = "SpinIndicator";
            spinIndicatorInstance.transform.localScale = Vector3.one * 0.05f;

            Destroy(spinIndicatorInstance.GetComponent<Collider>());

            var renderer = spinIndicatorInstance.GetComponent<Renderer>();
            if (renderer)
            {
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.color = Color.red;
                renderer.material.EnableKeyword("_EMISSION");
                renderer.material.SetColor("_EmissionColor", Color.red);
            }
        }
        else
        {
            spinIndicatorInstance = Instantiate(spinIndicatorPrefab);
        }

        spinIndicatorInstance.transform.SetParent(cueBall);
    }

    void UpdateBallIndicator()
    {
        if (!spinIndicatorInstance || !cueBall) return;

        // حساب موقع النقطة على الكرة
        float ballRadius = 0.25f;

        // تحويل قيم الـSpin لموقع 3D
        Vector3 offset = new Vector3(
            horizontalSpin * ballRadius,
            0f,
            verticalSpin * ballRadius
        );

        spinIndicatorInstance.transform.localPosition = offset;
        spinIndicatorInstance.SetActive(true);
    }

    public void ResetSpin()
    {
        verticalSpin = 0f;
        horizontalSpin = 0f;
        spinOffset = Vector2.zero;

        if (spinDot)
        {
            spinDot.anchoredPosition = Vector2.zero;
        }

        if (spinIndicatorInstance)
        {
            spinIndicatorInstance.SetActive(false);
        }
    }

    public Vector3 GetSpinVector()
    {
        // إرجاع الـSpin كـVector3 للاستخدام في الفيزياء
        return new Vector3(horizontalSpin, 0f, verticalSpin) * spinMultiplier;
    }

    public void ApplySpin(Rigidbody rb, Vector3 shotDirection, float shotPower)
    {
        if (!rb) return;

        // 1. Top/Back Spin (تأثير طولي)
        if (Mathf.Abs(verticalSpin) > 0.01f)
        {
            Vector3 spinAxis = Vector3.Cross(shotDirection, Vector3.up).normalized;
            float spinAmount = -verticalSpin * spinMultiplier * shotPower * 0.5f;
            rb.angularVelocity += spinAxis * spinAmount;
        }

        // 2. Side Spin (تأثير جانبي - English)
        if (Mathf.Abs(horizontalSpin) > 0.01f)
        {
            // دوران حول المحور العمودي
            float sideSpinAmount = horizontalSpin * spinMultiplier * shotPower * 0.3f;
            rb.angularVelocity += Vector3.up * sideSpinAmount;

            // انحراف جانبي طفيف
            Vector3 sideForce = Vector3.Cross(shotDirection, Vector3.up).normalized;
            rb.AddForce(sideForce * horizontalSpin * shotPower * 0.2f, ForceMode.Impulse);
        }

        Debug.Log($"Spin Applied: V={verticalSpin:F2}, H={horizontalSpin:F2}");

        // إعادة تعيين بعد الضربة
        if (resetAfterShot)
        {
            Invoke(nameof(ResetSpin), 0.5f);
        }
    }

    // دوال مساعدة للUI Buttons (اختياري)
    public void SetTopSpin() { verticalSpin = 1f; horizontalSpin = 0f; UpdateDotPosition(); }
    public void SetBackSpin() { verticalSpin = -1f; horizontalSpin = 0f; UpdateDotPosition(); }
    public void SetLeftSpin() { horizontalSpin = -1f; verticalSpin = 0f; UpdateDotPosition(); }
    public void SetRightSpin() { horizontalSpin = 1f; verticalSpin = 0f; UpdateDotPosition(); }
    public void SetCenterHit() { ResetSpin(); }

    void UpdateDotPosition()
    {
        spinOffset = new Vector2(horizontalSpin * maxRadius, verticalSpin * maxRadius);
        if (spinDot) spinDot.anchoredPosition = spinOffset;
    }
}