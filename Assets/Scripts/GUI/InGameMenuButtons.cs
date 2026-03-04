using UnityEngine;
using UnityEngine.SceneManagement;

public class InGameMenuButtons : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Menu")]
    public GameObject MenuPanel;

    public void GoToMainMenu()
    {
        Haptics.Selection(); // ✅ اهتزاز الخروج
        Time.timeScale = 1f;
        SceneTransitionManager.Instance.LoadScene(mainMenuSceneName);
    }

    public void MenuPanelDisplay()
    {
        Haptics.Light(); // ✅ اهتزاز فتح القائمة
        if (MenuPanel != null) MenuPanel.SetActive(true);
    }

    public void MenuPanelHide()
    {
        if (HapticManager.Instance != null) Haptics.Light();

        // ✅ التعديل السحري هنا: أضفنا (MenuPanel.activeInHierarchy) 
        // لنتأكد أن القائمة ظاهرة فعلاً قبل أن نحاول إخفاءها أو تشغيل الأنيميشن!
        if (MenuPanel != null && MenuPanel.activeInHierarchy)
        {
            var anim = MenuPanel.GetComponent<PanelAnimator>();
            if (anim != null)
            {
                anim.Hide(); // تشغيل حركة الإغلاق
                StartCoroutine(DisableMenuPanelRealtime(MenuPanel, 0.4f));
            }
            else
            {
                MenuPanel.SetActive(false); // إخفاء فوري إذا لم يوجد أنيميشن
            }
        }
    }

    private System.Collections.IEnumerator DisableMenuPanelRealtime(GameObject panel, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (panel != null) panel.SetActive(false);
    }
}