﻿using System;
using Interface;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Solis.Interface.Lobby
{
    /// <summary>
    /// Represents a save list entry in the save list.
    /// </summary>
    public class SaveListEntry : ListEntry
    {
        #region Inspector Fields
        public RawImage imagePreview;
        public TMP_Text textName;
        public TMP_Text textLastModification;
        public TMP_Text textPlayTime;
        #endregion
    }
}