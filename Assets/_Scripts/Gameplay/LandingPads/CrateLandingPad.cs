using My.Scripts.EventBus;
using UnityEngine;

namespace My.Scripts.Gameplay.LandingPads
{
    public class CrateLandingPad : MonoBehaviour
    {
        #region Constants

        private const int MAX_CRATES = 5;
        private const string CRATE_NAME_PREFIX = "[CrateLandingPad] Crate";

        #endregion

        #region Serialized Fields

        [Header("References")]
        [SerializeField] private Transform _landedCratesContainer;
        [SerializeField] private CrateLandingPadArea _crateLandingArea;
        [SerializeField] private GameObject _background;

        [Header("Settings")]
        [SerializeField] private int _maxCrates = MAX_CRATES;

        #endregion

        #region Private Fields

        private int _deliveredCratesCount;
        private float _currentProgress;
        private Collider2D _landingAreaCollider;

        #endregion

        #region Properties

        public float CurrentProgress => _currentProgress;
        public bool CanAcceptCrates => _deliveredCratesCount < _maxCrates;
        public int DeliveredCratesCount => _deliveredCratesCount;
        public int MaxCrates => _maxCrates;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            CacheLandingAreaCollider();
        }

        private void Start()
        {
            HideAllCrates();
        }

        #endregion

        #region Public Methods Ч Delivery

        public void RegisterCrateDelivery()
        {
            if (!CanAcceptCrates) return;

            if (TryShowNextCrate())
            {
                _deliveredCratesCount++;
                BroadcastCrateDelivered();

                Debug.Log($"[CrateLandingPad] Crate delivered! Total: {_deliveredCratesCount}/{_maxCrates}");

                if (!CanAcceptCrates)
                {
                    OnAllCratesDelivered();
                }
            }
        }

        #endregion

        #region Public Methods Ч Progress

        public void UpdateDeliveryProgress(float progress)
        {
            _currentProgress = Mathf.Clamp01(progress);
        }

        public void ResetDeliveryProgress()
        {
            _currentProgress = 0f;
        }

        #endregion

        #region Private Methods Ч Initialization

        private void CacheLandingAreaCollider()
        {
            if (_crateLandingArea != null)
            {
                _landingAreaCollider = _crateLandingArea.GetComponent<Collider2D>();
            }
        }

        private void HideAllCrates()
        {
            if (_landedCratesContainer == null) return;

            foreach (Transform child in _landedCratesContainer)
            {
                child.gameObject.SetActive(false);
            }
        }

        #endregion

        #region Private Methods Ч Crate Management

        private bool TryShowNextCrate()
        {
            if (_landedCratesContainer == null) return false;

            int crateIndex = _deliveredCratesCount; // 0, 1, 2, 3, 4...

            if (crateIndex >= _landedCratesContainer.childCount)
            {
                Debug.LogWarning($"[CrateLandingPad] No crate at index {crateIndex}!", this);
                return false;
            }

            Transform crate = _landedCratesContainer.GetChild(crateIndex);
            crate.gameObject.SetActive(true);

            Debug.Log($"[CrateLandingPad] Activated crate: '{crate.name}'");
            return true;
        }

        private string GetCrateName(int crateNumber)
        {
            return $"{CRATE_NAME_PREFIX}{crateNumber}";
        }

        #endregion

        #region Private Methods Ч Completion

        private void OnAllCratesDelivered()
        {
            DisableLandingArea();
            HideBackground();
            BroadcastAllCratesDelivered();

            Debug.Log("[CrateLandingPad] All crates delivered! Landing area disabled.");
        }

        private void DisableLandingArea()
        {
            if (_landingAreaCollider != null)
            {
                _landingAreaCollider.enabled = false;
            }
        }

        private void HideBackground()
        {
            if (_background != null)
            {
                _background.SetActive(false);
            }
        }

        #endregion

        #region Private Methods Ч Events

        private void BroadcastCrateDelivered()
        {
            EventManager.Instance?.Broadcast(GameEvents.CrateDrop);
        }

        private void BroadcastAllCratesDelivered()
        {
            // ћожно добавить отдельное событие, если нужно
            // EventManager.Instance?.Broadcast(GameEvents.AllCratesDelivered);
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        private void OnValidate()
        {
            _maxCrates = Mathf.Max(1, _maxCrates);

            // јвтоматически находим CrateLandingArea в дочерних объектах
            if (_crateLandingArea == null)
            {
                _crateLandingArea = GetComponentInChildren<CrateLandingPadArea>();
            }
        }
#endif

        #endregion
    }
}