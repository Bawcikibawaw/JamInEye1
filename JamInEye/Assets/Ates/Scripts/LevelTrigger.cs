// LevelTrigger.cs — cleaned up, no FadeIn logic
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;

public class LevelTrigger : MonoBehaviour
{
    [Header("Next Level")]
    public string nextSceneName;

    [Header("Fade Settings")]
    public float fadeDuration = 0.5f;

    private bool _triggered = false;
    private Image _fadeImage;

    void Awake()
    {
        _fadeImage = GetOrCreateFadeImage();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_triggered) return;
        if (other.GetComponent<SlimeThrower>() == null) return;

        _triggered = true;
        FadeAndLoad();
    }

    void FadeAndLoad()
    {
        _fadeImage.color = new Color(0f, 0f, 0f, 0f);
        _fadeImage.DOFade(1f, fadeDuration)
            .SetEase(Ease.InQuad)
            .SetUpdate(true)
            .OnComplete(() => SceneManager.LoadScene(nextSceneName));
            // No FadeIn here — SceneFadeIn.cs handles that in the new scene
    }

    Image GetOrCreateFadeImage()
    {
        GameObject existing = GameObject.Find("FadeCanvas");
        if (existing != null)
            return existing.GetComponentInChildren<Image>();

        GameObject canvasObj = new GameObject("FadeCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(canvasObj);

        GameObject imageObj = new GameObject("FadePanel");
        imageObj.transform.SetParent(canvasObj.transform, false);
        Image img = imageObj.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f);

        RectTransform rt = img.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        return img;
    }
}