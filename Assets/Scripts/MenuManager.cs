using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [SerializeField] string gameSceneName = "Main";
    [SerializeField] GameObject settingsPanel; // اختياري
    [SerializeField] GameObject profilePanel;
    [SerializeField] GameObject MainMenuPanel;
    [SerializeField] GameObject ChangeNamePanel;
    
    public void Play()
    {
        SceneManager.LoadScene(gameSceneName);
    }

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
    public void Quit()
    {
        Application.Quit();
    }
}
