using UnityEngine;
using UnityEngine.UI;

public class OrientationCheck : MonoBehaviour
{
    [Header("UI Settings")]
    [Tooltip("اسحب هنا الـ Panel الذي يحتوي على رسالة التحذير")]
    public GameObject portraitWarningPanel;

    void Update()
    {
        // التحقق من أبعاد الشاشة في كل فريم
        // إذا كان الارتفاع أكبر من العرض، فهذا يعني أن الجهاز في وضع الطول
        if (Screen.height > Screen.width)
        {
            // إظهار رسالة التحذير
            if (portraitWarningPanel.activeSelf == false)
            {
                portraitWarningPanel.SetActive(true);
                Time.timeScale = 0; // إيقاف اللعبة مؤقتاً (اختياري)
            }
        }
        else
        {
            // إخفاء الرسالة عندما يكون العرض أكبر (وضع Landscape)
            if (portraitWarningPanel.activeSelf == true)
            {
                portraitWarningPanel.SetActive(false);
                Time.timeScale = 1; // استئناف اللعبة
            }
        }
    }
}