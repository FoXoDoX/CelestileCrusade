using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class StatsUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI statsTextMesh;
    [SerializeField] private Image fuelImage;
    [SerializeField] private RectTransform lowFuel;

    Image lowFuelBackgroundImage;

    private Sequence blinkSequence;
    private bool isGameOver = false;

    private void Start()
    {
        lowFuelBackgroundImage = lowFuel.GetComponentInChildren<Image>();

        Hide();

        Lander.Instance.OnStateChanged += Lander_OnStateChanged;

        lowFuel.gameObject.SetActive(false);
    }

    private void Lander_OnStateChanged(object sender, Lander.OnStateChangedEventArgs e)
    {
        if (e.state != Lander.State.WaitingToStart)
        {
            Show();
        }
        if (e.state == Lander.State.GameOver)
        {
            isGameOver = true;
        }
    }

    private void Update()
    {
        UpdateStatsTextMesh();
        UpdateLowFuelWarning();
    }

    private void UpdateStatsTextMesh()
    {
        statsTextMesh.text =
            GameData.CurrentLevel + "\n" +
            GameManager.Instance.GetScore() + "\n" +
            Mathf.Round(GameManager.Instance.GetTime());

        fuelImage.fillAmount = Lander.Instance.GetFuelAmountNormalized();
    }

    private void UpdateLowFuelWarning()
    {
        if (isGameOver) 
        {
            lowFuel.gameObject.SetActive(false);
            StopLowFuelBlinking();
            return; 
        }

        bool isLowFuel = Lander.Instance.GetFuelAmountNormalized() < 0.25f;

        if (isLowFuel)
        {
            lowFuel.gameObject.SetActive(true);
            if (blinkSequence == null)
            {
                StartLowFuelBlinking();
            }
        }
        else if (!isLowFuel)
        {
            lowFuel.gameObject.SetActive(false);
            StopLowFuelBlinking();
        }
    }

    private void StartLowFuelBlinking()
    {
        blinkSequence = DOTween.Sequence();

        blinkSequence.Append(lowFuelBackgroundImage.DOFade(0.5f, 0.5f));
        blinkSequence.Append(lowFuelBackgroundImage.DOFade(0.2f, 0.5f));

        blinkSequence.SetLoops(-1, LoopType.Restart);

        blinkSequence.Play();
    }

    private void StopLowFuelBlinking()
    {
        if (blinkSequence != null)
        {
            blinkSequence.Kill();
        }
    }

    private void Show()
    {
        gameObject.SetActive(true);
    }

    private void Hide()
    {
        gameObject.SetActive(false);

        StopLowFuelBlinking();
    }
}