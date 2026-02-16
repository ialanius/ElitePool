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

    [Header("Mode Panels")]
    public GameObject standardGamePanel;
    public GameObject challengePanel;

    [Header("Challenge UI")]
    public TextMeshProUGUI shotsLeftText;
    public TextMeshProUGUI levelNameText;

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
    public float messageDuration = 2f; // ✅ تمت إضافته (كان ناقصاً)
    private float messageTimer = 0f;

    [Header("End Screens")]
    public GameObject winPanel;
    public TextMeshProUGUI winnerText;
    public GameObject losePanel;
    public TextMeshProUGUI loseReasonText;
    public Button restartButton;

    // ✅✅✅ 1. قائمة الأزرار المراد قفلها
    [Header("Buttons Lock 🔒")]
    public Button[] gameplayButtons; // اسحب هنا كل الأزرار (Spin, Camera, TopView, etc)
    public GameObject fineTuneWheel; // 🎡 اسحب كائن العجلة هنا

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

        // ✅✅✅ الحل السحري: نادِ دالة التحديث فوراً
        RefreshReferences();

        SetupModeUI();
        UpdateUI();
    }

    // ✅✅✅ 2. دالة الإيقاف الشاملة (المعدلة)
    public void SetGameplayControlsActive(bool isActive)
    {
        // 1. الأزرار (Spin, Camera, TopView)
        if (gameplayButtons != null)
        {
            foreach (var btn in gameplayButtons)
            {
                if (btn) btn.interactable = isActive;
            }
        }

        // 2. العجلة (إخفاء/إظهار)
        if (fineTuneWheel)
        {
            fineTuneWheel.SetActive(isActive); // تخفي العجلة تماماً عند الخسارة
        }

        // 3. العصا والسلايدر
        if (cueStick)
        {
            cueStick.enabled = isActive; // شلل العصا

            // قفل السلايدر (يصبح رمادي ولا يتحرك)
            cueStick.SetSliderInteractable(isActive);

            // إخفاء العصا عند التوقف
            if (!isActive) cueStick.Hide();
            else cueStick.ResetStickBehindCueBall(true); // إظهارها عند العودة
        }

        // تأكيد إضافي لقفل السلايدر
        if (powerSlider) powerSlider.interactable = isActive;
    }

    // 👇👇 أضف هذه الدالة الجديدة في أي مكان في السكربت (مثلاً في النهاية)
    public void RefreshReferences()
    {
        // 1. إذا العصا ضائعة، ابحث عنها في المشهد
        if (cueStick == null)
        {
            cueStick = FindObjectOfType<CueStickController3D>();
        }

        // 2. إذا وجدنا العصا، نعطيها السلايدر (حقن التبعيات)
        if (cueStick != null)
        {
            if (powerMeterPanel) cueStick.powerSliderPanel = powerMeterPanel;
            if (powerSlider) cueStick.powerSlider = powerSlider;

            // تحديث إعدادات العصا لضمان ظهورها
            cueStick.SetSliderInteractable(true);
        }
        else
        {
            Debug.LogWarning("⚠️ GameUI: Still cannot find CueStick!");
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
            if (shotsLeft <= 1) shotsLeftText.color = Color.red;
            else shotsLeftText.color = Color.white;
        }
    }

    void UpdateUI()
    {
        if (!gameState) return;
        if (gameState.isChallengeMode) return;

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

        // 🛑 قفل كل شيء
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

        // 🛑 قفل كل شيء
        SetGameplayControlsActive(false);

        if (loseReasonText) loseReasonText.text = reason;
        PlayLoseSound();
    }

    // ====== Event Callbacks ======
    void OnPlayerChanged(Player newPlayer) { UpdateUI(); }
    void OnScoreChanged(Player p, int s) { UpdateUI(); }
    void OnGameWon(Player w) { ShowWinPanel(w.ToString()); }

    // ✅✅✅ تمت إضافة الدالة الناقصة هنا
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
        // ✅✅✅ الإضافة هنا: إخفاء المينو أيضاً عند الريستارت
        MenuPanelHide();

        // ▶️ تشغيل كل شيء
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
}