using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [Header("References")]
    public GameStateManager gameState;
    public CueStickController3D cueStick;
    public AIPlayer aiPlayer;
    public GameObject tutorialPanel;

    [Header("Mode Panels")]
    public GameObject standardGamePanel;
    public GameObject challengePanel;

    [Header("AI Mode Settings")]
    public GameObject difficultyPanel; // تأكد أن هذا الكائن يحتوي على سكربت AIDifficultySelector

    [Header("Challenge UI")]
    public TextMeshProUGUI shotsLeftText;
    public TextMeshProUGUI levelNameText;
    public TextMeshProUGUI levelDescriptionText;
    public GameObject levelInfoPanel;
    public TextMeshProUGUI levelNameText2;
    public Button startButton;

    [Header("Audio Settings")]
    public AudioSource uiAudioSource;
    public AudioClip winSound;
    public AudioClip loseSound;
    public AudioClip foulSound;

    [Header("Player Display (Standard)")]
    public TextMeshProUGUI currentPlayerText;
    public TextMeshProUGUI player1NameText;
    public TextMeshProUGUI player2NameText;
    public TextMeshProUGUI player1ScoreText;
    public TextMeshProUGUI player2ScoreText;
    public TextMeshProUGUI player1GroupText;
    public TextMeshProUGUI player2GroupText;
    public Image player1Highlight;
    public Image player2Highlight;
    public Color activeColor = new Color(0.2f, 0.8f, 0.2f, 0.5f);
    public Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);

    [Header("Common UI")]
    public GameObject powerMeterPanel;
    public Slider powerSlider;
    public Image powerFillImage;
    public Gradient powerGradient;
    public TextMeshProUGUI powerPercentText;

    [Header("Messages")]
    public TextMeshProUGUI messageText;
    public float messageDuration = 2f;
    private float messageTimer = 0f;

    [Header("End Screens")]
    public GameObject winPanel;
    public TextMeshProUGUI winnerText;
    public GameObject losePanel;
    public TextMeshProUGUI loseReasonText;
    public Button restartButton;

    [Header("Buttons Lock 🔒")]
    public Button[] gameplayButtons;
    public GameObject fineTuneWheel;

    [Header("Foul Indicator")]
    public GameObject foulPanel;
    public TextMeshProUGUI foulText;

    [Header("Menu")]
    public GameObject MenuPanel;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    void Start()
    {
        if (!gameState) gameState = GameStateManager.Instance;
        if (!uiAudioSource) uiAudioSource = GetComponent<AudioSource>();
        if (!aiPlayer) aiPlayer = FindObjectOfType<AIPlayer>();

        if (gameState)
        {
            gameState.OnPlayerChanged.AddListener(OnPlayerChanged);
            gameState.OnGroupAssigned.AddListener(OnGroupAssigned);
            gameState.OnScoreChanged.AddListener(OnScoreChanged);
            gameState.OnGameWon.AddListener(OnGameWon);
            gameState.OnFoulCommitted.AddListener(OnFoulCommitted);
        }

        if (restartButton) restartButton.onClick.AddListener(OnRestartButtonClicked);

        // إخفاء اللوحات المبدئية
        if (powerMeterPanel) powerMeterPanel.SetActive(false);
        if (winPanel) winPanel.SetActive(false);
        if (losePanel) losePanel.SetActive(false);
        if (foulPanel) foulPanel.SetActive(false);
        if (MenuPanel) MenuPanel.SetActive(false);

        RefreshReferences();
        SetupModeUI();
        UpdateUI();
    }

    // ✅✅✅ 1. إصلاح دالة التحكم (لجعل العصا ظاهرة للـ AI)
    public void SetGameplayControlsActive(bool isActive)
    {
        // 1. الأزرار الجانبية (إخفاء كامل)
        if (gameplayButtons != null)
        {
            foreach (var btn in gameplayButtons)
            {
                if (btn) btn.gameObject.SetActive(isActive);
            }
        }

        // 2. العجلة
        if (fineTuneWheel) fineTuneWheel.SetActive(isActive);

        // 3. العصا (التعديل الجوهري هنا 🔥)
        if (cueStick)
        {
            // نوقف استجابة السكربت للماوس، لكن نترك الكائن مفعلاً ليراه اللاعب
            cueStick.enabled = isActive;

            // ✅ نضمن أن جسم العصا ظاهر دائماً (لأن الـ AI يحركها أمامه)
            cueStick.gameObject.SetActive(true);

            // نقفل السلايدر فقط
            cueStick.SetSliderInteractable(isActive);

            // إذا كان دور اللاعب البشري، نعيد العصا خلف الكرة
            // أما إذا كان دور الـ AI (isActive = false)، نترك العصا حرة ليحركها الـ AI
            if (isActive)
            {
                cueStick.ResetStickBehindCueBall(true);
            }
        }

        if (powerSlider) powerSlider.interactable = isActive;
    }

    public void RefreshReferences()
    {
        if (cueStick == null) cueStick = FindObjectOfType<CueStickController3D>();

        if (cueStick != null)
        {
            if (powerMeterPanel) cueStick.powerSliderPanel = powerMeterPanel;
            if (powerSlider) cueStick.powerSlider = powerSlider;
            cueStick.SetSliderInteractable(true);
        }
    }

    void SetupModeUI()
    {
        bool isChallenge = (gameState && gameState.isChallengeMode);

        if (standardGamePanel) standardGamePanel.SetActive(!isChallenge);
        if (challengePanel) challengePanel.SetActive(isChallenge);

        if (isChallenge)
        {
            if (ChallengeManager.Instance && ChallengeManager.Instance.currentLevel)
            {
                UpdateChallengeText(ChallengeManager.Instance.currentLevel.maxShots);
                if (levelNameText) levelNameText.text = ChallengeManager.Instance.currentLevel.levelName;
                if (levelDescriptionText) levelDescriptionText.text = ChallengeManager.Instance.currentLevel.description;
            }
        }
    }

    void Update()
    {
        UpdatePowerDisplay();
        UpdateMessage();
    }

    public void UpdateChallengeText(int shotsLeft)
    {
        if (shotsLeftText)
        {
            shotsLeftText.text = $"Shots: {shotsLeft}";
            shotsLeftText.color = (shotsLeft <= 1) ? Color.red : Color.white;
        }
    }

    void UpdateUI()
    {
        if (!gameState || gameState.isChallengeMode) return;

        if (player1NameText) player1NameText.text = "Player 1";
        if (player2NameText) player2NameText.text = (aiPlayer && aiPlayer.isAIEnabled) ? "AI" : "Player 2";

        if (player1ScoreText) player1ScoreText.text = gameState.player1Score.ToString();
        if (player2ScoreText) player2ScoreText.text = gameState.player2Score.ToString();

        UpdateGroupText(player1GroupText, Player.Player1);
        UpdateGroupText(player2GroupText, Player.Player2);
        UpdateCurrentPlayerDisplay();
    }

    void UpdateGroupText(TextMeshProUGUI textUI, Player player)
    {
        if (textUI)
        {
            string group = gameState.GetPlayerGroupText(player);
            textUI.text = group == "Unassigned" ? "-" : group;
            textUI.color = Color.white;
        }
    }

    void UpdateCurrentPlayerDisplay()
    {
        if (!gameState) return;
        Player current = gameState.currentPlayer;

        if (currentPlayerText)
        {
            string pName = current.ToString();
            if (current == Player.Player2 && aiPlayer && aiPlayer.isAIEnabled) pName = "AI";
            else if (current == Player.Player1) pName = "Player 1";
            else if (current == Player.Player2) pName = "Player 2";

            currentPlayerText.text = $"{pName}'s Turn";
        }

        if (player1Highlight) player1Highlight.color = (current == Player.Player1) ? activeColor : inactiveColor;
        if (player2Highlight) player2Highlight.color = (current == Player.Player2) ? activeColor : inactiveColor;
    }

    void UpdatePowerDisplay()
    {
        if (!cueStick || !powerSlider) return;
        float power = cueStick.power01;
        if (powerFillImage && powerGradient != null) powerFillImage.color = powerGradient.Evaluate(power);
        if (powerPercentText) powerPercentText.text = $"{Mathf.RoundToInt(power * 100f)}%";
    }

    void UpdateMessage()
    {
        if (messageTimer > 0f)
        {
            messageTimer -= Time.deltaTime;
            if (messageTimer <= 0f && messageText)
            {
                messageText.text = "";
                messageText.gameObject.SetActive(false);
            }
        }
    }

    public void ShowMessage(string msg)
    {
        if (messageText)
        {
            messageText.text = msg;
            messageText.gameObject.SetActive(true);
            messageTimer = messageDuration;
        }
    }

    public void ShowWinPanel(string winnerName)
    {
        if (winPanel) winPanel.SetActive(true);
        SetGameplayControlsActive(false);

        if (winnerText)
        {
            if (gameState.isChallengeMode)
                winnerText.text = "CHALLENGE COMPLETE!";
            else
            {
                string displayName = winnerName;
                if (winnerName == "Player2" && aiPlayer && aiPlayer.isAIEnabled) displayName = "AI";
                winnerText.text = $"{displayName} WINS!";
            }
        }
        PlayWinSound();
    }

    public void ShowLosePanel(string reason)
    {
        if (losePanel) losePanel.SetActive(true);
        SetGameplayControlsActive(false);
        if (loseReasonText) loseReasonText.text = reason;
        PlayLoseSound();
    }

    void OnPlayerChanged(Player newPlayer) { UpdateUI(); }
    void OnScoreChanged(Player p, int s) { UpdateUI(); }
    void OnGameWon(Player w) { ShowWinPanel(w.ToString()); }

    void OnGroupAssigned(Player player, BallGroup group)
    {
        UpdateUI();
        string pName = player.ToString();
        if (player == Player.Player2 && aiPlayer && aiPlayer.isAIEnabled) pName = "AI";
        ShowMessage($"{pName} Assigned {group}");
    }

    void OnFoulCommitted()
    {
        if (foulPanel)
        {
            foulPanel.SetActive(true);
            if (foulText) foulText.text = "FOUL!";
            Invoke(nameof(HideFoulPanel), 1.5f);
        }
        ShowMessage("Foul committed!");
    }

    void HideFoulPanel() { if (foulPanel) foulPanel.SetActive(false); }

    public void OnRestartButtonClicked()
    {
        if (winPanel) winPanel.SetActive(false);
        if (losePanel) losePanel.SetActive(false);
        MenuPanelHide();

        // إعادة تفعيل التحكم
        SetGameplayControlsActive(true);

        if (gameState && gameState.isChallengeMode && ChallengeManager.Instance)
        {
            ChallengeManager.Instance.StartChallenge(ChallengeManager.Instance.currentLevel);
        }
        else
        {
            var restartBtn = FindObjectOfType<RestartButton>();
            if (restartBtn) restartBtn.RestartGame();
            else SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        UpdateUI();
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void MenuPanelDisplay() { if (MenuPanel) MenuPanel.SetActive(true); }
    public void MenuPanelHide() { if (MenuPanel) MenuPanel.SetActive(false); }

    public void PlayWinSound() { if (uiAudioSource && winSound) uiAudioSource.PlayOneShot(winSound); }
    public void PlayLoseSound() { if (uiAudioSource && loseSound) uiAudioSource.PlayOneShot(loseSound); }

    public void CloseTutorialAndStartGame()
    {
        if (tutorialPanel) tutorialPanel.SetActive(false);
        SetGameplayControlsActive(true);

        RestartButton restarter = FindObjectOfType<RestartButton>();
        if (restarter) restarter.RestartGame();
        else FindObjectOfType<BallRack3D>().RackBalls();
    }

    // ✅✅✅ 2. دالة لفتح قائمة الـ AI فقط (بدون بدء اللعبة مباشرة)
    // اربط هذه الدالة بزر "AI Mode" في القائمة الرئيسية إذا كان لديك
    public void OpenAIDifficultyPanel()
    {
        if (difficultyPanel)
        {
            difficultyPanel.SetActive(true);

            // محاولة استدعاء تحديث الواجهة داخل السكربت الجديد
            var selector = difficultyPanel.GetComponent<AIDifficultySelector>();
            if (selector) selector.ReopenDifficultyPanel();
        }
    }

    // تم إيقاف الدالة القديمة لتجنب التعارض (يمكنك حذفها)
    /*
    public void SelectDifficultyAndStart(int level)
    {
        // ... (Old Logic Removed to prevent conflicts) ...
    }
    */
}