using System.Collections;
using System.Collections.Generic;
using TMPro.Examples;
using UnityEngine;

public class ChallengeManager : MonoBehaviour
{
    public static ChallengeManager Instance;

    [Header("Essential Prefabs & Data")]
    public ChallengeLevelData currentLevel; // اسحب ملف المرحلة هنا
    public GameObject ballPrefab;           // اسحب بريفاب الكرة هنا (مهم جداً!)

    [Header("Levels List 📚")]
    public ChallengeLevelData[] allLevels; // ضع كل ملفات المراحل هنا

    [Header("Materials")]
    public Material cueBallMaterial;
    public Material eightBallMaterial;
    public Material[] randomBallMaterials;

    [Header("Auto-Detected References (Don't touch)")]
    public GameStateManager gameState;
    public CueStickController3D cueStick;
    public GameUI gameUI;
    public CameraController mainCamera;

    [Header("Status")]
    public int shotsTaken = 0;
    public bool isChallengeActive = false;

    void Awake()
    {
        // إعداد النسخة الوحيدة (Singleton)
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    IEnumerator Start()
    {
        Debug.Log("⏳ ChallengeManager: Initializing sequence...");

        // 1. تجميد الوقت مؤقتاً لضمان استقرار السكربتات
        // yield return new WaitForEndOfFrame(); 

        // 2. البحث الذكي عن المكونات المفقودة
        FindAllReferences();

        // 3. إجبار النظام على وضع التحدي
        SetupGameState();

        // 4. تجهيز الواجهة
        SetupUI();

        // 5. الانتظار قليلاً لضمان تحميل كل شيء (هذا هو سر حل مشكلة الريستارت)
        yield return new WaitForSeconds(0.1f);

        // 6. البدء الفعلي للمرحلة
        if (currentLevel != null)
        {
            StartChallenge(currentLevel);
        }
        else
        {
            Debug.LogError("❌ ChallengeManager: No Level Data Assigned!");
        }
    }

    void FindAllReferences()
    {
        // البحث عن المدراء والعناصر الأساسية
        if (!gameState) gameState = GameStateManager.Instance;
        if (!gameUI) gameUI = FindObjectOfType<GameUI>();
        if (!cueStick) cueStick = FindObjectOfType<CueStickController3D>();
        if (!mainCamera) mainCamera = Camera.main.GetComponent<CameraController>();

        // ربط السلايدر بالعصا يدوياً إذا كانت الواجهة والعصا موجودين
        if (cueStick && gameUI)
        {
            if (gameUI.powerSlider) cueStick.powerSlider = gameUI.powerSlider;
            if (gameUI.powerMeterPanel) cueStick.powerSliderPanel = gameUI.powerMeterPanel;
            cueStick.SetSliderInteractable(true);
        }
    }

    void SetupGameState()
    {
        if (gameState)
        {
            gameState.isChallengeMode = true;
            gameState.canShoot = true; // السماح باللعب فوراً
            gameState.isBreakShot = false;
            gameState.player1Score = 0;
            gameState.player2Score = 0;
            gameState.gameOver = false;
        }
    }

    void SetupUI()
    {
        if (gameUI)
        {
            // إظهار لوحة التحدي وإخفاء العادية
            if (gameUI.standardGamePanel) gameUI.standardGamePanel.SetActive(false);
            if (gameUI.challengePanel) gameUI.challengePanel.SetActive(true);

            // تحديث النصوص الأولية
            if (currentLevel)
            {
                gameUI.UpdateChallengeText(currentLevel.maxShots);
                if (gameUI.levelNameText) gameUI.levelNameText.text = currentLevel.levelName;
            }
        }
    }

    public void StartChallenge(ChallengeLevelData level)
    {
        Debug.Log($"🚀 Starting Level: {level.levelName}");
        currentLevel = level;
        shotsTaken = 0;
        isChallengeActive = true;

        // 1. تنظيف الطاولة بالكامل
        ClearTable();

        // 2. وضع الكرة البيضاء
        SpawnCueBall(level.cueBallPosition);

        // 3. وضع باقي الكرات
        if (level.targetBalls != null)
        {
            foreach (var target in level.targetBalls)
            {
                SpawnTargetBall(target.position, target.type);
            }
        }

        // 4. ✅ تحديث الواجهة (UI) بما فيها الوصف الجديد
        if (gameUI)
        {
            // تحديث عدد الضربات
            gameUI.UpdateChallengeText(level.maxShots);

            // تحديث اسم المرحلة
            if (gameUI.levelNameText)
                gameUI.levelNameText.text = level.levelName;

            if (gameUI.levelNameText2)
                gameUI.levelNameText2.text = level.levelName;

            if (gameUI.levelDescriptionText)
                gameUI.levelDescriptionText.text = level.description;

            if (gameUI.levelInfoPanel)
            {
                gameUI.levelInfoPanel.SetActive(true);
                gameUI.SetGameplayControlsActive(false);
            }

            // إظهار رسالة ترحيبية
            //gameUI.ShowMessage($"Challenge: {level.maxShots} Shots Left");
        }

        // 5. ✅ تحديث مراجع الفيزياء ليعرف المدير عن الكرات الجديدة
        if (gameState) gameState.RefreshBallReferences();
        if (PoolGameManager3D.Instance) PoolGameManager3D.Instance.RefreshRefs();
    }

    void ClearTable()
    {
        Ball3D[] balls = FindObjectsOfType<Ball3D>();
        foreach (var b in balls)
        {
            Destroy(b.gameObject);
        }
    }
    // دالة مساعدة لضبط فيزياء الكرة لتطابق اللعبة الأصلية
    void SetupBallPhysics(GameObject ballObj)
    {
        Rigidbody rb = ballObj.GetComponent<Rigidbody>();
        if (rb)
        {
            // إعدادات الفيزياء القياسية (من PoolGameManager)
            rb.mass = 0.17f;
            rb.drag = 0.05f;
            rb.angularDrag = 0.3f;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // تنويم الكرة فوراً
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }

        // إضافة مادة فيزيائية للاحتكاك
        Collider col = ballObj.GetComponent<Collider>();
        if (col)
        {
            PhysicMaterial mat = new PhysicMaterial();
            mat.dynamicFriction = 0.15f;
            mat.staticFriction = 0.15f;
            mat.bounciness = 0.85f;
            mat.frictionCombine = PhysicMaterialCombine.Minimum;
            mat.bounceCombine = PhysicMaterialCombine.Maximum;
            col.material = mat;
        }
    }
    void SpawnCueBall(Vector3 pos)
    {
        if (!ballPrefab) return;

        GameObject cbObj = Instantiate(ballPrefab, pos, Quaternion.identity);
        cbObj.name = "CueBall";

        Ball3D ballScript = cbObj.GetComponent<Ball3D>();
        if (ballScript) ballScript.type = BallType.Cue;

        ApplyMaterial(cbObj, cueBallMaterial);

        // ✅ ضبط الفيزياء
        SetupBallPhysics(cbObj);

        if (cueStick)
        {
            cueStick.cueBall = cbObj.transform;
            cueStick.ResetStickBehindCueBall(true);
        }

        if (mainCamera)
        {
            mainCamera.CameraTarget = cbObj.transform;
        }

        var aimLine = FindObjectOfType<AimLine3D>();
        if (aimLine) aimLine.cueBall = cbObj.transform;
    }

    void SpawnTargetBall(Vector3 pos, BallType type)
    {
        if (!ballPrefab) return;

        GameObject ballObj = Instantiate(ballPrefab, pos, Quaternion.identity);
        Ball3D ballScript = ballObj.GetComponent<Ball3D>();
        ballScript.type = type;

        if (type == BallType.Eight)
        {
            ballObj.name = "8-Ball";
            ballScript.number = 8;
            ApplyMaterial(ballObj, eightBallMaterial);
        }
        else
        {
            ballScript.number = Random.Range(1, 15);
            ballObj.name = $"Target_{ballScript.number}";
            if (randomBallMaterials.Length > 0)
                ApplyMaterial(ballObj, randomBallMaterials[Random.Range(0, randomBallMaterials.Length)]);
        }

        // ✅ ضبط الفيزياء
        SetupBallPhysics(ballObj);
    }

    void ApplyMaterial(GameObject obj, Material mat)
    {
        if (!mat) return;
        Renderer r = obj.GetComponent<Renderer>();
        if (r) r.material = mat;
    }

    // يتم استدعاؤها من GameState عند تنفيذ ضربة
    public void OnShotTaken()
    {
        if (!isChallengeActive) return;

        shotsTaken++;
        int left = currentLevel.maxShots - shotsTaken;

        if (gameUI) gameUI.UpdateChallengeText(left);

        // ملاحظة: منطق الفوز والخسارة يتم التحقق منه بعد توقف الكرات في GameStateManager
    }

    public void WinChallenge()
    {
        isChallengeActive = false;
        Debug.Log("🎉 Challenge Won!");
        if (gameUI) gameUI.ShowWinPanel("Challenge Complete!");
    }

    public void LoseChallenge()
    {
        isChallengeActive = false;
        Debug.Log("💀 Challenge Lost!");
        if (gameUI) gameUI.ShowLosePanel("Out of shots!");
    }

    public void LoadNextLevel()
    {
        // 1. البحث عن فهرس المرحلة الحالية
        int currentIndex = -1;
        for (int i = 0; i < allLevels.Length; i++)
        {
            if (allLevels[i] == currentLevel)
            {
                currentIndex = i;
                break;
            }
        }

        // 2. هل توجد مرحلة بعدها؟
        if (currentIndex != -1 && currentIndex < allLevels.Length - 1)
        {
            // نعم، شغل المرحلة التالية
            StartChallenge(allLevels[currentIndex + 1]);
        }
        else
        {
            Debug.Log("You Finished All Levels!");
            // هنا ممكن ترجعه للقائمة الرئيسية أو تعيد المرحلة الأولى
            if (gameUI) gameUI.ShowMessage("ALL LEVELS COMPLETED!");
        }
    }

    public void closeInfoPanel()
    {

        if (gameUI.levelInfoPanel)
        {
            gameUI.levelInfoPanel.SetActive(false);
            gameUI.SetGameplayControlsActive(true);
        }
    }
}