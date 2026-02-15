using My.Scripts.Core.Data;
using My.Scripts.Environment.Light;
using My.Scripts.EventBus;
using My.Scripts.Gameplay.Player;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace My.Scripts.Managers
{
    /// <summary>
    /// ÷ентральный менеджер освещени€ дл€ пещер.
    ///  оординирует работу нескольких CaveZone и управл€ет Global Light.
    /// </summary>
    public class LightingManager : MonoBehaviour
    {
        #region Singleton

        private static LightingManager _instance;
        public static LightingManager Instance => _instance;
        public static bool HasInstance => _instance != null;

        #endregion

        #region Serialized Fields

        [Header("Ќастройки по умолчанию")]
        [Tooltip("»нтенсивность света вне всех пещер")]
        [SerializeField] private float _defaultOutsideIntensity = 1f;

        [Tooltip("—корость возврата освещени€ при выходе из всех пещер")]
        [SerializeField] private float _exitTransitionSpeed = 6f;

        [Header("Flashlight")]
        [Tooltip("»нтенсивность фонарика во включЄнном состо€нии")]
        [SerializeField] private float _flashlightActiveIntensity = 1f;

        #endregion

        #region Private Fields

        private Light2D _globalLight;
        private Light2D _flashlight;
        private Light2D _outerLight;
        private float _currentIntensity;
        private float _outerLightIntensity;
        private bool _isInitialized;
        private bool _isGameOver;
        private float _frozenIntensity;

        private readonly HashSet<CaveZone> _activeZones = new HashSet<CaveZone>();

        #endregion

        #region Properties

        public bool IsPlayerInAnyCave => _activeZones.Count > 0;
        public float CurrentIntensity => _currentIntensity;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[LightingManager] Duplicate instance destroyed");
                Destroy(gameObject);
                return;
            }

            _instance = this;
        }

        private void Start()
        {
            Initialize();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            if (!_isInitialized || _globalLight == null) return;

            if (_isGameOver)
            {
                _globalLight.intensity = _frozenIntensity;
                return;
            }

            UpdateLighting();
            ApplyLighting();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();

            if (_instance == this)
            {
                _instance = null;
            }
        }

        #endregion

        #region Event Subscription

        private void SubscribeToEvents()
        {
            EventManager.Instance?.AddHandler<LanderLandedData>(
                GameEvents.LanderLanded,
                OnLanderLanded
            );
        }

        private void UnsubscribeFromEvents()
        {
            EventManager.Instance?.RemoveHandler<LanderLandedData>(
                GameEvents.LanderLanded,
                OnLanderLanded
            );
        }

        private void OnLanderLanded(LanderLandedData data)
        {
            FreezeCurrentLighting();
        }

        private void FreezeCurrentLighting()
        {
            _isGameOver = true;
            _frozenIntensity = _currentIntensity;
            Debug.Log($"[LightingManager] Lighting frozen at intensity: {_frozenIntensity}");
        }

        #endregion

        #region Public Methods Ч Zone Registration

        public void RegisterZoneEntry(CaveZone zone)
        {
            if (zone == null) return;

            _activeZones.Add(zone);
            UpdateFlashlight();

            Debug.Log($"[LightingManager] Player entered {zone.name}. Active zones: {_activeZones.Count}");
        }

        public void RegisterZoneExit(CaveZone zone)
        {
            if (zone == null) return;

            _activeZones.Remove(zone);
            UpdateFlashlight();

            Debug.Log($"[LightingManager] Player exited {zone.name}. Active zones: {_activeZones.Count}");
        }

        public void UnregisterZone(CaveZone zone)
        {
            _activeZones.Remove(zone);
        }

        public void ResetManager()
        {
            _activeZones.Clear();
            _currentIntensity = _defaultOutsideIntensity;
            _isGameOver = false;
            _frozenIntensity = _defaultOutsideIntensity;

            ApplyLighting();
            SetFlashlightActive(false);

            Debug.Log("[LightingManager] Manager reset");
        }

        #endregion

        #region Private Methods Ч Initialization

        private void Initialize()
        {
            FindGlobalLight();
            FindFlashlight();

            _currentIntensity = _defaultOutsideIntensity;
            _isInitialized = true;

            if (_flashlight != null)
            {
                _flashlight.gameObject.SetActive(true);
                _flashlight.intensity = 0f;
            }

            // CHANGED: прогреваем OuterLight тоже
            if (_outerLight != null)
            {
                _outerLight.gameObject.SetActive(true);
                _outerLightIntensity = _outerLight.intensity; // запоминаем оригинальную
                _outerLight.intensity = 0f;
            }

            Debug.Log("[LightingManager] Initialized (flashlight shader warmed up)");
        }

        #endregion

        #region Private Methods Ч Lighting

        private void UpdateLighting()
        {
            if (_activeZones.Count == 0)
            {
                _currentIntensity = Mathf.MoveTowards(
                    _currentIntensity,
                    _defaultOutsideIntensity,
                    _exitTransitionSpeed * Time.deltaTime
                );
                return;
            }

            float targetIntensity = _defaultOutsideIntensity;

            foreach (CaveZone zone in _activeZones)
            {
                if (zone == null) continue;

                float zoneIntensity = zone.CalculateCurrentIntensity();
                targetIntensity = Mathf.Min(targetIntensity, zoneIntensity);
            }

            _currentIntensity = targetIntensity;
        }

        private void ApplyLighting()
        {
            if (_globalLight != null)
            {
                _globalLight.intensity = _currentIntensity;
            }
        }

        private void UpdateFlashlight()
        {
            bool shouldBeActive = _activeZones.Count > 0;
            SetFlashlightActive(shouldBeActive);
        }

        // CHANGED: управл€ем intensity вместо SetActive
        private void SetFlashlightActive(bool active)
        {
            if (_flashlight == null && Lander.HasInstance)
            {
                FindFlashlight();

                if (_flashlight != null)
                {
                    _flashlight.gameObject.SetActive(true);
                    _flashlight.intensity = 0f;
                }

                if (_outerLight != null)
                {
                    _outerLight.gameObject.SetActive(true);
                    _outerLight.intensity = 0f;
                }
            }

            if (_flashlight != null)
            {
                _flashlight.intensity = active ? _flashlightActiveIntensity : 0f;
            }

            if (_outerLight != null)
            {
                _outerLight.intensity = active ? _outerLightIntensity : 0f;
            }
        }

        private void FindGlobalLight()
        {
            Light2D[] allLights = FindObjectsByType<Light2D>(FindObjectsSortMode.None);

            foreach (Light2D light in allLights)
            {
                if (light.lightType == Light2D.LightType.Global)
                {
                    _globalLight = light;
                    Debug.Log($"[LightingManager] Found Global Light: {light.gameObject.name}");
                    return;
                }
            }

            Debug.LogError("[LightingManager] Global Light 2D не найден на сцене!");
        }

        private void FindFlashlight()
        {
            if (!Lander.HasInstance) return;

            Light2D[] landerLights = Lander.Instance.GetComponentsInChildren<Light2D>(true);

            foreach (Light2D light in landerLights)
            {
                if (light.lightType == Light2D.LightType.Freeform)
                {
                    _flashlight = light;
                    Debug.Log($"[LightingManager] Found Flashlight: {light.gameObject.name}");

                    // »щем OuterLight среди дочерних объектов фонарика
                    Light2D[] childLights = _flashlight.GetComponentsInChildren<Light2D>(true);
                    foreach (Light2D childLight in childLights)
                    {
                        if (childLight != _flashlight)
                        {
                            _outerLight = childLight;
                            _outerLightIntensity = childLight.intensity;
                            Debug.Log($"[LightingManager] Found OuterLight: {childLight.gameObject.name}");
                            break;
                        }
                    }

                    return;
                }
            }
        }

        #endregion
    }
}