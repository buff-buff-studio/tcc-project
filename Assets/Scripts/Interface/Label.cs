﻿using Solis.i18n;
using TMPro;
using UnityEngine;

namespace Interface
{
    /// <summary>
    /// Automatically localizes a TextMeshPro label.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    [DisallowMultipleComponent]
    public class Label : MonoBehaviour
    {
        #region Private Fields
        private TMP_Text _text;
        private string _buffer;
        #endregion

        #region Unity Callbacks
        private void OnEnable()
        {
            _text = GetComponent<TMP_Text>();
            _buffer = _text.text;
            
            LanguagePalette.OnLanguageChanged += _Localize;
            _Localize();
        }

        private void OnDisable()
        {
            _text.text = _buffer;
            LanguagePalette.OnLanguageChanged -= _Localize;
        }
        #endregion

        #region Private Methods
        private void _Localize()
        {
            _text.text = LanguagePalette.Localize(_buffer);
        }
        #endregion
    }
}