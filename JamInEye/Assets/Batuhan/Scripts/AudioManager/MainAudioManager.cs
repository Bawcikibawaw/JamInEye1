using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class MainAudioManager : MonoBehaviour
{
    public static MainAudioManager Instance;

    public AudioMixerGroup mainMixerGroup;

    public Sound[] sounds;
    public float fadeInTime;
    public float fadeOutTime;

    private List<Sound> unassignedPlayerSounds = new List<Sound>();

    // NEW: Dictionary to track active fades and prevent them from overlapping
    private Dictionary<string, Coroutine> activeFades = new Dictionary<string, Coroutine>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        foreach (Sound s in sounds)
        {
            if (s.attachedOnPlayer)
            {
                unassignedPlayerSounds.Add(s);
                if (s.playOnAwake)
                    Play(s.name);
            }
            else
            {
                AttachSoundToObject(s, s.attachedObject != null ? s.attachedObject : gameObject);
                if (s.playOnAwake)
                    Play(s.name);
            }
        }
    }

    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        //Play("MainSong");
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            OnPlayerLoaded(player);
        }
    }

    public void OnPlayerLoaded(GameObject newPlayer)
    {
        foreach (Sound s in unassignedPlayerSounds.ToArray())
        {
            if (newPlayer != null)
            {
                AttachSoundToObject(s, newPlayer);
                //unassignedPlayerSounds.Remove(s); 
            }
        }
    }

    public void AttachSoundToObject(Sound s, GameObject targetObject)
    {
        if (s.source != null)
        {
            Destroy(s.source);
        }

        s.attachedObject = targetObject;
        s.source = targetObject.AddComponent<AudioSource>();

        s.source.clip = s.clip;
        s.source.volume = s.volume;
        s.source.loop = s.loop;
        s.source.playOnAwake = s.playOnAwake;
        s.source.outputAudioMixerGroup = s.mixerGroup;
    }

    // ORIGINAL PLAY
    public void Play(string sound)
    {
        Sound s = Array.Find(sounds, item => item.name == sound);
        if (s == null)
        {
            Debug.LogWarning("Sound: " + sound + " not found!");
            return;
        }

        s.source.volume = s.volume * (1f + UnityEngine.Random.Range(-s.volumeVariance / 2f, s.volumeVariance / 2f));
        s.source.pitch = s.pitch * (1f + UnityEngine.Random.Range(-s.pitchVariance / 2f, s.pitchVariance / 2f));

        s.source.Play();
    }

    // NEW OVERLOAD: PLAY WITH FADE IN
    public void Play(string sound, float fadeDuration)
    {
        Sound s = Array.Find(sounds, item => item.name == sound);
        if (s == null)
        {
            Debug.LogWarning("Sound: " + sound + " not found!");
            return;
        }

        // Cancel existing fade if one is currently happening for this sound
        if (activeFades.ContainsKey(sound) && activeFades[sound] != null)
        {
            StopCoroutine(activeFades[sound]);
        }

        activeFades[sound] = StartCoroutine(FadeInCoroutine(s, fadeDuration));
    }

    // ORIGINAL STOP
    public void Stop(string sound)
    {
        Sound s = Array.Find(sounds, item => item.name == sound);
        if (s == null)
        {
            Debug.LogWarning("Sound: " + sound + " not found!");
            return;
        }

        if (s.source.isPlaying) s.source.Stop();
    }

    // NEW OVERLOAD: STOP WITH FADE OUT
    public void Stop(string sound, float fadeDuration)
    {
        Sound s = Array.Find(sounds, item => item.name == sound);
        if (s == null)
        {
            Debug.LogWarning("Sound: " + sound + " not found!");
            return;
        }

        // Cancel existing fade if one is currently happening
        if (activeFades.ContainsKey(sound) && activeFades[sound] != null)
        {
            StopCoroutine(activeFades[sound]);
        }

        if (s.source.isPlaying)
        {
            activeFades[sound] = StartCoroutine(FadeOutCoroutine(s, fadeDuration));
        }
    }

    // NEW: FADE IN COROUTINE
    private IEnumerator FadeInCoroutine(Sound s, float duration)
    {
        // Calculate the target volume using your existing variance logic
        float targetVolume = s.volume * (1f + UnityEngine.Random.Range(-s.volumeVariance / 2f, s.volumeVariance / 2f));
        s.source.pitch = s.pitch * (1f + UnityEngine.Random.Range(-s.pitchVariance / 2f, s.pitchVariance / 2f));

        s.source.volume = 0f;
        s.source.Play();

        float currentTime = 0f;
        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            s.source.volume = Mathf.Lerp(0f, targetVolume, currentTime / duration);
            yield return null;
        }

        s.source.volume = targetVolume;
    }

    // NEW: FADE OUT COROUTINE
    private IEnumerator FadeOutCoroutine(Sound s, float duration)
    {
        float startVolume = s.source.volume;
        float currentTime = 0f;

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            s.source.volume = Mathf.Lerp(startVolume, 0f, currentTime / duration);
            yield return null;
        }

        s.source.volume = 0f;
        s.source.Stop();

        // Reset the volume back to its default so it plays correctly next time without a fade
        s.source.volume = s.volume;
    }

    public void PlayAtLocation(string sound, Vector3 pos, float spatialBlendValue = 1f, float minRangeValue = 1, float maxRangeValue = 10)
    {
        Debug.Log(sound + " sound is playing");
        Sound s = Array.Find(sounds, item => item.name == sound);
        if (s == null)
        {
            Debug.LogWarning("Sound: " + sound + " not found!");
            return;
        }

        GameObject tempAudioObject = new GameObject("TempAudio_" + sound);
        tempAudioObject.transform.position = pos;
        //tempAudioObject.transform.parent=Referances.Instance.audioContainer.transform;

        AudioSource tempAudioSource = tempAudioObject.AddComponent<AudioSource>();

        tempAudioSource.clip = s.clip;
        tempAudioSource.volume = s.volume * (1f + UnityEngine.Random.Range(-s.volumeVariance / 2f, s.volumeVariance / 2f));
        tempAudioSource.pitch = s.pitch * (1f + UnityEngine.Random.Range(-s.pitchVariance / 2f, s.pitchVariance / 2f));
        tempAudioSource.outputAudioMixerGroup = s.mixerGroup;
        tempAudioSource.spatialBlend = spatialBlendValue;
        tempAudioSource.loop = false;
        tempAudioSource.minDistance = minRangeValue;
        tempAudioSource.maxDistance = maxRangeValue;

        tempAudioSource.Play();

        Destroy(tempAudioObject, s.clip.length / tempAudioSource.pitch);
    }
}