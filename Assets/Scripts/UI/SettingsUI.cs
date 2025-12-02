using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    public static event EventHandler OnBackButtonClick;

    [SerializeField] private Button backButton;
    [SerializeField] private Button soundVolumeButton;
    [SerializeField] private TextMeshProUGUI soundVolumeTextMesh;
    [SerializeField] private Button musicVolumeButton;
    [SerializeField] private TextMeshProUGUI musicVolumeTextMesh;

    private void Awake()
    {
        soundVolumeButton.onClick.AddListener(() =>
        {
            SoundManager.Instance.ChangeSoundVolume();
            soundVolumeTextMesh.text = "SOUND " + SoundManager.Instance.GetSoundVolume();
        });
        musicVolumeButton.onClick.AddListener(() =>
        {
            MusicManager.Instance.ChangeMusicVolume();
            musicVolumeTextMesh.text = "MUSIC " + MusicManager.Instance.GetMusicVolume();
        });
        backButton.onClick.AddListener(() =>
        {
            Hide();
            OnBackButtonClick?.Invoke(this, EventArgs.Empty);
        });
    }

    private void Start()
    {
        MainMenuUI.OnSettingsButtonClick += MainMenuUI_OnSettingsButtonClick;

        soundVolumeTextMesh.text = "SOUND " + SoundManager.Instance.GetSoundVolume();
        musicVolumeTextMesh.text = "MUSIC " + MusicManager.Instance.GetMusicVolume();

        Hide();
    }

    private void MainMenuUI_OnSettingsButtonClick(object sender, System.EventArgs e)
    {
        Show();
    }

    private void Show()
    {
        gameObject.SetActive(true);

        backButton.Select();
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        MainMenuUI.OnSettingsButtonClick -= MainMenuUI_OnSettingsButtonClick;
    }
}
