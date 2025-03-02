
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Aryz.Localization
{
    public class LocalizedText : UdonSharpBehaviour
    {
        [Header("文本标识（Key）")]
        public string key;

        [Header("LocalizationManager")]
        public LocalizationManager localizationManager;

        [Header("UI 文本组件 (自动获取)")]
        private TextMeshProUGUI textComponent;

        void Start()
        {
            textComponent = GetComponent<TextMeshProUGUI>();
            UpdateText();
        }

        public void UpdateText()
        {
            if (textComponent != null && localizationManager != null)
            {
                textComponent.text = localizationManager.GetTranslation(key);
            }
        }
    }
}

