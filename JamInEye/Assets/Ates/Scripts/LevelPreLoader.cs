using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LevelPreloader : MonoBehaviour
{
    [Header("Scene to preload")]
    public string nextSceneName;

    public AsyncOperation AsyncOperation { get; private set; }

    private bool _started = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_started) return;
        if (other.GetComponent<SlimeThrower>() == null) return;

        _started = true;
        StartCoroutine(PreloadScene());
    }

    IEnumerator PreloadScene()
    {
        Debug.Log("Preloading: " + nextSceneName);

        AsyncOperation = SceneManager.LoadSceneAsync(nextSceneName);

        // Hold at 90% — don't activate until LevelTrigger allows it
        AsyncOperation.allowSceneActivation = false;

        while (AsyncOperation.progress < 0.9f)
        {
            Debug.Log("Load progress: " + (AsyncOperation.progress * 100f) + "%");
            yield return null;
        }

        Debug.Log("Preload complete — waiting for fade trigger");
        // Sits here at 90% until LevelTrigger sets allowSceneActivation = true
    }
}