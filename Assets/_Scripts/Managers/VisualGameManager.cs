using My.Scripts.Core.Data;
using My.Scripts.Core.Utility;
using My.Scripts.EventBus;
using My.Scripts.Gameplay.Player;
using My.Scripts.UI.Popups;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace My.Scripts.Managers
{
    public class VisualGameManager : PersistentSingleton<VisualGameManager>
    {
        #region Constants

        private const float LOW_ENERGY_THRESHOLD = 0.25f;
        private const float CHROMATIC_ABERRATION_MAX = 0.8f;
        private const float VFX_LIFETIME = 1.5f;

        #endregion

        #region Serialized Fields

        [Header("Post Processing")]
        [Tooltip("Основной Volume сцены с ChromaticAberration")]
        [SerializeField] private Volume _sceneVolume;

        [Header("Prefabs")]
        [SerializeField] private ScorePopup _scorePopupPrefab;
        [SerializeField] private Transform _pickupVfxPrefab;
        [SerializeField] private Transform _confettiVfxPrefab;

        [Header("Camera Effects")]
        [SerializeField] private CinemachineImpulseSource _pickupImpulseSource;
        [SerializeField] private CinemachineImpulseSource _crashImpulseSource;

        [Header("Settings")]
        [SerializeField] private float _pickupImpulsePower = 0.5f;
        [SerializeField] private float _crashImpulsePower = 50f;
        [SerializeField] private Vector3 _popupOffset = new(1.5f, 2f, 0f);
        [SerializeField] private Vector3 _energyBookPopupOffset = new(1f, 1.5f, 0f);
        [SerializeField] private Vector3 _popupRotation = new(0f, 0f, -20f);

        #endregion

        #region Private Fields

        private Volume _globalVolume;
        private ChromaticAberration _chromaticAberration;
        private Quaternion _popupQuaternion;
        private bool _isGameOver;

        #endregion

        #region Unity Lifecycle

        protected override void OnSingletonAwake()
        {
            _popupQuaternion = Quaternion.Euler(_popupRotation);

            // Подписываемся на загрузку сцены
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            // Инициализируем при первом запуске
            InitializeForCurrentScene();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            UpdateChromaticAberration();
        }

        #endregion

        #region Private Methods — Scene Management

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[VisualGameManager] Scene loaded: {scene.name}");

            // Сбрасываем состояние при загрузке новой сцены
            ResetState();

            // Переинициализируем ссылки для новой сцены
            InitializeForCurrentScene();
        }

        private void ResetState()
        {
            _isGameOver = false;
            _globalVolume = null;
            _chromaticAberration = null;

            Debug.Log("[VisualGameManager] State reset for new scene");
        }

        private void InitializeForCurrentScene()
        {
            FindGlobalVolume();
            InitializePostProcessing();
            FindCameraEffects();
        }

        private void FindGlobalVolume()
        {
            // Если назначен через инспектор — используем его
            if (_sceneVolume != null)
            {
                _globalVolume = _sceneVolume;
                Debug.Log($"[VisualGameManager] Using assigned Volume: {_globalVolume.gameObject.name}");
                return;
            }

            // Иначе ищем Volume с ChromaticAberration
            Volume[] allVolumes = FindObjectsByType<Volume>(FindObjectsSortMode.None);

            foreach (var volume in allVolumes)
            {
                if (volume.profile != null && volume.profile.TryGet<ChromaticAberration>(out _))
                {
                    _globalVolume = volume;
                    Debug.Log($"[VisualGameManager] Found Volume with ChromaticAberration: {volume.gameObject.name}");
                    return;
                }
            }

            // Fallback — берём первый попавшийся с профилем
            foreach (var volume in allVolumes)
            {
                if (volume.profile != null)
                {
                    _globalVolume = volume;
                    Debug.Log($"[VisualGameManager] Fallback Volume: {volume.gameObject.name}");
                    return;
                }
            }

            Debug.LogWarning("[VisualGameManager] No suitable Volume found on scene!");
        }

        private void FindCameraEffects()
        {
            // Ищем Impulse Sources если они не назначены или уничтожены
            if (_pickupImpulseSource == null)
            {
                var sources = FindObjectsByType<CinemachineImpulseSource>(FindObjectsSortMode.None);
                foreach (var source in sources)
                {
                    if (source.gameObject.name.ToLower().Contains("pickup"))
                    {
                        _pickupImpulseSource = source;
                        break;
                    }
                }
            }

            if (_crashImpulseSource == null)
            {
                var sources = FindObjectsByType<CinemachineImpulseSource>(FindObjectsSortMode.None);
                foreach (var source in sources)
                {
                    if (source.gameObject.name.ToLower().Contains("crash"))
                    {
                        _crashImpulseSource = source;
                        break;
                    }
                }
            }
        }

        #endregion

        #region Private Methods — Initialization

        private void InitializePostProcessing()
        {
            if (_globalVolume == null)
            {
                Debug.LogWarning("[VisualGameManager] Cannot initialize post processing - no Volume");
                return;
            }

            if (_globalVolume.profile == null)
            {
                Debug.LogError("[VisualGameManager] Volume has no profile!");
                return;
            }

            bool found = _globalVolume.profile.TryGet(out _chromaticAberration);

            if (!found)
            {
                Debug.LogWarning("[VisualGameManager] ChromaticAberration not found in Volume profile");
            }
            else
            {
                // Убеждаемся что intensity можно изменять
                _chromaticAberration.intensity.overrideState = true;
                Debug.Log("[VisualGameManager] ChromaticAberration initialized successfully");
            }
        }

        #endregion

        #region Private Methods — Event Subscription

        private void SubscribeToEvents()
        {
            var em = EventManager.Instance;
            if (em == null) return;

            em.AddHandler<PickupEventData>(GameEvents.CoinPickup, OnCoinPickup);
            em.AddHandler<PickupEventData>(GameEvents.EnergyBookPickup, OnEnergyBookPickup);
            em.AddHandler(GameEvents.KeyPickup, OnKeyPickup);
            em.AddHandler(GameEvents.CrateDrop, OnCrateDrop);
            em.AddHandler(GameEvents.CrateCracked, OnCrateCracked);
            em.AddHandler(GameEvents.RopeWithCrateSpawned, OnRopeWithCrateSpawned);
            em.AddHandler<LanderLandedData>(GameEvents.LanderLanded, OnLanderLanded);
            em.AddHandler<LanderStateData>(GameEvents.LanderStateChanged, OnLanderStateChanged);
        }

        private void UnsubscribeFromEvents()
        {
            var em = EventManager.Instance;
            if (em == null) return;

            em.RemoveHandler<PickupEventData>(GameEvents.CoinPickup, OnCoinPickup);
            em.RemoveHandler<PickupEventData>(GameEvents.EnergyBookPickup, OnEnergyBookPickup);
            em.RemoveHandler(GameEvents.KeyPickup, OnKeyPickup);
            em.RemoveHandler(GameEvents.CrateDrop, OnCrateDrop);
            em.RemoveHandler(GameEvents.CrateCracked, OnCrateCracked);
            em.RemoveHandler(GameEvents.RopeWithCrateSpawned, OnRopeWithCrateSpawned);
            em.RemoveHandler<LanderLandedData>(GameEvents.LanderLanded, OnLanderLanded);
            em.RemoveHandler<LanderStateData>(GameEvents.LanderStateChanged, OnLanderStateChanged);
        }

        #endregion

        #region Private Methods — Event Handlers

        private void OnCoinPickup(PickupEventData data)
        {
            SpawnScorePopup(data.Position, $"+{GameManager.SCORE_PER_COIN}");
            SpawnPickupVfx(data.Position);
            GeneratePickupImpulse();
        }

        private void OnEnergyBookPickup(PickupEventData data)
        {
            SpawnScorePopup(data.Position + _energyBookPopupOffset, "+ENERGY");
            GeneratePickupImpulse();
        }

        private void OnKeyPickup()
        {
            GeneratePickupImpulse();
        }

        private void OnCrateDrop()
        {
            if (!Lander.HasInstance) return;

            Vector3 popupPosition = Lander.Instance.transform.position + _popupOffset;

            SpawnScorePopup(
                popupPosition,
                $"+{GameManager.SCORE_PER_CRATE}",
                backgroundColor: Color.yellow,
                textColor: Color.black,
                isBold: true
            );
        }

        private void OnCrateCracked() => GeneratePickupImpulse();

        private void OnRopeWithCrateSpawned()
        {
        }

        private void OnLanderLanded(LanderLandedData data)
        {
            if (data.LandingType != Lander.LandingType.Success)
            {
                HandleCrashEffect();
            }
            else
            {
                HandleSuccessEffect();
            }
        }

        private void OnLanderStateChanged(LanderStateData data)
        {
            if (data.State == Lander.State.GameOver)
            {
                _isGameOver = true;
                Debug.Log("[VisualGameManager] Game Over state set");
            }
            else if (data.State == Lander.State.WaitingToStart || data.State == Lander.State.Normal)
            {
                // Сбрасываем при начале новой игры
                _isGameOver = false;
            }
        }

        #endregion

        #region Private Methods — Visual Effects

        private void SpawnScorePopup(
    Vector3 position,
    string text,
    Color? backgroundColor = null,
    Color? textColor = null,
    bool isBold = false)
        {
            if (_scorePopupPrefab == null) return;

            Vector3 popupPosition = position + _popupOffset;
            var popup = Instantiate(_scorePopupPrefab, popupPosition, _popupQuaternion);

            if (backgroundColor.HasValue && textColor.HasValue)
            {
                popup.Setup(text, backgroundColor.Value, textColor.Value, isBold);
            }
            else
            {
                popup.Setup(text);
            }
        }

        private void SpawnPickupVfx(Vector3 position)
        {
            if (_pickupVfxPrefab == null) return;

            var vfx = Instantiate(_pickupVfxPrefab, position, Quaternion.identity);
            Destroy(vfx.gameObject, VFX_LIFETIME);
        }

        private void GeneratePickupImpulse()
        {
            if (_pickupImpulseSource != null)
            {
                _pickupImpulseSource.GenerateImpulse(_pickupImpulsePower);
            }
        }

        private void HandleCrashEffect()
        {
            if (_crashImpulseSource != null)
            {
                _crashImpulseSource.GenerateImpulse(_crashImpulsePower);
            }
        }

        private void HandleSuccessEffect()
        {
            if (_confettiVfxPrefab == null || !Lander.HasInstance) return;

            Instantiate(
                _confettiVfxPrefab,
                Lander.Instance.transform.position,
                Quaternion.identity,
                Lander.Instance.transform
            );
        }

        #endregion

        #region Private Methods — Post Processing

        private void UpdateChromaticAberration()
        {
            // Проверяем валидность ссылок
            if (_chromaticAberration == null || _globalVolume == null)
            {
                return;
            }

            if (_isGameOver)
            {
                _chromaticAberration.intensity.value = 0f;
                return;
            }

            if (!Lander.HasInstance)
            {
                _chromaticAberration.intensity.value = 0f;
                return;
            }

            float energyNormalized = Lander.Instance.GetEnergyNormalized();
            bool isLowEnergy = energyNormalized < LOW_ENERGY_THRESHOLD;

            if (isLowEnergy)
            {
                _chromaticAberration.intensity.value = Mathf.PingPong(Time.time, CHROMATIC_ABERRATION_MAX);
            }
            else
            {
                _chromaticAberration.intensity.value = 0f;
            }
        }

        #endregion
    }
}