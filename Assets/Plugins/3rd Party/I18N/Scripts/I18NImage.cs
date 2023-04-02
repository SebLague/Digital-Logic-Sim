using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace Honeti
{
    public class I18NImage : MonoBehaviour
    {
        private bool _initialized = false;
        private Image _img;

        [SerializeField]
        private Sprite _defaultImage;
        [SerializeField]
        private I18NSprites[] _images;

        void OnEnable()
        {
            if (!_initialized)
                _init();

            _updateTranslation(I18N.instance.gameLang);
        }

        void OnDestroy()
        {
            if (_initialized)
            {
                I18N.OnLanguageChanged -= _onLanguageChanged;
            }
        }

        private void _init()
        {
            _img = GetComponent<Image>();
            if (_defaultImage == null)
            {
                _defaultImage = _img.sprite;
            }
            _initialized = true;

            I18N.OnLanguageChanged += _onLanguageChanged;
        }

        private void _onLanguageChanged(LanguageCode newLang)
        {
            _updateTranslation(newLang);
        }

        private void _updateTranslation(LanguageCode newLang)
        {
            if (_images == null || (_images != null && _images.Length == 0))
            {
                return;
            }

            Sprite newSprite = _defaultImage;

            for (int i=0; i<_images.Length; i++)
            {
                if (_images[i].language == newLang)
                {
                    newSprite = _images[i].image;
                    break;
                }
            }

            _img.sprite = newSprite;
        }
    }
}