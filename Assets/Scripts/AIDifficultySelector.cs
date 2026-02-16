using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// AI Difficulty Selector - Enhanced Version
/// Clean interface with complete interaction blocking during selection
/// </summary>
public class AIDifficultySelector : MonoBehaviour
{
    [Header("UI Panel")]
    public GameObject difficultyPanel;

    [Header("Interaction Blocking")]
    [Tooltip("Main game Canvas - to block interaction")]
    public CanvasGroup gameCanvasGroup;
    [Tooltip("Cue stick - to hide it")]
    public CueStickController3D cueStick;

    [Header("Difficulty Buttons")]
    public Button easyButton;
    public Button mediumButton;
    public Button hardButton;

    [Header("AI Player")]
    public AIPlayer aiPlayer;

    [Header("Game Components")]
    public GameStateManager gameState;

    [Header("UI Text")]
    public TMP_Text titleText;
    public TMP_Text selectedDifficultyText;
    public TMP_Text descriptionText;

    [Header("Navigation")]
    public Button backToMenuButton;
    public string menuSceneName = "MainMenu";

    [Header("Audio (Optional)")]
    public AudioSource audioSource;
    public AudioClip buttonClickSound;
    public AudioClip gameStartSound;

    [Header("Settings")]
    [Tooltip("Delay before starting game (for animation)")]
    public float startGameDelay = 0.3f;
    [Tooltip("Show difficulty description")]
    public bool showDifficultyDescription = true;

    private AIDifficulty selectedDifficulty = AIDifficulty.Medium;
    private bool isPanelActive = false;

    void Start()
    {
        SetupButtons();
        ShowDifficultyPanel();

        // Enable AI
        if (aiPlayer)
        {
            aiPlayer.SetAIEnabled(true);
        }
    }

    void SetupButtons()
    {
        if (easyButton)
            easyButton.onClick.AddListener(() => SelectDifficulty(AIDifficulty.Easy));

        if (mediumButton)
            mediumButton.onClick.AddListener(() => SelectDifficulty(AIDifficulty.Medium));

        if (hardButton)
            hardButton.onClick.AddListener(() => SelectDifficulty(AIDifficulty.Hard));

        if (backToMenuButton)
            backToMenuButton.onClick.AddListener(BackToMenu);
    }

    void ShowDifficultyPanel()
    {
        if (!difficultyPanel) return;

        isPanelActive = true;
        difficultyPanel.SetActive(true);

        // 1. Pause time
        Time.timeScale = 0f;

        // 2. Block interaction with game UI
        if (gameCanvasGroup)
        {
            gameCanvasGroup.interactable = false;
            gameCanvasGroup.blocksRaycasts = false;
        }

        // 3. Hide cue stick
        if (cueStick)
        {
            cueStick.gameObject.SetActive(false);
        }

        // Update texts
        if (titleText)
            titleText.text = "Select AI Difficulty";

        // Update button colors
        UpdateButtonColors();
        UpdateDifficultyDescription();
    }

    void SelectDifficulty(AIDifficulty difficulty)
    {
        selectedDifficulty = difficulty;

        // Click sound
        PlaySound(buttonClickSound);

        // Update colors and description
        UpdateButtonColors();
        UpdateDifficultyDescription();

        Debug.Log("[Selector] Difficulty selected: " + difficulty);

        // Start game after slight delay
        Invoke(nameof(StartGame), startGameDelay);
    }

    void StartGame()
    {
        // Set AI difficulty
        if (aiPlayer)
        {
            aiPlayer.SetDifficulty(selectedDifficulty);
        }

        // Game start sound
        PlaySound(gameStartSound);

        // Hide panel
        if (difficultyPanel)
        {
            difficultyPanel.SetActive(false);
        }
        isPanelActive = false;

        // Resume time
        Time.timeScale = 1f;

        // Enable game UI
        if (gameCanvasGroup)
        {
            gameCanvasGroup.interactable = true;
            gameCanvasGroup.blocksRaycasts = true;
        }

        // Show and prepare cue stick
        if (cueStick)
        {
            cueStick.gameObject.SetActive(true);
            cueStick.ResetStickBehindCueBall(true);
        }

        // Reset game
        if (gameState)
        {
            gameState.ResetGame();
        }

        Debug.Log("[Selector] Game started with " + selectedDifficulty + " AI");
    }

    void UpdateButtonColors()
    {
        Color selectedColor = new Color(0.3f, 1f, 0.3f);     // Green
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
        button.colors = colors;
    }

    void UpdateDifficultyDescription()
    {
        if (!showDifficultyDescription || !descriptionText) return;

        string description = "";

        switch (selectedDifficulty)
        {
            case AIDifficulty.Easy:
                description = "EASY\n\n" +
                             "* AI makes many mistakes\n" +
                             "* Random shot selection\n" +
                             "* Perfect for beginners";
                break;

            case AIDifficulty.Medium:
                description = "MEDIUM\n\n" +
                             "* Balanced gameplay\n" +
                             "* Good accuracy\n" +
                             "* Fair challenge";
                break;

            case AIDifficulty.Hard:
                description = "HARD\n\n" +
                             "* Professional level\n" +
                             "* Very high accuracy\n" +
                             "* Tough challenge!";
                break;
        }

        descriptionText.text = description;

        // Update short text
        if (selectedDifficultyText)
        {
            selectedDifficultyText.text = selectedDifficulty.ToString();
        }
    }

    void BackToMenu()
    {
        PlaySound(buttonClickSound);

        Debug.Log("[Selector] Returning to main menu...");

        // Resume time
        Time.timeScale = 1f;

        // Load main menu
        SceneManager.LoadScene(menuSceneName);
    }

    void PlaySound(AudioClip clip)
    {
        if (!audioSource || !clip) return;

        // Play with unscaled time (works even when time is paused)
        audioSource.PlayOneShot(clip);
    }

    // Public Methods (for external use)

    /// <summary>
    /// Reopen difficulty panel (useful for changing difficulty during game)
    /// </summary>
    public void ReopenDifficultyPanel()
    {
        if (!isPanelActive)
        {
            ShowDifficultyPanel();
        }
    }

    /// <summary>
    /// Set difficulty directly without opening panel
    /// </summary>
    public void SetDifficultyDirectly(AIDifficulty difficulty)
    {
        selectedDifficulty = difficulty;

        if (aiPlayer)
        {
            aiPlayer.SetDifficulty(difficulty);
        }

        Debug.Log("[Selector] Difficulty set directly to: " + difficulty);
    }
}