using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    public void PlayGame()
    {
        Haptics.Selection();
        AudioListener.pause = false;
        AudioListener.volume = 1.0f;
        Time.timeScale = 1;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
        Haptics.Selection(); // ✅ اهتزاز عند الخروج من اللعبة
        Application.Quit();
    }
}