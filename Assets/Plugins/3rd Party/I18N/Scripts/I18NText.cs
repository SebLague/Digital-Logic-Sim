using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace Honeti
{
    public class I18NText : MonoBehaviour
    {
        private string _key = "";
        private Text _text;
        private bool _initialized = false;
        private bool _isValidKey = false;
        private Font _defaultFont;
        private float _defaultLineSpacing;
        private int _defaultFontSize;
        private TextAnchor _defaultAlignment;

        [SerializeField]
        private bool _dontOverwrite = false;

        [SerializeField]
        private string[] _params;

        void OnEnable()
        {
            if (!_initialized)
                _init();

            updateTranslation();
        }

        void OnDestroy()
        {
            if (_initialized)
            {
                I18N.OnLanguageChanged -= _onLanguageChanged;
                I18N.OnFontChanged -= _onFontChanged;
            }
        }

        /// <summary>
        /// Change text in Text component.
        /// </summary>
        private void _updateTranslation()
        {
            if (_text)
            {
                if (!_isValidKey)
                {
                    _key = _text.text;

                    if (_key.StartsWith("^"))
                    {
                        _isValidKey = true;
                    }
                }

                _text.text = I18N.instance.getValue(_key, _params);
            }
        }

        /// <summary>
        /// Update translation text.
        /// </summary>
        /// <param name="invalidateKey">Force to invalidate current translation key</param>
        public void updateTranslation(bool invalidateKey = false)
        {
            if (invalidateKey)
            {
                _isValidKey = false;
            }

            _updateTranslation();
        }

        /// <summary>
        /// Init component.
        /// </summary>
        private void _init()
        {
            _text = GetComponent<Text>();
            _defaultFont = _text.font;
            _defaultLineSpacing = _text.lineSpacing;
            _defaultFontSize = _text.fontSize;
            _defaultAlignment = _text.alignment;
            _key = _text.text;
            _initialized = true;

            if (I18N.instance.useCustomFonts)
            {
                _changeFont(I18N.instance.customFont);
            }

            I18N.OnLanguageChanged += _onLanguageChanged;
            I18N.OnFontChanged += _onFontChanged;

            if (!_key.StartsWith("^"))
            {
                Debug.LogWarning(string.Format("{0}: Translation key was not found! Found {1}", this, _key));
                _isValidKey = false;
            }
            else
            {
                _isValidKey = true;
            }

            if (!_text)
            {
                Debug.LogWarning(string.Format("{0}: Text component was not found!", this));
            }
        }

        private void _onLanguageChanged(LanguageCode newLang)
        {
            _updateTranslation();
        }

        private void _onFontChanged(I18NFonts newFont)
        {
            _changeFont(newFont);
        }

        private void _changeFont(I18NFonts f)
        {
            if (_dontOverwrite)
            {
                return;
            }

            if (f != null)
            {
                if (f.font)
                {
                    _text.font = f.font;
                }
                else
                {
                    _text.font = _defaultFont;
                }
                if (f.customLineSpacing)
                {
                    _text.lineSpacing = f.lineSpacing;
                }
                if (f.customFontSizeOffset)
                {
                    _text.fontSize = (int)(_defaultFontSize + (_defaultFontSize * f.fontSizeOffsetPercent /100));
                }
                if (f.customAlignment)
                {
                    _text.alignment = _getAnchorFromAlignment(f.alignment);
                }
            }
            else
            {
                _text.font = _defaultFont;
                _text.lineSpacing = _defaultLineSpacing;
                _text.fontSize = _defaultFontSize;
                _text.alignment = _defaultAlignment;
            }
        }

        private TextAnchor _getAnchorFromAlignment(TextAlignment alignment)
        {
            switch (_defaultAlignment)
            {
                case TextAnchor.UpperLeft:
                //case TextAnchor.UpperCenter:
                case TextAnchor.UpperRight:
                    if (alignment == TextAlignment.Left)
                        return TextAnchor.UpperLeft;
                    else if (alignment == TextAlignment.Right)
                        return TextAnchor.UpperRight;
                    break;
                case TextAnchor.MiddleLeft:
                //case TextAnchor.MiddleCenter:
                case TextAnchor.MiddleRight:
                    if (alignment == TextAlignment.Left)
                        return TextAnchor.MiddleLeft;
                    else if (alignment == TextAlignment.Right)
                        return TextAnchor.MiddleRight;
                    break;
                case TextAnchor.LowerLeft:
                //case TextAnchor.LowerCenter:
                case TextAnchor.LowerRight:
                    if (alignment == TextAlignment.Left)
                        return TextAnchor.LowerLeft;
                    else if (alignment == TextAlignment.Right)
                        return TextAnchor.LowerRight;
                    break;
            }

            return _defaultAlignment;
        }
    }
}