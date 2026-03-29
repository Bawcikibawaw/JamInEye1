using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class LevelTrigger : MonoBehaviour
{
    public string nextSceneName;
    public float fadeDuration = 0.5f;
    public bool isFinalLevel = false;
    public GameObject winPanel;

    private bool _triggered = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_triggered || other.GetComponent<SlimeThrower>() == null) return;
        _triggered = true;
        StartCoroutine(TransitionRoutine());
    }

    IEnumerator TransitionRoutine()
    {
        // 1. Fade to black
        yield return FadeManager.Instance.FadeOut(fadeDuration).WaitForCompletion();

        // 2. Victory Check
        if (isFinalLevel)
        {
            if (GameTimer.Instance != null) GameTimer.Instance.StopTimer();
            if (winPanel != null) winPanel.SetActive(true);
            
            // Because we aren't changing scenes, we manually fade back in
            FadeManager.Instance.FadeIn(fadeDuration);
            yield break; 
        }

        // 3. Load next scene 
        // (The moment this runs, this script is destroyed, but that's okay now!)
        MainAudioManager.Instance.Play("LevelChangeSFX");

        SceneManager.LoadScene(nextSceneName);
    }
}