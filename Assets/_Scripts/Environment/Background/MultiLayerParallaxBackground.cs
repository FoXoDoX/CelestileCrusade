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

            [Tooltip("Масштаб текстуры (меньше = крупнее звёзды)")]
            [Range(0.01f, 5f)]
            public float TextureScale = 1f;

            [HideInInspector]
            public Material Material;

            [HideInInspector]
            public Vector2 CorrectedTiling;
        }

        [Header("Target")]
        [SerializeField] private Transform _target;

        [Header("Parallax Layers")]
        [SerializeField] private List<ParallaxLayer> _layers = new();

        private Vector3 _startTargetPosition;
        private static readonly int ParallaxOffsetProperty = Shader.PropertyToID("_ParallaxOffset");
        private static readonly int TilingProperty = Shader.PropertyToID("_Tiling");

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
                    ApplyTiling(layer);
                }
                else
                {
                    Debug.LogWarning($"[{nameof(MultiLayerParallaxBackground)}] Layer renderer is null!");
                }
            }
        }

        private void ApplyTiling(ParallaxLayer layer)
        {
            if (layer.Material == null) return;

            Texture tex = null;

            SpriteRenderer spriteRenderer = layer.LayerRenderer as SpriteRenderer;
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                tex = spriteRenderer.sprite.texture;
            }
            else
            {
                tex = layer.Material.mainTexture;
            }

            if (tex != null && tex.height > 0)
            {
                float aspectRatio = (float)tex.width / tex.height; // 768/512 = 1.5

                // ИСПРАВЛЕНИЕ: делим X на aspectRatio, чтобы компенсировать растяжение
                layer.CorrectedTiling = new Vector2(
                    layer.TextureScale,                      // X оставляем как есть
                    layer.TextureScale * aspectRatio         // Y умножаем на aspect ratio
                );

                // Или альтернативный вариант:
                // layer.CorrectedTiling = new Vector2(
                //     layer.TextureScale / aspectRatio,     // X делим на aspect ratio
                //     layer.TextureScale                    // Y оставляем как есть
                // );

                Debug.Log($"Texture: {tex.name}, Size: {tex.width}x{tex.height}, " +
                          $"AspectRatio: {aspectRatio:F2}, " +
                          $"CorrectedTiling: {layer.CorrectedTiling}");
            }
            else
            {
                layer.CorrectedTiling = new Vector2(layer.TextureScale, layer.TextureScale);
                Debug.LogWarning("Texture is null or has zero height!");
            }

            layer.Material.SetVector(TilingProperty, layer.CorrectedTiling);
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

                // Простой offset без учёта tiling
                Vector2 parallaxOffset = new Vector2(
                    -targetDelta.x * layer.ParallaxFactor,
                    -targetDelta.y * layer.ParallaxFactor
                );

                layer.Material.SetVector(ParallaxOffsetProperty, parallaxOffset);
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
                if (layer.LayerRenderer != null)
                {
                    if (Application.isPlaying && layer.Material != null)
                    {
                        ApplyTiling(layer);
                    }
                    else if (!Application.isPlaying)
                    {
                        var sharedMat = layer.LayerRenderer.sharedMaterial;
                        if (sharedMat != null)
                        {
                            Texture tex = null;

                            // Получаем текстуру из SpriteRenderer
                            SpriteRenderer spriteRenderer = layer.LayerRenderer as SpriteRenderer;
                            if (spriteRenderer != null && spriteRenderer.sprite != null)
                            {
                                tex = spriteRenderer.sprite.texture;
                            }

                            if (tex != null && tex.height > 0)
                            {
                                float aspectRatio = (float)tex.width / tex.height;
                                Vector2 correctedTiling = new Vector2(
                                    layer.TextureScale,
                                    layer.TextureScale * aspectRatio
                                );
                                sharedMat.SetVector(TilingProperty, correctedTiling);
                            }
                        }
                    }
                }
            }
        }
#endif
    }
}