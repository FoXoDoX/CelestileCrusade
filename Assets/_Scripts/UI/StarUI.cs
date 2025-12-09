using System;
using UnityEngine;
using UnityEngine.UI;

namespace My.Scripts.UI
{
    /// <summary>
    ///  омпонент отображени€ звезды (заработанна€/незаработанна€).
    /// UnearnedImage служит фоном и видна всегда, когда звезда отображаетс€.
    /// EarnedImage накладываетс€ поверх при заработанном состо€нии.
    /// </summary>
    [Serializable]
    public class StarUI
    {
        [SerializeField] private Image _earnedImage;
        [SerializeField] private Image _unearnedImage;

        /// <summary>
        /// ”станавливает состо€ние звезды.
        /// </summary>
        /// <param name="earned">«везда заработана</param>
        /// <param name="visible">«везда видима</param>
        public void SetState(bool earned, bool visible)
        {
            // ‘он (unearned) виден всегда, когда звезда отображаетс€
            if (_unearnedImage != null)
            {
                _unearnedImage.enabled = visible;
            }

            // «аработанна€ звезда накладываетс€ поверх
            if (_earnedImage != null)
            {
                _earnedImage.enabled = earned && visible;
            }
        }

        /// <summary>
        /// —крывает звезду полностью.
        /// </summary>
        public void Hide()
        {
            SetState(earned: false, visible: false);
        }

        /// <summary>
        /// ѕоказывает звезду как заработанную.
        /// </summary>
        public void ShowEarned()
        {
            SetState(earned: true, visible: true);
        }

        /// <summary>
        /// ѕоказывает звезду как незаработанную.
        /// </summary>
        public void ShowUnearned()
        {
            SetState(earned: false, visible: true);
        }
    }
}