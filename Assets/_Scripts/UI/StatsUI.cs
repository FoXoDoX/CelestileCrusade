using My.Scripts.Core.Data;
using My.Scripts.EventBus;
using My.Scripts.Gameplay.Player;
using My.Scripts.Managers;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace My.Scripts.UI
{
    public class StatsUI : MonoBehaviour
    {
        #region Constants

        private const float LOW_FUEL_THRESHOLD = 0.25f;
        private const float BLINK_FADE_HIGH = 0.5f;
        private const float BLINK_FADE_LOW = 0.2f;
        private const float BLINK_DURATION = 0.5f;

        #endregion

        #region Serialized Fields

        [Header("Stats Display")]
        [SerializeField] private TextMeshProUGUI _statsText;
        [SerializeField] private Image _fuelFillImage;

        [Header("Low Fuel Warning")]
        [SerializeField] private RectTransform _lowFuelWarning;
        [SerializeField] private Image _lowFuelBackgroundImage;

        #endregion

        #region Private Fields

        private Sequence _blinkSequence;
        private bool _isGameOver;
        private bool _isLowFuelWarningActive;
        private bool _isSubscribed;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            CacheComponents();
        }

        private void Start()
        {
            SubscribeToUIEvents();  // ѕодписка один раз
            Hide();
            HideLowFuelWarning();
        }

        private void OnDestroy()
        {
            UnsubscribeFromUIEvents();  // ќтписка только при уничтожении
            StopBlinking();
        }

        private void Update()
        {
            // ќбновл€ем только если активны
            if (!gameObject.activeInHierarchy) return;
            if (_isGameOver) return;

            UpdateStats();
            UpdateFuelIndicator();
            UpdateLowFuelWarning();
        }

        #endregion

        #region Private Methods Ч Initialization

        private void CacheComponents()
        {
            if (_lowFuelBackgroundImage == null && _lowFuelWarning != null)
            {
                _lowFuelBackgroundImage = _lowFuelWarning.GetComponentInChildren<Image>();
            }
        }

        #endregion

        #region Private Methods Ч Event Subscription

        private void SubscribeToUIEvents()
        {
            if (_isSubscribed) return;

            EventManager.Instance?.AddHandler<LanderStateData>(
                GameEvents.LanderStateChanged,
                OnLanderStateChanged
            );

            _isSubscribed = true;
        }

        private void UnsubscribeFromUIEvents()
        {
            if (!_isSubscribed) return;
            if (!EventManager.HasInstance) return;

            EventManager.Instance.RemoveHandler<LanderStateData>(
                GameEvents.LanderStateChanged,
                OnLanderStateChanged
            );

            _isSubscribed = false;
        }

        #endregion

        #region Private Methods Ч Event Handlers

        private void OnLanderStateChanged(LanderStateData data)
        {
            switch (data.State)
            {
                case Lander.State.WaitingToStart:
                    break;

                case Lander.State.Normal:
                    Show();
                    break;

                case Lander.State.GameOver:
                    HandleGameOver();
                    break;
            }
        }

        #endregion

        #region Private Methods Ч UI Updates

        private void UpdateStats()
        {
            if (_statsText == null) return;

            int level = GameData.CurrentLevel;
            int score = GameManager.HasInstance ? GameManager.Instance.Score : 0;
            float time = GameManager.HasInstance ? GameManager.Instance.Time : 0f;

            _statsText.text = $"{level}\n{score}\n{Mathf.Round(time)}";
        }

        private void UpdateFuelIndicator()
        {
            if (_fuelFillImage == null) return;
            if (!Lander.HasInstance) return;

            _fuelFillImage.fillAmount = Lander.Instance.GetFuelNormalized();
        }

        private void UpdateLowFuelWarning()
        {
            if (!Lander.HasInstance) return;

            bool isLowFuel = Lander.Instance.GetFuelNormalized() < LOW_FUEL_THRESHOLD;

            if (isLowFuel && !_isLowFuelWarningActive)
            {
                ShowLowFuelWarning();
            }
            else if (!isLowFuel && _isLowFuelWarningActive)
            {
                HideLowFuelWarning();
            }
        }

        #endregion

        #region Private Methods Ч Low Fuel Warning

        private void ShowLowFuelWarning()
        {
            _isLowFuelWarningActive = true;

            if (_lowFuelWarning != null)
            {
                _lowFuelWarning.gameObject.SetActive(true);
            }

            StartBlinking();
        }

        private void HideLowFuelWarning()
        {
            _isLowFuelWarningActive = false;

            if (_lowFuelWarning != null)
            {
                _lowFuelWarning.gameObject.SetActive(false);
            }

            StopBlinking();
        }

        private void StartBlinking()
        {
            if (_lowFuelBackgroundImage == null) return;
            if (_blinkSequence != null) return;

            _blinkSequence = DOTween.Sequence()
                .Append(_lowFuelBackgroundImage.DOFade(BLINK_FADE_HIGH, BLINK_DURATION))
                .Append(_lowFuelBackgroundImage.DOFade(BLINK_FADE_LOW, BLINK_DURATION))
                .SetLoops(-1, LoopType.Restart)
                .SetLink(gameObject);
        }

        private void StopBlinking()
        {
            if (_blinkSequence == null) return;

            _blinkSequence.Kill();
            _blinkSequence = null;

            if (_lowFuelBackgroundImage != null)
            {
                _lowFuelBackgroundImage.DOKill();
                var color = _lowFuelBackgroundImage.color;
                color.a = BLINK_FADE_LOW;
                _lowFuelBackgroundImage.color = color;
            }
        }

        #endregion

        #region Private Methods Ч Visibility

        private void HandleGameOver()
        {
            _isGameOver = true;
            HideLowFuelWarning();
        }

        private void Show()
        {
            gameObject.SetActive(true);
        }

        private void Hide()
        {
            gameObject.SetActive(false);
            StopBlinking();
        }

        #endregion
    }
}