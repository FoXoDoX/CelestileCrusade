using System.Collections.Generic;
using My.Scripts.Core.Data;
using My.Scripts.EventBus;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace My.Scripts.Environment.Hazards
{
    public class HotZoneEffect : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Transition")]
        [SerializeField] private float _transitionSpeed = 3f;

        [Header("Color")]
        [Tooltip("Цвет, в который окрашивается всё изображение")]
        [SerializeField] private Color _color = new Color(1f, 0.5f, 0.2f, 1f);

        #endregion

        #region Private Fields

        private Volume _volume;
        private ColorAdjustments _colorAdjustments;

        private float _currentWeight;
        private float _targetWeight;
        private bool _isGameOver;

        // Белый = нет эффекта, потому что colorFilter умножает
        private static readonly Color NoEffect = Color.white;

        private readonly HashSet<HotZone> _activeZones = new();

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
                GameEvents.LanderLanded, OnLanderLanded);
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (EventManager.HasInstance)
                EventManager.Instance.RemoveHandler<LanderLandedData>(
                    GameEvents.LanderLanded, OnLanderLanded);
        }

        private void OnDestroy()
        {
            if (_volume != null)
                Destroy(_volume.gameObject);
        }

        private void Update()
        {
            if (_isGameOver) return;

            _currentWeight = Mathf.MoveTowards(
                _currentWeight, _targetWeight,
                _transitionSpeed * Time.deltaTime);

            if (_colorAdjustments != null)
            {
                // Lerp от белого (нет эффекта) к выбранному цвету
                _colorAdjustments.colorFilter.value =
                    Color.Lerp(NoEffect, _color, _currentWeight);
            }
        }

        #endregion

        #region Public Methods

        public void RegisterZone(HotZone zone)
        {
            if (_isGameOver) return;
            _activeZones.Add(zone);
            _targetWeight = 1f;
        }

        public void UnregisterZone(HotZone zone)
        {
            _activeZones.Remove(zone);
            if (_activeZones.Count == 0)
                _targetWeight = 0f;
        }

        public void ResetEffect()
        {
            _activeZones.Clear();
            _targetWeight = 0f;
            _currentWeight = 0f;
            _isGameOver = false;

            if (_colorAdjustments != null)
                _colorAdjustments.colorFilter.value = NoEffect;
        }

        #endregion

        #region Private Methods

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ResetEffect();

        private void OnLanderLanded(LanderLandedData data)
        {
            _isGameOver = true;
        }

        private void CreateVolume()
        {
            var go = new GameObject("HotZoneVolume");
            go.transform.SetParent(transform);

            _volume = go.AddComponent<Volume>();
            _volume.isGlobal = true;
            _volume.priority = 10;
            _volume.weight = 1f; // Всегда активен, управляем через colorFilter

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _volume.profile = profile;

            _colorAdjustments = profile.Add<ColorAdjustments>();
            _colorAdjustments.active = true;
            _colorAdjustments.colorFilter.overrideState = true;
            _colorAdjustments.colorFilter.value = NoEffect;
        }

        #endregion
    }
}