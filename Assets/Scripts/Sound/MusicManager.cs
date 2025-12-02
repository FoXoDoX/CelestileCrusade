using System;
using System.Collections.Generic;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    private const int MUSIC_VOLUME_MAX = 10;

    public static MusicManager Instance { get; private set; }

    private static int musicVolume = 4;

    public event EventHandler OnMusicVolumeChanged;

    [SerializeField] private AudioClip[] musicTracks;

    private AudioSource musicAudioSource;
    private List<int> availableTracks = new List<int>();
    private int currentTrackIndex = -1;
    private bool isApplicationFocused = true;
    private bool isMusicPaused = false;

    private void Awake()
    {
        // Обеспечиваем единственность экземпляра
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        musicAudioSource = GetComponent<AudioSource>();

        // Настраиваем AudioSource если он существует
        if (musicAudioSource != null)
        {
            musicAudioSource.loop = false; // Мы сами управляем повторением
            musicAudioSource.volume = GetMusicVolumeNormalized();
        }

        InitializeTrackList();

        if (musicTracks != null && musicTracks.Length > 0)
        {
            PlayNextTrack();
        }
    }

    private void OnEnable()
    {
        // При активации возобновляем музыку, если она была на паузе
        if (isApplicationFocused && musicAudioSource != null && !musicAudioSource.isPlaying &&
            musicAudioSource.clip != null && !isMusicPaused)
        {
            // Если трек закончился, играем следующий
            if (musicAudioSource.time >= musicAudioSource.clip.length - 0.1f)
            {
                PlayNextTrack();
            }
            else
            {
                musicAudioSource.Play();
            }
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        isApplicationFocused = hasFocus;

        if (hasFocus)
        {
            // При возвращении фокуса
            if (musicAudioSource != null && !isMusicPaused)
            {
                if (musicAudioSource.clip != null)
                {
                    // Если трек закончился, играем следующий
                    if (musicAudioSource.time >= musicAudioSource.clip.length - 0.1f)
                    {
                        PlayNextTrack();
                    }
                    else if (!musicAudioSource.isPlaying)
                    {
                        musicAudioSource.Play();
                    }
                }
                else if (musicTracks != null && musicTracks.Length > 0)
                {
                    PlayNextTrack();
                }
            }
        }
        else
        {
            // При потере фокуса
            if (musicAudioSource != null && musicAudioSource.isPlaying)
            {
                musicAudioSource.Pause();
            }
        }
    }

    private void InitializeTrackList()
    {
        availableTracks.Clear();
        if (musicTracks != null)
        {
            for (int i = 0; i < musicTracks.Length; i++)
            {
                availableTracks.Add(i);
            }
        }
    }

    private void Update()
    {
        if (!isApplicationFocused || isMusicPaused || musicAudioSource == null) return;

        // Проверяем, закончился ли текущий трек
        if (musicAudioSource.clip != null &&
            musicAudioSource.isPlaying &&
            musicAudioSource.time >= musicAudioSource.clip.length - 0.1f)
        {
            // Трек почти закончился, начинаем следующий
            PlayNextTrack();
        }
        // Если по какой-то причине музыка не играет, но должен быть активный трек
        else if (musicAudioSource.clip != null &&
                 !musicAudioSource.isPlaying &&
                 musicTracks != null &&
                 musicTracks.Length > 0)
        {
            // Проверяем, не закончился ли трек полностью
            if (musicAudioSource.time < musicAudioSource.clip.length - 0.1f)
            {
                // Если трек не закончился, продолжаем его воспроизведение
                musicAudioSource.Play();
            }
            else
            {
                // Трек закончился, играем следующий
                PlayNextTrack();
            }
        }
    }

    private void PlayNextTrack()
    {
        if (musicAudioSource == null || musicTracks == null || musicTracks.Length == 0)
        {
            return;
        }

        // Если список доступных треков пуст, переинициализируем его
        if (availableTracks.Count == 0)
        {
            InitializeTrackList();

            // Исключаем текущий трек из следующего раунда, если есть другие треки
            if (currentTrackIndex != -1 && availableTracks.Count > 1)
            {
                availableTracks.Remove(currentTrackIndex);
            }
        }

        // Выбираем случайный трек
        int randomIndex = UnityEngine.Random.Range(0, availableTracks.Count);
        currentTrackIndex = availableTracks[randomIndex];
        availableTracks.RemoveAt(randomIndex);

        // Останавливаем текущее воспроизведение
        if (musicAudioSource.isPlaying)
        {
            musicAudioSource.Stop();
        }

        // Устанавливаем новый трек
        musicAudioSource.clip = musicTracks[currentTrackIndex];
        musicAudioSource.volume = GetMusicVolumeNormalized();
        musicAudioSource.Play();

        isMusicPaused = false;

        Debug.Log($"Playing track: {musicTracks[currentTrackIndex].name}");
    }

    public void ChangeMusicVolume()
    {
        musicVolume = (musicVolume + 1) % MUSIC_VOLUME_MAX;

        if (musicAudioSource != null)
        {
            musicAudioSource.volume = GetMusicVolumeNormalized();
        }

        OnMusicVolumeChanged?.Invoke(this, EventArgs.Empty);

        Debug.Log($"Music volume changed to: {musicVolume}");
    }

    public void PauseMusic()
    {
        if (musicAudioSource != null && musicAudioSource.isPlaying)
        {
            musicAudioSource.Pause();
            isMusicPaused = true;
        }
    }

    public void ResumeMusic()
    {
        if (musicAudioSource != null && !musicAudioSource.isPlaying && isMusicPaused)
        {
            musicAudioSource.Play();
            isMusicPaused = false;
        }
    }

    public void RestartMusic()
    {
        if (musicAudioSource != null && musicTracks != null && musicTracks.Length > 0)
        {
            InitializeTrackList();
            PlayNextTrack();
        }
    }

    public int GetMusicVolume()
    {
        return musicVolume;
    }

    public float GetMusicVolumeNormalized()
    {
        return ((float)musicVolume) / MUSIC_VOLUME_MAX;
    }
}