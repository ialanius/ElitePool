using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    // تأكد أن هذا الإحداثي يطابق مكان الكرة البيضاء في بداية اللعبة عندك
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
    public void RefreshBallReferences()
    {
        allBalls = FindObjectsOfType<Rigidbody>();
        ballScripts = FindObjectsOfType<Ball3D>();
    }

    public void RestartGame()
    {
        // ✅ 0. أهم سطر: ضمان أن الزمن يعمل (يحل مشاكل التوقف بعد القوائم)
        Time.timeScale = 1f;

        // ✅ 1. فحص وضع التحدي
        if (gameState && gameState.isChallengeMode)
        {
            Debug.Log("🔄 Restarting Challenge Level...");

            if (gameUI)
            {
                // إخفاء القوائم
                if (gameUI.winPanel) gameUI.winPanel.SetActive(false);
                if (gameUI.losePanel) gameUI.losePanel.SetActive(false);
                if (gameUI.foulPanel) gameUI.foulPanel.SetActive(false);

                // تأكد أن التحكم مفعل للتحدي
                gameUI.SetGameplayControlsActive(true);
            }

            // إعادة بناء المرحلة عبر المدير
            if (ChallengeManager.Instance && ChallengeManager.Instance.currentLevel)
            {
                ChallengeManager.Instance.StartChallenge(ChallengeManager.Instance.currentLevel);
            }
            return; // 🛑 خروج، لا ننفذ باقي الكود
        }

        // ==========================================================
        // 👇 كود اللعبة العادية (Standard / AI Mode) 👇
        // ==========================================================

        // 2. إيقاف العصا مؤقتاً
        if (cueStick)
        {
            cueStick.Hide();
            cueStick.StopAllCoroutines();
            cueStick.SetSliderInteractable(false);
        }

        if (resetCo != null) { StopCoroutine(resetCo); resetCo = null; }

        // 3. تصفير الحالة والسكور
        if (gameState) gameState.ResetGame();

        // 4. تصفير نظام الأخطاء (Scratch)
        if (scratch) scratch.ResetScratchManager();

        // 5. تحديث المراجع وتصفير فيزياء الكرات
        RefreshBallReferences();
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

        // 6. رص الكرات في المثلث
        float waitTime = 0.1f;
        if (rack)
        {
            rack.RackBalls();
            // زيادة وقت الانتظار إذا كان هناك أنيميشن للرص
            if (rack.animateRacking) waitTime = 1.5f;

            // إعادة الكرة البيضاء لنقطة البداية بدقة
            if (rack.cueBall)
            {
                rack.cueBall.position = cueBallResetPos;
                rack.cueBall.rotation = Quaternion.identity;

                // تأكيد تصفير السرعة
                Rigidbody rb = rack.cueBall.GetComponent<Rigidbody>();
                if (rb) { rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
            }
        }

        // 7. تنظيف الواجهة (UI)
        if (gameUI)
        {
            if (gameUI.winPanel) gameUI.winPanel.SetActive(false);
            if (gameUI.foulPanel) gameUI.foulPanel.SetActive(false);
            if (gameUI.losePanel) gameUI.losePanel.SetActive(false);
            gameUI.MenuPanelHide();
        }

        // 8. الانتظار قليلاً ثم بدء اللعب
        if (cueStick)
            resetCo = StartCoroutine(ResetCueAfterRack(waitTime));
    }

    IEnumerator ResetCueAfterRack(float delay)
    {
        // انتظار انتهاء الرص
        yield return new WaitForSeconds(delay);
        yield return new WaitForFixedUpdate();

        // ✅ إعادة تفعيل أزرار التحكم (Spin, Camera, TopView)
        // هذا يضمن ظهور الأزرار حتى لو اختفت بسبب قائمة الصعوبة
        if (gameUI)
        {
            gameUI.SetGameplayControlsActive(true);
        }

        // ✅ إعادة تفعيل العصا للعب
        if (cueStick)
        {
            cueStick.ResetStickBehindCueBall(false);
            cueStick.SetSliderInteractable(true);
        }
        resetCo = null;
    }
}