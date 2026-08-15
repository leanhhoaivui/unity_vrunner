using UnityEngine;
using System.Collections.Generic;
using VRunner.Data;
using VRunner.Gameplay.Obstacles;

namespace VRunner.Core
{
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

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetupAudioSources();
        }
        private void Start()
        {
            ApplySavedVolumes();
        }
        private void OnEnable()
        {
            if (EventManager.Instance == null) return;
            EventManager.Instance.OnPlayerJump += OnJump;
            EventManager.Instance.OnCoinCollected += OnCoin;
            EventManager.Instance.OnPowerupActivated += OnPowerup;
            EventManager.Instance.OnObstacleHit += OnHit;       // hurt / va chạm sống
            EventManager.Instance.OnPlayerDeath += OnDeath;
        }
        private void OnDisable()
        {
            if (EventManager.Instance == null) return;
            EventManager.Instance.OnPlayerJump -= OnJump;
            EventManager.Instance.OnCoinCollected -= OnCoin;
            EventManager.Instance.OnPowerupActivated -= OnPowerup;
            EventManager.Instance.OnObstacleHit -= OnHit;
            EventManager.Instance.OnPlayerDeath -= OnDeath;
        }
        private void OnJump(float height) => PlaySFX(jumpSound);
        private void OnCoin(int value) => PlaySFX(coinSound);
        private void OnPowerup(PowerupType type, float dur) => PlaySFX(powerupSound);
        private void OnHit(ObstacleType type) => PlaySFX(hitSound);
        private void OnDeath() => PlaySFX(deathSound);
        public void PlayMenuMusic() => PlayMusic(menuMusic);
        public void PlayGameMusic() => PlayMusic(gameMusic);
        public void PlayMusic(AudioClip clip)
        {
            if (clip == null) return;
            if (musicSource.clip == clip && musicSource.isPlaying) return;
            musicSource.clip = clip;
            musicSource.Play();
        }
        public void PlaySFX(AudioClip clip)
        {
            if (clip != null) sfxSource.PlayOneShot(clip);
        }
        public void PlayButtonClick() => PlaySFX(buttonClick);
        public void SetMusicVolume(float volume) => musicSource.volume = Mathf.Clamp01(volume);
        public void SetSFXVolume(float volume) => sfxSource.volume = Mathf.Clamp01(volume);
        private void ApplySavedVolumes()
        {
            if (SaveManager.Instance == null) return;
            var save = SaveManager.Instance.CurrentSave;
            SetMusicVolume(save.musicVolume);
            SetSFXVolume(save.sfxVolume);
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
    }
}
