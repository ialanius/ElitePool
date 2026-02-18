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
    public Vector3 cueBallResetPos = new Vector3(-5.1235f, 0.25f, -0.88f);

    Coroutine resetCo;

    void Start()
    {
        if (!gameState) gameState = GameStateManager.Instance;
        if (!gameUI) gameUI = FindObjectOfType<GameUI>();
        RefreshBallReferences();
    }

    public void RefreshBallReferences()
    {
        allBalls = FindObjectsOfType<Rigidbody>();
        ballScripts = FindObjectsOfType<Ball3D>();
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;

        // ✅ 1. فحص وضع التحدي
        if (gameState && gameState.isChallengeMode)
        {
            Debug.Log("🔄 Restarting Challenge Level...");

            if (gameUI)
            {
                // إخفاء لوحات الفوز والخسارة والفاول
                if (gameUI.winPanel) gameUI.winPanel.SetActive(false);
                if (gameUI.losePanel) gameUI.losePanel.SetActive(false);
                if (gameUI.foulPanel) gameUI.foulPanel.SetActive(false);

                // ✅✅✅ الإضافة الهامة: إخفاء القائمة الرئيسية أيضاً في وضع التحدي
                gameUI.MenuPanelHide();

                // تأكد أن التحكم مفعل
                gameUI.SetGameplayControlsActive(true);
            }

            // إعادة بناء المرحلة
            if (ChallengeManager.Instance && ChallengeManager.Instance.currentLevel)
            {
                ChallengeManager.Instance.StartChallenge(ChallengeManager.Instance.currentLevel);
            }
            return; // 🛑 خروج
        }

        // ==========================================================
        // 👇 كود اللعبة العادية (Standard / AI Mode)
        // ==========================================================

        if (cueStick)
        {
            cueStick.Hide();
            cueStick.StopAllCoroutines();
            cueStick.SetSliderInteractable(false);
        }

        if (resetCo != null) { StopCoroutine(resetCo); resetCo = null; }

        if (gameState) gameState.ResetGame();
        if (scratch) scratch.ResetScratchManager();

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

        float waitTime = 0.1f;
        if (rack)
        {
            rack.RackBalls();
            if (rack.animateRacking) waitTime = 1.5f;

            if (rack.cueBall)
            {
                rack.cueBall.position = cueBallResetPos;
                rack.cueBall.rotation = Quaternion.identity;
                Rigidbody rb = rack.cueBall.GetComponent<Rigidbody>();
                if (rb) { rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
            }
        }

        // تنظيف الواجهة للوضع العادي
        if (gameUI)
        {
            if (gameUI.winPanel) gameUI.winPanel.SetActive(false);
            if (gameUI.foulPanel) gameUI.foulPanel.SetActive(false);
            if (gameUI.losePanel) gameUI.losePanel.SetActive(false);

            // ✅ إخفاء القائمة
            gameUI.MenuPanelHide();
        }

        if (cueStick)
            resetCo = StartCoroutine(ResetCueAfterRack(waitTime));
    }

    IEnumerator ResetCueAfterRack(float delay)
    {
        yield return new WaitForSeconds(delay);
        yield return new WaitForFixedUpdate();

        if (gameUI) gameUI.SetGameplayControlsActive(true);

        if (cueStick)
        {
            cueStick.ResetStickBehindCueBall(false);
            cueStick.SetSliderInteractable(true);
        }
        resetCo = null;
    }
}