using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class LevelTrigger : MonoBehaviour
{
    public string nextSceneName;
    public float fadeDuration = 0.5f;
    public bool isFinalLevel = false;
    public string victorySceneName = "Victory"; 

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
            // Stop the game timer
            if (GameTimer.Instance != null) GameTimer.Instance.StopTimer();
            
            // Optional: Play a specific Victory SFX
            MainAudioManager.Instance.Play("LevelChangeSFX"); 

            // Load the Victory Scene
            SceneManager.LoadScene(victorySceneName);
            
            // We stop here. FadeManager will automatically FadeIn in the new scene.
            yield break; 
        }

        // 3. Load next scene 
        // (The moment this runs, this script is destroyed, but that's okay now!)
        MainAudioManager.Instance.Play("LevelChangeSFX");

        SceneManager.LoadScene(nextSceneName);
    }
}