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
    public Rigidbody[] allBalls;   // 16 Rigidbodies
    public Ball3D[] ballScripts;   // 16 Ball3D

    [Header("Cue Ball Settings")]
    public Vector3 cueBallResetPos = new Vector3(-5.1235f, 0.25f, -0.88f);

    Coroutine resetCo;

    void Start()
    {
        if (!gameState) gameState = GameStateManager.Instance;
        if (!gameUI) gameUI = FindObjectOfType<GameUI>();

        if (allBalls == null || allBalls.Length == 0)
            allBalls = FindObjectsOfType<Rigidbody>();

        if (ballScripts == null || ballScripts.Length == 0)
            ballScripts = FindObjectsOfType<Ball3D>();
    }

    public void RestartGame()
    {
        // 1. إخفاء العصا
        if (cueStick)
        {
            cueStick.Hide();
            cueStick.StopAllCoroutines();
            // ✅✅ إضافة جديدة: قفل السلايدر فوراً عند بدء الرص
            cueStick.SetSliderInteractable(false);
        }

        if (resetCo != null) { StopCoroutine(resetCo); resetCo = null; }

        // 2. تصفير الحالة
        if (gameState) gameState.ResetGame();

        // 3. تصفير ScratchManager
        if (scratch)
        {
            scratch.ResetScratchManager();
        }

        // 4. تفعيل الكرات
        if (ballScripts != null)
        {
            foreach (var b in ballScripts)
            {
                if (!b) continue;
                b.inPocket = false;
                b.gameObject.SetActive(true);
            }
        }

        // 5. تصفير الفيزياء للجميع (قبل الرص)
        if (allBalls != null)
        {
            foreach (var rb in allBalls)
            {
                if (!rb) continue;
                // نلغي الكينماتك أولاً لنتمكن من تصفير السرعة دون تحذيرات
                rb.isKinematic = false;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.Sleep();
            }
        }

        // 6. رص الكرات الملونة
        float waitTime = 0.1f;
        if (rack)
        {
            rack.RackBalls(); // هذا السكربت سيقوم بتجميد الكرات (Kinematic)

            if (rack.animateRacking)
            {
                waitTime = 2.0f;
            }

            // ✅✅ تصحيح مكان الكرة البيضاء ✅✅
            if (rack.cueBall)
            {
                // نفرض الموقع المحدد بدقة
                rack.cueBall.position = cueBallResetPos;
                rack.cueBall.rotation = Quaternion.identity;

                // ❌ أزلت كود تصفير الفيزياء من هنا
                // لأن سكربت BallRack3D قام بجعلها Kinematic بالفعل
                // ومحاولة تصفير السرعة الآن ستسبب التحذير الأصفر
            }
        }

        // 7. إخفاء القوائم
        if (gameUI)
        {
            if (gameUI.winPanel) gameUI.winPanel.SetActive(false);
            if (gameUI.foulPanel) gameUI.foulPanel.SetActive(false);
            if (gameUI.losePanel) gameUI.losePanel.SetActive(false); // إخفاء لوحة الخسارة أيضاً
            gameUI.MenuPanelHide();
        }

        // 8. تجهيز العصا
        if (cueStick)
            resetCo = StartCoroutine(ResetCueAfterRack(waitTime));
    }

    IEnumerator ResetCueAfterRack(float delay)
    {
        // الانتظار حتى تنتهي حركة الكرات وتعود الفيزياء للعمل
        yield return new WaitForSeconds(delay);
        yield return new WaitForFixedUpdate();

        if (cueStick)
        {
            cueStick.ResetStickBehindCueBall(false);
            // ✅✅ إضافة جديدة: فتح السلايدر الآن فقط
            cueStick.SetSliderInteractable(true);
        }

        resetCo = null;
    }
}