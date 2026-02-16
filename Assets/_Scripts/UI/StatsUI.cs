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

        private const float LOW_ENERGY_THRESHOLD = 0.25f;
        private const float BLINK_FADE_HIGH = 0.5f;
        private const float BLINK_FADE_LOW = 0.2f;
        private const float BLINK_DURATION = 0.5f;

        #endregion

        #region Serialized Fields

        [Header("Stats Display")]
        [SerializeField] private TextMeshProUGUI _statsText;
        [SerializeField] private Image _energyFillImage;

        [Header("Low Energy Warning")]
        [SerializeField] private RectTransform _lowEnergyWarning;
        [SerializeField] private Image _lowEnergyBackgroundImage;

        #endregion

        #region Private Fields

        private Sequence _blinkSequence;
        private bool _isGameOver;
        private bool _isLowFuelWarningActive;
        private bool _isSubscribed;
        private bool _isTutorialActive;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            CacheComponents();
        }

        private void Start()
        {
            SubscribeToEvents();
            CheckInitialTutorialState();
            UpdateVisibility();
            HideLowFuelWarning();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            StopBlinking();
        }

        private void Update()
        {
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
            if (_lowEnergyBackgroundImage == null && _lowEnergyWarning != null)
            {
                _lowEnergyBackgroundImage = _lowEnergyWarning.GetComponentInChildren<Image>();
            }
        }

        private void CheckInitialTutorialState()
        {
            // ѕровер€ем, есть ли TutorialManager на сцене при старте
            var tutorialManager = FindFirstObjectByType<TutorialManager>();
            _isTutorialActive = tutorialManager != null && tutorialManager.IsTutorialActive;
        }

        #endregion

        #region Private Methods Ч Event Subscription

        private void SubscribeToEvents()
        {
            if (_isSubscribed) return;
            if (!EventManager.HasInstance) return;

            EventManager.Instance.AddHandler<LanderStateData>(
                GameEvents.LanderStateChanged,
                OnLanderStateChanged
            );

            EventManager.Instance.AddHandler(
                GameEvents.TutorialStarted,
                OnTutorialStarted
            );

            EventManager.Instance.AddHandler(
                GameEvents.TutorialCompleted,
                OnTutorialCompleted
            );

            _isSubscribed = true;
        }

        private void UnsubscribeFromEvents()
        {
            if (!_isSubscribed) return;
            if (!EventManager.HasInstance) return;

            EventManager.Instance.RemoveHandler<LanderStateData>(
                GameEvents.LanderStateChanged,
                OnLanderStateChanged
            );

            EventManager.Instance.RemoveHandler(
                GameEvents.TutorialStarted,
                OnTutorialStarted
            );

            EventManager.Instance.RemoveHandler(
                GameEvents.TutorialCompleted,
                OnTutorialCompleted
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
                    UpdateVisibility();
                    break;

                case Lander.State.Normal:
                    Show();
                    break;

                case Lander.State.GameOver:
                    HandleGameOver();
                    break;
            }
        }

        private void OnTutorialStarted()
        {
            _isTutorialActive = true;
            Show();
        }

        private void OnTutorialCompleted()
        {
            _isTutorialActive = false;
            UpdateVisibility();
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
            if (_energyFillImage == null) return;
            if (!Lander.HasInstance) return;

            _energyFillImage.fillAmount = Lander.Instance.GetEnergyNormalized();
        }

        private void UpdateLowFuelWarning()
        {
            if (!Lander.HasInstance) return;

            bool isLowFuel = Lander.Instance.GetEnergyNormalized() < LOW_ENERGY_THRESHOLD;

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

            if (_lowEnergyWarning != null)
            {
                _lowEnergyWarning.gameObject.SetActive(true);
            }

            StartBlinking();
        }

        private void HideLowFuelWarning()
        {
            _isLowFuelWarningActive = false;

            if (_lowEnergyWarning != null)
            {
                _lowEnergyWarning.gameObject.SetActive(false);
            }

            StopBlinking();
        }

        private void StartBlinking()
        {
            if (_lowEnergyBackgroundImage == null) return;
            if (_blinkSequence != null) return;

            _blinkSequence = DOTween.Sequence()
                .Append(_lowEnergyBackgroundImage.DOFade(BLINK_FADE_HIGH, BLINK_DURATION))
                .Append(_lowEnergyBackgroundImage.DOFade(BLINK_FADE_LOW, BLINK_DURATION))
                .SetLoops(-1, LoopType.Restart)
                .SetLink(gameObject);
        }

        private void StopBlinking()
        {
            if (_blinkSequence == null) return;

            _blinkSequence.Kill();
            _blinkSequence = null;

            if (_lowEnergyBackgroundImage != null)
            {
                _lowEnergyBackgroundImage.DOKill();
                var color = _lowEnergyBackgroundImage.color;
                color.a = BLINK_FADE_LOW;
                _lowEnergyBackgroundImage.color = color;
            }
        }

        #endregion

        #region Private Methods Ч Visibility

        private void UpdateVisibility()
        {
            // ≈сли туториал активен Ч показываем UI
            if (_isTutorialActive)
            {
                Show();
                return;
            }

            // ≈сли туториала нет и Lander не в Normal Ч скрываем
            if (!Lander.HasInstance || Lander.Instance.CurrentState != Lander.State.Normal)
            {
                Hide();
                return;
            }

            // Lander в Normal Ч показываем
            Show();
        }

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