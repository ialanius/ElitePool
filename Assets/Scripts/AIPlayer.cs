using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// AI Player - Enhanced Version
/// Smart AI opponent with 3 difficulty levels and advanced strategy
/// </summary>
public class AIPlayer : MonoBehaviour
{
    [Header("AI Configuration")]
    public bool isAIEnabled = true;
    public Player aiPlayerID = Player.Player2;
    public AIDifficulty difficulty = AIDifficulty.Medium;

    [Header("Thinking & Strategy")]
    [Tooltip("Minimum thinking time (seconds)")]
    public float thinkingTimeMin = 1.5f;
    [Tooltip("Maximum thinking time (seconds)")]
    public float thinkingTimeMax = 3.5f;
    [Tooltip("Delay before shooting (for realism)")]
    public float shotPreparationTime = 0.8f;
    [Tooltip("Use defensive play strategy")]
    public bool useDefensivePlay = true;
    [Tooltip("Prefer easy shots")]
    public bool preferEasyShots = true;

    [Header("Physics & Power")]
    [Tooltip("Minimum power")]
    [Range(0.1f, 0.5f)]
    public float minPower = 0.25f;
    [Tooltip("Maximum power")]
    [Range(0.5f, 1f)]
    public float maxPower = 0.85f;
    [Tooltip("Break shot power multiplier")]
    public float breakPowerMultiplier = 1.3f;

    [Header("Accuracy Settings")]
    [Tooltip("Easy mode error (degrees)")]
    public float errorEasy = 12f;
    [Tooltip("Medium mode error (degrees)")]
    public float errorMedium = 5f;
    [Tooltip("Hard mode error (degrees)")]
    public float errorHard = 1f;
    [Tooltip("Power calculation error")]
    [Range(0f, 0.1f)]
    public float powerError = 0.03f;

    [Header("References")]
    public GameStateManager gameState;
    public CueStickController3D cueStick;
    public Transform[] pockets;

    [Header("Detection")]
    [Tooltip("Layer for balls and obstacles")]
    public LayerMask obstacleLayer;
    [Tooltip("Ball radius")]
    public float ballRadius = 0.25f;
    [Tooltip("Raycast distance")]
    public float raycastDistance = 20f;

    [Header("Debug")]
    public bool showDebugLogs = true;
    public bool showGizmos = true;

    // Private variables
    private bool isThinking = false;
    private Ball3D cueBall;
    private List<Ball3D> myBalls = new List<Ball3D>();
    private List<Ball3D> opponentBalls = new List<Ball3D>();
    private Ball3D eightBall;
    private BestShot currentBestShot;

    void Start()
    {
        // Find components
        if (!gameState) gameState = GameStateManager.Instance;
        if (!cueStick) cueStick = FindObjectOfType<CueStickController3D>();

        // Subscribe to player changed event
        if (gameState)
        {
            gameState.OnPlayerChanged.AddListener(OnTurnChanged);
        }

        // Find pockets
        FindPockets();

        Log("AI initialized - Difficulty: " + difficulty);
    }

    void FindPockets()
    {
        if (pockets != null && pockets.Length > 0) return;

        List<Transform> foundPockets = new List<Transform>();

        // Search by tag
        var pocketObjs = GameObject.FindGameObjectsWithTag("Pocket");
        if (pocketObjs.Length == 0) pocketObjs = GameObject.FindGameObjectsWithTag("Pockets");

        if (pocketObjs.Length > 0)
        {
            foundPockets.AddRange(pocketObjs.Select(p => p.transform));
        }
        else
        {
            // Search by name as fallback
            var allObjects = FindObjectsOfType<Transform>();
            foundPockets.AddRange(allObjects.Where(t =>
                t.name.ToLower().Contains("pocket") ||
                t.name.ToLower().Contains("hole")
            ));
        }

        if (foundPockets.Count > 0)
        {
            pockets = foundPockets.ToArray();
            Log("Found " + pockets.Length + " pockets");
        }
        else
        {
            LogWarning("No pockets found! AI may not work properly.");
        }
    }

