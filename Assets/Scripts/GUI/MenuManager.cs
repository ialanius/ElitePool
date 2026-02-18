using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [SerializeField] GameObject settingsPanel; // اختياري
    [SerializeField] GameObject profilePanel;
    [SerializeField] GameObject MainMenuPanel;
    [SerializeField] GameObject ChangeNamePanel;
    
    [Header("🎮 Main Menu Buttons")]
    public Button playVsPlayerButton;
    public Button playVsAIButton;
    public Button ChallengesButton;

    [Header("📝 Scene Names")]
    public string pvpSceneName = "PvP_Mode";
    public string pvaiSceneName = "PvAI_Mode";
    public string ChallengessceneName = "Challenges_Mode";

    void Start()
    {
        SetupButtons();
    }

    void SetupButtons()
    {
        if (playVsPlayerButton)
        {
            Haptics.Selection(); // أول سطر
            playVsPlayerButton.onClick.AddListener(LoadPvPScene);
        }

        if (playVsAIButton)
        {
            Haptics.Selection(); // أول سطر
            playVsAIButton.onClick.AddListener(LoadPvAIScene);
        }

        if (ChallengesButton)
        {
            Haptics.Selection(); // أول سطر
            ChallengesButton.onClick.AddListener(LoadChallengesScene);
        }
    }

    public void LoadPvPScene()
    {
        SceneTransitionManager.Instance.LoadScene(pvpSceneName);
    }

    public void LoadPvAIScene() 
    {
        SceneTransitionManager.Instance.LoadSceneWithLoading(pvaiSceneName);
    }

    public void LoadChallengesScene() 
    {
        SceneManager.LoadScene(ChallengessceneName);
    }
    //==========================================
    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            MainMenuPanel.SetActive(false);
        }
        
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            MainMenuPanel.SetActive(true);
        }

        }

    public void OpenProfile()
    {
        if (profilePanel != null)
        {
            profilePanel.SetActive(true);
            MainMenuPanel.SetActive(false);
        }
    }

    public void CloseProfile()
    {
        if (profilePanel != null)
        {
            profilePanel.SetActive(false);
            MainMenuPanel.SetActive(true);
        }
    }

    public void OpenChangeName() 
    {
        if (ChangeNamePanel != null) 
        {
            ChangeNamePanel.SetActive(true);
        }
    }
    public void CloseChangeName()
    {
        if (ChangeNamePanel != null)
        {
            ChangeNamePanel.SetActive(false);
        }
    }
    
}
