using UnityEngine;

namespace My.Scripts.Gameplay.LandingPads
{
    /// <summary>
    /// «она приземлени€ дл€ €щиков. ƒелегирует обработку доставки родительскому CrateLandingPad.
    /// </summary>
    public class CrateLandingPadArea : MonoBehaviour
    {
        #region Serialized Fields

        [Header("References")]
        [SerializeField] private CrateLandingPad _landingPad;

        #endregion

        #region Private Fields

        private bool _hasValidReferences;

        #endregion

        #region Properties

        public CrateLandingPad LandingPad => _landingPad;
        public bool CanAcceptCrates => _hasValidReferences && _landingPad.CanAcceptCrates;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeReferences();
            ValidateReferences();
        }

        #endregion

        #region Public Methods

        public void RegisterCrateDelivery()
        {
            if (!_hasValidReferences)
            {
                Debug.LogWarning($"[{nameof(CrateLandingPadArea)}] Cannot register delivery Ч missing LandingPad!", this);
                return;
            }

            _landingPad.RegisterCrateDelivery();
        }

        #endregion

        #region Private Methods Ч Initialization

        private void InitializeReferences()
        {
            if (_landingPad == null)
            {
                _landingPad = GetComponentInParent<CrateLandingPad>();
            }
        }

        private void ValidateReferences()
        {
            _hasValidReferences = _landingPad != null;

            if (!_hasValidReferences)
            {
                Debug.LogError($"[{nameof(CrateLandingPadArea)}] Missing CrateLandingPad reference! " +
                              $"Ensure this object is a child of a CrateLandingPad.", this);
            }
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_landingPad == null)
            {
                _landingPad = GetComponentInParent<CrateLandingPad>();
            }
        }

        private void Reset()
        {
            _landingPad = GetComponentInParent<CrateLandingPad>();
        }
#endif

        #endregion
    }
}