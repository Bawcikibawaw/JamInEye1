// SceneFadeIn.cs
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

public class SceneFadeIn : MonoBehaviour
{
    public float fadeDuration = 0.8f;

    void Start()
    {
        StartCoroutine(FadeInNextFrame());
    }

    IEnumerator FadeInNextFrame()
    {
        Image fadeImage = GetOrCreateFadeImage();

        // Kill any running tween
        fadeImage.DOKill();

        // Force black instantly
        fadeImage.color = new Color(0f, 0f, 0f, 1f);

        // Wait one frame so scene is fully rendered before fading
        yield return null;
        yield return null;

        // Now fade to clear
        fadeImage.DOFade(0f, fadeDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
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
        img.color = new Color(0f, 0f, 0f, 1f);

        RectTransform rt = img.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        return img;
    }
}