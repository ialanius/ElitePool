using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [Header("References")]
    public GameStateManager gameState;
    public CueStickController3D cueStick;

    [Header("AI Reference (New)")]
    public AIPlayer aiPlayer; // ✅ أضفنا هذا لنعرف حالة الـ AI

    [Header("Audio Settings")]
    public AudioSource uiAudioSource;
    public AudioClip winSound;
    public AudioClip loseSound;
    public AudioClip foulSound;

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
    public TextMeshProUGUI loseReasonText;

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

        // ✅ محاولة العثور على AIPlayer تلقائياً إذا نسيت سحبه
        if (!aiPlayer) aiPlayer = FindObjectOfType<AIPlayer>();

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

    void UpdateUI()
    {
        if (!gameState) return;

        // تحديث اسم اللاعب الأول
        if (player1NameText) player1NameText.text = "Player 1";

        // ✅✅✅ هنا التعديل الذكي
        if (player2NameText)
        {
            // إذا كان الـ AI موجوداً ومفعلاً، نكتب اسمه وصعوبته
            if (aiPlayer && aiPlayer.isAIEnabled)
            {
                player2NameText.text = $"AI"; // مثال: AI (Medium)
            }
            else
            {
                // إذا لم يكن مفعلاً، يعني أننا في مود لاعب ضد لاعب
                player2NameText.text = "Player 2";
            }
        }

        if (player1ScoreText) player1ScoreText.text = gameState.player1Score.ToString();
        if (player2ScoreText) player2ScoreText.text = gameState.player2Score.ToString();

        UpdateGroupText(player1GroupText, Player.Player1);
        UpdateGroupText(player2GroupText, Player.Player2);

        UpdateCurrentPlayerDisplay();
    }

    // دالة مساعدة لتحديث نصوص المجموعات
    void UpdateGroupText(TextMeshProUGUI textUI, Player player)
    {
        if (textUI)
        {
            string group = gameState.GetPlayerGroupText(player);
            textUI.text = group == "Unassigned" ? "-" : group;

            if (group == "Solids") textUI.color = Color.white;
            else if (group == "Stripes") textUI.color = Color.white;
            else textUI.color = Color.gray;
        }
    }

    void UpdateCurrentPlayerDisplay()
    {
        if (!gameState) return;
        Player current = gameState.currentPlayer;

        // ✅ تحديث نص "دور فلان"
        if (currentPlayerText)
        {
            string pName = current.ToString(); // الافتراضي Player1 أو Player2

            // إذا كان دور اللاعب الثاني وهو AI، نغير الاسم في الرسالة أيضاً
            if (current == Player.Player2 && aiPlayer && aiPlayer.isAIEnabled)
            {
                pName = "AI";
            }
            // تحسين شكلي للاعب 1
            else if (current == Player.Player1)
            {
                pName = "Player 1";
            }
            // تحسين شكلي للاعب 2 (بشري)
            else if (current == Player.Player2)
            {
                pName = "Player 2";
            }

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

    // ====== Audio & Panels ======
    public void PlayWinSound() { if (uiAudioSource && winSound) uiAudioSource.PlayOneShot(winSound); }
    public void PlayLoseSound() { if (uiAudioSource && loseSound) uiAudioSource.PlayOneShot(loseSound); }

    public void ShowWinPanel(string winnerName)
    {
        if (winPanel) winPanel.SetActive(true);

        // ✅ تعديل اسم الفائز في لوحة الفوز أيضاً
        string displayName = winnerName;
        if (winnerName == "Player2" && aiPlayer && aiPlayer.isAIEnabled)
        {
            displayName = "AI";
        }

        if (winnerText) winnerText.text = $"{displayName} WINS!";
    }

    public void ShowLosePanel(string reason)
    {
        if (losePanel) losePanel.SetActive(true);
        if (loseReasonText) loseReasonText.text = reason;
        PlayLoseSound();
    }

    // ====== Event Callbacks ======
    void OnPlayerChanged(Player newPlayer)
    {
        UpdateUI();
        // لن نظهر رسالة هنا لأن UpdateUI ستقوم بتحديث نص الدور في الأعلى
        // ShowMessage($"{newPlayer}'s Turn"); 
    }

    void OnGroupAssigned(Player player, BallGroup group)
    {
        UpdateUI();

        string pName = player.ToString();
        if (player == Player.Player2 && aiPlayer && aiPlayer.isAIEnabled) pName = "AI";

        ShowMessage($"{pName} → {group}");
    }

    void OnScoreChanged(Player player, int newScore) => UpdateUI();

    void OnGameWon(Player winner)
    {
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

    public void OnRestartButtonClicked()
    {
        if (winPanel) winPanel.SetActive(false);
        if (losePanel) losePanel.SetActive(false);

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