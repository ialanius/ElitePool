using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class SpinControllerAdvanced : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("Spin UI")]
    public RectTransform spinCircle;
    public RectTransform spinDot;
    public Image spinCircleImage;
    public Image spinDotImage;
    public float maxRadius = 60f;

    [Header("🔌 Link to Panel")]
    public SpinPanelToggle spinPanelToggle;

    [Header("UI Adjustment")]
    public Vector3 defaultOffset = new Vector3(0, -17, 0);

    [Header("Spin Values")]
    [Range(-1f, 1f)]
    public float verticalSpin = 0f;
    [Range(-1f, 1f)]
    public float horizontalSpin = 0f;

    [Header("Spin Force")]
    public float spinMultiplier = 5f;

    [Header("Visual Feedback")]
    public Transform cueBall;
    public bool showSpinOnBall = true;

    [Header("🎨 Advanced Visuals")]
    public bool enableParticles = true;
    public bool enableTrail = true;
    public bool enableGlow = true;
    public bool enablePulse = true;
    public bool enableDirectionArrow = true;

    [Header("Particle System")]
    public ParticleSystem spinParticles;
    public GameObject spinParticlePrefab;

    [Header("Trail")]
    public TrailRenderer ballTrail;
    public float trailTime = 0.5f;
    public Gradient trailGradient;

    [Header("Glow Effect")]
    public SpriteRenderer glowSprite;
    public GameObject glowPrefab;
    public float glowIntensity = 2f;
    public float glowPulseSpeed = 2f;

    [Header("Direction Arrow")]
    public GameObject arrowPrefab;
    private GameObject arrowInstance;
    public float arrowDistance = 0.4f;

    [Header("Colors - Enhanced")]
    public Color topSpinColor = new Color(0f, 1f, 0.3f, 1f);
    public Color backSpinColor = new Color(1f, 0.2f, 0f, 1f);
    public Color sideSpinColor = new Color(1f, 0.9f, 0f, 1f);
    public Color centerColor = Color.white;

    [Header("UI Animation")]
    public bool animateUI = true;
    public float scaleOnDrag = 1.2f;
    public float pulseSpeed = 1.5f;
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 1, 1, 1);

    [Header("Audio")]
    public AudioSource spinAudioSource;
    public AudioClip spinDragSound;
    public AudioClip spinReleaseSound;

    [Header("Advanced Options")]
    public bool resetAfterShot = true;
    public bool showSpinText = true;
    public Text spinInfoText;

    private Vector2 spinOffset = Vector2.zero;
    private bool isDragging = false;
    private GameObject spinIndicatorInstance;
    private Vector3 originalScale;
    private Coroutine pulseCo;
    private Material glowMaterial;

    void Start()
    {
        if (!spinCircle || !spinDot)
        {
            Debug.LogError("⚠️ Spin UI elements not assigned!");
            enabled = false;
            return;
        }

        originalScale = spinCircle.localScale;
        ResetSpin();
        SetupAdvancedVisuals();
    }

    void Update()
    {
        UpdateSpinVisual();

        if (showSpinOnBall && cueBall)
        {
            UpdateBallIndicator();
        }

        if (showSpinText && spinInfoText)
        {
            UpdateSpinText();
        }

        if (enablePulse && !isDragging)
        {
            PulseEffect();
        }
    }

    void SetupAdvancedVisuals()
    {
        if (enableGlow && !glowSprite && cueBall)
        {
            if (glowPrefab)
            {
                GameObject glow = Instantiate(glowPrefab, cueBall);
                glowSprite = glow.GetComponent<SpriteRenderer>();
            }
            else
            {
                CreateGlowSprite();
            }
        }

        if (enableTrail && !ballTrail && cueBall)
        {
            ballTrail = cueBall.gameObject.AddComponent<TrailRenderer>();
            SetupTrail();
        }

        if (enableParticles && !spinParticles && cueBall)
        {
            if (spinParticlePrefab)
            {
                GameObject particles = Instantiate(spinParticlePrefab, cueBall);
                spinParticles = particles.GetComponent<ParticleSystem>();
            }
            else
            {
                CreateParticleSystem();
            }
        }

        if (enableDirectionArrow)
        {
            SetupDirectionArrow();
        }

        if (showSpinOnBall && cueBall)
        {
            SetupSpinIndicator();
        }
    }

    void CreateGlowSprite()
    {
        GameObject glowObj = new GameObject("SpinGlow");
        glowObj.transform.SetParent(cueBall);
        glowObj.transform.localPosition = Vector3.zero;
        glowObj.transform.localRotation = Quaternion.Euler(90, 0, 0);
        glowObj.transform.localScale = Vector3.one * 0.6f;

        glowSprite = glowObj.AddComponent<SpriteRenderer>();

        Texture2D tex = new Texture2D(128, 128);
        Color[] pixels = new Color[128 * 128];
        Vector2 center = new Vector2(64, 64);

        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha = 1f - Mathf.Clamp01(dist / 64f);
                alpha = Mathf.Pow(alpha, 2);
                pixels[y * 128 + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        glowSprite.sprite = Sprite.Create(tex, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
        glowSprite.sortingOrder = -1;

        glowMaterial = new Material(Shader.Find("Sprites/Default"));
        glowMaterial.color = Color.white;
        glowSprite.material = glowMaterial;

        glowSprite.enabled = false;
    }

    void SetupTrail()
    {
        if (!ballTrail) return;

        ballTrail.time = trailTime;
        ballTrail.startWidth = 0.15f;
        ballTrail.endWidth = 0.02f;
        ballTrail.material = new Material(Shader.Find("Sprites/Default"));

        if (trailGradient == null)
        {
            trailGradient = new Gradient();
            trailGradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white, 0.0f),
                    new GradientColorKey(Color.cyan, 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1.0f, 0.0f),
                    new GradientAlphaKey(0.0f, 1.0f)
                }
            );
        }

        ballTrail.colorGradient = trailGradient;
        ballTrail.enabled = false;
    }

    void CreateParticleSystem()
    {
        GameObject particlesObj = new GameObject("SpinParticles");
        particlesObj.transform.SetParent(cueBall);
        particlesObj.transform.localPosition = Vector3.zero;

        spinParticles = particlesObj.AddComponent<ParticleSystem>();

        var main = spinParticles.main;
        main.startLifetime = 0.5f;
        main.startSpeed = 2f;
        main.startSize = 0.05f;
        main.maxParticles = 20;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = spinParticles.emission;
        emission.rateOverTime = 0;
        emission.enabled = true;

        var shape = spinParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;

        var colorOverLifetime = spinParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;

        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0.0f),
                new GradientColorKey(Color.cyan, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );

        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);
        spinParticles.Stop();
    }

    void SetupDirectionArrow()
    {
        if (arrowInstance) return;

        if (arrowPrefab)
        {
            arrowInstance = Instantiate(arrowPrefab);
            arrowInstance.transform.SetParent(cueBall);
        }
        else
        {
            arrowInstance = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            arrowInstance.name = "SpinDirectionArrow";
            arrowInstance.transform.SetParent(cueBall);
            arrowInstance.transform.localScale = new Vector3(0.05f, 0.2f, 0.05f);

            Destroy(arrowInstance.GetComponent<Collider>());

            var renderer = arrowInstance.GetComponent<Renderer>();
            if (renderer)
            {
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.color = Color.yellow;
                renderer.material.EnableKeyword("_EMISSION");
                renderer.material.SetColor("_EmissionColor", Color.yellow);
            }
        }
        arrowInstance.SetActive(false);
    }

    void SetupSpinIndicator()
    {
        if (spinIndicatorInstance) return;

        spinIndicatorInstance = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        spinIndicatorInstance.name = "SpinIndicator";
        spinIndicatorInstance.transform.SetParent(cueBall);
        spinIndicatorInstance.transform.localScale = Vector3.one * 0.08f;

        Destroy(spinIndicatorInstance.GetComponent<Collider>());

        var renderer = spinIndicatorInstance.GetComponent<Renderer>();
        if (renderer)
        {
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.EnableKeyword("_EMISSION");
        }
        spinIndicatorInstance.SetActive(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isDragging = true;

        if (animateUI)
        {
            if (pulseCo != null) StopCoroutine(pulseCo);
            pulseCo = StartCoroutine(ScaleAnimation(scaleOnDrag));
        }

        PlaySound(spinDragSound);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;

        if (animateUI)
        {
            if (pulseCo != null) StopCoroutine(pulseCo);
            pulseCo = StartCoroutine(ScaleAnimation(1f));
        }

        PlaySound(spinReleaseSound);

        if (spinPanelToggle != null && spinPanelToggle.autoHide)
        {
            spinPanelToggle.Invoke("ClosePanel", spinPanelToggle.hideDelay);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            spinCircle,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint
        );

        Vector2 adjustedPoint = localPoint - (Vector2)defaultOffset;

        float distance = adjustedPoint.magnitude;
        if (distance > maxRadius)
        {
            adjustedPoint = adjustedPoint.normalized * maxRadius;
            localPoint = adjustedPoint + (Vector2)defaultOffset;
        }

        spinOffset = localPoint;

        horizontalSpin = Mathf.Clamp(adjustedPoint.x / maxRadius, -1f, 1f);
        verticalSpin = Mathf.Clamp(adjustedPoint.y / maxRadius, -1f, 1f);

        spinDot.anchoredPosition = spinOffset;
    }

    void UpdateSpinVisual()
    {
        if (!spinDot || !spinDotImage) return;

        Color targetColor = GetSpinColor();
        spinDotImage.color = targetColor;

        if (enableGlow && glowSprite)
        {
            bool hasSpinbool = Mathf.Abs(verticalSpin) > 0.01f || Mathf.Abs(horizontalSpin) > 0.01f;
            glowSprite.enabled = hasSpinbool;

            if (hasSpinbool)
            {
                glowSprite.color = targetColor;
                float intensity = (Mathf.Abs(verticalSpin) + Mathf.Abs(horizontalSpin)) * glowIntensity;

                if (glowMaterial)
                {
                    glowMaterial.SetColor("_EmissionColor", targetColor * intensity);
                }
            }
        }
    }

    Color GetSpinColor()
    {
        float totalSpin = Mathf.Abs(verticalSpin) + Mathf.Abs(horizontalSpin);

        if (totalSpin < 0.1f)
            return centerColor;

        if (Mathf.Abs(verticalSpin) > Mathf.Abs(horizontalSpin))
        {
            return verticalSpin > 0 ? topSpinColor : backSpinColor;
        }
        else
        {
            return sideSpinColor;
        }
    }

    void UpdateBallIndicator()
    {
        if (!spinIndicatorInstance || !cueBall) return;

        bool hasSpin = Mathf.Abs(verticalSpin) > 0.01f || Mathf.Abs(horizontalSpin) > 0.01f;
        spinIndicatorInstance.SetActive(hasSpin);

        if (hasSpin)
        {
            float ballRadius = 0.25f;
            Vector3 offset = new Vector3(
                horizontalSpin * ballRadius,
                0f,
                verticalSpin * ballRadius
            );

            spinIndicatorInstance.transform.localPosition = offset;

            var renderer = spinIndicatorInstance.GetComponent<Renderer>();
            if (renderer)
            {
                Color spinColor = GetSpinColor();
                renderer.material.color = spinColor;
                renderer.material.SetColor("_EmissionColor", spinColor * 2f);
            }
        }

        if (enableDirectionArrow && arrowInstance)
        {
            arrowInstance.SetActive(hasSpin);

            if (hasSpin)
            {
                Vector3 direction = new Vector3(horizontalSpin, 0, verticalSpin).normalized;
                arrowInstance.transform.localPosition = direction * arrowDistance;
                arrowInstance.transform.localRotation = Quaternion.LookRotation(direction);

                var renderer = arrowInstance.GetComponent<Renderer>();
                if (renderer)
                {
                    renderer.material.color = GetSpinColor();
                }
            }
        }
    }

    void UpdateSpinText()
    {
        if (!spinInfoText) return;

        string spinType = "Center";
        if (Mathf.Abs(verticalSpin) > 0.1f || Mathf.Abs(horizontalSpin) > 0.1f)
        {
            if (Mathf.Abs(verticalSpin) > Mathf.Abs(horizontalSpin))
            {
                spinType = verticalSpin > 0 ? "Top Spin" : "Back Spin";
            }
            else
            {
                spinType = horizontalSpin > 0 ? "Right English" : "Left English";
            }
        }

        spinInfoText.text = $"{spinType}\nV: {verticalSpin:F2} | H: {horizontalSpin:F2}";
        spinInfoText.color = GetSpinColor();
    }

    void PulseEffect()
    {
        float pulse = Mathf.Sin(Time.time * pulseSpeed) * 0.05f + 1f;
        spinCircle.localScale = originalScale * pulse;
    }

    IEnumerator ScaleAnimation(float targetScale)
    {
        Vector3 target = originalScale * targetScale;
        float elapsed = 0f;
        float duration = 0.2f;
        Vector3 start = spinCircle.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float curved = scaleCurve.Evaluate(t);
            spinCircle.localScale = Vector3.Lerp(start, target, curved);
            yield return null;
        }

        spinCircle.localScale = target;
    }

    public void ApplySpin(Rigidbody rb, Vector3 shotDirection, float shotPower)
    {
        if (!rb) return;

        if (Mathf.Abs(verticalSpin) > 0.01f)
        {
            Vector3 spinAxis = Vector3.Cross(shotDirection, Vector3.up).normalized;
            float spinAmount = -verticalSpin * spinMultiplier * shotPower * 0.5f;
            rb.angularVelocity += spinAxis * spinAmount;
        }

        if (Mathf.Abs(horizontalSpin) > 0.01f)
        {
            float sideSpinAmount = horizontalSpin * spinMultiplier * shotPower * 0.3f;
            rb.angularVelocity += Vector3.up * sideSpinAmount;

            Vector3 sideForce = Vector3.Cross(shotDirection, Vector3.up).normalized;
            rb.AddForce(sideForce * horizontalSpin * shotPower * 0.2f, ForceMode.Impulse);
        }

        StartCoroutine(SpinVisualEffects(shotPower));

        if (resetAfterShot)
        {
            Invoke(nameof(ResetSpin), 0.5f);
        }
    }

    IEnumerator SpinVisualEffects(float power)
    {
        if (enableParticles && spinParticles)
        {
            var emission = spinParticles.emission;
            emission.rateOverTime = power * 30f;
            spinParticles.Play();

            yield return new WaitForSeconds(0.5f);
            spinParticles.Stop();
        }

        if (enableTrail && ballTrail)
        {
            ballTrail.enabled = true;
            ballTrail.colorGradient = CreateTrailGradient(GetSpinColor());

            yield return new WaitForSeconds(2f);
            ballTrail.enabled = false;
        }
    }

    Gradient CreateTrailGradient(Color baseColor)
    {
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(baseColor, 0.0f),
                new GradientColorKey(baseColor * 0.5f, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.8f, 0.0f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        return grad;
    }

    void PlaySound(AudioClip clip)
    {
        if (!spinAudioSource || !clip) return;
        spinAudioSource.PlayOneShot(clip);
    }

    public void ResetSpin()
    {
        verticalSpin = 0f;
        horizontalSpin = 0f;

        spinOffset = new Vector2(defaultOffset.x, defaultOffset.y);

        if (spinDot)
        {
            spinDot.anchoredPosition3D = defaultOffset;
        }

        if (spinIndicatorInstance)
            spinIndicatorInstance.SetActive(false);

        if (arrowInstance)
            arrowInstance.SetActive(false);

        if (glowSprite)
            glowSprite.enabled = false;
    }

    public Vector3 GetSpinVector()
    {
        return new Vector3(horizontalSpin, 0f, verticalSpin) * spinMultiplier;
    }

    // ==========================================
    // 🎯 أزرار السبين الأساسية (5 ضربات)
    // ==========================================
    public void SetTopSpin() { verticalSpin = 1f; horizontalSpin = 0f; UpdateDotPosition(); }
    public void SetBackSpin() { verticalSpin = -1f; horizontalSpin = 0f; UpdateDotPosition(); }
    public void SetLeftSpin() { horizontalSpin = -1f; verticalSpin = 0f; UpdateDotPosition(); }
    public void SetRightSpin() { horizontalSpin = 1f; verticalSpin = 0f; UpdateDotPosition(); }
    public void SetCenterHit() { ResetSpin(); }

    // ==========================================
    // 🔥 أزرار السبين المركب (الزوايا الأربع)
    // ==========================================
    public void SetTopRightSpin() { verticalSpin = 0.707f; horizontalSpin = 0.707f; UpdateDotPosition(); }
    public void SetTopLeftSpin() { verticalSpin = 0.707f; horizontalSpin = -0.707f; UpdateDotPosition(); }
    public void SetBackRightSpin() { verticalSpin = -0.707f; horizontalSpin = 0.707f; UpdateDotPosition(); }
    public void SetBackLeftSpin() { verticalSpin = -0.707f; horizontalSpin = -0.707f; UpdateDotPosition(); }

    // ✅ تم تحديث هذه الدالة لتأخذ defaultOffset في الحسبان (حل مشكلة الإزاحة للأزرار)
    void UpdateDotPosition()
    {
        // حساب الموقع النسبي
        Vector2 calculatedPos = new Vector2(horizontalSpin * maxRadius, verticalSpin * maxRadius);

        // إضافة الإزاحة الافتراضية للمركز
        spinOffset = calculatedPos + new Vector2(defaultOffset.x, defaultOffset.y);

        if (spinDot) spinDot.anchoredPosition = spinOffset;
    }
}