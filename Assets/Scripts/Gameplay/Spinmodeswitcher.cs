using UnityEngine;
using UnityEngine.UI;

public class SpinModeSwitcher : MonoBehaviour
{
    [Header("Spin Modes")]
    public GameObject dragMode;      // SpinCircle (الدائرة)
    public GameObject buttonsMode;   // QuickButtons (الأزرار الخمسة)
    public GameObject spinDot;       // النقطة الحمراء

    [Header("Toggle Buttons")]
    public Button dragModeButton;    // زر "Drag Mode"
    public Button buttonsModeButton; // زر "Buttons"

    [Header("Visual Feedback")]
    public Color activeColor = Color.white;
    public Color inactiveColor = Color.gray;

    private bool isDragMode = true;

    void Start()
    {
        // 1. تفعيل الوضع الافتراضي
        SetDragMode();

        // 2. ربط الأزرار برمجياً (تأكد أن الأزرار مربوطة في الـ Inspector)
        if (dragModeButton)
        {
            dragModeButton.onClick.RemoveAllListeners(); // تنظيف لضمان عدم التكرار
            dragModeButton.onClick.AddListener(SetDragMode);
        }

        if (buttonsModeButton)
        {
            buttonsModeButton.onClick.RemoveAllListeners();
            buttonsModeButton.onClick.AddListener(SetButtonsMode);
        }
    }

    public void SetDragMode()
    {
        Haptics.Selection(); // ✅ إضافة
        isDragMode = true;

        if (dragMode) dragMode.SetActive(true);
        if (buttonsMode) buttonsMode.SetActive(false);

        // ✅ إصلاح: إعادة تفعيل النقطة عند العودة لوضع السحب
        if (spinDot) spinDot.SetActive(true);

        UpdateButtonColors();
        Debug.Log("🎯 Drag Mode Activated");
    }

    public void SetButtonsMode()
    {
        isDragMode = false;

        if (dragMode) dragMode.SetActive(false);
        // إخفاء النقطة لأننا نستخدم الأزرار
        if (spinDot) spinDot.SetActive(false);

        if (buttonsMode) buttonsMode.SetActive(true);

        UpdateButtonColors();
        Debug.Log("🎯 Buttons Mode Activated");
    }

    void UpdateButtonColors()
    {
        if (dragModeButton)
        {
            // نستخدم colors بدلاً من color مباشرة لتلوين الحالة Normal
            var colors = dragModeButton.colors;
            colors.normalColor = isDragMode ? activeColor : inactiveColor;
            dragModeButton.colors = colors;
        }

        if (buttonsModeButton)
        {
            var colors = buttonsModeButton.colors;
            colors.normalColor = isDragMode ? inactiveColor : activeColor;
            buttonsModeButton.colors = colors;
        }
    }
}