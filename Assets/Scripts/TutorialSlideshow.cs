using UnityEngine;
using UnityEngine.UI;

public class TutorialSlideshow : MonoBehaviour
{
    [Header("الصفحات")]
    public GameObject[] pages; // هنا نسحب Page1, Page2, Page3

    [Header("الأزرار")]
    public Button nextButton;
    public Button backButton;
    public Button playButton;

    // متغير عشان نعرف أي صفحة مفتوحة الآن
    private int currentIndex = 0;

    void Start()
    {
        // عرض الصفحة الأولى عند البداية
        ShowPage(0);

        // ربط الأزرار
        nextButton.onClick.AddListener(Next);
        backButton.onClick.AddListener(Back);
        playButton.onClick.AddListener(CloseTutorial);
    }

    void ShowPage(int index)
    {
        currentIndex = index;

        // 1. نلف على كل الصفحات
        for (int i = 0; i < pages.Length; i++)
        {
            if (i == index)
            {
                pages[i].SetActive(true); // شغل الصفحة المطلوبة
            }
            else
            {
                pages[i].SetActive(false); // طف الباقي
            }
        }

        // 2. التحكم في ظهور الأزرار
        // زر "السابق" يختفي في أول صفحة
        backButton.gameObject.SetActive(currentIndex > 0);

        // زر "التالي" يختفي في آخر صفحة
        bool isLastPage = (currentIndex == pages.Length - 1);
        nextButton.gameObject.SetActive(!isLastPage);

        // زر "ابدأ" يظهر فقط في آخر صفحة
        playButton.gameObject.SetActive(isLastPage);
    }

    public void Next()
    {
        if (currentIndex < pages.Length - 1)
        {
            ShowPage(currentIndex + 1);
        }
    }

    public void Back()
    {
        if (currentIndex > 0)
        {
            ShowPage(currentIndex - 1);
        }
    }

    public void CloseTutorial()
    {
        gameObject.SetActive(false); // إغلاق الشرح كامل
        // هنا ممكن تضيف كود تفعيل العصا CueController.enabled = true
    }
}