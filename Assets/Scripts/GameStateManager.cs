using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;

public enum Player { None, Player1, Player2 }
public enum BallGroup { Unassigned, Solids, Stripes }

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance;

    [Header("Game Mode Settings")]
    public bool isChallengeMode = false; // ✅ يحدد الوضع تلقائياً

    [Header("Game State")]
    public Player currentPlayer = Player.Player1;
    public Player player1 = Player.Player1;
    public Player player2 = Player.Player2;

    [Header("Ball Assignments")]
    public BallGroup player1Group = BallGroup.Unassigned;
    public BallGroup player2Group = BallGroup.Unassigned;

    [Header("Scoring")]
    public int player1Score = 0;
    public int player2Score = 0;

    [Header("Game Rules")]
    public bool isBreakShot = true;
    public bool canShoot = true;
    public bool gameOver = false;
    public Player winner = Player.None;
    public bool foulCommitted = false;
    public bool scratchOnBreak = false;

    [Header("Shot Tracking")]
    private bool shotInProgress = false;
    private bool validHit = false;
    private bool hitAnyCushion = false;
    private bool anyBallPocketed = false;
    private bool pocketedOwnBall = false;
    private bool pocketedEightBall = false;
    private List<Ball3D> ballsPocketedThisShot = new List<Ball3D>();

    [Header("References")]
    public PoolGameManager3D poolManager;
    public Ball3D cueBall;
    public Ball3D eightBall;
    public List<Ball3D> allBalls = new List<Ball3D>(); // ✅ جعلناها List للمرونة

    [Header("UI & Audio")]
    public GameUI gameUI;
    public AudioSource gameAudioSource;
    public AudioClip foulSound;

    [Header("Events")]
    public UnityEvent<Player> OnPlayerChanged;
    public UnityEvent<Player, BallGroup> OnGroupAssigned;
    public UnityEvent<Player, int> OnScoreChanged;
    public UnityEvent<Player> OnGameWon;
    public UnityEvent OnFoulCommitted;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (!gameUI) gameUI = FindObjectOfType<GameUI>();
        if (!gameAudioSource) gameAudioSource = GetComponent<AudioSource>();

        // ✅ 1. كشف الوضع تلقائياً
        if (FindObjectOfType<ChallengeManager>())
        {
            isChallengeMode = true;
            Debug.Log("🧩 Challenge Mode Detected!");
            // في التحدي، ننتظر ChallengeManager ينشئ الكرات، فلا نفعل شيئاً هنا
        }
        else
        {
            // في الوضع العادي، نبدأ فوراً
            RefreshBallReferences();
            ResetGame();
        }
    }

    // ✅ 2. دالة ذكية لجلب الكرات في أي وضع
    public void RefreshBallReferences()
    {
        allBalls.Clear();

        // أ) الوضع العادي: نأخذ الكرات من PoolManager
        if (!isChallengeMode)
        {
            if (!poolManager) poolManager = PoolGameManager3D.Instance;
            if (poolManager && poolManager.balls != null)
            {
                cueBall = poolManager.cueBall;
                allBalls = new List<Ball3D>(poolManager.balls);
            }
        }
        // ب) وضع التحدي: نبحث في المشهد لأن PoolManager قد لا يملك الكرات الجديدة
        else
        {
            Ball3D[] found = FindObjectsOfType<Ball3D>();
            allBalls = found.ToList();
            cueBall = allBalls.FirstOrDefault(b => b.type == BallType.Cue);
        }

        // البحث عن الـ 8 في القائمة الجديدة
        eightBall = allBalls.FirstOrDefault(b => b.number == 8 || b.type == BallType.Eight);
    }

    public void ResetGame()
    {
        currentPlayer = Player.Player1;
        player1Group = BallGroup.Unassigned;
        player2Group = BallGroup.Unassigned;
        player1Score = 0;
        player2Score = 0;
        isBreakShot = true;
        canShoot = true;
        gameOver = false;
        winner = Player.None;
        foulCommitted = false;
        scratchOnBreak = false;
        shotInProgress = false;
        ResetShotTracking();

        OnPlayerChanged?.Invoke(currentPlayer);
        OnScoreChanged?.Invoke(Player.Player1, 0);
        OnScoreChanged?.Invoke(Player.Player2, 0);
    }

    public void OnShotStart()
    {
        if (!canShoot || gameOver) return;
        ResetShotTracking();
        shotInProgress = true;
        canShoot = false;

        // ✅ إبلاغ نظام التحديات إذا كان فعالاً
        if (isChallengeMode && ChallengeManager.Instance)
        {
            ChallengeManager.Instance.OnShotTaken();
        }
    }

    public void OnAllBallsStopped()
    {
        if (gameOver) return;
        if (!shotInProgress) return;

        EvaluateShot();
        shotInProgress = false;

        if (!gameOver) canShoot = true;
    }

    public void OnBallPocketed(Ball3D ball)
    {
        if (!ball || gameOver || !shotInProgress) return;

        ballsPocketedThisShot.Add(ball);
        anyBallPocketed = true;

        // هذا يضمن أنه إذا أدخل الـ 8 في التحدي، يفوز فوراً ويتجاهل باقي القوانين
        if ((ball.type == BallType.Eight || ball.number == 8) && ChallengeManager.Instance && ChallengeManager.Instance.isChallengeActive)
        {
            ChallengeManager.Instance.WinChallenge();
            return; // 🛑 توقف هنا ولا تكمل منطق اللعبة العادي
        }

        // ✅ منطق خاص للتحديات (الفوز الفوري)
        if (isChallengeMode && (ball.type == BallType.Eight || ball.number == 8))
        {
            if (ChallengeManager.Instance) ChallengeManager.Instance.WinChallenge();
            return;
        }

        if (ball.type == BallType.Cue)
        {
            if (isBreakShot) scratchOnBreak = true;
            else foulCommitted = true;
            return;
        }

        if (ball.type == BallType.Eight || ball.number == 8)
        {
            pocketedEightBall = true;
            return;
        }

        // منطق اللعبة العادية (Solids/Stripes)
        if (!isChallengeMode)
        {
            BallGroup ballGroup = (ball.type == BallType.Solid) ? BallGroup.Solids : BallGroup.Stripes;
            BallGroup playerGroup = GetPlayerGroup(currentPlayer);

            if (player1Group == BallGroup.Unassigned && player2Group == BallGroup.Unassigned)
            {
                AssignGroup(currentPlayer, ballGroup);
                pocketedOwnBall = true;
                AddScore(currentPlayer);
            }
            else if (playerGroup == ballGroup)
            {
                pocketedOwnBall = true;
                AddScore(currentPlayer);
            }
            else
            {
                Player opponent = (currentPlayer == Player.Player1) ? Player.Player2 : Player.Player1;
                AddScore(opponent);
            }
        }
    }

    public void OnCueBallHitBall(Ball3D hitBall)
    {
        if (validHit || !hitBall || !shotInProgress) return;

        // في التحدي، أي ضربة تعتبر صحيحة مبدئياً لتسهيل اللعب
        if (isChallengeMode)
        {
            validHit = true;
            return;
        }

        BallGroup playerGroup = GetPlayerGroup(currentPlayer);

        if (player1Group == BallGroup.Unassigned && player2Group == BallGroup.Unassigned)
        {
            if (hitBall.type != BallType.Eight) validHit = true;
            return;
        }

        BallGroup hitGroup = (hitBall.type == BallType.Solid) ? BallGroup.Solids : BallGroup.Stripes;

        if (hitGroup == playerGroup) validHit = true;
        else if (hitBall.type == BallType.Eight && AllOwnBallsPocketed(currentPlayer)) validHit = true;
        else foulCommitted = true;
    }

    public void OnBallHitCushion(Ball3D ball)
    {
        if (shotInProgress) hitAnyCushion = true;
    }

    void EvaluateShot()
    {
        // =========================================================
        // 🧩 منطق وضع التحدي (Challenge Mode Logic)
        // =========================================================
        if (isChallengeMode)
        {
            // 1. إذا التحدي انتهى بالفعل (فوز أو خسارة سابقة)، لا تفعل شيئاً
            if (ChallengeManager.Instance && !ChallengeManager.Instance.isChallengeActive)
            {
                ResetShotTracking();
                return;
            }

            // 2. التحقق من الخسارة: سقوط الكرة البيضاء (Scratch)
            if (anyBallPocketed && ballsPocketedThisShot.Any(b => b.type == BallType.Cue))
            {
                Debug.Log("💀 Lost: Cue Ball Scratched!");
                if (ChallengeManager.Instance) ChallengeManager.Instance.LoseChallenge();
            }

            // 3. التحقق من الخسارة: انتهاء عدد الضربات
            // (نصل هنا فقط إذا لم نفز، لأن الفوز يتم التقاطه فوراً في OnBallPocketed)
            else if (ChallengeManager.Instance && ChallengeManager.Instance.shotsTaken >= ChallengeManager.Instance.currentLevel.maxShots)
            {
                Debug.Log("💀 Lost: Out of Shots!");
                ChallengeManager.Instance.LoseChallenge();
            }

            ResetShotTracking();
            return; // 🛑 توقف هنا، لا تكمل منطق اللعبة العادية
        }

        // =========================================================
        // 🎱 منطق اللعبة العادية (Standard Game Logic)
        // =========================================================

        if (scratchOnBreak)
        {
            TriggerFoulSound(); SwitchPlayer(); ResetShotTracking(); return;
        }
        if (!validHit && !isBreakShot) foulCommitted = true;
        if (!isBreakShot && validHit && !foulCommitted && !anyBallPocketed && !hitAnyCushion) foulCommitted = true;

        if (foulCommitted)
        {
            OnFoulCommitted?.Invoke(); TriggerFoulSound(); SwitchPlayer(); ResetShotTracking(); return;
        }

        if (pocketedEightBall)
        {
            if (AllOwnBallsPocketed(currentPlayer)) WinGame(currentPlayer);
            else LoseGame("Potted 8-Ball Early!");
            return;
        }

        if (pocketedOwnBall) { /* Continue */ }
        else SwitchPlayer();

        isBreakShot = false;
        ResetShotTracking();
    }

    // دوال مساعدة
    void TriggerFoulSound() { if (gameAudioSource && foulSound) gameAudioSource.PlayOneShot(foulSound); }

    void WinGame(Player player)
    {
        gameOver = true; winner = player; canShoot = false; shotInProgress = false;
        OnGameWon?.Invoke(player);
        if (gameUI) { gameUI.ShowWinPanel(player.ToString()); if (player == Player.Player1) gameUI.PlayWinSound(); else gameUI.PlayLoseSound(); }
    }

    void LoseGame(string reason)
    {
        gameOver = true; canShoot = false; shotInProgress = false;
        if (gameUI) gameUI.ShowLosePanel(reason);
    }

    void ResetShotTracking()
    {
        validHit = false; hitAnyCushion = false; anyBallPocketed = false;
        pocketedOwnBall = false; pocketedEightBall = false; foulCommitted = false;
        scratchOnBreak = false; ballsPocketedThisShot.Clear();
    }

    void SwitchPlayer() { currentPlayer = (currentPlayer == Player.Player1) ? Player.Player2 : Player.Player1; OnPlayerChanged?.Invoke(currentPlayer); }

    void AssignGroup(Player player, BallGroup group)
    {
        if (player == Player.Player1) { player1Group = group; player2Group = (group == BallGroup.Solids) ? BallGroup.Stripes : BallGroup.Solids; }
        else { player2Group = group; player1Group = (group == BallGroup.Solids) ? BallGroup.Stripes : BallGroup.Solids; }
        OnGroupAssigned?.Invoke(player, group);
    }

    void AddScore(Player player) { if (player == Player.Player1) player1Score++; else player2Score++; OnScoreChanged?.Invoke(player, GetPlayerScore(player)); }

    bool AllOwnBallsPocketed(Player player)
    {
        BallGroup group = GetPlayerGroup(player);
        if (group == BallGroup.Unassigned) return false;
        foreach (var ball in allBalls)
        {
            if (!ball || ball.inPocket) continue; if (ball.type == BallType.Eight) continue;
            BallGroup bg = (ball.type == BallType.Solid) ? BallGroup.Solids : BallGroup.Stripes; if (bg == group) return false;
        }
        return true;
    }

    public BallGroup GetPlayerGroup(Player player) => (player == Player.Player1) ? player1Group : player2Group;
    public int GetPlayerScore(Player player) => (player == Player.Player1) ? player1Score : player2Score;
    public string GetPlayerGroupText(Player player) { BallGroup g = GetPlayerGroup(player); return g == BallGroup.Unassigned ? "Unassigned" : g.ToString(); }
}