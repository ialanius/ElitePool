using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    // دالة زر البدء
    public void PlayGame()
    {
        // 1. إجبار الصوت على العمل (لحل مشكلة الويب)
        AudioListener.pause = false; // إلغاء الإيقاف المؤقت للصوت
        AudioListener.volume = 1.0f; // التأكد أن الصوت مرفوع 100%

        // 2. التأكد من أن الزمن يعمل (في حال توقف بسبب كود التدوير السابق)
        Time.timeScale = 1;

        // 3. الانتقال للمشهد التالي
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}