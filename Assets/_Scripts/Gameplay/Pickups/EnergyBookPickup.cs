using DG.Tweening;
using My.Scripts.Core.Data;
using My.Scripts.EventBus;
using My.Scripts.Gameplay.Player;
using System.Collections;
using UnityEngine;

namespace My.Scripts.Gameplay.Pickups
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(Collider2D))]
    public class EnergyBookPickup : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Energy")]
        [SerializeField] private float _energyAmount = 15f;

        [Header("Initial Effect")]
        [Tooltip("Префаб ParticleSystem, который появляется сразу при подборе")]
        [SerializeField] private ParticleSystem _pickupParticlePrefab;

        [Tooltip("Задержка (сек) перед началом анимации книги (время на ParticleSystem)")]
        [SerializeField] private float _initialDelay = 0.15f;

        [Header("Timing")]
        [Tooltip("Задержка (сек) от начала анимации книги до появления луча")]
        [SerializeField] private float _beamDelay = 0.7f;

        [Header("Beam — Points")]
        [Tooltip("Пустой дочерний объект — откуда начинается луч")]
        [SerializeField] private Transform _beamOriginPoint;

        [Header("Beam — Visuals")]
        [SerializeField] private float _beamDuration = 0.5f;
        [SerializeField] private Color _beamColor = new Color(0.3f, 0.6f, 1f, 1f);
        [SerializeField] private float _beamStartWidth = 0.15f;
        [SerializeField] private float _beamEndWidth = 0.05f;
        [SerializeField] private int _beamSortingOrder = 10;

        [Header("Beam — Animation")]
        [SerializeField] private float _beamPulseSpeed = 15f;
        [SerializeField] private float _beamPulseAmount = 0.3f;
        [SerializeField] private float _beamFadeInRatio = 0.1f;
        [SerializeField] private float _beamFadeOutRatio = 0.4f;

        [Header("Beam — Material (опционально)")]
        [Tooltip("Если не назначен — создастся Sprites/Default")]
        [SerializeField] private Material _beamMaterial;

        [Header("Disappear Animation")]
        [Tooltip("Общая длительность анимации исчезновения")]
        [SerializeField] private float _disappearDuration = 0.2f;

        [Tooltip("Во сколько раз книга растянется по Y перед сворачиванием")]
        [SerializeField] private float _disappearStretchY = 1.4f;

        [Tooltip("Какую долю времени занимает фаза растяжения (0-1)")]
        [SerializeField, Range(0f, 1f)] private float _stretchRatio = 0.35f;

        #endregion

        #region Private Fields

        private Animator _animator;
        private Collider2D _collider;
        private PickupAnimation _pickupAnimation;
        private bool _isPickedUp;
        private LineRenderer _lineRenderer;
        private Sequence _disappearSequence;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _collider = GetComponent<Collider2D>();
            _pickupAnimation = GetComponent<PickupAnimation>();
            _animator.enabled = false;
        }

        private void OnDestroy()
        {
            _disappearSequence?.Kill();
        }

        #endregion

        #region Public Methods

        public void Pickup()
        {
            if (_isPickedUp) return;
            _isPickedUp = true;

            _collider.enabled = false;

            // ── Останавливаем idle-анимацию и убираем компонент ──
            if (_pickupAnimation != null)
            {
                _pickupAnimation.Stop();
                Destroy(_pickupAnimation);
                _pickupAnimation = null;
            }

            EventManager.Instance?.Broadcast(
                GameEvents.EnergyBookPickup,
                new PickupEventData(transform.position)
            );

            StartCoroutine(PickupSequence());
        }

        #endregion

        #region Private Methods — Sequence

        private IEnumerator PickupSequence()
        {
            // ── 1. ParticleSystem — сразу при подборе ──
            SpawnPickupParticle();

            // ── 2. Начальная задержка ──
            if (_initialDelay > 0f)
            {
                yield return new WaitForSeconds(_initialDelay);
            }

            // ── 3. Анимация книги ──
            _animator.enabled = true;

            // ── 4. Ждём пока анимация доиграет ──
            yield return new WaitForSeconds(_beamDelay);

            // ── 5. Останавливаем аниматор ──
            _animator.enabled = false;

            // ── 6. Луч + постепенное начисление энергии ──
            if (Lander.HasInstance)
            {
                yield return StartCoroutine(PlayBeamEffect());
            }

            // ── 7. Луч закончился, книга остаётся видимой ──
            // ── 8. Анимация исчезновения через DOTween ──
            yield return PlayDisappearAnimation();

            // ── 9. Уничтожаем объект ──
            Destroy(gameObject);
        }

        #endregion

        #region Private Methods — Particle

        private void SpawnPickupParticle()
        {
            if (_pickupParticlePrefab == null) return;

            ParticleSystem particle = Instantiate(
                _pickupParticlePrefab,
                transform.position,
                Quaternion.identity
            );

            float lifetime = particle.main.duration + particle.main.startLifetime.constantMax;
            Destroy(particle.gameObject, lifetime);

            // ── Звук частиц — одновременно с ParticleSystem ──
            EventManager.Instance?.Broadcast(
                GameEvents.EnergyBookParticle,
                new PickupEventData(transform.position)
            );
        }

        #endregion

        #region Private Methods — Disappear Animation

        private YieldInstruction PlayDisappearAnimation()
        {
            _disappearSequence?.Kill();

            float stretchTime = _disappearDuration * _stretchRatio;
            float collapseTime = _disappearDuration * (1f - _stretchRatio);

            Vector3 originalScale = transform.localScale;

            // Целевой масштаб при растяжении: шире по Y, чуть сжатие по X
            Vector3 stretchedScale = new Vector3(
                originalScale.x * 0.85f,
                originalScale.y * _disappearStretchY,
                originalScale.z
            );

            _disappearSequence = DOTween.Sequence();

            // Фаза 1: растяжение вверх
            _disappearSequence.Append(
                transform.DOScale(stretchedScale, stretchTime)
                    .SetEase(Ease.OutQuad)
            );

            // Фаза 2: сворачивание в ничто
            _disappearSequence.Append(
                transform.DOScale(Vector3.zero, collapseTime)
                    .SetEase(Ease.InBack)
            );

            return _disappearSequence.WaitForCompletion();
        }

        #endregion

        #region Private Methods — Beam

        private IEnumerator PlayBeamEffect()
        {
            CreateBeamRenderer();

            Transform landerTransform = Lander.Instance.transform;
            float elapsed = 0f;

            float energyPerSecond = _energyAmount / _beamDuration;

            while (elapsed < _beamDuration)
            {
                float dt = Time.deltaTime;
                elapsed += dt;
                float t = elapsed / _beamDuration;

                Lander.Instance.AddEnergy(energyPerSecond * dt);

                Vector3 origin = _beamOriginPoint != null
                    ? _beamOriginPoint.position
                    : transform.position;

                _lineRenderer.SetPosition(0, origin);
                _lineRenderer.SetPosition(1, landerTransform.position);

                float pulse = 1f + _beamPulseAmount * Mathf.Sin(elapsed * _beamPulseSpeed);
                _lineRenderer.startWidth = _beamStartWidth * pulse;
                _lineRenderer.endWidth = _beamEndWidth * pulse;

                float alpha = CalculateBeamAlpha(t);

                Color c = _beamColor;
                c.a = _beamColor.a * alpha;
                _lineRenderer.startColor = c;
                _lineRenderer.endColor = c;

                yield return null;
            }

            _lineRenderer.enabled = false;
        }

        private float CalculateBeamAlpha(float t)
        {
            if (t < _beamFadeInRatio)
                return t / _beamFadeInRatio;

            float fadeOutStart = 1f - _beamFadeOutRatio;
            if (t > fadeOutStart)
                return Mathf.Lerp(1f, 0f, (t - fadeOutStart) / _beamFadeOutRatio);

            return 1f;
        }

        private void CreateBeamRenderer()
        {
            _lineRenderer = gameObject.AddComponent<LineRenderer>();

            _lineRenderer.positionCount = 2;
            _lineRenderer.useWorldSpace = true;
            _lineRenderer.numCapVertices = 5;
            _lineRenderer.sortingOrder = _beamSortingOrder;

            if (_beamMaterial != null)
            {
                _lineRenderer.material = _beamMaterial;
            }
            else
            {
                _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            }

            _lineRenderer.startWidth = _beamStartWidth;
            _lineRenderer.endWidth = _beamEndWidth;
            _lineRenderer.startColor = _beamColor;
            _lineRenderer.endColor = _beamColor;
        }

        #endregion
    }
}