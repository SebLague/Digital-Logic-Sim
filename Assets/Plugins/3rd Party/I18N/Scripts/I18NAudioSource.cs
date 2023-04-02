using UnityEngine;
using System.Collections;

namespace Honeti
{
    public class I18NAudioSource : MonoBehaviour
    {
        private bool _initialized = false;
        private AudioSource _audioSource;

        [SerializeField]
        private AudioClip _defaultClip;
        [SerializeField]
        private I18NSounds[] _clips;

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
            _audioSource = GetComponent<AudioSource>();
            if (_defaultClip == null)
            {
                _defaultClip = _audioSource.clip;
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
            if (_clips == null || (_clips != null && _clips.Length == 0))
            {
                return;
            }

            AudioClip newClip = _defaultClip;

            for (int i=0; i<_clips.Length; i++)
            {
                if (_clips[i].language == newLang)
                {
                    newClip = _clips[i].clip;
                    break;
                }
            }

            _audioSource.clip = newClip;
        }
    }
}