using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System;

/// <summary>
/// Bug Reporter - Automatic bug detection and reporting
/// Catches common issues and logs them clearly
/// </summary>
public class BugReporter : MonoBehaviour
{
    public static BugReporter Instance { get; private set; }

    [Header("Detection Settings")]
    public bool enableBugDetection = true;
    public bool detectPhysicsIssues = true;
    public bool detectUIIssues = true;
    public bool detectPerformanceIssues = true;
    public bool detectMemoryLeaks = true;

    [Header("Performance Thresholds")]
    [Tooltip("Log warning if FPS drops below this")]
    public int lowFPSThreshold = 30;

    [Tooltip("Log error if physics takes too long (ms)")]
    public float maxPhysicsTime = 10f;

    [Tooltip("Log warning if GC collects too often")]
    public int maxGCPerMinute = 5;

    [Header("Logging")]
    public bool logToConsole = true;
    public bool logToFile = false;
    public string logFilePath = "bug_report.txt";

    [Header("Auto-Fix")]
    public bool autoFixCommonIssues = true;

    // Tracking
    private List<BugReport> detectedBugs = new List<BugReport>();
    private float lastFPSCheck = 0f;
    private float fpsCheckInterval = 1f;
    private int frameCount = 0;
    private float currentFPS = 0f;

    private int gcCount = 0;
    private float gcTimer = 0f;

    private Dictionary<string, int> errorCounts = new Dictionary<string, int>();

    [System.Serializable]
    public class BugReport
    {
        public string bugType;
        public string description;
        public string stackTrace;
        public float timestamp;
        public BugSeverity severity;
    }

    public enum BugSeverity
    {
        Low,        // Minor issue, game still playable
        Medium,     // Noticeable issue, may affect gameplay
        High,       // Major issue, gameplay affected
        Critical    // Game breaking, unplayable
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Subscribe to Unity error logs
            Application.logMessageReceived += HandleLog;

            StartDetection();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void StartDetection()
    {
        if (enableBugDetection)
        {
            Debug.Log("[BugReporter] Bug detection started");

            // Initial checks
            CheckInitialSetup();
        }
    }

    void Update()
    {
        if (!enableBugDetection) return;

        // Performance checks
        if (detectPerformanceIssues)
        {
            UpdateFPS();
            CheckPerformance();
        }

        // Memory checks
        if (detectMemoryLeaks)
        {
            UpdateGCCounter();
        }
    }

    void FixedUpdate()
    {
        if (!enableBugDetection || !detectPhysicsIssues) return;

        // Physics timing check
        float physicsTime = Time.fixedDeltaTime * 1000f;
        if (physicsTime > maxPhysicsTime)
        {
            ReportBug(
                "Physics Performance",
                "Physics update taking too long: " + physicsTime.ToString("F2") + "ms",
                BugSeverity.Medium
            );
        }
    }

    // ================================================================
    // DETECTION METHODS
    // ================================================================

    void CheckInitialSetup()
    {
        Debug.Log("[BugReporter] Running initial setup checks...");

        // Check for essential components
        CheckEssentialManagers();
        CheckPhysicsSettings();
        CheckUISetup();
    }

    void CheckEssentialManagers()
    {
        // Check for GameStateManager
        if (FindObjectOfType<GameStateManager>() == null)
        {
            ReportBug(
                "Missing Component",
                "GameStateManager not found in scene!",
                BugSeverity.Critical
            );
        }

        // Check for HapticManager
        if (FindObjectOfType<HapticManager>() == null)
        {
            ReportBug(
                "Missing Component",
                "HapticManager not found - haptics won't work",
                BugSeverity.Low
            );
        }

        // Check for SceneTransitionManager
        if (FindObjectOfType<SceneTransitionManager>() == null)
        {
            ReportBug(
                "Missing Component",
                "SceneTransitionManager not found - transitions won't work",
                BugSeverity.Medium
            );
        }
    }

