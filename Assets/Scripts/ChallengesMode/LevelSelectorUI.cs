using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LevelSelectorUI : MonoBehaviour
{
    [Header("Buttons")]
    public Button[] levelButtons; // اسحب أزرار المراحل هنا بالترتيب (1, 2, 3...)
    public Sprite lockedSprite;   // (اختياري) صورة القفل
    public Sprite unlockedSprite; // (اختياري) صورة الزر العادي

    [Header("Development & Testing")]
    [Tooltip("فعّل هذا الخيار لفتح جميع المراحل فوراً (للتطوير فقط)")]
    public bool unlockAllLevelsForTesting = false; // ✅ إضافة خيار التطوير هنا

    public GameUI gameUI;

    void Start()
    {
        UpdateLevelButtons();
    }

    void UpdateLevelButtons()
    {
        // جلب أعلى مرحلة وصل لها اللاعب فعلياً
        int reachedLevel = PlayerPrefs.GetInt("LevelReached", 1);

        // ✅ التعديل الذكي: إذا كان خيار المطور مفعلاً، نعتبر اللاعب وصل لآخر مرحلة
        if (unlockAllLevelsForTesting)
        {
            reachedLevel = levelButtons.Length;
            Debug.Log("🛠️ Developer Mode: All levels are unlocked!");
        }

        for (int i = 0; i < levelButtons.Length; i++)
        {
            int levelNum = i + 1;

            if (levelNum <= reachedLevel)
            {
                // ✅ المرحلة مفتوحة
                levelButtons[i].interactable = true;
                if (unlockedSprite) levelButtons[i].image.sprite = unlockedSprite;

                int levelIndex = i;
                levelButtons[i].onClick.RemoveAllListeners();

                // ربط الزر بالدالة مع الاهتزاز
                levelButtons[i].onClick.AddListener(() => {
                    Haptics.Selection();
                    SelectLevel(levelIndex);
                });
            }
            else
            {
                // 🔒 المرحلة مغلقة
                levelButtons[i].interactable = false;
                if (lockedSprite) levelButtons[i].image.sprite = lockedSprite;

                // إضافة اهتزاز "خطأ" عند محاولة ضغط مرحلة مغلقة
                levelButtons[i].onClick.RemoveAllListeners();
                levelButtons[i].onClick.AddListener(() => Haptics.Error());
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