using System;
using System.Collections.Generic;
using UnityEngine;

namespace My.Scripts.Environment.Background
{
    public class MultiLayerParallaxBackground : MonoBehaviour
    {
        [Serializable]
        public class ParallaxLayer
        {
            [Tooltip("Рендерер слоя")]
            public Renderer LayerRenderer;

            [Tooltip("Множитель параллакса (0 = статичный, 1 = движется вместе с камерой)")]
            [Range(0f, 1f)]
            public float ParallaxFactor = 0.1f;

            [Tooltip("Масштаб текстуры (меньше = крупнее элементы)")]
            [Range(0.01f, 5f)]
            public float TextureScale = 1f;

            [Tooltip("Количество вариантов в атласе (по горизонтали)")]
            [Range(1, 16)]
            public int VariantsCount = 1;

            [Tooltip("Сид для рандомизации (разные значения = разный паттерн)")]
            public float RandomSeed = 0f;

            [HideInInspector]
            public Material Material;

            [HideInInspector]
            public Vector2 CorrectedTiling;
        }

        [Header("Target")]
        [SerializeField] private Transform _target;

        [Header("Parallax Layers")]
        [SerializeField] private List<ParallaxLayer> _layers = new();

        [Header("Randomization")]
        [SerializeField] private bool _randomizeSeedsOnStart = true;

        private Vector3 _startTargetPosition;

        // Shader property IDs
        private static readonly int ParallaxOffsetProperty = Shader.PropertyToID("_ParallaxOffset");
        private static readonly int TilingProperty = Shader.PropertyToID("_Tiling");
        private static readonly int VariantsCountProperty = Shader.PropertyToID("_VariantsCount");
        private static readonly int SeedProperty = Shader.PropertyToID("_Seed");

        private void Start()
        {
            if (_target == null)
            {
                Debug.LogError($"[{nameof(MultiLayerParallaxBackground)}] Target is not assigned!");
                return;
            }

            _startTargetPosition = _target.position;

            foreach (var layer in _layers)
            {
                if (layer.LayerRenderer != null)
                {
                    layer.Material = layer.LayerRenderer.material;

                    if (_randomizeSeedsOnStart)
                    {
                        layer.RandomSeed = UnityEngine.Random.Range(0f, 1000f);
                    }

                    ApplyLayerSettings(layer);
                }
                else
                {
                    Debug.LogWarning($"[{nameof(MultiLayerParallaxBackground)}] Layer renderer is null!");
                }
            }
        }

        private void ApplyLayerSettings(ParallaxLayer layer)
        {
            if (layer.Material == null) return;

            // Применяем tiling
            ApplyTiling(layer);

            // Применяем настройки вариантов
            layer.Material.SetFloat(VariantsCountProperty, layer.VariantsCount);
            layer.Material.SetFloat(SeedProperty, layer.RandomSeed);
        }

        private void ApplyTiling(ParallaxLayer layer)
        {
            if (layer.Material == null) return;

            Texture tex = GetLayerTexture(layer);

            if (tex != null && tex.height > 0)
            {
                // Для атласа учитываем, что ширина делится на количество вариантов
                float singleVariantWidth = (float)tex.width / layer.VariantsCount;
                float aspectRatio = singleVariantWidth / tex.height;

                layer.CorrectedTiling = new Vector2(
                    layer.TextureScale,
                    layer.TextureScale * aspectRatio
                );

                Debug.Log($"[Parallax] Texture: {tex.name}, " +
                          $"Atlas size: {tex.width}x{tex.height}, " +
                          $"Variants: {layer.VariantsCount}, " +
                          $"Single variant aspect: {aspectRatio:F2}, " +
                          $"Tiling: {layer.CorrectedTiling}");
            }
            else
            {
                layer.CorrectedTiling = new Vector2(layer.TextureScale, layer.TextureScale);
                Debug.LogWarning("[Parallax] Texture is null or has zero height!");
            }

            layer.Material.SetVector(TilingProperty, layer.CorrectedTiling);
        }

        private Texture GetLayerTexture(ParallaxLayer layer)
        {
            if (layer.LayerRenderer is SpriteRenderer spriteRenderer &&
                spriteRenderer.sprite != null)
            {
                return spriteRenderer.sprite.texture;
            }

            return layer.Material?.mainTexture;
        }

        private void Update()
        {
            if (_target == null) return;

            Vector3 targetPos = _target.position;
            transform.position = new Vector3(targetPos.x, targetPos.y, transform.position.z);

            Vector3 targetDelta = _target.position - _startTargetPosition;

            foreach (var layer in _layers)
            {
                if (layer.Material == null) continue;

                Vector2 parallaxOffset = new Vector2(
                    -targetDelta.x * layer.ParallaxFactor,
                    -targetDelta.y * layer.ParallaxFactor
                );

                layer.Material.SetVector(ParallaxOffsetProperty, parallaxOffset);
            }
        }

        /// <summary>
        /// Перегенерировать случайные сиды для всех слоёв
        /// </summary>
        public void RandomizeAllSeeds()
        {
            foreach (var layer in _layers)
            {
                layer.RandomSeed = UnityEngine.Random.Range(0f, 1000f);
                if (layer.Material != null)
                {
                    layer.Material.SetFloat(SeedProperty, layer.RandomSeed);
                }
            }
        }

        private void OnDestroy()
        {
            foreach (var layer in _layers)
            {
                if (layer.Material != null)
                {
                    Destroy(layer.Material);
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            foreach (var layer in _layers)
            {
                if (layer.LayerRenderer == null) continue;

                if (Application.isPlaying && layer.Material != null)
                {
                    ApplyLayerSettings(layer);
                }
                else if (!Application.isPlaying)
                {
                    var sharedMat = layer.LayerRenderer.sharedMaterial;
                    if (sharedMat != null)
                    {
                        ApplyEditorSettings(layer, sharedMat);
                    }
                }
            }
        }

        private void ApplyEditorSettings(ParallaxLayer layer, Material mat)
        {
            Texture tex = null;

            if (layer.LayerRenderer is SpriteRenderer spriteRenderer &&
                spriteRenderer.sprite != null)
            {
                tex = spriteRenderer.sprite.texture;
            }

            if (tex != null && tex.height > 0)
            {
                float singleVariantWidth = (float)tex.width / layer.VariantsCount;
                float aspectRatio = singleVariantWidth / tex.height;

                Vector2 correctedTiling = new Vector2(
                    layer.TextureScale,
                    layer.TextureScale * aspectRatio
                );

                mat.SetVector(TilingProperty, correctedTiling);
                mat.SetFloat(VariantsCountProperty, layer.VariantsCount);
                mat.SetFloat(SeedProperty, layer.RandomSeed);
            }
        }
#endif
    }
}