using UnityEngine;

namespace My.Scripts.Gameplay.Levels
{
    public class GameLevel : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Level Info")]
        [SerializeField] private int _levelNumber;

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

        #region Public Methods

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

        private void OnDrawGizmosSelected()
        {
            DrawLanderStartGizmo();
            DrawCameraTargetGizmo();
            DrawCameraViewGizmos();
        }

        private void DrawLanderStartGizmo()
        {
            if (_landerStartPosition == null) return;

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_landerStartPosition.position, 1f);
            Gizmos.DrawLine(
                _landerStartPosition.position,
                _landerStartPosition.position + Vector3.up * 2f
            );
        }

        private void DrawCameraTargetGizmo()
        {
            if (_cameraStartTarget == null) return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(_cameraStartTarget.position, Vector3.one * 2f);
        }

        private void DrawCameraViewGizmos()
        {
            if (_cameraStartTarget == null) return;

            Vector3 center = _cameraStartTarget.position;

            // Вычисляем aspect ratio (стандартный 16:9, но можно взять из Game view)
            float aspectRatio = GetEditorAspectRatio();

            // Рисуем область zoomed out камеры (внешний прямоугольник)
            DrawCameraRect(center, _zoomedOutOrthographicSize, aspectRatio, Color.yellow);

            // Рисуем область normal камеры (внутренний прямоугольник)
            DrawCameraRect(center, _normalOrthographicSize, aspectRatio, new Color(1f, 0.5f, 0f)); // Orange
        }

        private void DrawCameraRect(Vector3 center, float orthographicSize, float aspectRatio, Color color)
        {
            // Для ортографической камеры:
            // высота = orthographicSize * 2
            // ширина = высота * aspectRatio
            float height = orthographicSize * 2f;
            float width = height * aspectRatio;

            Vector3 size = new Vector3(width, height, 0f);

            // Рисуем прямоугольник
            Gizmos.color = color;
            Gizmos.DrawWireCube(center, size);

            // Рисуем диагонали для лучшей видимости
            Vector3 halfSize = size * 0.5f;
            Vector3 topLeft = center + new Vector3(-halfSize.x, halfSize.y, 0f);
            Vector3 topRight = center + new Vector3(halfSize.x, halfSize.y, 0f);
            Vector3 bottomLeft = center + new Vector3(-halfSize.x, -halfSize.y, 0f);
            Vector3 bottomRight = center + new Vector3(halfSize.x, -halfSize.y, 0f);

            // Крестик в центре
            float crossSize = orthographicSize * 0.1f;
            Gizmos.DrawLine(center - Vector3.right * crossSize, center + Vector3.right * crossSize);
            Gizmos.DrawLine(center - Vector3.up * crossSize, center + Vector3.up * crossSize);

            // Подпись (через Handles для текста)
            DrawGizmoLabel(topLeft + Vector3.up * 0.5f, $"Size: {orthographicSize}", color);
        }

        private float GetEditorAspectRatio()
        {
            // Пытаемся получить aspect ratio из Game view
            // Если не получается — используем стандартный 16:9

#if UNITY_EDITOR
            // Получаем размер Game view через рефлексию (опционально)
            try
            {
                var gameViewType = System.Type.GetType("UnityEditor.GameView,UnityEditor");
                if (gameViewType != null)
                {
                    var getMainGameView = gameViewType.GetMethod(
                        "GetMainGameView",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static
                    );

                    if (getMainGameView != null)
                    {
                        var gameView = getMainGameView.Invoke(null, null) as UnityEditor.EditorWindow;
                        if (gameView != null)
                        {
                            var position = gameView.position;
                            return position.width / position.height;
                        }
                    }
                }
            }
            catch
            {
                // Игнорируем ошибки рефлексии
            }
#endif

            // Fallback: стандартный 16:9
            return 16f / 9f;
        }

        private void DrawGizmoLabel(Vector3 position, string text, Color color)
        {
#if UNITY_EDITOR
            var style = new GUIStyle();
            style.normal.textColor = color;
            style.fontSize = 12;
            style.fontStyle = FontStyle.Bold;

            UnityEditor.Handles.Label(position, text, style);
#endif
        }
#endif

        #endregion
    }
}