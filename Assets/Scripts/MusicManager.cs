using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    private static MusicManager instance;
    private AudioSource audioSource;

    public AudioClip menuMusic;
    public AudioClip levelMusic;

    void Awake()
    {
        if (instance != null) 
        { 
            Destroy(gameObject); return; 
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        audioSource = GetComponent<AudioSource>();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool isLevel = GameObject.FindWithTag("GameController");
        AudioClip targetClip = isLevel ? levelMusic : menuMusic;

        if (audioSource.clip != targetClip)
        {
            audioSource.clip = targetClip;
            audioSource.Play();
        }
    }
    public static void PauseMusic() => instance.audioSource.Pause();
    public static void ResumeMusic() => instance.audioSource.UnPause();
}