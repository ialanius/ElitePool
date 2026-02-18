using UnityEngine;
using UnityEngine.UI;

public class TutorialHandAnim : MonoBehaviour
{
    [Header("Settings")]
    public Vector2 moveOffset = new Vector2(0, 100); // المسافة التي تقطعها اليد (X, Y)
    public float speed = 2.0f; // سرعة الحركة

    [Header("Fade Settings")]
    public bool useFading = true; // هل تختفي اليد في النهاية؟
    private Image handImage;
    private Vector2 startPos;

    void Start()
    {
        // حفظ مكان البداية
        startPos = GetComponent<RectTransform>().anchoredPosition;
        handImage = GetComponent<Image>();
    }

    void Update()
    {
        // حساب الحركة باستخدام دالة PingPong (رايح جاي)
        // أو Repeat (رايح فقط ثم يعيد من البداية)

        float t = Mathf.Repeat(Time.unscaledTime * speed, 1.0f); // من 0 إلى 1

        // تحريك اليد
        Vector2 endPos = startPos + moveOffset;
        GetComponent<RectTransform>().anchoredPosition = Vector2.Lerp(startPos, endPos, t);

        // جعل اليد تختفي تدريجياً في نهاية الحركة (شفافية)
        if (useFading && handImage)
        {
            Color c = handImage.color;
            // 0 = شفاف، 1 = ظاهر
            // سنجعلها تختفي عند الاقتراب من النهاية
            c.a = 1.0f - (t * t); // تتلاشى في النهاية
            handImage.color = c;
        }
    }
}