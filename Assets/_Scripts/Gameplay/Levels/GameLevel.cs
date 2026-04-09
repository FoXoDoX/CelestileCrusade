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

        [Header("Crest Thresholds")]
        [Tooltip("Points required to receive 1, 2, or 3 crests")]
        [SerializeField] private int[] _crestThresholds = new int[3] { 100, 200, 300 };

        [Header("Terrain Generation")]
        [Tooltip("Сид для генерации бесконечного ландшафта. 0 = случайный при каждом запуске.")]
        [SerializeField] private int _terrainSeed = 1;

        #endregion

        #region Properties

        public int LevelNumber => _levelNumber;
        public float NormalOrthographicSize => _normalOrthographicSize;
        public float ZoomedOutOrthographicSize => _zoomedOutOrthographicSize;
        public int GetTerrainSeed() => _terrainSeed;

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
        public int[] GetCrestThresholds() => _crestThresholds;

        public int GetEarnedCrestsCount(int score)
        {
            int crests = 0;
            for (int i = 0; i < _crestThresholds.Length; i++)
            {
                if (score >= _crestThresholds[i])
                {
                    crests++;
                }
            }
            return crests;
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        private void OnValidate()
        {
            for (int i = 1; i < _crestThresholds.Length; i++)
            {
                if (_crestThresholds[i] < _crestThresholds[i - 1])
                {
                    Debug.LogWarning(
                        $"[GameLevel] Level {_levelNumber}: Crest thresholds should be in ascending order!",
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