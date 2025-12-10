using TMPro;
using UnityEngine;

namespace My.Scripts.Gameplay.LandingPads
{
    /// <summary>
    /// Визуальное отображение множителя очков на посадочной площадке.
    /// </summary>
    public class LandingPadVisual : MonoBehaviour
    {
        #region Constants

        private const string MULTIPLIER_FORMAT = "x{0}";

        #endregion

        #region Serialized Fields

        [Header("References")]
        [SerializeField] private TextMeshPro _scoreMultiplierText;
        [SerializeField] private LandingPad _landingPad;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            CacheReferences();
            UpdateVisual();
        }

        #endregion

        #region Private Methods

        private void CacheReferences()
        {
            if (_landingPad == null)
            {
                _landingPad = GetComponent<LandingPad>();
            }

            if (_landingPad == null)
            {
                Debug.LogError($"[{nameof(LandingPadVisual)}] LandingPad component not found!", this);
            }
        }

        private void UpdateVisual()
        {
            if (_scoreMultiplierText == null || _landingPad == null) return;

            _scoreMultiplierText.text = string.Format(MULTIPLIER_FORMAT, _landingPad.ScoreMultiplier);
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_landingPad == null)
            {
                _landingPad = GetComponent<LandingPad>();
            }

            if (_scoreMultiplierText == null)
            {
                _scoreMultiplierText = GetComponentInChildren<TextMeshPro>();
            }
        }

        private void Reset()
        {
            _landingPad = GetComponent<LandingPad>();
            _scoreMultiplierText = GetComponentInChildren<TextMeshPro>();
        }
#endif

        #endregion
    }
}