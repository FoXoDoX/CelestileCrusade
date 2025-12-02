using UnityEngine;

public class LanderAudio : MonoBehaviour
{
    [SerializeField] private AudioSource thrusterAudioSource;

    private Lander lander;
    private bool isThrusting = false;

    private void Awake()
    {
        lander = GetComponent<Lander>();
    }

    private void Start()
    {
        lander.OnBeforeForce += Lander_OnBeforeForce;
        lander.OnUpForce += Lander_OnUpForce;
        lander.OnRightForce += Lander_OnRightForce;
        lander.OnLeftForce += Lander_OnLeftForce;

        SoundManager.Instance.OnSoundVolumeChanged += SoundManager_OnSoundVolumeChanged;

        thrusterAudioSource.volume = 0f;
        thrusterAudioSource.loop = true;
        thrusterAudioSource.Play();
    }

    private void SoundManager_OnSoundVolumeChanged(object sender, System.EventArgs e)
    {
        if (isThrusting)
        {
            thrusterAudioSource.volume = SoundManager.Instance.GetSoundVolumeNormalized();
        }
    }

    private void Lander_OnLeftForce(object sender, System.EventArgs e)
    {
        StartThrusting();
    }

    private void Lander_OnRightForce(object sender, System.EventArgs e)
    {
        StartThrusting();
    }

    private void Lander_OnUpForce(object sender, System.EventArgs e)
    {
        StartThrusting();
    }

    private void Lander_OnBeforeForce(object sender, System.EventArgs e)
    {
        StopThrusting();
    }

    private void StartThrusting()
    {
        if (!isThrusting)
        {
            isThrusting = true;
            thrusterAudioSource.volume = SoundManager.Instance.GetSoundVolumeNormalized();
        }
    }

    private void StopThrusting()
    {
        if (isThrusting)
        {
            isThrusting = false;
            thrusterAudioSource.volume = 0f;
        }
    }

    private void OnDestroy()
    {
        SoundManager.Instance.OnSoundVolumeChanged -= SoundManager_OnSoundVolumeChanged;
    }
}