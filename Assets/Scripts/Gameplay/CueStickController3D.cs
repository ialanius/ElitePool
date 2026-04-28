using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;



public class CueStickController3D : MonoBehaviour
{
    [Header("Refs")]
    public Transform cueBall;
    public Camera cam;
    public ScratchManager scratch;
    public Rigidbody[] allBalls;
    public GameStateManager gameState;

    [Header("Visual")]
    public Transform visualRoot;
    public bool hideWhenReleased = true;

    [Header("8BP Power Slider")]
    public Slider powerSlider;
    public GameObject powerSliderPanel;
    public float minPower = 4f;
    public float maxPower = 12f;
    public AnimationCurve powerCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float breakBoost = 1.35f;
    public GameObject wheelButton;

    [Header("Aiming")]
    public float stickDistance = 0.14f;
    public float pullBackDistance = 0.40f;
    public float stopSpeed = 0.02f;

    [Header("Stick Fix")]
    public bool invertStickDirection = true;
    public bool modelForwardIsWrong = true;
    public float tipOffset = 0.1f;
    public float cueBallRadius = 0.25f;

    [Header("Aiming")]
    public bool dragStickHandle = true;
    public float stickDistancee = 0.14f;

    [Header("Shot Animation")]
    public bool animateShot = true;
    public float shootForwardDistance = 0.25f;
    public float shootForwardTime = 0.06f;
    public float recoilBackTime = 0.08f;
    public float returnTime = 0.10f;

    [Header("Aim Line")]
    public AimLine3D aimLine;

    [Header("Stick Body Scale")]
    public Transform stickBody;
    Vector3 bodyBaseScale = Vector3.one;

    [Header("Camera Kick")]
    public bool enableCameraKick = true;
    public float camKickMin = 0.01f;
    public float camKickMax = 0.04f;
    public float camKickTime = 0.06f;
    Coroutine camKickCo;

    [Header("Raycast")]
    public LayerMask tableMask;
    public Vector3 defaultAimDir = Vector3.right;

    [Header("Audio")]
    public AudioSource cueAudioSource;
    public AudioClip cueHitSound;

    [Header("Spin System")]
    public SpinControllerAdvanced spinController;

    [Header("Cue Elevation (Rail Fix)")]
    public bool autoElevateStick = true;    // تفعيل الرفع التلقائي
    public float stickLength = 1.5f;        // طول العصا التقريبي
    public float maxElevationAngle = 35f;   // أقصى زاوية لرفع العصا للأعلى
    public LayerMask cushionLayer;          // الطبقة (Layer) الخاصة بحواف الطاولة
    // State
    enum ShootState { Aiming, ReadyToShoot, Shooting }
    ShootState currentState = ShootState.Aiming;

    bool isDraggingToAim = false;
    bool isSliderBeingDragged = false;
    bool stickPositionInitialized = false;
    public float power01 { get; private set; }

    // ✅ للـAI: جعل shotDir قابل للوصول
    internal Vector3 shotDir = Vector3.right;
    internal Vector3 aimDirection = Vector3.right;
    [Header("AI Reference")]
    public AIPlayer aiPlayer; // 👈 اسحب كائن الـ AI هنا
    int activeFingerId = -1;

    Vector3 baseStickPos;
    Quaternion baseStickRot;
    Coroutine shotCo;
    Vector3 stickVisualDir = Vector3.forward;

