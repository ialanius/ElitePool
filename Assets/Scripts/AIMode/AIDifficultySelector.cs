using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class AIDifficultySelector : MonoBehaviour
{
    [Header("UI Panel")]
    public GameObject difficultyPanel;

    [Header("Interaction Blocking")]
    public CanvasGroup gameCanvasGroup;
    public CueStickController3D cueStick;

    [Header("Difficulty Buttons")]
    public Button easyButton;
    public Button mediumButton;
    public Button hardButton;

    // ✅✅✅ الإضافة الجديدة: زر التشغيل
    [Header("Action Buttons")]
    public Button playButton;

    [Header("AI Player")]
    public AIPlayer aiPlayer;

    [Header("Game Components")]
    public GameStateManager gameState;

    [Header("UI Text")]
    public TMP_Text titleText;
    public TMP_Text selectedDifficultyText;
    public TMP_Text descriptionText;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip buttonClickSound;
    public AudioClip gameStartSound;

    [Header("Settings")]
    public bool showDifficultyDescription = true;

    // المتغير الذي يحفظ اختيار اللاعب مؤقتاً
    private AIDifficulty selectedDifficulty = AIDifficulty.Medium;
    private bool isPanelActive = false;

    void Start()
    {
        SetupButtons();

        // تفعيل القائمة عند البداية
        ShowDifficultyPanel();

        // تفعيل الـ AI في الخلفية
        if (aiPlayer)
        {
            aiPlayer.SetAIEnabled(true);
        }

        // تحديث الواجهة لتظهر الـ Medium كمختار افتراضياً
        UpdateUI();
    }

    void SetupButtons()
    {
        // ربط أزرار الصعوبة (للاختيار فقط)
        if (easyButton)
            easyButton.onClick.AddListener(() => SelectDifficulty(AIDifficulty.Easy));

        if (mediumButton)
            mediumButton.onClick.AddListener(() => SelectDifficulty(AIDifficulty.Medium));

        if (hardButton)
            hardButton.onClick.AddListener(() => SelectDifficulty(AIDifficulty.Hard));

        // ✅✅✅ ربط زر اللعب ببدء اللعبة فعلياً
        if (playButton)
            playButton.onClick.AddListener(StartGame);
    }

    void ShowDifficultyPanel()
    {
        if (!difficultyPanel) return;

        isPanelActive = true;
        difficultyPanel.SetActive(true);

        // إيقاف الزمن والتحكم
        Time.timeScale = 0f;

        if (gameCanvasGroup)
        {
            gameCanvasGroup.interactable = false;
            gameCanvasGroup.blocksRaycasts = false;
        }

        if (cueStick) cueStick.gameObject.SetActive(false);

        UpdateUI();
    }

    // هذه الدالة الآن تختار فقط ولا تبدأ اللعبة
    void SelectDifficulty(AIDifficulty difficulty)
    {
        if (selectedDifficulty == difficulty) return;

        Haptics.Selection(); // ✅ اهتزاز عند تغيير الصعوبة
        selectedDifficulty = difficulty;

        PlaySound(buttonClickSound);
        UpdateUI();
    }

    void UpdateUI()
    {
        UpdateButtonColors();
        UpdateDifficultyDescription();
    }

    // ✅✅✅ هذه الدالة تعمل فقط عند ضغط زر Play
    void StartGame()
    {
        Haptics.Heavy(); // ✅ اهتزاز قوي إيذاناً ببدء المباراة!
        
        Debug.Log("🚀 Play Button Clicked! Starting Game...");

        // 1. تطبيق الصعوبة المختارة على الـ AI
        if (aiPlayer)
        {
            aiPlayer.SetDifficulty(selectedDifficulty);
        }

        PlaySound(gameStartSound);

        // 2. إخفاء القائمة
        if (difficultyPanel) difficultyPanel.SetActive(false);
        isPanelActive = false;

        // 3. إعادة الزمن للحياة (مهم جداً)
        Time.timeScale = 1f;

        // 4. إعادة تفعيل واجهة اللعبة
        if (gameCanvasGroup)
        {
            gameCanvasGroup.interactable = true;
            gameCanvasGroup.blocksRaycasts = true;
        }

        // 5. إظهار العصا
        if (cueStick) cueStick.gameObject.SetActive(true);

        // 6. استدعاء زر الريستارت لرص الكرات وبدء اللعب
        RestartButton restarter = FindObjectOfType<RestartButton>();
        if (restarter)
        {
            restarter.RestartGame();
        }
        else
        {
            // كود احتياطي
            if (gameState) gameState.ResetGame();
            BallRack3D rack = FindObjectOfType<BallRack3D>();
            if (rack) rack.RackBalls();
        }
    }

    void UpdateButtonColors()
    {
        Color selectedColor = new Color(0.5485938f, 0.7294118f, 0.4039216f); // أخضر
        Color normalColor = Color.white;

        UpdateButtonColor(easyButton, selectedDifficulty == AIDifficulty.Easy, selectedColor, normalColor);
        UpdateButtonColor(mediumButton, selectedDifficulty == AIDifficulty.Medium, selectedColor, normalColor);
        UpdateButtonColor(hardButton, selectedDifficulty == AIDifficulty.Hard, selectedColor, normalColor);
    }

    void UpdateButtonColor(Button button, bool isSelected, Color selectedColor, Color normalColor)
    {
        if (!button) return;
        var colors = button.colors;
        colors.normalColor = isSelected ? selectedColor : normalColor;
        colors.selectedColor = isSelected ? selectedColor : normalColor;
        button.colors = colors;
    }

    void UpdateDifficultyDescription()
    {
        if (!showDifficultyDescription || !descriptionText) return;

        string description = "";

        switch (selectedDifficulty)
        {
            case AIDifficulty.Easy:
                description = "EASY MODE\n\n- AI makes mistakes\n- Low accuracy\n- Best for learning";
                break;

            case AIDifficulty.Medium:
                description = "MEDIUM MODE\n\n- Balanced gameplay\n- Decent accuracy\n- Good challenge";
                break;

            case AIDifficulty.Hard:
                description = "HARD MODE\n\n- Professional AI\n- Pinpoint accuracy\n- No mercy!";
                break;
        }

        descriptionText.text = description;
        if (selectedDifficultyText) selectedDifficultyText.text = selectedDifficulty.ToString();
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource && clip) audioSource.PlayOneShot(clip);
    }

    public void ReopenDifficultyPanel()
    {
        if (!isPanelActive)
        {
            ShowDifficultyPanel();
        }
    }
}