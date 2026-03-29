using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

public class SceneFadeIn : MonoBehaviour
{
    public float fadeDuration = 0.8f;

    private static bool _isFading = false;

    void Start()
    {
        // If another SceneFadeIn already started this scene, skip
        if (_isFading)
        {
            Destroy(this);
            return;
        }

        _isFading = true;
        StartCoroutine(FadeInNextFrame());
    }

    IEnumerator FadeInNextFrame()
    {
        Image fadeImage = GetFadeImage();
        if (fadeImage == null)
        {
            _isFading = false;
            yield break;
        }

        fadeImage.DOKill();
        fadeImage.color = new Color(0f, 0f, 0f, 1f);

        yield return null;
        yield return null;

        fadeImage.DOFade(0f, fadeDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true)
            .OnComplete(() => _isFading = false);
    }

    Image GetFadeImage()
    {
        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (canvas.gameObject.name == "FadeCanvas")
                return canvas.GetComponentInChildren<Image>();
        return null;
    }
}