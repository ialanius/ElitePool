using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject tutorialPanel; // اللوحة التي صممناها
    public Button playButton;        // زر البدء

    [Header("Game References")]
    public CueStickController3D cueController; // عشان نوقف اللعب واللوحة مفتوحة

    [Header("Settings")]
    public bool showOnlyFirstTime = true; // هل نعرضها مرة واحدة فقط للأبد؟

    void Start()
    {
        // التحقق هل رآها اللاعب سابقاً؟
        bool viewedBefore = PlayerPrefs.GetInt("TutorialViewed", 0) == 1;

        if (showOnlyFirstTime && viewedBefore)
        {
            // رآها سابقاً، اخفها فوراً وابدأ اللعب
            tutorialPanel.SetActive(false);
            ActivateGame(true);
        }
        else
        {
            // أول مرة أو الإعداد يطلب العرض دائماً
            ShowTutorial();
        }

        // ربط الزر بالدالة
        playButton.onClick.AddListener(CloseTutorial);
    }

    void ShowTutorial()
    {
        tutorialPanel.SetActive(true);
        ActivateGame(false); // تجميد التحكم بالعصا
    }

    public void CloseTutorial()
    {
        tutorialPanel.SetActive(false);
        ActivateGame(true); // تفعيل التحكم بالعصا

        // حفظ أن اللاعب شاهد التعليمات
        PlayerPrefs.SetInt("TutorialViewed", 1);
        PlayerPrefs.Save();
    }

    void ActivateGame(bool status)
    {
        if (cueController)
        {
            cueController.enabled = status; // يمنع أو يسمح بالتحكم

            // إذا عندك سلايدر وعجلة، يفضل تخفيهم أو تقفلهم أيضاً
            if (status == false)
            {
                cueController.Hide(); // إخفاء العصا مؤقتاً
            }
            else
            {
                // إعادة تهيئة العصا للظهور
                cueController.ResetStickBehindCueBall(true);
            }
        }
    }
}