using System;
using System.Collections.Generic;
using UnityEngine;

namespace My.Data.Tutorials
{
    [CreateAssetMenu(fileName = "TutorialData_Level", menuName = "Scriptable Object/Tutorial Data")]
    public class TutorialData : ScriptableObject
    {
        [Serializable]
        public class TutorialImage
        {
            [Tooltip("Имя объекта изображения в иерархии туториала")]
            public string ImageObjectName;

            [Header("Fade In Animation")]
            public bool AddFadeInAnimation;
            public float FadeInDuration = 0.5f;
            public float FadeInDelay = 0f;

            [Header("Arrow Animation")]
            public bool AddArrowAnimation;
            public ArrowDirection Direction = ArrowDirection.Right;
            public float MoveDistance = 20f;
            public float AnimationDuration = 0.8f;
        }

        public enum ArrowDirection
        {
            Up,
            Down,
            Left,
            Right
        }

        [Serializable]
        public class TutorialBlock
        {
            [TextArea(3, 10)]
            public string Text;

            [Header("Text Position (Normalized Screen Space)")]
            [Range(0f, 1f)]
            public float NormalizedX = 0.5f;

            [Range(0f, 1f)]
            public float NormalizedY = 0.5f;

            public Vector2 PixelOffset = Vector2.zero;
            public Vector2 Pivot = new Vector2(0.5f, 0.5f);

            [Header("Images")]
            public List<TutorialImage> Images = new();
        }

        [Header("Level Settings")]
        [Tooltip("Номер уровня, для которого этот туториал")]
        public int LevelNumber;

        [Header("Tutorial Blocks")]
        public List<TutorialBlock> Blocks = new();
    }
}