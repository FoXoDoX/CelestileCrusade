using System.Collections;
using DG.Tweening;
using My.Scripts.Core.Utility;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace My.Scripts.Core.Scene
{
    public enum TransitionDirection
    {
        Right,
        Left
    }

    public enum TransitionType
    {
        Strips,
        Circle
    }

    public class SceneTransitionManager : PersistentSingleton<SceneTransitionManager>
    {
        #region Constants

        private const int CANVAS_SORT_ORDER = 999;
        private const float REFERENCE_WIDTH = 1920f;
        private const float REFERENCE_HEIGHT = 1080f;
        private const string PROGRESS_PROPERTY = "_Progress";

        #endregion

        #region Serialized Fields

        [Header("Strips")]
        [SerializeField] private int _stripCount = 6;
        [SerializeField] private Color _stripColor = Color.black;

        [Header("Strips — Cover")]
        [SerializeField] private float _coverDuration = 0.35f;
        [SerializeField] private float _coverStagger = 0.04f;
        [SerializeField] private Ease _coverEase = Ease.InCubic;

        [Header("Strips — Reveal")]
        [SerializeField] private float _revealDuration = 0.35f;
        [SerializeField] private float _revealStagger = 0.04f;
        [SerializeField] private Ease _revealEase = Ease.OutCubic;

        [Header("Strips — Hold")]
        [SerializeField] private float _stripsHoldDuration = 0.1f;

        [Header("Circle")]
        [SerializeField] private Color _circleColor = Color.black;
        [SerializeField] private Shader _circleShader;

        [Header("Circle — Close")]
        [SerializeField] private float _circleCloseDuration = 0.6f;
        [SerializeField] private Ease _circleCloseEase = Ease.InCubic;

        [Header("Circle — Open")]
        [SerializeField] private float _circleOpenDuration = 0.6f;
        [SerializeField] private Ease _circleOpenEase = Ease.OutCubic;

        [Header("Circle — Hold")]
        [SerializeField] private float _circleHoldDuration = 0.15f;

        #endregion

        #region Private Fields

        private Canvas _canvas;
        private RectTransform _canvasRect;
        private RectTransform[] _strips;
        private Image _circleImage;
        private Material _circleMaterial;
        private Sequence _sequence;
        private Tween _circleTween;
        private bool _isTransitioning;

        #endregion

        #region Properties

        public bool IsTransitioning => _isTransitioning;

        #endregion

        #region Unity Lifecycle

        protected override void OnSingletonAwake()
        {
            CreateCanvas();
            CreateInputBlocker();
            CreateStrips();
            CreateCircleOverlay();
            _canvas.enabled = false;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            KillSequence();
            KillCircleTween();

            if (_circleMaterial != null)
            {
                Destroy(_circleMaterial);
            }
        }

        #endregion

        #region Public Methods

        public void TransitionToScene(string sceneName, TransitionType type,
            TransitionDirection direction = TransitionDirection.Right)
        {
            if (_isTransitioning) return;

            switch (type)
            {
                case TransitionType.Strips:
                    StartCoroutine(StripsTransitionCoroutine(sceneName, direction));
                    break;

                case TransitionType.Circle:
                    StartCoroutine(CircleTransitionCoroutine(sceneName));
                    break;
            }
        }

        #endregion

        #region Private Methods — Setup

        private void CreateCanvas()
        {
            var canvasGO = new GameObject("TransitionCanvas");
            canvasGO.transform.SetParent(transform);

            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = CANVAS_SORT_ORDER;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(REFERENCE_WIDTH, REFERENCE_HEIGHT);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            _canvasRect = canvasGO.GetComponent<RectTransform>();
        }

        private void CreateInputBlocker()
        {
            var go = new GameObject("InputBlocker");
            go.transform.SetParent(_canvas.transform, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f);
            img.raycastTarget = true;
        }

        private void CreateStrips()
        {
            _strips = new RectTransform[_stripCount];
            float stripHeight = 1f / _stripCount;

            for (int i = 0; i < _stripCount; i++)
            {
                var go = new GameObject($"Strip_{i}");
                go.transform.SetParent(_canvas.transform, false);

                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, i * stripHeight);
                rt.anchorMax = new Vector2(1f, (i + 1) * stripHeight);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                var img = go.AddComponent<Image>();
                img.color = _stripColor;
                img.raycastTarget = false;

                _strips[i] = rt;
            }
        }

        private void CreateCircleOverlay()
        {
            if (_circleShader == null)
            {
                _circleShader = Shader.Find("UI/CircleTransition");
            }

            if (_circleShader == null)
            {
                Debug.LogError("[SceneTransitionManager] CircleTransition shader not found!");
                return;
            }

            _circleMaterial = new Material(_circleShader);
            _circleMaterial.SetColor("_Color", _circleColor);
            _circleMaterial.SetFloat(PROGRESS_PROPERTY, 0f);

            var go = new GameObject("CircleOverlay");
            go.transform.SetParent(_canvas.transform, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            _circleImage = go.AddComponent<Image>();
            _circleImage.material = _circleMaterial;
            _circleImage.color = Color.white;
            _circleImage.raycastTarget = false;

            go.SetActive(false);
        }

        #endregion

        #region Private Methods — Strips Transition

        private IEnumerator StripsTransitionCoroutine(string sceneName, TransitionDirection direction)
        {
            _isTransitioning = true;
            _canvas.enabled = true;
            SetStripsActive(true);
            SetCircleActive(false);

            float canvasWidth = _canvasRect.rect.width;
            if (canvasWidth <= 0f) canvasWidth = REFERENCE_WIDTH;

            float enterX = direction == TransitionDirection.Right ? canvasWidth : -canvasWidth;
            float exitX = direction == TransitionDirection.Right ? -canvasWidth : canvasWidth;

            yield return AnimateStrips(enterX, 0f, _coverDuration, _coverStagger, _coverEase);

            var asyncOp = SceneManager.LoadSceneAsync(sceneName);
            if (asyncOp != null)
            {
                while (!asyncOp.isDone) yield return null;
            }

            if (_stripsHoldDuration > 0f)
                yield return new WaitForSecondsRealtime(_stripsHoldDuration);

            yield return AnimateStrips(0f, exitX, _revealDuration, _revealStagger, _revealEase);

            SetStripsActive(false);
            _canvas.enabled = false;
            _isTransitioning = false;
        }

        private IEnumerator AnimateStrips(float fromX, float toX,
            float duration, float stagger, Ease ease)
        {
            if (_strips == null || _strips.Length == 0) yield break;

            KillSequence();
            _sequence = DOTween.Sequence().SetUpdate(true);

            for (int i = 0; i < _strips.Length; i++)
            {
                _strips[i].anchoredPosition = new Vector2(fromX, 0f);

                _sequence.Insert(
                    i * stagger,
                    _strips[i].DOAnchorPosX(toX, duration).SetEase(ease)
                );
            }

            yield return _sequence.WaitForCompletion();
        }

        private void SetStripsActive(bool active)
        {
            if (_strips == null) return;
            foreach (var strip in _strips)
            {
                if (strip != null)
                    strip.gameObject.SetActive(active);
            }
        }

        #endregion

        #region Private Methods — Circle Transition

        private IEnumerator CircleTransitionCoroutine(string sceneName)
        {
            _isTransitioning = true;
            _canvas.enabled = true;
            SetStripsActive(false);
            SetCircleActive(true);

            // Close: 0 → 1
            yield return AnimateCircle(0f, 1f, _circleCloseDuration, _circleCloseEase);

            var asyncOp = SceneManager.LoadSceneAsync(sceneName);
            if (asyncOp != null)
            {
                while (!asyncOp.isDone) yield return null;
            }

            if (_circleHoldDuration > 0f)
                yield return new WaitForSecondsRealtime(_circleHoldDuration);

            // Open: 1 → 0
            yield return AnimateCircle(1f, 0f, _circleOpenDuration, _circleOpenEase);

            SetCircleActive(false);
            _canvas.enabled = false;
            _isTransitioning = false;
        }

        private IEnumerator AnimateCircle(float from, float to, float duration, Ease ease)
        {
            if (_circleMaterial == null) yield break;

            KillCircleTween();
            _circleMaterial.SetFloat(PROGRESS_PROPERTY, from);

            _circleTween = DOTween.To(
                    () => _circleMaterial.GetFloat(PROGRESS_PROPERTY),
                    x => _circleMaterial.SetFloat(PROGRESS_PROPERTY, x),
                    to,
                    duration)
                .SetEase(ease)
                .SetUpdate(true);

            yield return _circleTween.WaitForCompletion();
        }

        private void SetCircleActive(bool active)
        {
            if (_circleImage != null)
                _circleImage.gameObject.SetActive(active);
        }

        #endregion

        #region Private Methods — Cleanup

        private void KillSequence()
        {
            _sequence?.Kill();
            _sequence = null;
        }

        private void KillCircleTween()
        {
            _circleTween?.Kill();
            _circleTween = null;
        }

        #endregion
    }
}