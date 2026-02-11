using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum Player { None, Player1, Player2 }
public enum BallGroup { Unassigned, Solids, Stripes }

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance;

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

    [Header("Foul Tracking")]
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
    public Ball3D[] allBalls;

    // ✅✅ مرجع الـ UI والأصوات
    [Header("UI & Audio Integration")]
    public GameUI gameUI; // اسحب كائن GameUI هنا
    public AudioSource gameAudioSource; // مصدر صوتي خاص بالمدير (للفاول مثلاً)
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
        // محاولة العثور على GameUI إذا نسيت ربطه
        if (!gameUI) gameUI = FindObjectOfType<GameUI>();
        if (!gameAudioSource) gameAudioSource = GetComponent<AudioSource>();

        RefreshBallReferences();
        ResetGame();
    }

    public void RefreshBallReferences()
    {
        if (!poolManager) poolManager = PoolGameManager3D.Instance;
        if (poolManager)
        {
            cueBall = poolManager.cueBall;
            allBalls = poolManager.balls;
            foreach (var ball in allBalls)
            {
                if (ball.number == 8 || ball.type == BallType.Eight)
                {
                    eightBall = ball;
                    break;
                }
            }
        }
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
    }

    public void OnShotStart()
    {
        if (!canShoot || gameOver) return;
        ResetShotTracking();
        shotInProgress = true;
        canShoot = false;
        Debug.Log($"Shot started by {currentPlayer}");
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
        if (!ball || gameOver) return;
        if (!shotInProgress) return;

        ballsPocketedThisShot.Add(ball);
        anyBallPocketed = true;

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

    public void OnCueBallHitBall(Ball3D hitBall)
    {
        if (validHit || !hitBall || !shotInProgress) return;

        BallGroup playerGroup = GetPlayerGroup(currentPlayer);

        if (player1Group == BallGroup.Unassigned && player2Group == BallGroup.Unassigned)
        {
            validHit = true;
            return;
        }

        BallGroup hitGroup = (hitBall.type == BallType.Solid) ? BallGroup.Solids : BallGroup.Stripes;

        if (hitGroup == playerGroup)
        {
            validHit = true;
        }
        else if (hitBall.type == BallType.Eight && AllOwnBallsPocketed(currentPlayer))
        {
            validHit = true;
        }
        else
        {
            foulCommitted = true;
        }
    }

    public void OnBallHitCushion(Ball3D ball)
    {
        if (!shotInProgress) return;
        hitAnyCushion = true;
    }

    void EvaluateShot()
    {
        // 1) Scratch on break
        if (scratchOnBreak)
        {
            TriggerFoulSound();
            SwitchPlayer();
            ResetShotTracking();
            return;
        }

        // 2) No ball hit
        if (!validHit && !isBreakShot)
        {
            foulCommitted = true;
        }

        // 3) Cushion rule
        if (!isBreakShot && validHit && !foulCommitted)
        {
            if (!anyBallPocketed && !hitAnyCushion)
            {
                foulCommitted = true;
            }
        }

        // 4) Fouls handling
        if (foulCommitted)
        {
            OnFoulCommitted?.Invoke();
            TriggerFoulSound(); // ✅ تشغيل صوت الفاول
            SwitchPlayer();
            ResetShotTracking();
            return;
        }

        // 5) Eight Ball Logic
        if (pocketedEightBall)
        {
            // فوز نظيف: أدخل كل كراته ثم الـ 8
            if (AllOwnBallsPocketed(currentPlayer))
            {
                WinGame(currentPlayer);
            }
            // خسارة: أدخل الـ 8 مبكراً
            else
            {
                Player opponent = (currentPlayer == Player.Player1) ? Player.Player2 : Player.Player1;
                Debug.Log("LOST - pocketed 8-ball before clearing own balls");

                // ✅ استدعاء دالة الخسارة للاعب الحالي
                LoseGame("Potted 8-Ball Early!");
            }
            return;
        }

        // 6) Continue or Switch
        if (pocketedOwnBall)
        {
            // Player continues
        }
        else
        {
            SwitchPlayer();
        }

        isBreakShot = false;
        ResetShotTracking();
    }

    // ✅ دالة تشغيل صوت الفاول
    void TriggerFoulSound()
    {
        // الأولوية لـ GameStateManager لتشغيل الصوت فوراً
        if (gameAudioSource && foulSound)
        {
            gameAudioSource.PlayOneShot(foulSound);
        }
        // أو استخدام GameUI إذا كان الصوت هناك
        else if (gameUI && gameUI.uiAudioSource && gameUI.foulSound)
        {
            gameUI.uiAudioSource.PlayOneShot(gameUI.foulSound);
        }
    }

    // ✅ دالة إنهاء اللعبة
    void WinGame(Player player)
    {
        gameOver = true;
        winner = player;
        canShoot = false;
        shotInProgress = false;

        OnGameWon?.Invoke(player);
        Debug.Log($" {player} WINS!");

        // ✅✅ تشغيل أصوات الفوز والخسارة عبر GameUI
        if (gameUI)
        {
            // نعرض اللوحة صراحة (رغم أن الحدث يفعل ذلك أيضاً)
            gameUI.ShowWinPanel(player.ToString());

            // لنفترض أن Player 1 هو "أنت"
            if (player == Player.Player1)
            {
                gameUI.PlayWinSound();
            }
            else
            {
                // إذا فاز الخصم، نشغل صوت الخسارة
                gameUI.PlayLoseSound();
            }
        }
    }

    // ✅ دالة جديدة للخسارة
    void LoseGame(string reason)
    {
        gameOver = true;
        canShoot = false;
        shotInProgress = false;

        // اللاعب الحالي هو من خسر
        Debug.Log($" {currentPlayer} LOST! Reason: {reason}");

        if (gameUI)
        {
            gameUI.ShowLosePanel(reason); // إظهار اللوحة الحمراء
            // الصوت تم تشغيله داخل ShowLosePanel في GameUI
        }
    }
    void ResetShotTracking()
    {
        validHit = false;
        hitAnyCushion = false;
        anyBallPocketed = false;
        pocketedOwnBall = false;
        pocketedEightBall = false;
        foulCommitted = false;
        scratchOnBreak = false;
        ballsPocketedThisShot.Clear();
    }

    void SwitchPlayer()
    {
        currentPlayer = (currentPlayer == Player.Player1) ? Player.Player2 : Player.Player1;
        OnPlayerChanged?.Invoke(currentPlayer);
    }

    void AssignGroup(Player player, BallGroup group)
    {
        if (player == Player.Player1)
        {
            player1Group = group;
            player2Group = (group == BallGroup.Solids) ? BallGroup.Stripes : BallGroup.Solids;
        }
        else
        {
            player2Group = group;
            player1Group = (group == BallGroup.Solids) ? BallGroup.Stripes : BallGroup.Solids;
        }
        OnGroupAssigned?.Invoke(player, group);
    }

    void AddScore(Player player)
    {
        if (player == Player.Player1) player1Score++;
        else player2Score++;
        OnScoreChanged?.Invoke(player, GetPlayerScore(player));
    }

    bool AllOwnBallsPocketed(Player player)
    {
        BallGroup group = GetPlayerGroup(player);
        if (group == BallGroup.Unassigned) return false;

        foreach (var ball in allBalls)
        {
            if (!ball || ball.inPocket) continue;
            if (ball.type == BallType.Eight) continue;

            BallGroup ballGroup = (ball.type == BallType.Solid) ? BallGroup.Solids : BallGroup.Stripes;
            if (ballGroup == group) return false;
        }
        return true;
    }

    public BallGroup GetPlayerGroup(Player player)
    {
        return (player == Player.Player1) ? player1Group : player2Group;
    }

    public int GetPlayerScore(Player player)
    {
        return (player == Player.Player1) ? player1Score : player2Score;
    }

    public string GetPlayerGroupText(Player player)
    {
        BallGroup group = GetPlayerGroup(player);
        return group == BallGroup.Unassigned ? "Unassigned" : group.ToString();
    }
}