    void CheckPhysicsSettings()
    {
        // Check physics timestep
        if (Time.fixedDeltaTime > 0.03f)
        {
            ReportBug(
                "Physics Settings",
                "Physics timestep too high (" + Time.fixedDeltaTime + "), may cause instability",
                BugSeverity.Medium
            );

            if (autoFixCommonIssues)
            {
                Time.fixedDeltaTime = 0.02f;
                Debug.Log("[BugReporter] Auto-fixed: Set physics timestep to 0.02");
            }
        }

        // Check if balls exist
        Ball3D[] balls = FindObjectsOfType<Ball3D>();
        if (balls.Length == 0)
        {
            ReportBug(
                "Game Setup",
                "No balls found in scene!",
                BugSeverity.Critical
            );
        }
        else
        {
            // Check each ball
            foreach (var ball in balls)
            {
                Rigidbody rb = ball.GetComponent<Rigidbody>();
                if (!rb)
                {
                    ReportBug(
                        "Missing Component",
                        "Ball '" + ball.name + "' missing Rigidbody!",
                        BugSeverity.Critical
                    );
                }

                if (!ball.GetComponent<Collider>())
                {
                    ReportBug(
                        "Missing Component",
                        "Ball '" + ball.name + "' missing Collider!",
                        BugSeverity.Critical
                    );
                }
            }
        }

        // Check for pockets gracefully
        GameObject[] pockets = new GameObject[0];
        try
        {
            pockets = GameObject.FindGameObjectsWithTag("Pocket");
            if (pockets.Length == 0)
            {
                pockets = GameObject.FindGameObjectsWithTag("Pockets");
            }
        }
        catch (UnityException)
        {
            // إذا لم يكن التاج موجوداً في Unity، نسجلها كمشكلة بدل أن نعلق اللعبة
            ReportBug(
                "Missing Tag",
                "The tag 'Pocket' or 'Pockets' is not defined in Unity Project Settings!",
                BugSeverity.High
            );
        }

        if (pockets.Length < 6)
        {
            ReportBug(
                "Game Setup",
                "Expected 6 pockets, found " + pockets.Length,
                BugSeverity.High
            );
        }
    }

    void CheckUISetup()
    {
        if (!detectUIIssues) return;

        // Check for Canvas
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        if (canvases.Length == 0)
        {
            ReportBug(
                "UI Setup",
                "No Canvas found in scene!",
                BugSeverity.High
            );
        }

        // Check for EventSystem
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            ReportBug(
                "UI Setup",
                "No EventSystem found - UI won't be clickable!",
                BugSeverity.High
            );
        }
    }

    void UpdateFPS()
    {
        frameCount++;
        lastFPSCheck += Time.unscaledDeltaTime;

        if (lastFPSCheck >= fpsCheckInterval)
        {
            currentFPS = frameCount / lastFPSCheck;
            frameCount = 0;
            lastFPSCheck = 0f;
        }
    }

    void CheckPerformance()
    {
        if (currentFPS < lowFPSThreshold && currentFPS > 0)
        {
            ReportBug(
                "Performance",
                "Low FPS detected: " + currentFPS.ToString("F1"),
                BugSeverity.Medium
            );
        }
    }

    void UpdateGCCounter()
    {
        gcTimer += Time.unscaledDeltaTime;

        int currentGC = GC.CollectionCount(0);
        if (currentGC > gcCount)
        {
            gcCount = currentGC;

            if (gcTimer < 60f)
            {
                int gcPerMinute = Mathf.RoundToInt(gcCount / (gcTimer / 60f));

                if (gcPerMinute > maxGCPerMinute)
                {
                    ReportBug(
                        "Memory",
                        "High GC frequency: " + gcPerMinute + " per minute",
                        BugSeverity.Low
                    );
                }
            }
        }

        if (gcTimer >= 60f)
        {
            gcTimer = 0f;
            gcCount = 0;
        }
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (!enableBugDetection) return;

        if (type == LogType.Error || type == LogType.Exception)
        {
            // Count error frequency
            if (!errorCounts.ContainsKey(logString))
            {
                errorCounts[logString] = 0;
            }
            errorCounts[logString]++;

            // Report if it's a new error or repeating too much
            if (errorCounts[logString] == 1 || errorCounts[logString] % 10 == 0)
            {
                BugSeverity severity = type == LogType.Exception ?
                    BugSeverity.High : BugSeverity.Medium;

                ReportBug(
                    type.ToString(),
                    logString + (errorCounts[logString] > 1 ?
                        " (x" + errorCounts[logString] + ")" : ""),
                    severity,
                    stackTrace
                );
            }
        }
    }

