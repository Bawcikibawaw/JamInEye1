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
        // When a new scene loads, it's usually black. 
        // We call FadeIn which will unpause the game when finished.
        FadeIn(defaultFadeDuration);
    }

    public Tween FadeOut(float duration)
    {
        _fadeImage.DOKill();
        _fadeImage.raycastTarget = true;

        // ── TIME FREEZE ──
        Time.timeScale = 0f; 

        return _fadeImage.DOFade(1f, duration).SetUpdate(true);
    }

    public void FadeIn(float duration)
    {
        _fadeImage.DOKill();
        
        // We keep time frozen during the FadeIn animation
        // and only unpause once the screen is clear.
        _fadeImage.DOFade(0f, duration).SetUpdate(true).OnComplete(() => {
            _fadeImage.raycastTarget = false;
            
            // ── RESUME TIME ──
            Time.timeScale = 1f; 
        });
    }
}