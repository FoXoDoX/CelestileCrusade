using UnityEngine;
using UnityEngine.UI;

namespace My.Scripts.Gameplay.Player
{
    public class EnergyIndicator : MonoBehaviour
    {
        #region Serialized Fields

        [Header("References")]
        [SerializeField] private Image _barImage;

        [Header("Smooth")]
        [SerializeField] private float _smoothSpeed = 5f;

        #endregion

        #region Private Fields

        private float _currentFillAmount = 1f;

        #endregion

        #region Unity Lifecycle

        private void LateUpdate()
        {
            if (_barImage == null) return;
            if (!Lander.HasInstance) return;

            float targetFill = Lander.Instance.GetEnergyNormalized();

            _currentFillAmount = Mathf.MoveTowards(
                _currentFillAmount,
                targetFill,
                _smoothSpeed * Time.deltaTime
            );

            _barImage.fillAmount = _currentFillAmount;
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        private void OnValidate()
        {
            _smoothSpeed = Mathf.Max(0.1f, _smoothSpeed);
        }
#endif

        #endregion
    }
}