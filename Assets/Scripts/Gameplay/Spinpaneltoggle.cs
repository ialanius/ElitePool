using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// نظام بسيط لفتح/إغلاق panel الـSpin
/// - يفتح لما تضغط الزر
/// - يقفل تلقائياً لما تختار spin
/// - بدون زر إغلاق
/// </summary>
public class SpinPanelToggle : MonoBehaviour
{
    [Header("🎯 الأساسيات")]
    public GameObject spinPanel;          // الـPanel اللي فيه دائرة الـSpin
    public Button openButton;             // الزر اللي يفتح الـPanel

    [Header("🎮 Spin Controller")]
    public MonoBehaviour spinController;  // SpinController أو SpinControllerAdvanced

    [Header("⚙️ إعدادات")]
    public bool autoHide = true;          // إخفاء تلقائي بعد الاختيار
    public float hideDelay = 0.3f;        // وقت الانتظار قبل الإخفاء

    private bool isOpen = false;

    void Start()
    {
        // تأكد إن كل شيء موجود
        if (!spinPanel)
        {
            Debug.LogError("❌ Spin Panel مو مربوط!");
            return;
        }

        if (!openButton)
        {
            Debug.LogError("❌ Open Button مو مربوط!");
            return;
        }

        // أخفِ الـPanel في البداية
        spinPanel.SetActive(false);
        isOpen = false;

        // ربط الزر بالدالة
        openButton.onClick.AddListener(TogglePanel);
    }

    /// <summary>
    /// فتح/إغلاق الـPanel
    /// </summary>
    public void TogglePanel()
    {
        if (isOpen)
        {
            ClosePanel();
        }
        else
        {
            OpenPanel();
        }
    }

    /// <summary>
    /// فتح الـPanel
    /// </summary>
    public void OpenPanel()
    {
        spinPanel.SetActive(true);
        isOpen = true;
        Debug.Log("✅ Spin Panel opened");
    }

    /// <summary>
    /// إغلاق الـPanel
    /// </summary>
    public void ClosePanel()
    {
        spinPanel.SetActive(false);
        isOpen = false;
        Debug.Log("✅ Spin Panel closed");
    }

    // ═══════════════════════════════════════════════════
    // 🎯 دوال للأزرار - اربطها على أزرار الـSpin
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// Top Spin - ربط هذا على زر Top
    /// </summary>
    public void OnTopSpin()
    {
        // استدعي الدالة من SpinController
        if (spinController != null)
        {
            spinController.SendMessage("SetTopSpin", SendMessageOptions.DontRequireReceiver);
        }

        // أخفِ الـPanel
        if (autoHide)
        {
            Invoke(nameof(ClosePanel), hideDelay);
        }
    }

    /// <summary>
    /// Back Spin - ربط هذا على زر Back
    /// </summary>
    public void OnBackSpin()
    {
        if (spinController != null)
        {
            spinController.SendMessage("SetBackSpin", SendMessageOptions.DontRequireReceiver);
        }

        if (autoHide)
        {
            Invoke(nameof(ClosePanel), hideDelay);
        }
    }

    /// <summary>
    /// Left Spin - ربط هذا على زر Left
    /// </summary>
    public void OnLeftSpin()
    {
        if (spinController != null)
        {
            spinController.SendMessage("SetLeftSpin", SendMessageOptions.DontRequireReceiver);
        }

        if (autoHide)
        {
            Invoke(nameof(ClosePanel), hideDelay);
        }
    }

    /// <summary>
    /// Right Spin - ربط هذا على زر Right
    /// </summary>
    public void OnRightSpin()
    {
        if (spinController != null)
        {
            spinController.SendMessage("SetRightSpin", SendMessageOptions.DontRequireReceiver);
        }

        if (autoHide)
        {
            Invoke(nameof(ClosePanel), hideDelay);
        }
    }

    /// <summary>
    /// Center Hit - ربط هذا على زر Center
    /// </summary>
    public void OnCenterHit()
    {
        if (spinController != null)
        {
            spinController.SendMessage("SetCenterHit", SendMessageOptions.DontRequireReceiver);
        }

        if (autoHide)
        {
            Invoke(nameof(ClosePanel), hideDelay);
        }
    }
}