using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AIPlayer : MonoBehaviour
{
    [Header("🤖 AI Configuration")]
    public bool isAIEnabled = true;
    public Player aiPlayerID = Player.Player2;
    public AIDifficulty difficulty = AIDifficulty.Medium;

    [Header("🧠 Thinking Parameters")]
    public float thinkingTimeMin = 2.0f;
    public float thinkingTimeMax = 4.0f;
    public LayerMask obstacleLayer;

    [Header("⚡ Physics & Accuracy")]
    public float minPower = 0.2f;
    public float maxPower = 0.9f;
    public float errorEasy = 10f;
    public float errorMedium = 4f;
    public float errorHard = 0.5f;

    [Header("🔗 References")]
    public GameStateManager gameState;
    public CueStickController3D cueStick;
    public Transform[] pockets;

    private bool isThinking = false;
    private Ball3D cueBall;
    private List<Ball3D> myBalls = new List<Ball3D>();
    private Ball3D eightBall;

    void Start()
    {
        if (!gameState) gameState = GameStateManager.Instance;
        if (!cueStick) cueStick = FindObjectOfType<CueStickController3D>();

        if (gameState)
        {
            gameState.OnPlayerChanged.AddListener(OnTurnChanged);
        }

        if (pockets == null || pockets.Length == 0)
        {
            var pocketObjs = GameObject.FindGameObjectsWithTag("Pocket");
            if (pocketObjs.Length == 0) pocketObjs = GameObject.FindGameObjectsWithTag("Pockets");
            if (pocketObjs.Length > 0) pockets = pocketObjs.Select(p => p.transform).ToArray();
        }
    }

    void OnTurnChanged(Player currentTurn)
    {
        if (!isAIEnabled) return;

        if (currentTurn == aiPlayerID)
        {
            StartCoroutine(ThinkAndShoot());
        }
    }

    IEnumerator ThinkAndShoot()
    {
        if (isThinking) yield break;
        isThinking = true;

        float waitTime = Random.Range(thinkingTimeMin, thinkingTimeMax);
        yield return new WaitForSeconds(waitTime);

        RefreshBallLists();
        BestShot bestShot = AnalyzeTable();

        if (bestShot != null)
        {
            Debug.Log($"🤖 AI Targeting Ball {bestShot.targetBall.number}");
            yield return StartCoroutine(ExecuteShot(bestShot));
        }
        else
        {
            yield return StartCoroutine(ShootRandomly());
        }

        isThinking = false;
    }

    void RefreshBallLists()
    {
        var allBalls = FindObjectsOfType<Ball3D>();
        cueBall = allBalls.FirstOrDefault(b => b.type == BallType.Cue);
        eightBall = allBalls.FirstOrDefault(b => b.type == BallType.Eight);

        myBalls.Clear();
        BallGroup myGroup = gameState.GetPlayerGroup(aiPlayerID);

        foreach (var ball in allBalls)
        {
            if (ball.inPocket || ball.type == BallType.Cue) continue;

            if (myGroup == BallGroup.Unassigned)
            {
                if (ball.type != BallType.Eight) myBalls.Add(ball);
            }
            else if (myGroup == BallGroup.Solids && ball.type == BallType.Solid)
            {
                myBalls.Add(ball);
            }
            else if (myGroup == BallGroup.Stripes && ball.type == BallType.Stripe)
            {
                myBalls.Add(ball);
            }
        }

        if (myBalls.Count == 0 && eightBall && !eightBall.inPocket)
        {
            myBalls.Add(eightBall);
        }
    }

    BestShot AnalyzeTable()
    {
        if (!cueBall) return null;
        List<BestShot> possibleShots = new List<BestShot>();

        foreach (var ball in myBalls)
        {
            foreach (var pocket in pockets)
            {
                BestShot shot = EvaluateShot(ball, pocket);
                if (shot != null) possibleShots.Add(shot);
            }
        }

        if (possibleShots.Count == 0) return null;
        possibleShots = possibleShots.OrderByDescending(s => s.score).ToList();

        if (difficulty == AIDifficulty.Easy)
        {
            int range = Mathf.Max(1, possibleShots.Count / 2);
            return possibleShots[Random.Range(0, range)];
        }
        else if (difficulty == AIDifficulty.Medium)
        {
            int range = Mathf.Min(3, possibleShots.Count);
            return possibleShots[Random.Range(0, range)];
        }
        else return possibleShots[0];
    }

    BestShot EvaluateShot(Ball3D targetBall, Transform pocket)
    {
        // ... (نفس منطق الحسابات السابق) ...
        Vector3 pocketPos = pocket.position;
        Vector3 ballPos = targetBall.transform.position;
        Vector3 cuePos = cueBall.transform.position;

        Vector3 ballToPocket = (pocketPos - ballPos).normalized;
        Vector3 ghostBallPos = ballPos - (ballToPocket * 0.5f);
        Vector3 shootDir = (ghostBallPos - cuePos).normalized;
        float distanceToGhost = Vector3.Distance(cuePos, ghostBallPos);
        float distanceToPocket = Vector3.Distance(ballPos, pocketPos);

        if (Physics.SphereCast(cuePos, 0.2f, shootDir, out RaycastHit hit, distanceToGhost, obstacleLayer))
        {
            if (hit.collider.gameObject != targetBall.gameObject) return null;
        }

        float angle = Vector3.Angle(shootDir, ballToPocket);
        if (angle > 85f) return null;

        float score = 100f - (angle * 1.2f) - (distanceToGhost * 1.5f) - (distanceToPocket * 1f);
        if (distanceToPocket < 1.0f) score += 20f;

        float neededPower = Mathf.Clamp((distanceToGhost + distanceToPocket) / 15f, minPower, maxPower);
        neededPower *= 1.1f;
        if (neededPower > 1f) neededPower = 1f;

        return new BestShot { targetBall = targetBall, targetPocket = pocket, shootDirection = shootDir, power = neededPower, score = score };
    }

    IEnumerator ExecuteShot(BestShot shot)
    {
        if (!cueStick) yield break;

        float errorAmount = (difficulty == AIDifficulty.Hard) ? errorHard : (difficulty == AIDifficulty.Medium) ? errorMedium : errorEasy;
        Vector3 finalDir = Quaternion.Euler(0, Random.Range(-errorAmount, errorAmount), 0) * shot.shootDirection;

        cueStick.SetAimDirection(finalDir);
        yield return new WaitForSeconds(1.0f);

        float finalPower = Mathf.Clamp01(shot.power + Random.Range(-0.02f, 0.02f));
        cueStick.Shoot(finalPower);

        yield return new WaitForSeconds(3.0f);
    }

    IEnumerator ShootRandomly()
    {
        Vector3 randomDir = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
        cueStick.SetAimDirection(randomDir);
        yield return new WaitForSeconds(0.5f);
        cueStick.Shoot(Random.Range(0.5f, 0.8f));
    }

    public void SetDifficulty(AIDifficulty newDifficulty) => difficulty = newDifficulty;
    public void SetAIEnabled(bool enabled) => isAIEnabled = enabled;

    class BestShot { public Ball3D targetBall; public Transform targetPocket; public Vector3 shootDirection; public float power; public float score; }
}

public enum AIDifficulty { Easy, Medium, Hard }