using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [SerializeField] GameObject settingsPanel;
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
        // ❌ الخطأ كان هنا: تم نقل Haptics لداخل دوال الـ Load لتعمل عند الضغط وليس عند البداية
        if (playVsPlayerButton) playVsPlayerButton.onClick.AddListener(LoadPvPScene);
        if (playVsAIButton) playVsAIButton.onClick.AddListener(LoadPvAIScene);
        if (ChallengesButton) ChallengesButton.onClick.AddListener(LoadChallengesScene);
    }

    public void LoadPvPScene()
    {
        Haptics.Selection(); // ✅ تم النقل هنا
        SceneTransitionManager.Instance.LoadScene(pvpSceneName);
    }

    public void LoadPvAIScene()
    {
        Haptics.Selection(); // ✅ تم النقل هنا
        SceneTransitionManager.Instance.LoadSceneWithLoading(pvaiSceneName);
    }

    public void LoadChallengesScene()
    {
        Haptics.Selection(); // ✅ تم النقل هنا
        SceneManager.LoadScene(ChallengessceneName);
    }

    //==========================================
    public void OpenSettings()
    {
        Haptics.Light(); // اهتزاز خفيف لفتح القائمة
        if (settingsPanel != null)
        {
            settingsPanel.GetComponent<PanelAnimator>().Show();
            MainMenuPanel.SetActive(false);
        }
    }

    public void CloseSettings()
    {
        Haptics.Light();
        if (settingsPanel != null)
        {
            settingsPanel.GetComponent<PanelAnimator>().Hide();
            MainMenuPanel.SetActive(true);
        }
    }

    public void OpenProfile()
    {
        Haptics.Light();
        if (profilePanel != null)
        {
            profilePanel.GetComponent<PanelAnimator>().Show();
            MainMenuPanel.SetActive(false);
        }
    }

    public void CloseProfile()
    {
        Haptics.Light();
        if (profilePanel != null)
        {
            profilePanel.GetComponent<PanelAnimator>().Hide();
            MainMenuPanel.SetActive(true);
        }
    }

    public void OpenChangeName()
    {
        Haptics.Light();
        if (ChangeNamePanel != null) ChangeNamePanel.GetComponent<PanelAnimator>().Show();
    }

    public void CloseChangeName()
    {
        Haptics.Light();
        if (ChangeNamePanel != null) ChangeNamePanel.GetComponent<PanelAnimator>().Hide();
    }
}