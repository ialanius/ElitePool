using UnityEngine;
using UnityEngine.UI;

public class TutorialSlideshow : MonoBehaviour
{
    [Header("الصفحات")]
    public GameObject[] pages;

    [Header("الأزرار")]
    public Button nextButton;
    public Button backButton;
    public Button playButton;

    private int currentIndex = 0;

    void Start()
    {
        ShowPage(0);
        nextButton.onClick.AddListener(Next);
        backButton.onClick.AddListener(Back);
        playButton.onClick.AddListener(CloseTutorial);
    }

    void ShowPage(int index)
    {
        currentIndex = index;
        for (int i = 0; i < pages.Length; i++)
        {
            pages[i].SetActive(i == index);
        }

        backButton.gameObject.SetActive(currentIndex > 0);
        bool isLastPage = (currentIndex == pages.Length - 1);
        nextButton.gameObject.SetActive(!isLastPage);
        playButton.gameObject.SetActive(isLastPage);
    }

    public void Next()
    {
        Haptics.Light(); // ✅ اهتزاز خفيف للتقليب
        if (currentIndex < pages.Length - 1) ShowPage(currentIndex + 1);
    }

    public void Back()
    {
        Haptics.Light(); // ✅ اهتزاز خفيف للتقليب
        if (currentIndex > 0) ShowPage(currentIndex - 1);
    }

    public void CloseTutorial()
    {
        Haptics.Selection();

        // 1. تشغيل أنيميشن الإخفاء
        PanelAnimator animator = GetComponent<PanelAnimator>();
        if (animator != null)
        {
            animator.Hide();
            // 2. تأجيل إغلاق اللوحة وبدء اللعبة حتى ينتهي الأنيميشن (نفترض أنه 0.5 ثانية)
            Invoke(nameof(StartGameAfterAnimation), 0.5f);
        }
        else
        {
            // إذا لم يكن هناك أنيميشن، ابدأ فوراً
            StartGameAfterAnimation();
        }
    }

    void StartGameAfterAnimation()
    {
        // 3. إخفاء اللوحة تماماً بعد انتهاء الأنيميشن
        gameObject.SetActive(false);

        // 4. إخبار مدير اللعبة ببدء اللعب وتفعيل العصا
        GameUI ui = FindObjectOfType<GameUI>();
        if (ui)
        {
            ui.CloseTutorialAndStartGame();
        }
    }
}