    void Awake()
    {
        if (!cam) cam = Camera.main;
        if (hideWhenReleased && visualRoot)
            visualRoot.gameObject.SetActive(false);
        if (stickBody)
            bodyBaseScale = stickBody.localScale;

        if (!gameState) gameState = GameStateManager.Instance;

        // ✅✅✅ الإضافة هنا: البحث التلقائي عن Spin Controller
        if (!spinController)
        {
            spinController = FindObjectOfType<SpinControllerAdvanced>();
        }

        if (powerSlider)
        {
            powerSlider.minValue = 0f;
            powerSlider.maxValue = 1f;
            powerSlider.value = 0f;

            powerSlider.onValueChanged.AddListener(OnSliderValueChanged);

            EventTrigger trigger = powerSlider.gameObject.GetComponent<EventTrigger>();
            if (!trigger) trigger = powerSlider.gameObject.AddComponent<EventTrigger>();

            EventTrigger.Entry pointerDown = new EventTrigger.Entry();
            pointerDown.eventID = EventTriggerType.PointerDown;
            pointerDown.callback.AddListener((data) => { OnSliderDragStart(); });
            trigger.triggers.Add(pointerDown);

            EventTrigger.Entry pointerUp = new EventTrigger.Entry();
            pointerUp.eventID = EventTriggerType.PointerUp;
            pointerUp.callback.AddListener((data) => { OnSliderDragEnd(); });
            trigger.triggers.Add(pointerUp);
        }

        //if (powerSliderPanel) powerSliderPanel.SetActive(false);
    }

    void Update()
    {
        // ✅ إذا كان الزمن متوقفاً (القائمة مفتوحة)، لا تفعل شيئاً
        if (Time.timeScale == 0f) return;

        // ✅✅ التعديل الذكي:
        // نمنع التحكم فقط إذا:
        // 1. يوجد سكربت AI
        // 2. الـ AI مفعل (شغال)
        // 3. الدور الحالي هو دور الـ AI
        if (aiPlayer && aiPlayer.isAIEnabled && gameState && gameState.currentPlayer == aiPlayer.aiPlayerID)
        {
            return; // هذا دور الـ AI، لا تلمس العصا!
        }

        if (!cueBall || !cam) return;

        if (gameState && gameState.gameOver) { Hide(); return; }
        if (gameState && !gameState.canShoot) { Hide(); return; }
        if (scratch && scratch.IsPlacing) { Hide(); return; }
        if (!BallsStopped()) { Hide(); return; }

        if (!stickPositionInitialized)
        {
            InitializeStickPosition();
        }

        switch (currentState)
        {
            case ShootState.Aiming:
                HandleAiming();
                break;

            case ShootState.ReadyToShoot:
                HandleReadyToShoot();
                break;

            case ShootState.Shooting:
                break;
        }
    }

    void InitializeStickPosition()
    {
        if (!cueBall) return;

        stickPositionInitialized = true;

        if (shotDir.sqrMagnitude < 0.0001f)
        {
            shotDir = SafeDefaultDir();
        }

        if (visualRoot && !visualRoot.gameObject.activeSelf)
            visualRoot.gameObject.SetActive(true);

        // ✅ إضافة: إظهار السلايدر فوراً عند تجهيز العصا
        if (powerSliderPanel) powerSliderPanel.SetActive(true);
        if (wheelButton) wheelButton.SetActive(true);
        UpdateStick(0f);
    }

    void HandleAiming()
    {
        if (visualRoot && !visualRoot.gameObject.activeSelf)
            visualRoot.gameObject.SetActive(true);

        if (!isDraggingToAim && GetPointerDown(out Vector2 pos, out int fid))
        {
            if (IsPointerOverSlider()) return;

            isDraggingToAim = true;
            activeFingerId = fid;
            UpdateAimDirection(pos);
            UpdateStick(0f);
        }

        if (isDraggingToAim && GetPointerHeld(activeFingerId, out Vector2 curPos))
        {
            UpdateAimDirection(curPos);
            UpdateStick(0f);
        }

        if (isDraggingToAim && GetPointerUp(activeFingerId))
        {
            isDraggingToAim = false;
            activeFingerId = -1;

            currentState = ShootState.ReadyToShoot;
            if (wheelButton) wheelButton.SetActive(true);
            if (powerSliderPanel) powerSliderPanel.SetActive(true);
            if (powerSlider) powerSlider.value = 0f;
            power01 = 0f;
        }
    }

    void HandleReadyToShoot()
    {
        if (!isSliderBeingDragged && GetPointerDown(out Vector2 pos, out _))
        {
            if (!IsPointerOverSlider())
            {
                currentState = ShootState.Aiming;
                //if (powerSliderPanel) powerSliderPanel.SetActive(false);
                if (powerSlider) powerSlider.value = 0f;
                power01 = 0f;
                UpdateStick(0f);
            }
        }
    }