    void OnTurnChanged(Player currentTurn)
    {
        if (!isAIEnabled) return;
        if (currentTurn != aiPlayerID) return;

        // Slight delay before thinking (for realism)
        StartCoroutine(DelayedThink());
    }

    IEnumerator DelayedThink()
    {
        yield return new WaitForSeconds(0.3f);
        StartCoroutine(ThinkAndShoot());
    }

    IEnumerator ThinkAndShoot()
    {
        if (isThinking) yield break;
        isThinking = true;

        Log("AI is thinking...");

        // Thinking time
        float waitTime = Random.Range(thinkingTimeMin, thinkingTimeMax);
        yield return new WaitForSeconds(waitTime);

        // Update ball lists
        RefreshBallLists();

        // Analyze table
        BestShot bestShot = AnalyzeTable();
        currentBestShot = bestShot;

        if (bestShot != null && bestShot.targetBall != null)
        {
            Log("AI targeting ball " + bestShot.targetBall.number + " to Pocket (Score: " + bestShot.score.ToString("F1") + ")");
            yield return StartCoroutine(ExecuteShot(bestShot));
        }
        else
        {
            LogWarning("No valid shot found - shooting randomly");
            yield return StartCoroutine(ShootRandomly());
        }

        isThinking = false;
    }

    void RefreshBallLists()
    {
        var allBalls = FindObjectsOfType<Ball3D>();

        // Cue ball
        cueBall = allBalls.FirstOrDefault(b => b.type == BallType.Cue);

        // Eight ball
        eightBall = allBalls.FirstOrDefault(b => b.type == BallType.Eight);

        // Clear lists
        myBalls.Clear();
        opponentBalls.Clear();

        // Determine my group
        BallGroup myGroup = gameState.GetPlayerGroup(aiPlayerID);

        // Determine opponent group
        Player opponent = (aiPlayerID == Player.Player1) ? Player.Player2 : Player.Player1;
        BallGroup opponentGroup = gameState.GetPlayerGroup(opponent);

        foreach (var ball in allBalls)
        {
            if (ball.inPocket || ball.type == BallType.Cue) continue;

            // Open table
            if (myGroup == BallGroup.Unassigned)
            {
                if (ball.type != BallType.Eight)
                {
                    myBalls.Add(ball);
                }
            }
            // Have assigned group
            else
            {
                if ((myGroup == BallGroup.Solids && ball.type == BallType.Solid) ||
                    (myGroup == BallGroup.Stripes && ball.type == BallType.Stripe))
                {
                    myBalls.Add(ball);
                }
                else if ((opponentGroup == BallGroup.Solids && ball.type == BallType.Solid) ||
                         (opponentGroup == BallGroup.Stripes && ball.type == BallType.Stripe))
                {
                    opponentBalls.Add(ball);
                }
            }
        }

        // If cleared my balls, target the 8
        if (myBalls.Count == 0 && eightBall && !eightBall.inPocket)
        {
            myBalls.Add(eightBall);
            Log("All my balls pocketed - targeting 8 ball!");
        }

        Log("My balls: " + myBalls.Count + ", Opponent balls: " + opponentBalls.Count);
    }

    BestShot AnalyzeTable()
    {
        if (!cueBall)
        {
            LogWarning("No cue ball found!");
            return null;
        }

        if (pockets == null || pockets.Length == 0)
        {
            LogWarning("No pockets found!");
            return null;
        }

        if (myBalls.Count == 0)
        {
            LogWarning("No target balls!");
            return null;
        }

        List<BestShot> possibleShots = new List<BestShot>();

        // Analyze each ball with each pocket
        foreach (var ball in myBalls)
        {
            if (ball == null || ball.inPocket) continue;

            foreach (var pocket in pockets)
            {
                if (pocket == null) continue;

                BestShot shot = EvaluateShot(ball, pocket);
                if (shot != null && shot.isValid)
                {
                    possibleShots.Add(shot);
                }
            }
        }

        if (possibleShots.Count == 0)
        {
            Log("No valid shots found");
            return null;
        }

        // Sort by score
        possibleShots = possibleShots.OrderByDescending(s => s.score).ToList();

        Log("Found " + possibleShots.Count + " possible shots");

        // Select shot by difficulty
        return SelectShotByDifficulty(possibleShots);
    }

