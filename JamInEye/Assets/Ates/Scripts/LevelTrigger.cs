// LevelTrigger.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

public class LevelTrigger : MonoBehaviour
{
    [Header("Next Level")]
    public string nextSceneName;

    [Header("Fade Settings")]
    public float fadeDuration = 0.5f;

    [Header("Preload Trigger — assign the mid-level trigger object")]
    public LevelPreloader preloader; // assign the preloader object in Inspector

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
        StartCoroutine(FadeAndLoad());
    }

    IEnumerator FadeAndLoad()
    {
        // Fade to black
        _fadeImage.DOKill();
        _fadeImage.color = new Color(0f, 0f, 0f, 0f);
        _fadeImage.DOFade(1f, fadeDuration)
            .SetEase(Ease.InQuad)
            .SetUpdate(true);

        yield return new WaitForSecondsRealtime(fadeDuration);

        // If preloader has already started async loading, wait for it
        // Otherwise load normally
        if (preloader != null && preloader.AsyncOperation != null)
        {
            // Allow scene to activate — it was held at 0.9
            preloader.AsyncOperation.allowSceneActivation = true;
        }
        else
        {
            // Preloader wasn't reached — load normally
            SceneManager.LoadScene(nextSceneName);
        }
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