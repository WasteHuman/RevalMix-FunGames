using System.Collections.Generic;
using UnityEngine;

namespace Core.Services.Audio
{
    public class AudioController : MonoBehaviour
    {
        public static AudioController Instance { get; private set; }

        [SerializeField] private AudioClip _music;
        [SerializeField] private List<AudioClip> _sounds;

        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private AudioSource _sfxSource;

        private float _masterVolume = 1f;
        private float _musicVolume = 1f;
        private float _sfxVolume = 1f;

        private const string PREF_MASTER_VOLUME = "Audio_MasterVolume";
        private const string PREF_MUSIC_VOLUME = "Audio_MusicVolume";
        private const string PREF_SFX_VOLUME = "Audio_SfxVolume";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (_musicSource == null)
                _musicSource = gameObject.AddComponent<AudioSource>();
            if (_sfxSource == null)
                _sfxSource = gameObject.AddComponent<AudioSource>();

            LoadVolumeSettings();

            PlayMusic(_music);
        }

        public void SetMusicVolume(float volume)
        {
            _musicVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(PREF_MUSIC_VOLUME, _musicVolume);
            UpdateVolumes();
        }

        public void SetSfxVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(PREF_SFX_VOLUME, _sfxVolume);
            UpdateVolumes();
        }

        /// <summary>Получить общий уровень громкости</summary>
        public float GetMasterVolume() => _masterVolume;

        /// <summary>Получить уровень громкости музыки</summary>
        public float GetMusicVolume() => _musicVolume;

        /// <summary>Получить уровень громкости эффектов</summary>
        public float GetSfxVolume() => _sfxVolume;

        /// <summary>Воспроизвести эффект</summary>
        public void PlaySfx(int clipIndex)
        {
            if (clipIndex > _sounds.Count)
                return;

            var clip = _sounds[clipIndex];

            if (clip == null)
            {
                Debug.LogWarning("[AudioManager] SFX clip is null");
                return;
            }

            _sfxSource.PlayOneShot(clip);
        }

        /// <summary>Воспроизвести музыку</summary>
        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (clip == null)
            {
                Debug.LogWarning("[AudioManager] Music clip is null");
                return;
            }

            if (_musicSource.isPlaying)
                _musicSource.Stop();

            _musicSource.clip = clip;
            _musicSource.loop = loop;
            _musicSource.Play();
        }

        /// <summary>Остановить музыку</summary>
        public void StopMusic()
        {
            _musicSource.Stop();
        }

        /// <summary>Пауза музыки</summary>
        public void PauseMusic()
        {
            _musicSource.Pause();
        }

        /// <summary>Возобновить музыку</summary>
        public void ResumeMusic()
        {
            _musicSource.UnPause();
        }

        private void UpdateVolumes()
        {
            _musicSource.volume = _musicVolume;
            _sfxSource.volume = _sfxVolume;
        }

        private void LoadVolumeSettings()
        {
            _masterVolume = PlayerPrefs.GetFloat(PREF_MASTER_VOLUME, 1f);
            _musicVolume = PlayerPrefs.GetFloat(PREF_MUSIC_VOLUME, 1f);
            _sfxVolume = PlayerPrefs.GetFloat(PREF_SFX_VOLUME, 1f);

            UpdateVolumes();
        }
    }
}