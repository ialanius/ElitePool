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
        int reachedLevel = PlayerPrefs.GetInt("LevelReached", 1);

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

                // ✅ الإصلاح هنا: وضعنا الاهتزاز داخل قوسين ليعمل عند الضغط فقط
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

                // إضافة اهتزاز "خطأ" عند محاولة ضغط مرحلة مغلقة (اختياري وجميل)
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