    // ================================================================
    // REPORTING
    // ================================================================

    void ReportBug(string bugType, string description, BugSeverity severity, string stackTrace = "")
    {
        BugReport bug = new BugReport
        {
            bugType = bugType,
            description = description,
            stackTrace = stackTrace,
            timestamp = Time.time,
            severity = severity
        };

        detectedBugs.Add(bug);

        if (logToConsole)
        {
            string prefix = GetSeverityPrefix(severity);
            Debug.LogWarning(prefix + " [" + bugType + "] " + description);
        }

        if (logToFile)
        {
            WriteToFile(bug);
        }
    }

    string GetSeverityPrefix(BugSeverity severity)
    {
        switch (severity)
        {
            case BugSeverity.Low: return "[BUG-LOW]";
            case BugSeverity.Medium: return "[BUG-MED]";
            case BugSeverity.High: return "[BUG-HIGH]";
            case BugSeverity.Critical: return "[BUG-CRITICAL]";
            default: return "[BUG]";
        }
    }

    void WriteToFile(BugReport bug)
    {
        try
        {
            string path = Application.persistentDataPath + "/" + logFilePath;
            string line = string.Format(
                "[{0}] [{1}] {2}: {3}\n",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                bug.severity,
                bug.bugType,
                bug.description
            );

            System.IO.File.AppendAllText(path, line);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to write bug report to file: " + e.Message);
        }
    }

    // ================================================================
    // PUBLIC METHODS
    // ================================================================

    public void GenerateReport()
    {
        StringBuilder report = new StringBuilder();

        report.AppendLine("=== BUG REPORT ===");
        report.AppendLine("Generated: " + DateTime.Now);
        report.AppendLine("Total Bugs: " + detectedBugs.Count);
        report.AppendLine();

        // Group by severity
        int critical = detectedBugs.FindAll(b => b.severity == BugSeverity.Critical).Count;
        int high = detectedBugs.FindAll(b => b.severity == BugSeverity.High).Count;
        int medium = detectedBugs.FindAll(b => b.severity == BugSeverity.Medium).Count;
        int low = detectedBugs.FindAll(b => b.severity == BugSeverity.Low).Count;

        report.AppendLine("By Severity:");
        report.AppendLine("  Critical: " + critical);
        report.AppendLine("  High: " + high);
        report.AppendLine("  Medium: " + medium);
        report.AppendLine("  Low: " + low);
        report.AppendLine();

        report.AppendLine("Details:");
        foreach (var bug in detectedBugs)
        {
            report.AppendLine("---");
            report.AppendLine("Type: " + bug.bugType);
            report.AppendLine("Severity: " + bug.severity);
            report.AppendLine("Description: " + bug.description);
            report.AppendLine("Time: " + bug.timestamp.ToString("F2") + "s");
            report.AppendLine();
        }

        Debug.Log(report.ToString());
    }

    public List<BugReport> GetBugsBySeveity(BugSeverity severity)
    {
        return detectedBugs.FindAll(b => b.severity == severity);
    }

    public void ClearBugs()
    {
        detectedBugs.Clear();
        errorCounts.Clear();
        Debug.Log("[BugReporter] Bug list cleared");
    }

    public int GetBugCount()
    {
        return detectedBugs.Count;
    }

    public bool HasCriticalBugs()
    {
        return detectedBugs.Exists(b => b.severity == BugSeverity.Critical);
    }
}