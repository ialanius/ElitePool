using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Automated Tester - Runs automated tests on game systems
/// Use this to catch bugs before releasing
/// </summary>
public class AutomatedTester : MonoBehaviour
{
    [Header("Test Configuration")]
    public bool runTestsOnStart = false;
    public bool runTestsInEditor = true;
    public bool runTestsInBuild = false;

    [Header("Test Selection")]
    public bool testPhysics = true;
    public bool testGameState = true;
    public bool testUI = true;
    public bool testAI = true;
    public bool testSceneTransitions = true;

    [Header("Test Parameters")]
    public int physicsTestDuration = 10;
    public int aiTestShots = 5;

    // Results
    private int testsRun = 0;
    private int testsPassed = 0;
    private int testsFailed = 0;
    private List<string> failedTests = new List<string>();

    void Start()
    {
        bool shouldRun = (Application.isEditor && runTestsInEditor) ||
                         (!Application.isEditor && runTestsInBuild);

        if (runTestsOnStart && shouldRun)
        {
            StartCoroutine(RunAllTests());
        }
    }

    [ContextMenu("Run All Tests")]
    public void RunAllTestsManually()
    {
        StartCoroutine(RunAllTests());
    }

    IEnumerator RunAllTests()
    {
        Debug.Log("=== STARTING AUTOMATED TESTS ===");

        testsRun = 0;
        testsPassed = 0;
        testsFailed = 0;
        failedTests.Clear();

        yield return new WaitForSeconds(1f);

        // Physics Tests
        if (testPhysics)
        {
            yield return StartCoroutine(TestPhysicsSystem());
        }

        // Game State Tests
        if (testGameState)
        {
            yield return StartCoroutine(TestGameStateSystem());
        }

        // UI Tests
        if (testUI)
        {
            yield return StartCoroutine(TestUISystem());
        }

        // AI Tests
        if (testAI)
        {
            yield return StartCoroutine(TestAISystem());
        }

        // Scene Transition Tests
        if (testSceneTransitions)
        {
            TestSceneTransitionSystem();
        }

        // Print Results
        PrintTestResults();
    }

    // ================================================================
    // PHYSICS TESTS
    // ================================================================

    IEnumerator TestPhysicsSystem()
    {
        Debug.Log("[TEST] Testing Physics System...");

        // Test 1: Check if balls exist
        Test("Balls exist in scene", () =>
        {
            Ball3D[] balls = FindObjectsOfType<Ball3D>();
            return balls.Length > 0;
        });

        // Test 2: Check if balls have rigidbodies
        Test("Balls have Rigidbodies", () =>
        {
            Ball3D[] balls = FindObjectsOfType<Ball3D>();
            foreach (var ball in balls)
            {
                if (!ball.GetComponent<Rigidbody>())
                    return false;
            }
            return balls.Length > 0;
        });

        // Test 3: Check if balls have colliders
        Test("Balls have Colliders", () =>
        {
            Ball3D[] balls = FindObjectsOfType<Ball3D>();
            foreach (var ball in balls)
            {
                if (!ball.GetComponent<Collider>())
                    return false;
            }
            return balls.Length > 0;
        });

        // Test 4: Physics timestep
        Test("Physics timestep is optimal", () =>
        {
            return Time.fixedDeltaTime >= 0.01f && Time.fixedDeltaTime <= 0.03f;
        });

        // Test 5: Check pockets
        Test("All 6 pockets exist", () =>
        {
            GameObject[] pockets = GameObject.FindGameObjectsWithTag("Pocket");
            if (pockets.Length == 0)
                pockets = GameObject.FindGameObjectsWithTag("Pockets");
            return pockets.Length == 6;
        });

        yield return null;
    }

    // ================================================================
    // GAME STATE TESTS
    // ================================================================

