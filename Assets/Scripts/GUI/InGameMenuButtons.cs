using UnityEngine;
using UnityEngine.SceneManagement;

public class InGameMenuButtons : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    // زر: رجوع للقائمة الرئيسية
    [Header("Menu")]
    public GameObject MenuPanel;
   
    public void GoToMainMenu()
    {
        Time.timeScale = 1f; // مهم لو عندك Pause
        SceneTransitionManager.Instance.LoadScene(mainMenuSceneName);
    }

    public void MenuPanelDisplay()
    {
        if (MenuPanel != null)
        {
            MenuPanel.SetActive(true);
        }
    }

    public void MenuPanelHide()
    {
        if (MenuPanel != null)
        {
            MenuPanel.SetActive(false);

        }
    }
}