    BestShot EvaluateShot(Ball3D targetBall, Transform pocket)
    {
        if (!targetBall || !pocket || !cueBall) return null;

        BestShot shot = new BestShot();
        shot.targetBall = targetBall;
        shot.targetPocket = pocket;

        Vector3 pocketPos = pocket.position;
        Vector3 ballPos = targetBall.transform.position;
        Vector3 cuePos = cueBall.transform.position;

        // Direction from ball to pocket
        Vector3 ballToPocket = (pocketPos - ballPos);
        ballToPocket.y = 0;
        ballToPocket.Normalize();

        // Ghost ball position
        Vector3 ghostBallPos = ballPos - (ballToPocket * ballRadius * 2f);

        // Shoot direction
        Vector3 shootDir = (ghostBallPos - cuePos);
        shootDir.y = 0;
        float distanceToGhost = shootDir.magnitude;
        shootDir.Normalize();

        shot.shootDirection = shootDir;
        shot.distanceToTarget = distanceToGhost;

        // Distance from ball to pocket
        float distanceToPocket = Vector3.Distance(ballPos, pocketPos);
        shot.distanceToPocket = distanceToPocket;

        // Check for obstacles
        if (!CheckClearPath(cuePos, shootDir, distanceToGhost, targetBall))
        {
            shot.isValid = false;
            return shot;
        }

        // Calculate angle
        Vector3 cueToBall = (ballPos - cuePos).normalized;
        float angle = Vector3.Angle(shootDir, cueToBall);
        shot.angle = angle;

        // Too difficult angle
        if (angle > 80f)
        {
            shot.isValid = false;
            return shot;
        }

        // Calculate required power
        float totalDistance = distanceToGhost + distanceToPocket;
        float neededPower = Mathf.Clamp(totalDistance / 15f, minPower, maxPower);

        // Increase power slightly for safety
        neededPower *= 1.1f;
        neededPower = Mathf.Clamp01(neededPower);

        shot.power = neededPower;
        shot.isValid = true;

        // Calculate score (higher = better)
        float score = 100f;

        // Penalties
        score -= angle * 1.5f;                    // Difficult angle = lower score
        score -= distanceToGhost * 2f;            // Far = lower score
        score -= distanceToPocket * 1.5f;         // Pocket far = lower score

        // Rewards
        if (distanceToPocket < 1.2f)              // Ball close to pocket
            score += 25f;
        if (angle < 30f)                          // Almost straight shot
            score += 20f;
        if (distanceToGhost < 2f)                 // Cue ball close
            score += 15f;

        // Prefer easy shots
        if (preferEasyShots && angle < 20f && distanceToGhost < 3f)
            score += 30f;

        shot.score = score;

        return shot;
    }

    bool CheckClearPath(Vector3 from, Vector3 direction, float maxDistance, Ball3D targetBall)
    {
        // Raycast to check for obstacles
        RaycastHit[] hits = Physics.SphereCastAll(from, ballRadius * 0.8f, direction, maxDistance, obstacleLayer);

        foreach (var hit in hits)
        {
            // Ignore cue ball
            if (hit.collider.gameObject == cueBall.gameObject)
                continue;

            // Ignore target ball
            if (hit.collider.gameObject == targetBall.gameObject)
                continue;

            // Obstacle found!
            Ball3D hitBall = hit.collider.GetComponent<Ball3D>();
            if (hitBall != null)
            {
                return false; // Another ball in the way
            }
        }

        return true; // Path is clear
    }

    BestShot SelectShotByDifficulty(List<BestShot> shots)
    {
        if (shots.Count == 0) return null;

        int selection = 0;

        switch (difficulty)
        {
            case AIDifficulty.Easy:
                // Easy: Random selection from first half
                int easyRange = Mathf.Max(1, shots.Count / 2);
                selection = Random.Range(0, easyRange);
                break;

            case AIDifficulty.Medium:
                // Medium: Select from best 3-4 shots
                int mediumRange = Mathf.Min(4, shots.Count);
                selection = Random.Range(0, mediumRange);
                break;

            case AIDifficulty.Hard:
                // Hard: Always best shot
                selection = 0;
                break;
        }

        return shots[selection];
    }

