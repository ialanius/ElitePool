using System.Collections;
using UnityEngine;

public class RestartButton : MonoBehaviour
{
    [Header("Refs")]
    public BallRack3D rack;
    public ScratchManager scratch;
    public CueStickController3D cueStick;
    public GameStateManager gameState;
    public GameUI gameUI;

    [Header("Balls")]
    public Rigidbody[] allBalls;
    public Ball3D[] ballScripts;

    [Header("Cue Ball Settings")]
    public Vector3 cueBallResetPos = new Vector3(-5.1235f, 0.25f, -0.88f);

    Coroutine resetCo;

    void Start()
    {
        if (!gameState) gameState = GameStateManager.Instance;
        if (!gameUI) gameUI = FindObjectOfType<GameUI>();

        // تحديث المراجع في البداية
        RefreshBallReferences();
    }

    // دالة مساعدة لتحديث مراجع الكرات
    void RefreshBallReferences()
    {
        allBalls = FindObjectsOfType<Rigidbody>();
        ballScripts = FindObjectsOfType<Ball3D>();
    }

    public void RestartGame()
    {
        // ✅ 1. فحص ذكي: هل نحن في وضع التحدي؟
        if (gameState && gameState.isChallengeMode)
        {
            Debug.Log("🔄 Restarting Challenge Level...");

            // نخفي لوحات الفوز والخسارة أولاً
            if (gameUI)
            {
                if (gameUI.winPanel) gameUI.winPanel.SetActive(false);
                if (gameUI.losePanel) gameUI.losePanel.SetActive(false);
                if (gameUI.foulPanel) gameUI.foulPanel.SetActive(false);
            }

            // نوجه الأمر لمدير التحديات ليعيد بناء المرحلة
            if (ChallengeManager.Instance && ChallengeManager.Instance.currentLevel)
            {
                ChallengeManager.Instance.StartChallenge(ChallengeManager.Instance.currentLevel);
            }
            return; // 🛑 توقف هنا! لا تكمل كود الرص العادي
        }

        // ==========================================================
        // 👇 كود اللعبة العادية (لن يتم تنفيذه إذا كنا في تحدي) 👇
        // ==========================================================

        // 1. إخفاء العصا وقفل السلايدر
        if (cueStick)
        {
            cueStick.Hide();
            cueStick.StopAllCoroutines();
            cueStick.SetSliderInteractable(false);
        }

        if (resetCo != null) { StopCoroutine(resetCo); resetCo = null; }

        // 2. تصفير الحالة
        if (gameState) gameState.ResetGame();

        // 3. تصفير ScratchManager
        if (scratch) scratch.ResetScratchManager();

        // 4. تحديث المراجع (لأن الكرات قد تكون تغيرت)
        RefreshBallReferences();

        // 5. تفعيل الكرات وتصفير فيزيائها
        if (ballScripts != null)
        {
            foreach (var b in ballScripts)
            {
                if (!b) continue;
                b.inPocket = false;
                b.gameObject.SetActive(true);

                if (b.rb)
                {
                    b.rb.isKinematic = false;
                    b.rb.velocity = Vector3.zero;
                    b.rb.angularVelocity = Vector3.zero;
                    b.rb.Sleep();
                }
            }
        }

        // 6. رص الكرات (للوضع العادي فقط)
        float waitTime = 0.1f;
        if (rack)
        {
            rack.RackBalls();
            if (rack.animateRacking) waitTime = 2.0f;

            if (rack.cueBall)
            {
                rack.cueBall.position = cueBallResetPos;
                rack.cueBall.rotation = Quaternion.identity;
            }
        }

        // 7. إخفاء القوائم
        if (gameUI)
        {
            if (gameUI.winPanel) gameUI.winPanel.SetActive(false);
            if (gameUI.foulPanel) gameUI.foulPanel.SetActive(false);
            if (gameUI.losePanel) gameUI.losePanel.SetActive(false);
            gameUI.MenuPanelHide();
        }

        // 8. تجهيز العصا
        if (cueStick)
            resetCo = StartCoroutine(ResetCueAfterRack(waitTime));
    }

    IEnumerator ResetCueAfterRack(float delay)
    {
        yield return new WaitForSeconds(delay);
        yield return new WaitForFixedUpdate();

        if (cueStick)
        {
            cueStick.ResetStickBehindCueBall(false);
            cueStick.SetSliderInteractable(true);
        }
        resetCo = null;
    }
}