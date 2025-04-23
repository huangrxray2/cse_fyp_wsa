using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }
    private AudioSource audioSource;

    public AudioClip classicClip;
    public AudioClip rainbowPabilionClip;
    public AudioClip heavenlyKingClip;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        audioSource = GetComponent<AudioSource>();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // if (!audioSource.isPlaying)
        // {
        //     audioSource.Play();
        // }

        string sceneName = scene.name;
        AudioClip targetClip = null;

        if (sceneName == "StartUpScene")
            targetClip = classicClip;
        else if (sceneName == "MainMenuScene")
            targetClip = rainbowPabilionClip;
        else if (sceneName == "GameScene")
            targetClip = heavenlyKingClip;

        if (targetClip != null && audioSource.clip != targetClip)
        {
            StartCoroutine(SwitchMusicWithFade(targetClip, 1.0f)); // 1秒淡入淡出
        }
        // 其它场景，不切换音乐
    }

    private IEnumerator SwitchMusicWithFade(AudioClip newClip, float fadeDuration)
    {
        // 淡出
        yield return StartCoroutine(FadeOutCoroutine(fadeDuration));
        // 切换音乐
        audioSource.clip = newClip;
        audioSource.Play();
        // 淡入
        yield return StartCoroutine(FadeInCoroutine(fadeDuration));
    }

    private IEnumerator FadeOutCoroutine(float duration)
    {
        float startVolume = audioSource.volume;
        while (audioSource.volume > 0)
        {
            audioSource.volume -= startVolume * Time.deltaTime / duration;
            yield return null;
        }
        audioSource.Stop();
        audioSource.volume = startVolume; // Reset volume for future use
    }
    
    private IEnumerator FadeInCoroutine(float duration)
    {
        audioSource.volume = 0;
        audioSource.Play();
        while (audioSource.volume < 1.0f)
        {
            audioSource.volume += Time.deltaTime / duration;
            yield return null;
        }
    }
}
