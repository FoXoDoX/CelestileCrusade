using System;
using System.Collections;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    private const int SOUND_VOLUME_MAX = 10;
    private const float WIND_FADE_TIME = 0.5f;

    public static SoundManager Instance { get; private set; }

    private static int soundVolume = 6;

    public event EventHandler OnSoundVolumeChanged;

    [SerializeField] private AudioSource soundEffectsSource;

    [SerializeField] private AudioClip fuelPickupAudioClip;
    [SerializeField] private AudioClip coinPickupAudioClip;
    [SerializeField] private AudioClip crashAudioClip;
    [SerializeField] private AudioClip landingSuccessAudioClip;
    [SerializeField] private AudioClip crateCrackedAudioClip;
    [SerializeField] private AudioClip crateDestroyedAudioClip;
    [SerializeField] private AudioClip crateDeliveredAudioClip;
    [SerializeField] private AudioClip keyPickupAudioClip;
    [SerializeField] private AudioClip keyDeliveredAudioClip;
    [SerializeField] private AudioClip progressBarAudioClip;
    [SerializeField] private AudioClip windAudioClip;

    private AudioSource progressBarAudioSource;
    private AudioSource windAudioSource;
    private Coroutine windFadeCoroutine;
    private bool isWindSoundPlaying = false;
    private float windTargetVolume = 0f;

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

        progressBarAudioSource = gameObject.AddComponent<AudioSource>();
        progressBarAudioSource.playOnAwake = false;
        progressBarAudioSource.loop = false;

        windAudioSource = gameObject.AddComponent<AudioSource>();
        windAudioSource.playOnAwake = false;
        windAudioSource.loop = true;
        windAudioSource.clip = windAudioClip;
        windAudioSource.volume = 0f;
    }

    private void Start()
    {
        SubscribeToEvents();
    }

    private void OnEnable()
    {
        // При активации подписываемся на события
        SubscribeToEvents();
    }

    private void OnDisable()
    {
        // При деактивации отписываемся от событий
        UnsubscribeFromEvents();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        UnsubscribeFromEvents();

        if (windFadeCoroutine != null)
        {
            StopCoroutine(windFadeCoroutine);
        }
    }

    private void SubscribeToEvents()
    {
        // Отписываемся сначала, чтобы избежать дублирования подписок
        UnsubscribeFromEvents();

        // Подписываемся на события Lander, если он существует
        if (Lander.Instance != null)
        {
            Lander.Instance.OnFuelPickup += Lander_OnFuelPickup;
            Lander.Instance.OnCoinPickup += Lander_OnCoinPickup;
            Lander.Instance.OnLanded += Lander_OnLanded;
            Lander.Instance.OnKeyDeliver += Lander_OnKeyDeliver;
        }
        else
        {
            // Если Lander еще не создан, начнем проверять позже
            StartCoroutine(WaitForLanderAndSubscribe());
        }

        // Подписываемся на статическое событие KeyHolder
        KeyHolder.OnKeyPickup += KeyHolder_OnKeyPickup;
    }

    private void UnsubscribeFromEvents()
    {
        if (Lander.Instance != null)
        {
            Lander.Instance.OnFuelPickup -= Lander_OnFuelPickup;
            Lander.Instance.OnCoinPickup -= Lander_OnCoinPickup;
            Lander.Instance.OnLanded -= Lander_OnLanded;
            Lander.Instance.OnKeyDeliver -= Lander_OnKeyDeliver;
        }

        KeyHolder.OnKeyPickup -= KeyHolder_OnKeyPickup;
    }

    private IEnumerator WaitForLanderAndSubscribe()
    {
        // Ждем, пока Lander не будет создан
        while (Lander.Instance == null)
        {
            yield return null;
        }

        // Подписываемся на события Lander
        Lander.Instance.OnFuelPickup += Lander_OnFuelPickup;
        Lander.Instance.OnCoinPickup += Lander_OnCoinPickup;
        Lander.Instance.OnLanded += Lander_OnLanded;
        Lander.Instance.OnKeyDeliver += Lander_OnKeyDeliver;
    }

    private void Lander_OnKeyDeliver(object sender, Lander.OnKeyDeliverEventArgs e)
    {
        PlaySoundEffect(keyDeliveredAudioClip);
    }

    private void KeyHolder_OnKeyPickup(object sender, EventArgs e)
    {
        PlaySoundEffect(keyPickupAudioClip);
    }

    private void Lander_OnLanded(object sender, Lander.OnLandedEventArgs e)
    {
        switch (e.landingType)
        {
            case Lander.LandingType.Success:
                PlaySoundEffect(landingSuccessAudioClip);
                break;
            default:
                PlaySoundEffect(crashAudioClip);
                break;
        }
    }

    private void Lander_OnCoinPickup(object sender, System.EventArgs e)
    {
        PlaySoundEffect(coinPickupAudioClip);
    }

    private void Lander_OnFuelPickup(object sender, System.EventArgs e)
    {
        PlaySoundEffect(fuelPickupAudioClip);
    }

    private void CrateOnRope_OnFuelPickup(object sender, EventArgs e)
    {
        PlaySoundEffect(fuelPickupAudioClip);
    }

    private void CrateOnRope_OnCoinPickup(object sender, EventArgs e)
    {
        PlaySoundEffect(coinPickupAudioClip);
    }

    public void RopeWithCrateSpawned()
    {
        // Отписываемся от старых событий, если они были
        if (CrateOnRope.Instance != null)
        {
            CrateOnRope.Instance.OnCoinPickup -= CrateOnRope_OnCoinPickup;
            CrateOnRope.Instance.OnFuelPickup -= CrateOnRope_OnFuelPickup;
            CrateOnRope.Instance.OnCrateDrop -= CrateOnRope_OnCrateDrop;
            CrateOnRope.Instance.OnCrateCracked -= CrateOnRope_OnCrateCracked;
            CrateOnRope.Instance.OnCrateDestroyed -= CrateOnRope_OnCrateDestroyed;
        }

        // Подписываемся на события нового экземпляра
        if (CrateOnRope.Instance != null)
        {
            CrateOnRope.Instance.OnCoinPickup += CrateOnRope_OnCoinPickup;
            CrateOnRope.Instance.OnFuelPickup += CrateOnRope_OnFuelPickup;
            CrateOnRope.Instance.OnCrateDrop += CrateOnRope_OnCrateDrop;
            CrateOnRope.Instance.OnCrateCracked += CrateOnRope_OnCrateCracked;
            CrateOnRope.Instance.OnCrateDestroyed += CrateOnRope_OnCrateDestroyed;
        }
    }

    private void CrateOnRope_OnCrateDrop(object sender, EventArgs e)
    {
        PlaySoundEffect(crateDeliveredAudioClip);
    }

    private void CrateOnRope_OnCrateDestroyed(object sender, EventArgs e)
    {
        PlaySoundEffect(crateDestroyedAudioClip);
    }

    private void CrateOnRope_OnCrateCracked(object sender, EventArgs e)
    {
        PlaySoundEffect(crateCrackedAudioClip);
    }

    public void PlayProgressBarSound()
    {
        if (progressBarAudioClip == null) return;

        StopProgressBarSound();

        progressBarAudioSource.clip = progressBarAudioClip;
        progressBarAudioSource.volume = GetSoundVolumeNormalized();
        progressBarAudioSource.Play();
    }

    public void StopProgressBarSound()
    {
        if (progressBarAudioSource != null && progressBarAudioSource.isPlaying)
        {
            progressBarAudioSource.Stop();
        }
    }

    private void PlaySoundEffect(AudioClip clip)
    {
        // Проверяем все компоненты на null перед воспроизведением
        if (clip == null || soundEffectsSource == null)
        {
            return;
        }

        if (progressBarAudioSource != null && progressBarAudioSource.isPlaying)
        {
            return;
        }

        soundEffectsSource.PlayOneShot(clip, GetSoundVolumeNormalized());
    }

    public void PlayWindSound()
    {
        if (windAudioClip == null || windAudioSource == null) return;

        windTargetVolume = GetSoundVolumeNormalized();

        if (windFadeCoroutine != null)
        {
            StopCoroutine(windFadeCoroutine);
        }

        if (!isWindSoundPlaying)
        {
            windAudioSource.volume = 0f;
            windAudioSource.Play();
            isWindSoundPlaying = true;
        }

        windFadeCoroutine = StartCoroutine(FadeWindVolume(windTargetVolume));
    }

    public void StopWindSound()
    {
        if (windAudioSource == null) return;

        windTargetVolume = 0f;

        if (windFadeCoroutine != null)
        {
            StopCoroutine(windFadeCoroutine);
        }

        windFadeCoroutine = StartCoroutine(FadeWindVolume(0f));
    }

    private IEnumerator FadeWindVolume(float targetVolume)
    {
        if (windAudioSource == null) yield break;

        float startVolume = windAudioSource.volume;
        float elapsedTime = 0f;

        while (elapsedTime < WIND_FADE_TIME && windAudioSource != null)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / WIND_FADE_TIME;
            windAudioSource.volume = Mathf.Lerp(startVolume, targetVolume, t);
            yield return null;
        }

        if (windAudioSource != null)
        {
            windAudioSource.volume = targetVolume;

            if (targetVolume <= 0f && isWindSoundPlaying)
            {
                windAudioSource.Stop();
                isWindSoundPlaying = false;
            }
        }

        windFadeCoroutine = null;
    }

    public void ChangeSoundVolume()
    {
        soundVolume = (soundVolume + 1) % SOUND_VOLUME_MAX;

        if (progressBarAudioSource != null)
        {
            progressBarAudioSource.volume = GetSoundVolumeNormalized();
        }

        if (windAudioSource != null && isWindSoundPlaying)
        {
            windTargetVolume = GetSoundVolumeNormalized();

            if (windFadeCoroutine != null)
            {
                StopCoroutine(windFadeCoroutine);
            }

            windFadeCoroutine = StartCoroutine(FadeWindVolume(windTargetVolume));
        }

        PlaySoundEffect(coinPickupAudioClip);
        OnSoundVolumeChanged?.Invoke(this, EventArgs.Empty);
    }

    public int GetSoundVolume()
    {
        return soundVolume;
    }

    public float GetSoundVolumeNormalized()
    {
        return ((float)soundVolume) / SOUND_VOLUME_MAX;
    }

    // Метод для принудительной переподписки на события (вызывать при загрузке нового уровня)
    public void RefreshSubscriptions()
    {
        SubscribeToEvents();
    }
}