using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class FadeManager : MonoBehaviour
{
    private static FadeManager _instance;
    public static FadeManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("~SystemFadeManager");
                _instance = go.AddComponent<FadeManager>();
                DontDestroyOnLoad(go);
                _instance.SetupCanvas();
            }
            return _instance;
        }
    }

    private Image _fadeImage;
    public float defaultFadeDuration = 0.5f;

    // ADJUSTMENT: Track fading state for the Pause Menu to check
    public bool IsFading { get; private set; } 

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init() { var i = Instance; }

    private void SetupCanvas()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;

        gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        GameObject imgGo = new GameObject("FadeImage");
        imgGo.transform.SetParent(transform);
        
        _fadeImage = imgGo.AddComponent<Image>();
        _fadeImage.color = new Color(0, 0, 0, 0);
        _fadeImage.raycastTarget = false;

        RectTransform rt = _fadeImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FadeIn(defaultFadeDuration);
    }

    public Tween FadeOut(float duration)
    {
        IsFading = true; // ADJUSTMENT: Set state
        _fadeImage.DOKill();
        
        // ADJUSTMENT: Force alpha to 0 before starting to ensure the tween plays
        _fadeImage.color = new Color(0, 0, 0, 0); 
        _fadeImage.raycastTarget = true;

        // ── TIME FREEZE ──
        Time.timeScale = 0f; 

        return _fadeImage.DOFade(1f, duration).SetUpdate(true);
    }

    public void FadeIn(float duration)
    {
        IsFading = true; // ADJUSTMENT: Set state
        _fadeImage.DOKill();
        
        // ADJUSTMENT: Force alpha to 1 before starting
        _fadeImage.color = new Color(0, 0, 0, 1);

        _fadeImage.DOFade(0f, duration).SetUpdate(true).OnComplete(() => {
            _fadeImage.raycastTarget = false;
            IsFading = false; // ADJUSTMENT: Reset state
            
            // ── RESUME TIME ──
            // ADJUSTMENT: Only resume if the player hasn't manually paused via ESC
            if (!PauseMenu.IsPaused)
            {
                Time.timeScale = 1f; 
            }
        });
    }
}