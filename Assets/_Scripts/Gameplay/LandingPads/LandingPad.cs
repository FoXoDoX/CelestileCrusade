using UnityEngine;

namespace My.Scripts.Gameplay.LandingPads
{
    /// <summary>
    /// ѕосадочна€ площадка с множителем очков.
    /// </summary>
    public class LandingPad : MonoBehaviour
    {
        #region Constants

        private const int DEFAULT_SCORE_MULTIPLIER = 1;

        #endregion

        #region Serialized Fields

        [Header("Settings")]
        [SerializeField]
        [Min(1)]
        [Tooltip("ћножитель очков за посадку на эту площадку")]
        private int _scoreMultiplier = DEFAULT_SCORE_MULTIPLIER;

        #endregion

        #region Properties

        public int ScoreMultiplier => _scoreMultiplier;

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_scoreMultiplier < 1)
            {
                _scoreMultiplier = DEFAULT_SCORE_MULTIPLIER;
                Debug.LogWarning($"[{nameof(LandingPad)}] Score multiplier must be at least 1!", this);
            }
        }
#endif

        #endregion
    }
}