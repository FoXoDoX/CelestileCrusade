using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace My.Scripts.UI.Menus
{
    public class LandedMenuUIAnimation : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float stretchOutDuration = 0.2f;
        [SerializeField] private float bounceBackDuration = 0.6f;
        [SerializeField] private float initialVerticalStretch = 1.5f;

        [Header("UI References")]
        [SerializeField] private RectTransform mainPanel;
        [SerializeField] private List<Transform> stars;

        private Vector3 originalScale;
        private List<Graphic> colorElements;
        private List<Color> originalColors = new List<Color>();

        private void Awake()
        {
            // Store original values
            if (mainPanel != null)
            {
                originalScale = mainPanel.localScale;
            }

            // Automatically get all Graphic components in children
            colorElements = new List<Graphic>(GetComponentsInChildren<Graphic>(true));

            // Store original colors
            foreach (Graphic element in colorElements)
            {
                if (element != null)
                {
                    originalColors.Add(element.color);
                }
            }

            // Initially set up the initial state
            ResetToInitialState();
        }

        public void PlayEnterAnimation()
        {
            // Reset to initial state before playing animation
            ResetToInitialState();

            // Create the main scale sequence
            Sequence scaleSequence = DOTween.Sequence();

            if (mainPanel != null)
            {
                // Start from original scale
                mainPanel.localScale = originalScale;

                // First: stretch vertically
                scaleSequence.Append(mainPanel.DOScaleY(originalScale.y * initialVerticalStretch, stretchOutDuration)
                    .SetEase(Ease.OutQuad));

                // Then: bounce back to original scale
                scaleSequence.Append(mainPanel.DOScaleY(originalScale.y, bounceBackDuration)
                    .SetEase(Ease.OutBack));
            }

            // Animate colors from white to original
            Sequence colorSequence = DOTween.Sequence();

            for (int i = 0; i < colorElements.Count; i++)
            {
                if (colorElements[i] != null)
                {
                    // Set initial white color
                    colorElements[i].color = Color.white;

                    // Animate to original color over the total duration
                    colorSequence.Join(colorElements[i].DOColor(originalColors[i], stretchOutDuration + bounceBackDuration)
                        .SetEase(Ease.OutQuad));
                }
            }

            // Animate stars with slight delay for each (starting after the main animation)
            if (stars != null && stars.Count > 0)
            {
                float starStartDelay = stretchOutDuration + bounceBackDuration;

                for (int i = 0; i < stars.Count; i++)
                {
                    if (stars[i] != null)
                    {
                        stars[i].localScale = Vector3.zero;

                        // Staggered animation for stars after main animation
                        scaleSequence.Insert(starStartDelay + i * 0.5f, stars[i].DOScale(Vector3.one, 0.3f)
                            .SetEase(Ease.OutBack));
                    }
                }
            }
        }

        private void ResetToInitialState()
        {
            // Reset scale to original (animation will start from here)
            if (mainPanel != null)
            {
                mainPanel.localScale = originalScale;
            }

            // Reset colors to white using automatically found elements
            if (colorElements != null)
            {
                foreach (Graphic element in colorElements)
                {
                    if (element != null)
                    {
                        element.color = Color.white;
                    }
                }
            }

            // Reset stars scale
            if (stars != null)
            {
                foreach (Transform star in stars)
                {
                    if (star != null)
                    {
                        star.localScale = Vector3.zero;
                    }
                }
            }
        }

        private void OnDestroy()
        {
            // Clean up DOTween tweens
            if (mainPanel != null)
            {
                mainPanel.DOKill();
            }

            if (colorElements != null)
            {
                foreach (Graphic element in colorElements)
                {
                    if (element != null)
                    {
                        element.DOKill();
                    }
                }
            }
        }
    }
}