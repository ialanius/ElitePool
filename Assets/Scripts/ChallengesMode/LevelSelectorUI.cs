using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LevelSelectorUI : MonoBehaviour
{
    [Header("Buttons")]
    public Button[] levelButtons; // اسحب أزرار المراحل هنا بالترتيب (1, 2, 3...)
    public Sprite lockedSprite;   // (اختياري) صورة القفل
    public Sprite unlockedSprite; // (اختياري) صورة الزر العادي

    public GameUI gameUI;
    void Start()
    {
        UpdateLevelButtons();
    }

    void UpdateLevelButtons()
    {
        // 1. جلب أعلى مرحلة وصل لها اللاعب (الافتراضي 1)
        int reachedLevel = PlayerPrefs.GetInt("LevelReached", 1);

        for (int i = 0; i < levelButtons.Length; i++)
        {
            int levelNum = i + 1; // لأن المصفوفة تبدأ من 0 والمراحل من 1

            if (levelNum <= reachedLevel)
            {
                // ✅ المرحلة مفتوحة
                levelButtons[i].interactable = true;
                if (unlockedSprite) levelButtons[i].image.sprite = unlockedSprite;

                // إضافة وظيفة الزر برمجياً
                int levelIndex = i; // نحتاج متغير محلي للكلوجر
                levelButtons[i].onClick.RemoveAllListeners();
                Haptics.Selection(); // أول سطر
                levelButtons[i].onClick.AddListener(() => SelectLevel(levelIndex));
            }
            else
            {
                // 🔒 المرحلة مغلقة
                levelButtons[i].interactable = false;
                if (lockedSprite) levelButtons[i].image.sprite = lockedSprite;
            }
        }
    }

    void SelectLevel(int index)
    {
        // هنا نرسل الأمر لمدير التحديات ليختار المرحلة
        if (ChallengeManager.Instance)
        {
            // نفترض أن لديك دالة تختار المرحلة من القائمة
            ChallengeManager.Instance.StartChallenge(ChallengeManager.Instance.allLevels[index]);

            // نخفي قائمة اختيار المراحل
            gameObject.SetActive(false);
        }
    }
}