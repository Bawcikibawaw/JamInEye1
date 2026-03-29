using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class PauseMenu : MonoBehaviour
{
    public static bool IsPaused { get; private set; }

    [Header("UI Setup")]
    public GameObject pauseMenuUI; 

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsPaused) Resume();
            else Pause();
        }
    }

    public void Resume()
    {
        IsPaused = false;
        
        // Only unfreeze time if we aren't in the middle of a Level Fade
        if (FadeManager.Instance != null && !FadeManager.Instance.IsFading)
        {
            Time.timeScale = 1f;
        }

        // Animated Close: Shrink the menu then turn it off
        pauseMenuUI.transform.DOScale(0.8f, 0.2f).SetEase(Ease.InBack).SetUpdate(true);
        
        // Wait for the animation to finish before deactivating the object
        DOVirtual.DelayedCall(0.2f, () => pauseMenuUI.SetActive(false)).SetUpdate(true);
    }

    public void Pause()
    {
        IsPaused = true;
        Time.timeScale = 0f; // Freeze the world

        pauseMenuUI.SetActive(true);

        // Animated Pop-in: Start small and "bounce" to full size
        pauseMenuUI.transform.localScale = Vector2.one * 0.7f;
        pauseMenuUI.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack).SetUpdate(true);
    }

    public void RestartLevel()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu"); 
    }
}