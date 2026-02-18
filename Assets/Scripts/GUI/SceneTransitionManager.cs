using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

/// <summary>
/// Scene Transition Manager - Handles smooth transitions between scenes
/// Singleton pattern - persists between scenes
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Fade Settings")]
    [Tooltip("The image used for fading (fullscreen black image)")]
    public Image fadeImage;

    [Tooltip("Duration of fade in/out")]
    [Range(0.1f, 2f)]
    public float fadeDuration = 0.5f;

    [Tooltip("Curve for fade animation")]
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Loading Screen")]
    [Tooltip("Loading screen panel (optional)")]
    public GameObject loadingScreen;

    [Tooltip("Loading progress bar (optional)")]
    public Slider progressBar;

    [Tooltip("Loading text (optional)")]
    public TextMeshProUGUI loadingText;

    [Tooltip("Minimum time to show loading screen")]
    public float minLoadingTime = 1f;

    [Header("Audio")]
    [Tooltip("Sound when transitioning")]
    public AudioClip transitionSound;

    private AudioSource audioSource;
    private bool isTransitioning = false;

    void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Setup
            if (fadeImage)
            {
                fadeImage.color = new Color(0, 0, 0, 0);
                fadeImage.raycastTarget = true;
            }

            if (loadingScreen)
            {
                loadingScreen.SetActive(false);
            }

            audioSource = GetComponent<AudioSource>();
            if (!audioSource)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Load scene with fade transition
    /// </summary>
    public void LoadScene(string sceneName)
    {
        if (!isTransitioning)
        {
            StartCoroutine(TransitionToScene(sceneName, false));
        }
    }

    /// <summary>
    /// Load scene with loading screen
    /// </summary>
    public void LoadSceneWithLoading(string sceneName)
    {
        if (!isTransitioning)
        {
            StartCoroutine(TransitionToScene(sceneName, true));
        }
    }

    /// <summary>
    /// Reload current scene
    /// </summary>
    public void ReloadCurrentScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        LoadScene(currentScene);
    }

    /// <summary>
    /// Main transition coroutine
    /// </summary>
    IEnumerator TransitionToScene(string sceneName, bool showLoading)
    {
        isTransitioning = true;

        // Play sound
        PlayTransitionSound();

        // Fade out
        yield return StartCoroutine(FadeOut());

        // Show loading screen if requested
        if (showLoading && loadingScreen)
        {
            loadingScreen.SetActive(true);
            yield return StartCoroutine(LoadSceneAsync(sceneName));
        }
        else
        {
            // Simple scene load
            SceneManager.LoadScene(sceneName);
        }

        // Fade in
        yield return StartCoroutine(FadeIn());

        // Hide loading screen
        if (loadingScreen)
        {
            loadingScreen.SetActive(false);
        }

        isTransitioning = false;
    }

    /// <summary>
    /// Load scene asynchronously with progress
    /// </summary>
    IEnumerator LoadSceneAsync(string sceneName)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        float startTime = Time.time;
        float progress = 0f;

        while (!operation.isDone)
        {
            // Calculate progress (0 to 0.9 from async, then 0.9 to 1 for minimum time)
            progress = Mathf.Clamp01(operation.progress / 0.9f);

            // Update UI
            if (progressBar)
            {
                progressBar.value = progress;
            }

            if (loadingText)
            {
                loadingText.text = "Loading... " + (progress * 100f).ToString("F0") + "%";
            }

            // Check if loading is complete
            if (operation.progress >= 0.9f)
            {
                // Wait for minimum loading time
                float elapsed = Time.time - startTime;
                if (elapsed >= minLoadingTime)
                {
                    // Activate scene
                    operation.allowSceneActivation = true;
                }
            }

            yield return null;
        }
    }

    /// <summary>
    /// Fade out (screen becomes black)
    /// </summary>
    public IEnumerator FadeOut()
    {
        if (!fadeImage) yield break;

        float elapsed = 0f;
        Color startColor = fadeImage.color;
        Color endColor = new Color(0, 0, 0, 1);

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = fadeCurve.Evaluate(elapsed / fadeDuration);
            fadeImage.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }

        fadeImage.color = endColor;
    }

    /// <summary>
    /// Fade in (screen becomes visible)
    /// </summary>
    public IEnumerator FadeIn()
    {
        if (!fadeImage) yield break;

        float elapsed = 0f;
        Color startColor = fadeImage.color;
        Color endColor = new Color(0, 0, 0, 0);

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = fadeCurve.Evaluate(elapsed / fadeDuration);
            fadeImage.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }

        fadeImage.color = endColor;
    }

    /// <summary>
    /// Instant fade to black (useful for game start)
    /// </summary>
    public void InstantFadeOut()
    {
        if (fadeImage)
        {
            fadeImage.color = new Color(0, 0, 0, 1);
        }
    }

    /// <summary>
    /// Instant fade to clear (useful for game start)
    /// </summary>
    public void InstantFadeIn()
    {
        if (fadeImage)
        {
            fadeImage.color = new Color(0, 0, 0, 0);
        }
    }

    void PlayTransitionSound()
    {
        if (audioSource && transitionSound)
        {
            audioSource.PlayOneShot(transitionSound);
        }
    }
}