    void OnSliderDragStart()
    {
        isSliderBeingDragged = true;
    }

    void OnSliderDragEnd()
    {
        isSliderBeingDragged = false;

        /*if (currentState == ShootState.ReadyToShoot && power01 > 0.01f)
        {
            Shoot();
        }*/
        if ((currentState == ShootState.ReadyToShoot || currentState == ShootState.Aiming) && power01 > 0.01f)
        {
            Shoot();
        }
    }

    void OnSliderValueChanged(float value)
    {
        //if (currentState != ShootState.ReadyToShoot) return;

        if (currentState == ShootState.Aiming)
            currentState = ShootState.ReadyToShoot;

        power01 = value;
        UpdateStick(power01);
    }

    bool IsPointerOverSlider()
    {
        if (!powerSlider || !powerSlider.gameObject.activeSelf) return false;
        if (EventSystem.current == null) return false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current);

        if (Input.GetMouseButton(0) || Input.GetMouseButtonDown(0))
            pointerData.position = Input.mousePosition;
        else if (Input.touchCount > 0)
            pointerData.position = Input.GetTouch(0).position;
        else
            return false;

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (var result in results)
        {
            if (result.gameObject == powerSlider.gameObject ||
                result.gameObject.transform.IsChildOf(powerSlider.transform))
                return true;
        }