    IEnumerator TestGameStateSystem()
    {
        Debug.Log("[TEST] Testing Game State System...");

        GameStateManager gameState = FindObjectOfType<GameStateManager>();

        // Test 1: GameStateManager exists
        Test("GameStateManager exists", () =>
        {
            return gameState != null;
        });

        if (gameState)
        {
            // Test 2: Initial state
            Test("Game starts in correct state", () =>
            {
                return gameState.currentPlayer == Player.Player1;
            });

            // Test 3: Ball groups
            Test("Ball groups initialized", () =>
            {
                return gameState.GetPlayerGroup(Player.Player1) == BallGroup.Unassigned &&
                       gameState.GetPlayerGroup(Player.Player2) == BallGroup.Unassigned;
            });
        }

        yield return null;
    }

    // ================================================================
    // UI TESTS
    // ================================================================

    IEnumerator TestUISystem()
    {
        Debug.Log("[TEST] Testing UI System...");

        // Test 1: Canvas exists
        Test("Canvas exists", () =>
        {
            return FindObjectOfType<Canvas>() != null;
        });

        // Test 2: EventSystem exists
        Test("EventSystem exists", () =>
        {
            return FindObjectOfType<UnityEngine.EventSystems.EventSystem>() != null;
        });

        // Test 3: Buttons are clickable
        Test("Buttons have GraphicRaycaster", () =>
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (!canvas) return false;
            return canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>() != null;
        });

        yield return null;
    }

    // ================================================================
    // AI TESTS
    // ================================================================

    IEnumerator TestAISystem()
    {
        Debug.Log("[TEST] Testing AI System...");

        AIPlayer ai = FindObjectOfType<AIPlayer>();

        // Test 1: AIPlayer exists (if in AI scene)
        if (ai != null)
        {
            Test("AIPlayer configured", () =>
            {
                return ai.aiPlayerID == Player.Player2;
            });

            // Test 2: AI references
            Test("AI has required references", () =>
            {
                return ai.cueStick != null && ai.gameState != null;
            });

            // Test 3: Pockets tagged correctly
            Test("AI can find pockets", () =>
            {
                return ai.pockets != null && ai.pockets.Length > 0;
            });
        }

        yield return null;
    }

    // ================================================================
    // SCENE TRANSITION TESTS
    // ================================================================

    void TestSceneTransitionSystem()
    {
        Debug.Log("[TEST] Testing Scene Transition System...");

        SceneTransitionManager transition = SceneTransitionManager.Instance;

        // Test 1: SceneTransitionManager exists
        Test("SceneTransitionManager exists", () =>
        {
            return transition != null;
        });

        if (transition)
        {
            // Test 2: Fade image configured
            Test("Fade image configured", () =>
            {
                return transition.fadeImage != null;
            });
        }
    }

    // ================================================================
    // TEST HELPER
    // ================================================================

    void Test(string testName, System.Func<bool> testFunction)
    {
        testsRun++;

        try
        {
            bool result = testFunction();

            if (result)
            {
                testsPassed++;
                Debug.Log("[TEST PASS] " + testName);
            }
            else
            {
                testsFailed++;
                failedTests.Add(testName);
                Debug.LogWarning("[TEST FAIL] " + testName);
            }
        }
        catch (System.Exception e)
        {
            testsFailed++;
            failedTests.Add(testName);
            Debug.LogError("[TEST ERROR] " + testName + ": " + e.Message);
        }
    }

    void PrintTestResults()
    {
        Debug.Log("=== TEST RESULTS ===");
        Debug.Log("Total Tests: " + testsRun);
        Debug.Log("Passed: " + testsPassed + " (" + (testsPassed * 100f / testsRun).ToString("F1") + "%)");
        Debug.Log("Failed: " + testsFailed);

        if (failedTests.Count > 0)
        {
            Debug.LogWarning("Failed Tests:");
            foreach (var test in failedTests)
            {
                Debug.LogWarning("  - " + test);
            }
        }

        Debug.Log("==================");
    }
}