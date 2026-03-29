using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class MainAudioManager : MonoBehaviour
{
    [System.Serializable]
    public class MusicQueue
    {
        public string queueName;
        [Tooltip("The names of the Sounds exactly as they appear in the sounds array.")]
        public string[] trackNames;
    }

    public static MainAudioManager Instance;

    public AudioMixerGroup mainMixerGroup;

    public Sound[] sounds;
    public float fadeInTime;
    public float fadeOutTime;

    public MusicQueue[] musicQueues;

    private List<Sound> unassignedPlayerSounds = new List<Sound>();

    // NEW: Dictionary to track active fades and prevent them from overlapping
    private Dictionary<string, Coroutine> activeFades = new Dictionary<string, Coroutine>();

    private Coroutine activeQueueCoroutine;
    private string currentQueueTrack;

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
        PlayQueue("FirstTrack");
        //Play("Music1");
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

    public void PlayQueue(string queueName)
    {
        MusicQueue queueToPlay = Array.Find(musicQueues, q => q.queueName == queueName);

        if (queueToPlay == null)
        {
            Debug.LogWarning("Music Queue: " + queueName + " not found!");
            return;
        }

        // PREWARM: Load the first track of the new queue into memory immediately
        if (queueToPlay.trackNames.Length > 0)
        {
            Sound firstSound = Array.Find(sounds, item => item.name == queueToPlay.trackNames[0]);
            if (firstSound != null && firstSound.clip != null && firstSound.clip.loadState != AudioDataLoadState.Loaded)
            {
                firstSound.clip.LoadAudioData();
            }
        }

        StopCurrentQueue();
        activeQueueCoroutine = StartCoroutine(ProcessQueueCoroutine(queueToPlay));
    }

    public void StopCurrentQueue()
    {
        if (activeQueueCoroutine != null)
        {
            StopCoroutine(activeQueueCoroutine);
            activeQueueCoroutine = null;
        }

        if (!string.IsNullOrEmpty(currentQueueTrack))
        {
            float fadeOut = fadeOutTime > 0 ? fadeOutTime : 1f;
            Stop(currentQueueTrack, fadeOut);
            currentQueueTrack = "";
        }
    }

    public void PlayRandomQueue()
    {
        if (musicQueues == null || musicQueues.Length == 0)
        {
            Debug.LogWarning("No music queues available to play randomly!");
            return;
        }

        int randomIndex = UnityEngine.Random.Range(1, musicQueues.Length);
        Debug.Log("Playing random queue: " + musicQueues[randomIndex].queueName);
        PlayQueue(musicQueues[randomIndex].queueName);
    }

    private IEnumerator ProcessQueueCoroutine(MusicQueue queue)
    {
        foreach (string trackName in queue.trackNames)
        {
            Sound s = Array.Find(sounds, item => item.name == trackName);

            if (s == null)
            {
                Debug.LogWarning($"Track '{trackName}' in Queue '{queue.queueName}' not found. Skipping.");
                continue;
            }

            currentQueueTrack = trackName;

            // Play the track using our fade-in method (defaults to 1 second if fadeInTime isn't set)
            //float fadeIn = fadeInTime > 0 ? fadeInTime : 1f;
            Play(trackName);

            // Wait until this specific track finishes playing
            // Using WaitWhile checks every frame if the audio source is still playing
            yield return new WaitWhile(() => s.source != null && s.source.isPlaying);
        }

        // If the loop finishes, the queue is complete. Play a random queue next!
        currentQueueTrack = "";
        PlayRandomQueue();
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