using System.Collections.Generic;
using My.Scripts.Core.Data;
using My.Scripts.EventBus;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace My.Scripts.Environment.Hazards
{
    public class AcidPoolEffect : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Transition")]
        [Tooltip("—корость перехода эффекта")]
        [SerializeField] private float _transitionSpeed = 3f;

        [Header("Vignette")]
        [Tooltip("»нтенсивность виньетки в кислотном озере")]
        [SerializeField, Range(0f, 1f)] private float _vignetteIntensity = 0.5f;

        [Tooltip("÷вет виньетки")]
        [SerializeField] private Color _vignetteColor = new Color(0.2f, 0.9f, 0.1f, 1f);

        [Header("Color Adjustments")]
        [Tooltip("ќттенок цветового фильтра в кислотном озере")]
        [SerializeField] private Color _colorFilterTint = new Color(0.7f, 1f, 0.5f, 1f);

        [Header("Pulse Animation")]
        [Tooltip("—корость пульсации виньетки")]
        [SerializeField] private float _pulseSpeed = 3f;

        [Tooltip("јмплитуда пульсации (добавл€етс€ к основной интенсивности)")]
        [SerializeField, Range(0f, 0.3f)] private float _pulseAmount = 0.15f;

        #endregion

        #region Private Fields

        private Volume _acidVolume;
        private Vignette _vignette;
        private ColorAdjustments _colorAdjustments;

        private float _currentWeight;
        private float _targetWeight;
        private bool _isInAcid;
        private bool _isGameOver;

        private int _activeSourceCount;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            CreateVolume();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;

            EventManager.Instance?.AddHandler<LanderLandedData>(
                GameEvents.LanderLanded,
                OnLanderLanded
            );
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            if (EventManager.HasInstance)
            {
                EventManager.Instance.RemoveHandler<LanderLandedData>(
                    GameEvents.LanderLanded,
                    OnLanderLanded
                );
            }
        }

        private void OnDestroy()
        {
            if (_acidVolume != null)
            {
                Destroy(_acidVolume.gameObject);
            }
        }

        private void Update()
        {
            if (_isGameOver) return;

            UpdateWeight();
            ApplyEffects();
        }

        #endregion

        #region Public Methods

        public void RegisterSource()
        {
            if (_isGameOver) return;

            _activeSourceCount++;
            _targetWeight = 1f;
            _isInAcid = true;
        }

        public void UnregisterSource()
        {
            _activeSourceCount = Mathf.Max(0, _activeSourceCount - 1);

            if (_activeSourceCount == 0)
            {
                _targetWeight = 0f;
                _isInAcid = false;
            }
        }

        public void ResetEffect()
        {
            _activeSourceCount = 0;
            _targetWeight = 0f;
            _currentWeight = 0f;
            _isInAcid = false;
            _isGameOver = false;

            if (_acidVolume != null)
            {
                _acidVolume.weight = 0f;
            }

            if (_vignette != null)
            {
                _vignette.intensity.value = _vignetteIntensity;
            }
        }

        #endregion

        #region Private Methods Ч Event Handlers

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ResetEffect();
        }

        private void OnLanderLanded(LanderLandedData data)
        {
            _isGameOver = true;
            _isInAcid = false;
        }

        #endregion

        #region Private Methods Ч Initialization

        private void CreateVolume()
        {
            var volumeGo = new GameObject("AcidPoolVolume");
            volumeGo.transform.SetParent(transform);

            _acidVolume = volumeGo.AddComponent<Volume>();
            _acidVolume.isGlobal = true;
            _acidVolume.priority = 10;
            _acidVolume.weight = 0f;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _acidVolume.profile = profile;

            _vignette = profile.Add<Vignette>();
            _vignette.active = true;
            _vignette.intensity.overrideState = true;
            _vignette.intensity.value = _vignetteIntensity;
            _vignette.color.overrideState = true;
            _vignette.color.value = _vignetteColor;
            _vignette.smoothness.overrideState = true;
            _vignette.smoothness.value = 0.4f;

            _colorAdjustments = profile.Add<ColorAdjustments>();
            _colorAdjustments.active = true;
            _colorAdjustments.colorFilter.overrideState = true;
            _colorAdjustments.colorFilter.value = _colorFilterTint;
        }

        #endregion

        #region Private Methods Ч Effect Update

        private void UpdateWeight()
        {
            _currentWeight = Mathf.MoveTowards(
                _currentWeight,
                _targetWeight,
                _transitionSpeed * Time.deltaTime
            );
        }

        private void ApplyEffects()
        {
            if (_acidVolume == null) return;

            float pulse = 0f;
            if (_isInAcid && _currentWeight > 0.5f)
            {
                pulse = Mathf.Sin(Time.time * _pulseSpeed) * _pulseAmount;
            }

            float finalWeight = Mathf.Clamp01(_currentWeight + pulse);
            _acidVolume.weight = finalWeight;

            if (_vignette != null)
            {
                float vignetteWithPulse = _vignetteIntensity + pulse;
                _vignette.intensity.value = Mathf.Clamp01(vignetteWithPulse);
            }
        }

        #endregion
    }
}