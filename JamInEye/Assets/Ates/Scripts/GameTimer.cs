// GameTimer.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;
using TMPro;

public class GameTimer : MonoBehaviour
{
    public static GameTimer Instance { get; private set; }

    [Header("Timer Settings")]
    public float totalTime = 300f;
    public string firstSceneName = "Level_0";
    public string finalSceneName = "Level_2";

    [Header("UI")]
    public TextMeshProUGUI timerText;
    public Color normalColor = Color.white;
    public Color dangerColor = Color.red;
    public float dangerThreshold = 30f;

    private float _timeRemaining;
    private bool _isRunning = false;
    private bool _gameOver = false;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        _timeRemaining = totalTime;
    }

    void Start()
    {
        StartTimer();
    }

    public void StartTimer()
    {
        _isRunning = true;
        _gameOver = false;
    }

    public void StopTimer()
    {
        _isRunning = false;
    }

    public void ResetTimer()
    {
        _timeRemaining = totalTime;
        _isRunning = true;
        _gameOver = false;
    }

    void Update()
    {
        if (!_isRunning || _gameOver) return;

        _timeRemaining -= Time.deltaTime;
        _timeRemaining = Mathf.Max(0f, _timeRemaining);

        UpdateTimerUI();

        if (_timeRemaining <= 0f)
        {
            _gameOver = true;
            StartCoroutine(TimeUpRoutine());
        }
    }

    private void UpdateTimerUI()
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(_timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(_timeRemaining % 60f);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

        timerText.color = _timeRemaining <= dangerThreshold ? dangerColor : normalColor;
    }

    private IEnumerator TimeUpRoutine()
    {
        _isRunning = false;

        if (FadeManager.Instance != null)
            yield return FadeManager.Instance.FadeOut(0.5f).WaitForCompletion();

        ResetTimer();
        UnityEngine.SceneManagement.SceneManager.LoadScene(firstSceneName);
    
        yield return new WaitForSecondsRealtime(0.2f);
    
        //if (FadeManager.Instance != null)
        //    FadeManager.Instance.FadeIn(0.5f);
    }

    private void OnSceneLoadedAfterTimeUp(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoadedAfterTimeUp;
        StartCoroutine(FadeInAfterLoad());
    }

    private IEnumerator FadeInAfterLoad()
    {
        yield return null;
        yield return null;

        Image fadeImage = GetFadeImage();
        if (fadeImage == null) yield break;

        fadeImage.DOKill();
        fadeImage.color = new Color(0f, 0f, 0f, 1f);
        fadeImage.DOFade(0f, 0.5f)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Don't resume if final level was completed and timer was stopped
        if (scene.name == finalSceneName && !_isRunning) return;

        if (!_isRunning && !_gameOver)
            StartTimer();
    }

    private Image GetFadeImage()
    {
        foreach (var canvas in FindObjectsByType<Canvas>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (canvas.gameObject.name == "FadeCanvas")
                return canvas.GetComponentInChildren<Image>();
        return null;
    }
}