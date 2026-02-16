using UnityEngine;
using System.Collections.Generic;

public class ChallengeManager : MonoBehaviour
{
    public static ChallengeManager Instance;

    [Header("References")]
    public GameStateManager gameState;
    public CueStickController3D cueStick;
    public GameUI gameUI;

    [Header("Prefabs")]
    public GameObject ballPrefab; // البريفاب الأساسي للكرة

    [Header("Current State")]
    public ChallengeLevelData currentLevel;
    public int shotsTaken = 0;
    public bool isChallengeActive = false;

    private List<GameObject> activeBalls = new List<GameObject>();

    void Awake()
    {
        Instance = this;
    }

    public void StartChallenge(ChallengeLevelData level)
    {
        currentLevel = level;
        shotsTaken = 0;
        isChallengeActive = true;

        // 1. تنظيف الطاولة
        ClearTable();

        // 2. وضع الكرة البيضاء
        ResetCueBall(level.cueBallPosition);

        // 3. وضع باقي الكرات
        foreach (var ballData in level.targetBalls)
        {
            SpawnBall(ballData.position, ballData.type);
        }

        // 4. تحديث الواجهة
        Debug.Log($"🚀 Started Challenge: {level.levelName}");
        if (gameUI) gameUI.ShowMessage($"Challenge: {level.maxShots} Shots Left");
    }

    void ClearTable()
    {
        // إخفاء أو تدمير الكرات الحالية (ما عدا البيضاء)
        var balls = FindObjectsOfType<Ball3D>();
        foreach (var ball in balls)
        {
            if (ball.type != BallType.Cue)
            {
                Destroy(ball.gameObject); // أو Disable لإعادة استخدامها (Object Pooling)
            }
        }
    }

    void ResetCueBall(Vector3 pos)
    {
        var cueBall = FindObjectOfType<CueStickController3D>().cueBall;
        if (cueBall)
        {
            cueBall.GetComponent<Rigidbody>().velocity = Vector3.zero;
            cueBall.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
            cueBall.position = pos;
            cueBall.gameObject.SetActive(true);
        }
    }

    void SpawnBall(Vector3 pos, BallType type)
    {
        GameObject newBall = Instantiate(ballPrefab, pos, Quaternion.identity);
        Ball3D ballScript = newBall.GetComponent<Ball3D>();

        // إعداد الكرة (لونها ورقمها)
        ballScript.type = type;

        // هنا يمكنك تغيير الـ Material حسب النوع (تحتاج كود إضافي في Ball3D لتلوينها)
        // ballScript.SetVisuals(type); 
    }

    // يتم استدعاء هذه الدالة عند كل ضربة (من GameStateManager)
    public void OnShotTaken()
    {
        if (!isChallengeActive) return;

        shotsTaken++;
        int shotsLeft = currentLevel.maxShots - shotsTaken;

        if (gameUI) gameUI.ShowMessage($"Shots Left: {shotsLeft}");

        if (shotsTaken > currentLevel.maxShots)
        {
            LoseChallenge();
        }
    }

    // يتم استدعاؤها عند دخول كرة
    public void CheckWinCondition()
    {
        if (!isChallengeActive) return;

        // شروط الفوز البسيطة: هل الـ 8 دخلت؟
        // يمكنك تعقيدها (مثلا: يجب إدخال كل الكرات ثم الـ 8)

        // مثال بسيط: الفوز إذا دخلت الـ 8
        // (يتم التعامل مع هذا المنطق داخل GameStateManager عادة)
    }

    public void LoseChallenge()
    {
        isChallengeActive = false;
        if (gameUI) gameUI.ShowLosePanel("Out of shots!");
    }

    public void WinChallenge()
    {
        isChallengeActive = false;
        if (gameUI) gameUI.ShowWinPanel("Challenge Complete!");
    }
}