using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [Header("References")]
    public GameStateManager gameState;
    public CueStickController3D cueStick;

    [Header("Audio Settings")]
    public AudioSource uiAudioSource; // ✅ مصدر الصوت (السماعة)
    public AudioClip winSound;        // ✅ ملف صوت الفوز
    public AudioClip loseSound;       // ✅ ملف صوت الخسارة
    public AudioClip foulSound;       // ✅ ملف صوت الفاول (اختياري هنا)

    [Header("Player Display")]
    public TextMeshProUGUI currentPlayerText;
    public TextMeshProUGUI player1NameText;
    public TextMeshProUGUI player2NameText;
    public TextMeshProUGUI player1ScoreText;
    public TextMeshProUGUI player2ScoreText;
    public TextMeshProUGUI player1GroupText;
    public TextMeshProUGUI player2GroupText;

    [Header("Player Highlights")]
    public Image player1Highlight;
    public Image player2Highlight;
    public Color activeColor = new Color(0.2f, 0.8f, 0.2f, 0.5f);
    public Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);

    [Header("Power Meter")]
    public GameObject powerMeterPanel;
    public Slider powerSlider;
    public Image powerFillImage;
    public Gradient powerGradient;
    public TextMeshProUGUI powerPercentText;

    [Header("Game Messages")]
    public TextMeshProUGUI messageText;
    public float messageDuration = 2f;
    private float messageTimer = 0f;

    [Header("Win Screen")]
    public GameObject winPanel;
    public TextMeshProUGUI winnerText;
    public Button restartButton;

    [Header("Lose Screen")]
    public GameObject losePanel;
    public TextMeshProUGUI loseReasonText; // لعرض سبب الخسارة (اختياري)

    [Header("Foul Indicator")]
    public GameObject foulPanel;
    public TextMeshProUGUI foulText;

    [Header("Menu")]
    public GameObject MenuPanel;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    void Start()
    {
        if (!gameState) gameState = GameStateManager.Instance;
        if (!uiAudioSource) uiAudioSource = GetComponent<AudioSource>(); // ✅ جلب تلقائي

        if (gameState)
        {
            gameState.OnPlayerChanged.AddListener(OnPlayerChanged);
            gameState.OnGroupAssigned.AddListener(OnGroupAssigned);
            gameState.OnScoreChanged.AddListener(OnScoreChanged);
            gameState.OnGameWon.AddListener(OnGameWon);
            gameState.OnFoulCommitted.AddListener(OnFoulCommitted);
        }

        if (cueStick && powerMeterPanel)
        {
            cueStick.powerSliderPanel = powerMeterPanel;
            cueStick.powerSlider = powerSlider;
        }

        if (powerMeterPanel) powerMeterPanel.SetActive(false);
        if (winPanel) winPanel.SetActive(false);
        if (foulPanel) foulPanel.SetActive(false);
        if (MenuPanel) MenuPanel.SetActive(false);

        if (restartButton) restartButton.onClick.AddListener(OnRestartButtonClicked);

        UpdateUI();
    }

    void Update()
    {
        UpdatePowerDisplay();
        UpdateMessage();
    }

    // ... (دوال UpdateUI, UpdatePowerDisplay, UpdateMessage كما هي بدون تغيير) ...
    void UpdateUI()
    {
        if (!gameState) return;

        if (player1NameText) player1NameText.text = "Player 1";
        if (player2NameText) player2NameText.text = "Player 2";
        if (player1ScoreText) player1ScoreText.text = gameState.player1Score.ToString();
        if (player2ScoreText) player2ScoreText.text = gameState.player2Score.ToString();

        if (player1GroupText)
        {
            string group = gameState.GetPlayerGroupText(Player.Player1);
            player1GroupText.text = group == "Unassigned" ? "-" : group;
            if (group == "Solids") player1GroupText.color = new Color(0.8f, 0.2f, 0.2f);
            else if (group == "Stripes") player1GroupText.color = new Color(0.2f, 0.2f, 0.8f);
            else player1GroupText.color = Color.gray;
        }

        if (player2GroupText)
        {
            string group = gameState.GetPlayerGroupText(Player.Player2);
            player2GroupText.text = group == "Unassigned" ? "-" : group;
            if (group == "Solids") player2GroupText.color = new Color(0.8f, 0.2f, 0.2f);
            else if (group == "Stripes") player2GroupText.color = new Color(0.2f, 0.2f, 0.8f);
            else player2GroupText.color = Color.gray;
        }
        UpdateCurrentPlayerDisplay();
    }

    void UpdateCurrentPlayerDisplay()
    {
        if (!gameState) return;
        Player current = gameState.currentPlayer;
        if (currentPlayerText) currentPlayerText.text = $"{current}'s Turn";
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

    void ShowMessage(string msg)
    {
        if (messageText)
        {
            messageText.text = msg;
            messageText.gameObject.SetActive(true);
            messageTimer = messageDuration;
        }
    }
    // ... (نهاية دوال التحديث) ...

    // ====== Audio Methods (الدوال الجديدة) ======
    public void PlayWinSound()
    {
        if (uiAudioSource && winSound) uiAudioSource.PlayOneShot(winSound);
    }

    public void PlayLoseSound()
    {
        if (uiAudioSource && loseSound) uiAudioSource.PlayOneShot(loseSound);
    }

    // دالة مساعدة لعرض لوحة الفوز (يتم استدعاؤها أيضاً عبر الحدث)
    public void ShowWinPanel(string winnerName)
    {
        if (winPanel) winPanel.SetActive(true);
        if (winnerText) winnerText.text = $"{winnerName} WINS!";
    }
    // دالة جديدة لإظهار لوحة الخسارة
    public void ShowLosePanel(string reason)
    {
        if (losePanel) losePanel.SetActive(true);
        if (loseReasonText) loseReasonText.text = reason;

        // تشغيل صوت الخسارة
        PlayLoseSound();
    }
    // ====== Event Callbacks ======
    void OnPlayerChanged(Player newPlayer)
    {
        UpdateUI();
        ShowMessage($"{newPlayer}'s Turn");
    }

    void OnGroupAssigned(Player player, BallGroup group)
    {
        UpdateUI();
        ShowMessage($"{player} → {group}");
    }

    void OnScoreChanged(Player player, int newScore) => UpdateUI();

    void OnGameWon(Player winner)
    {
        // هذه الدالة تستدعى تلقائياً من الحدث، لكن سنقوم بتشغيل الصوت يدوياً في GameStateManager لضمان الدقة
        ShowWinPanel(winner.ToString());
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

    void HideFoulPanel()
    {
        if (foulPanel) foulPanel.SetActive(false);
    }

    // تحديث دالة زر الريستارت لتخفي لوحة الخسارة أيضاً
    public void OnRestartButtonClicked()
    {
        if (winPanel) winPanel.SetActive(false);
        if (losePanel) losePanel.SetActive(false); // ✅ إخفاء لوحة الخسارة

        var restartBtn = FindObjectOfType<RestartButton>();
        if (restartBtn) restartBtn.RestartGame();

        UpdateUI();
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void MenuPanelDisplay() { if (MenuPanel) MenuPanel.SetActive(true); }
    public void MenuPanelHide() { if (MenuPanel) MenuPanel.SetActive(false); }
}