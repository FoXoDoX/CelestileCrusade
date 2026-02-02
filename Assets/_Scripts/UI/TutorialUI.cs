using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace My.Scripts.UI
{
    /// <summary>
    /// Контейнер UI элементов туториала. Размещается на игровой сцене.
    /// </summary>
    public class TutorialUI : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Text")]
        [SerializeField] private TMP_Text _tutorialText;

        [Header("Background")]
        [SerializeField] private Sprite _backgroundSprite;
        [SerializeField] private Vector2 _backgroundPadding = new Vector2(40f, 30f);

        [Header("Skip UI")]
        [SerializeField] private Image _skipProgressCircle;
        [SerializeField] private GameObject _skipHintContainer;

        [Header("Tutorial Images")]
        [Tooltip("Все изображения для туториалов. Ключ = имя объекта")]
        [SerializeField] private List<GameObject> _tutorialImages = new();

        #endregion

        #region Properties

        public TMP_Text TutorialText => _tutorialText;

        public RectTransform TutorialTextRect
        {
            get
            {
                if (_tutorialText == null) return null;
                return _tutorialText.rectTransform;
            }
        }

        public Sprite BackgroundSprite => _backgroundSprite;
        public Vector2 BackgroundPadding => _backgroundPadding;
        public Image SkipProgressCircle => _skipProgressCircle;
        public GameObject SkipHintContainer => _skipHintContainer;

        #endregion

        #region Private Fields

        private Dictionary<string, GameObject> _imagesByName;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ValidateSetup();
            BuildImageDictionary();
            HideAllImages();
        }

        #endregion

        #region Public Methods

        public GameObject GetImageByName(string name)
        {
            if (_imagesByName == null)
            {
                BuildImageDictionary();
            }

            if (_imagesByName.TryGetValue(name, out GameObject image))
            {
                return image;
            }

            Debug.LogWarning($"[TutorialUI] Image '{name}' not found");
            return null;
        }

        public void HideAllImages()
        {
            foreach (var image in _tutorialImages)
            {
                if (image != null)
                {
                    image.SetActive(false);
                }
            }
        }

        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }

        #endregion

        #region Private Methods

        private void BuildImageDictionary()
        {
            _imagesByName = new Dictionary<string, GameObject>();

            foreach (var image in _tutorialImages)
            {
                if (image != null)
                {
                    _imagesByName[image.name] = image;
                }
            }
        }

        private void ValidateSetup()
        {
            if (_tutorialText == null)
            {
                Debug.LogError("[TutorialUI] Tutorial Text is not assigned!");
            }
        }

        #endregion
    }
}