    IEnumerator ExecuteShot(BestShot shot)
    {
        if (!cueStick || shot == null)
        {
            LogWarning("Cannot execute shot!");
            yield break;
        }

        // Calculate error based on difficulty
        float errorAmount = difficulty == AIDifficulty.Hard ? errorHard :
                           difficulty == AIDifficulty.Medium ? errorMedium : errorEasy;

        // Apply directional error
        Vector3 finalDir = shot.shootDirection;
        float randomError = Random.Range(-errorAmount, errorAmount);
        finalDir = Quaternion.Euler(0, randomError, 0) * finalDir;
        finalDir.y = 0;
        finalDir.Normalize();

        // Set aim direction
        cueStick.SetAimDirection(finalDir);

        Log("Aiming at ball " + shot.targetBall.number + " with error: " + randomError.ToString("F2") + " degrees");

        // Wait (shot preparation)
        yield return new WaitForSeconds(shotPreparationTime);

        // Calculate final power with slight error
        float finalPower = shot.power;
        float powerErrorAmount = Random.Range(-powerError, powerError);
        finalPower += powerErrorAmount;
        finalPower = Mathf.Clamp01(finalPower);

        // Break shot needs more power
        if (gameState && gameState.isBreakShot)
        {
            finalPower *= breakPowerMultiplier;
            finalPower = Mathf.Clamp01(finalPower);
            Log("Break shot! Power boosted to " + finalPower.ToString("F2"));
        }

        // Set power
        cueStick.SetPower(finalPower);

        Log("Power set to " + finalPower.ToString("F2"));

        // Wait a bit before shooting
        yield return new WaitForSeconds(0.2f);

        // Shoot!
        cueStick.ExecuteAIShot();

        Log("Shot executed!");
    }

    IEnumerator ShootRandomly()
    {
        if (!cueStick) yield break;

        Log("Shooting randomly...");

        // Random direction
        Vector3 randomDir = new Vector3(
            Random.Range(-1f, 1f),
            0,
            Random.Range(-1f, 1f)
        ).normalized;

        cueStick.SetAimDirection(randomDir);

        yield return new WaitForSeconds(0.5f);

        // Random medium power
        float randomPower = Random.Range(0.4f, 0.7f);
        cueStick.SetPower(randomPower);

        yield return new WaitForSeconds(0.2f);

        cueStick.ExecuteAIShot();
    }

    // Public Methods
    public void SetDifficulty(AIDifficulty newDifficulty)
    {
        difficulty = newDifficulty;
        Log("Difficulty changed to: " + difficulty);
    }

    public void SetAIEnabled(bool enabled)
    {
        isAIEnabled = enabled;
        Log("AI " + (enabled ? "Enabled" : "Disabled"));
    }

    // Logging
    void Log(string message)
    {
        if (showDebugLogs)
            Debug.Log("[AI] " + message);
    }

    void LogWarning(string message)
    {
        if (showDebugLogs)
            Debug.LogWarning("[AI] " + message);
    }

    // Gizmos (for debugging)
    void OnDrawGizmos()
    {
        if (!showGizmos || !Application.isPlaying) return;
        if (currentBestShot == null || !currentBestShot.isValid) return;
        if (!cueBall || !currentBestShot.targetBall) return;

        // Line from cue ball to target ball
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(cueBall.transform.position, currentBestShot.targetBall.transform.position);

        // Line from target ball to pocket
        if (currentBestShot.targetPocket)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(currentBestShot.targetBall.transform.position, currentBestShot.targetPocket.position);
        }

        // Point on target ball
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(currentBestShot.targetBall.transform.position, 0.1f);
    }
}

// Best Shot Class
public class BestShot
{
    public Ball3D targetBall;
    public Transform targetPocket;
    public Vector3 shootDirection;
    public float power;
    public float score;
    public bool isValid;

    // Additional info
    public float distanceToTarget;
    public float distanceToPocket;
    public float angle;
}

// AI Difficulty Enum
public enum AIDifficulty
{
    Easy,      // Easy - many mistakes
    Medium,    // Medium - good accuracy
    Hard       // Hard - very high accuracy
}