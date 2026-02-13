using UnityEngine;
using UnityEngine.EventSystems;

public class FineTuningWheel : MonoBehaviour, IDragHandler
{
    [Header("References")]
    public CueStickController3D cueController;

    [Header("Settings")]
    [Tooltip("حساسية العجلة (رقم صغير = دقة أعلى)")]
    public float sensitivity = 0.1f;

    [Tooltip("هل العجلة عمودية (يمين الشاشة) أم أفقية؟")]
    public bool isVerticalWheel = true;

    public void OnDrag(PointerEventData eventData)
    {
        if (!cueController) return;

        // حساب كمية الحركة من حركة الإصبع
        float delta = isVerticalWheel ? eventData.delta.y : eventData.delta.x;

        // إذا كانت الحركة صغيرة جداً نتجاهلها
        if (Mathf.Abs(delta) < 0.1f) return;

        // تدوير العجلة نفسها بصرياً (لتعطي شعوراً بالحركة)
        float visualRot = delta * 2f;
        transform.Rotate(0f, 0f, visualRot);

        // إرسال أمر التدوير للعصا
        // نضرب في الحساسية لتقليل السرعة (دقة عالية)
        cueController.RotateStickFine(delta * sensitivity);
    }
}