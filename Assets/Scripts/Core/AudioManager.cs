using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    
    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    
    [Header("Music")]
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip gameMusic;
    
    [Header("SFX")]
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip landSound;
    [SerializeField] private AudioClip coinSound;
    [SerializeField] private AudioClip powerupSound;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioClip buttonClick;
    
    private Dictionary<string, AudioClip> sfxDictionary;
    

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        SetupAudioSources();
        CacheSFX();
        SubscribeToEvents();
    }

    private void OnEnable()
    {
        EventManager.Instance.OnCoinCollected += PlayCoinSound;
        EventManager.Instance.OnPlayerJump += PlayJumpSound;
        EventManager.Instance.OnObstacleHit += PlayHitSound;
    }
    
    private void OnDisable()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnCoinCollected -= PlayCoinSound;
            EventManager.Instance.OnPlayerJump -= PlayJumpSound;
            EventManager.Instance.OnObstacleHit -= PlayHitSound;
        }
    }
    
    private void SetupAudioSources()
    {
        if (musicSource == null)
        {
            GameObject musicObj = new GameObject("MusicSource");
            musicObj.transform.SetParent(transform);
            musicSource = musicObj.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }
        
        if (sfxSource == null)
        {
            GameObject sfxObj = new GameObject("SFXSource");
            sfxObj.transform.SetParent(transform);
            sfxSource = sfxObj.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }
    }
    
    private void CacheSFX()
    {
        sfxDictionary = new Dictionary<string, AudioClip>
        {
            { "jump", jumpSound },
            { "land", landSound },
            { "coin", coinSound },
            { "powerup", powerupSound },
            { "hit", hitSound },
            { "death", deathSound },
            { "button", buttonClick }
        };
    }
    
    private void SubscribeToEvents()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnPlayerJump += (height) => PlaySFX("jump");
            EventManager.Instance.OnCoinCollected += (value) => PlaySFX("coin");
            EventManager.Instance.OnPowerupActivated += (type, dur) => PlaySFX("powerup");
            EventManager.Instance.OnObstacleHit += (type) => PlaySFX("hit");
            EventManager.Instance.OnPlayerDeath += () => PlaySFX("death");
        }
    }
    
    public void PlayMusic(AudioClip clip)
    {
        if (musicSource.clip == clip && musicSource.isPlaying) return;
        
        musicSource.clip = clip;
        musicSource.Play();
    }
    
    public void PlayGameMusic()
    {
        PlayMusic(gameMusic);
    }
    
    public void PlaySFX(string sfxName)
    {
        if (sfxDictionary.TryGetValue(sfxName, out AudioClip clip))
        {
            PlaySFX(clip);
        }
    }
    
    public void PlaySFX(AudioClip clip)
    {
        if (clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }
    
    public void SetMusicVolume(float volume)
    {
        musicSource.volume = Mathf.Clamp01(volume);
    }
    
    public void SetSFXVolume(float volume)
    {
        sfxSource.volume = Mathf.Clamp01(volume);
    }

    private void PlayCoinSound(int value) { /* ... */ }
    private void PlayJumpSound(float height) { /* ... */ }
    private void PlayHitSound(ObstacleType type) { /* ... */ }
}