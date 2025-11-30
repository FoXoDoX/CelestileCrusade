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

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        musicAudioSource = GetComponent<AudioSource>();
        InitializeTrackList();

        if (musicTracks != null && musicTracks.Length > 0)
        {
            PlayNextTrack();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        isApplicationFocused = hasFocus;

        if (hasFocus && musicAudioSource.clip != null && !musicAudioSource.isPlaying)
        {
            if (musicAudioSource.time < musicAudioSource.clip.length - 0.1f)
            {
                musicAudioSource.Play();
            }
        }
    }

    private void InitializeTrackList()
    {
        availableTracks.Clear();
        for (int i = 0; i < musicTracks.Length; i++)
        {
            availableTracks.Add(i);
        }
    }

    private void Update()
    {
        if (!isApplicationFocused) return;

        if (musicAudioSource.clip != null &&
            !musicAudioSource.isPlaying &&
            musicTracks != null &&
            musicTracks.Length > 0)
        {
            PlayNextTrack();
        }
    }

    private void PlayNextTrack()
    {
        if (musicTracks.Length == 0) return;

        if (availableTracks.Count == 0)
        {
            InitializeTrackList();

            if (currentTrackIndex != -1 && availableTracks.Count > 1)
            {
                availableTracks.Remove(currentTrackIndex);
            }
        }

        int randomIndex = UnityEngine.Random.Range(0, availableTracks.Count);
        currentTrackIndex = availableTracks[randomIndex];
        availableTracks.RemoveAt(randomIndex);

        musicAudioSource.clip = musicTracks[currentTrackIndex];
        musicAudioSource.Play();
    }

    public void ChangeMusicVolume()
    {
        musicVolume = (musicVolume + 1) % MUSIC_VOLUME_MAX;
        musicAudioSource.volume = GetMusicVolumeNormalized();
        OnMusicVolumeChanged?.Invoke(this, EventArgs.Empty);
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