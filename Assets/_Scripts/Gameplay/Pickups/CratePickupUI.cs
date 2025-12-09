using UnityEngine;
using UnityEngine.UI;

namespace My.Scripts.Gameplay.Pickups
{
    public class CratePickupUI : MonoBehaviour
    {
        #region Serialized Fields

        [Header("UI Elements")]
        [SerializeField] private Image _progressFillImage;

        [Header("References")]
        [SerializeField] private CratePickup _cratePickup;

        #endregion

        #region Private Fields

        private bool _hasValidReferences;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ValidateReferences();
        }

        private void Update()
        {
            if (_hasValidReferences)
            {
                UpdateProgressBar();
            }
        }

        #endregion

        #region Private Methods

        private void ValidateReferences()
        {
            _hasValidReferences = _progressFillImage != null && _cratePickup != null;

            if (!_hasValidReferences)
            {
                Debug.LogWarning($"[{nameof(CratePickupUI)}] Missing references!", this);
            }
        }

        private void UpdateProgressBar()
        {
            _progressFillImage.fillAmount = _cratePickup.PickupProgress;
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Автоматически находим CratePickup на родителе
            if (_cratePickup == null)
            {
                _cratePickup = GetComponentInParent<CratePickup>();
            }
        }
#endif

        #endregion
    }
}