        return false;
    }

    public void Shoot()
    {
        currentState = ShootState.Shooting;

        if (powerSliderPanel) powerSliderPanel.SetActive(false);
        if (aimLine) aimLine.Hide();
        if (wheelButton) wheelButton.SetActive(false);
        if (!cueBall) return;

        var rb = cueBall.GetComponent<Rigidbody>();
        if (!rb) return;

        // ✅✅✅ مهم جداً: أخبر GameStateManager إن الضربة بدأت!
        if (gameState)
        {
            gameState.OnShotStart();
        }

        rb.isKinematic = false;
        rb.WakeUp();

        Vector3 dir = shotDir;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = SafeDefaultDir();

        float curved = (powerCurve != null) ? powerCurve.Evaluate(power01) : power01;
        float finalPower = Mathf.Lerp(minPower, maxPower, curved);

        if (gameState && gameState.isBreakShot)
            finalPower *= breakBoost;
        // Haptic feedback حسب قوة الضربة
        if (power01 < 0.3f)
            Haptics.Light();        // ضربة خفيفة
        else if (power01 < 0.7f)
            Haptics.Medium();       // ضربة متوسطة
        else
            Haptics.Heavy();        // ضربة قوية
        // تشغيل صوت الضربة
        if (cueAudioSource && cueHitSound)
        {
            // تغيير حدة الصوت بناءً على قوة الضربة ليعطي واقعية
            cueAudioSource.pitch = 0.9f + (power01 * 0.2f); // كلما كانت أقوى، زادت الحدة قليلاً
            cueAudioSource.PlayOneShot(cueHitSound, 0.8f + (power01 * 0.2f)); // الصوت يعلو مع القوة
        }

        Ball3D ball = cueBall.GetComponent<Ball3D>();
        if (ball) ball.inPocket = false;

        // ✅ الضربة باتجاه shotDir
        if (ball != null)
            ball.Shoot(dir, finalPower);

        else
            rb.AddForce(dir.normalized * finalPower, ForceMode.VelocityChange);

        // ✅ تطبيق الـSpin
        if (spinController)
        {
            spinController.ApplySpin(rb, dir, finalPower);
        }

        ClampLaunchVelocity(rb);

        if (PoolGameManager3D.Instance)
            PoolGameManager3D.Instance.RegisterShot(rb);

        if (enableCameraKick)
        {
            if (camKickCo != null) StopCoroutine(camKickCo);
            float amt = Mathf.Lerp(camKickMin, camKickMax, power01);
            camKickCo = StartCoroutine(CameraKick(amt, camKickTime));
        }

        if (animateShot && visualRoot)
        {
            if (shotCo != null) StopCoroutine(shotCo);
            shotCo = StartCoroutine(ShotAnimThenHide());
        }
        else
        {
            Hide();
        }
    }

    void ClampLaunchVelocity(Rigidbody rb)
    {
        if (!rb) return;

        BallSafety safety = rb.GetComponent<BallSafety>();
        float maxSpeed = safety ? safety.maxVelocity : 14f;
        float maxUpward = safety ? safety.maxUpwardVelocity : 0.12f;

        if (maxSpeed > 0f && rb.velocity.sqrMagnitude > maxSpeed * maxSpeed)
        {
            rb.velocity = rb.velocity.normalized * maxSpeed;
        }

        if (maxUpward > 0f && rb.velocity.y > maxUpward)
        {
            rb.velocity = new Vector3(rb.velocity.x, maxUpward, rb.velocity.z);
        }
    }

    Vector3 SafeDefaultDir()
    {
        return (defaultAimDir.sqrMagnitude > 0.0001f) ? defaultAimDir.normalized : Vector3.right;
    }

    void UpdateAimDirection(Vector2 screenPos)
    {
        Ray ray = cam.ScreenPointToRay(screenPos);
        int mask = (tableMask.value == 0) ? ~0 : tableMask.value;

        if (Physics.Raycast(ray, out RaycastHit hit, 200f, mask, QueryTriggerInteraction.Ignore))
        {
            Vector3 target = hit.point;
            target.y = cueBall.position.y;

            Vector3 dir;

            if (dragStickHandle)
            {
                dir = cueBall.position - target;
            }
            else
            {
                dir = target - cueBall.position;
            }

            dir.y = 0f;

            if (dir.sqrMagnitude > 0.0001f)
                shotDir = dir.normalized;
        }
    }

    public void UpdateStick(float normalizedPower01)
    {
        if (!visualRoot || !cueBall) return;

        float pull = Mathf.Lerp(0f, pullBackDistance, Mathf.Clamp01(normalizedPower01));

        Vector3 stickDir = -shotDir;
        stickDir.y = 0f;

        if (stickDir.sqrMagnitude < 0.0001f)
            stickDir = -SafeDefaultDir();
        stickDir.Normalize();

        stickVisualDir = stickDir;

        Vector3 pos = cueBall.position + stickDir * (cueBallRadius + stickDistance + pull + tipOffset);
        visualRoot.position = pos;

        // التدوير الأساسي باتجاه الكرة
        Quaternion rot = Quaternion.LookRotation(-stickDir, Vector3.up);

        // ✅ نظام الرفع التلقائي لتفادي اختراق الطاولة
        if (autoElevateStick)
        {
            float elevationAngle = 0f;

            // نطلق شعاعاً خلف الكرة البيضاء باتجاه العصا
            if (Physics.Raycast(cueBall.position, stickDir, out RaycastHit hit, stickLength, cushionLayer))
            {
                // إذا ضرب الشعاع حافة الطاولة، نحسب المسافة
                float distanceToCushion = hit.distance;

                // كلما كان الجدار أقرب، زادت زاوية رفع العصا
                float elevationPercent = 1f - (distanceToCushion / stickLength);
                elevationAngle = Mathf.Lerp(0f, maxElevationAngle, elevationPercent);
            }

            // نطبق زاوية الرفع على محور X
            rot *= Quaternion.Euler(elevationAngle, 0f, 0f);
        }
        if (modelForwardIsWrong) rot *= Quaternion.Euler(0f, 180f, 0f);
        visualRoot.rotation = rot;

        baseStickPos = visualRoot.position;
        baseStickRot = visualRoot.rotation;

        if (stickBody)
        {
            float s = Mathf.Lerp(1f, 1.12f, normalizedPower01);
            stickBody.localScale = bodyBaseScale * s;
        }

        if (aimLine)
        {
            if (currentState == ShootState.Aiming || currentState == ShootState.ReadyToShoot)
            {
                aimLine.SetAimDirection(shotDir);
                aimLine.SetPower01(normalizedPower01);
                aimLine.RenderLine();
            }
            else aimLine.Hide();
        }
    }

    public void Hide()
    {
        currentState = ShootState.Aiming;
        isDraggingToAim = false;
        isSliderBeingDragged = false;
        stickPositionInitialized = false;
        activeFingerId = -1;
        power01 = 0f;

        if (hideWhenReleased && visualRoot)
            visualRoot.gameObject.SetActive(false);

        if (stickBody)
            stickBody.localScale = bodyBaseScale;

        if (aimLine)
            aimLine.Hide();

        if (powerSliderPanel)
            powerSliderPanel.SetActive(false);

        if (powerSlider)
            powerSlider.value = 0f;

        if (wheelButton)
            wheelButton.SetActive(false);
    }


    bool BallsStopped()
    {
        if (allBalls == null || allBalls.Length == 0) return true;
        float s2 = stopSpeed * stopSpeed;

        foreach (var rb in allBalls)
        {
            if (!rb) continue;
            if (!rb.gameObject.activeInHierarchy) continue;
            if (rb.isKinematic) continue;

            if (rb.velocity.sqrMagnitude > s2) return false;
            if (rb.angularVelocity.sqrMagnitude > s2) return false;
        }
        return true;
    }

    IEnumerator ShotAnimThenHide()
    {
        if (hideWhenReleased && visualRoot) visualRoot.gameObject.SetActive(true);
        yield return StartCoroutine(ShotAnim());
        Hide();
    }

    IEnumerator CameraKick(float amount, float time)
    {
        if (!cam) yield break;

        Vector3 p0 = cam.transform.localPosition;
        Vector3 p1 = p0 + (Random.insideUnitSphere * amount);

        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            cam.transform.localPosition = Vector3.Lerp(p0, p1, t / time);
            yield return null;
        }

        cam.transform.localPosition = p0;
        camKickCo = null;
    }

    IEnumerator ShotAnim()
    {
        if (!visualRoot) yield break;

        Vector3 dir = stickVisualDir;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) yield break;
        dir.Normalize();

        Vector3 p0 = baseStickPos;
        Quaternion r0 = baseStickRot;

        Vector3 p1 = p0 - dir * shootForwardDistance;

        float t = 0f;
        while (t < shootForwardTime)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / shootForwardTime);
            visualRoot.position = Vector3.Lerp(p0, p1, a);
            visualRoot.rotation = r0;
            yield return null;
        }

        t = 0f;
        while (t < recoilBackTime)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / recoilBackTime);
            visualRoot.position = Vector3.Lerp(p1, p0, a);
            visualRoot.rotation = r0;
            yield return null;
        }

        Vector3 pNow = visualRoot.position;
        t = 0f;
        while (t < returnTime)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / returnTime);
            visualRoot.position = Vector3.Lerp(pNow, p0, a);
            visualRoot.rotation = r0;
            yield return null;
        }

        visualRoot.position = p0;
        visualRoot.rotation = r0;
        shotCo = null;
    }

    bool GetPointerDown(out Vector2 pos, out int fingerId)
    {
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began)
            {
                pos = t.position;
                fingerId = t.fingerId;
                return true;
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            pos = Input.mousePosition;
            fingerId = -999;
            return true;
        }

        pos = default;
        fingerId = -1;
        return false;
    }

    bool GetPointerHeld(int fingerId, out Vector2 pos)
    {
        if (fingerId == -999)
        {
            if (Input.GetMouseButton(0))
            {
                pos = Input.mousePosition;
                return true;
            }
            pos = default;
            return false;
        }

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch t = Input.GetTouch(i);
            if (t.fingerId != fingerId) continue;

            if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
            {
                pos = t.position;
                return true;
            }
        }

        pos = default;
        return false;
    }

    bool GetPointerUp(int fingerId)
    {
        if (fingerId == -999)
            return Input.GetMouseButtonUp(0);

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch t = Input.GetTouch(i);
            if (t.fingerId != fingerId) continue;

            if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                return true;
        }

        return false;
    }

    public void ResetStickBehindCueBall(bool showStick = false)
    {
        if (shotCo != null) { StopCoroutine(shotCo); shotCo = null; }
        if (camKickCo != null) { StopCoroutine(camKickCo); camKickCo = null; }

        Hide();
        stickPositionInitialized = false;

        if (!visualRoot || !cueBall) return;
        shotDir = SafeDefaultDir();

        visualRoot.gameObject.SetActive(true);
        UpdateStick(0f);
        Physics.SyncTransforms();

        if (!showStick && hideWhenReleased)
            visualRoot.gameObject.SetActive(false);
    }

    // أضف هذه الدالة في آخر الملف
    public void SetSliderInteractable(bool state)
    {
        if (powerSlider)
        {
            powerSlider.interactable = state; // True = شغال، False = مقفول (رمادي)
        }
    }

    // ✅ دالة جديدة للضبط الدقيق (يتم استدعاؤها من العجلة)
    public void RotateStickFine(float angleDelta)
    {
        if (currentState != ShootState.Aiming && currentState != ShootState.ReadyToShoot) return;

        // تدوير متجه التصويب بزاوية صغيرة حول المحور Y
        Quaternion rot = Quaternion.Euler(0f, angleDelta, 0f);
        shotDir = rot * shotDir;
        shotDir.Normalize();

        // تحديث شكل العصا وخط التصويب فوراً
        UpdateStick(power01);
    }


    // ════════════════════════════════════════════════════════════════════
    // 🤖 AI CONTROL FUNCTIONS
    // ════════════════════════════════════════════════════════════════════
    // هذه الدوال تسمح للـAI بالتحكم في العصا برمجياً

    /// <summary>
    /// للـAI: ضبط اتجاه التصويب
    /// </summary>
    public void SetAimDirection(Vector3 direction)
    {
        // تنظيف الاتجاه
        direction.y = 0f;
        direction.Normalize();

        // ضبط الاتجاه
        shotDir = direction;
        aimDirection = direction;

        // تحديث خط التصويب
        if (aimLine)
        {
            aimLine.SetAimDirection(direction);
        }

        // تحديث موقع العصا
        UpdateStick(power01);

        Debug.Log($"🎯 AI aim set: {direction}");
    }

    /// <summary>
    /// للـAI: ضبط قوة الضربة (0.0 - 1.0)
    /// </summary>
    public void SetPower(float powerValue)
    {
        power01 = Mathf.Clamp01(powerValue);

        // تحديث السلايدر إذا موجود
        if (powerSlider)
        {
            powerSlider.value = power01;
        }

        // تحديث موقع العصا
        UpdateStick(power01);

        Debug.Log($"⚡ AI power set: {power01:F2}");
    }

    /// <summary>
    /// للـAI: تنفيذ الضربة مباشرة
    /// </summary>
    public void ExecuteAIShot()
    {
        if (currentState == ShootState.Shooting)
        {
            Debug.LogWarning("⚠️ Cannot shoot - already shooting!");
            return;
        }

        // تأكد من تجهيز العصا
        if (!stickPositionInitialized)
        {
            InitializeStickPosition();
        }

        // ضبط الحالة للـReadyToShoot
        currentState = ShootState.ReadyToShoot;

        // تنفيذ الضربة
        Shoot();

        Debug.Log("🤖 AI shot executed!");
    }

    // ════════════════════════════════════════════════════════════════════
    // ✅ هذه الدالة مخصصة للذكاء الاصطناعي لكي يستطيع تحديد القوة
    // ✅ التعديل الجديد: ربط الـ AI بوظائف العصا الرئيسية
    public void Shoot(float power)
    {
        // 1. تحديث قيمة القوة الداخلية للعصا
        power01 = Mathf.Clamp01(power);

        // 2. إذا كان السلايدر موجوداً، نحدثه شكلياً
        if (powerSlider) powerSlider.value = power01;

        // 3. نضبط حالة العصا لتكون جاهزة للضرب
        currentState = ShootState.ReadyToShoot;

        // 4. نستدعي دالة الضرب الرئيسية (التي تشغل الصوت، وتحسب السبين، وتبلغ مدير اللعبة)
        Shoot();

        Debug.Log($"🤖 AI Fired with power: {power01}");


        // تشغيل صوت الضربة إن وجد
        // if (audioSource) audioSource.Play();
    }
}
