using UnityEngine;
using My.Scripts.Managers;

namespace My.Scripts.Gameplay.Levels
{
    public class GameLevel : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Level Info")]
        [SerializeField] private int _levelNumber;

        [Header("Tutorial")]
        [Tooltip("Запускать ли туториал при старте уровня")]
        [SerializeField] private bool _hasTutorial = false;

        [Header("Spawn Points")]
        [SerializeField] private Transform _landerStartPosition;
        [SerializeField] private Transform _cameraStartTarget;

        [Header("Camera Settings")]
        [SerializeField] private float _normalOrthographicSize = 12f;
        [SerializeField] private float _zoomedOutOrthographicSize = 18f;

        [Header("Star Thresholds")]
        [Tooltip("Points required to receive 1, 2, or 3 stars")]
        [SerializeField] private int[] _starThresholds = new int[3] { 100, 200, 300 };

        #endregion

        #region Properties

        public int LevelNumber => _levelNumber;
        public float NormalOrthographicSize => _normalOrthographicSize;
        public float ZoomedOutOrthographicSize => _zoomedOutOrthographicSize;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            if (_hasTutorial)
            {
                TryStartTutorial();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Пытается запустить туториал для этого уровня
        /// </summary>
        public void TryStartTutorial()
        {
            if (TutorialManager.HasInstance)
            {
                TutorialManager.Instance.TryStartTutorialForLevel(_levelNumber);
            }
            else
            {
                Debug.LogWarning($"[GameLevel] TutorialManager not found!");
            }
        }

        public int GetLevelNumber() => _levelNumber;

        public Vector3 GetLanderStartPosition()
        {
            if (_landerStartPosition == null)
            {
                Debug.LogError($"[GameLevel] Level {_levelNumber}: Lander start position not assigned!");
                return Vector3.zero;
            }
            return _landerStartPosition.position;
        }

        public Transform GetCameraStartTargetTransform()
        {
            if (_cameraStartTarget == null)
            {
                Debug.LogError($"[GameLevel] Level {_levelNumber}: Camera start target not assigned!");
                return transform;
            }
            return _cameraStartTarget;
        }

        public float GetNormalOrthographicSize() => _normalOrthographicSize;
        public float GetZoomedOutOrthographicSize() => _zoomedOutOrthographicSize;
        public int[] GetStarThresholds() => _starThresholds;

        public int GetEarnedStarsCount(int score)
        {
            int stars = 0;
            for (int i = 0; i < _starThresholds.Length; i++)
            {
                if (score >= _starThresholds[i])
                {
                    stars++;
                }
            }
            return stars;
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        private void OnValidate()
        {
            for (int i = 1; i < _starThresholds.Length; i++)
            {
                if (_starThresholds[i] < _starThresholds[i - 1])
                {
                    Debug.LogWarning(
                        $"[GameLevel] Level {_levelNumber}: Star thresholds should be in ascending order!",
                        this
                    );
                    break;
                }
            }

            if (_levelNumber <= 0)
            {
                Debug.LogWarning($"[GameLevel] Level number should be positive!", this);
            }
        }

        // ... остальные методы гизмо без изменений ...
#endif

        #endregion
    }
}