
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Aryz.Localization
{
    // 定义支持的语言类型，可扩展其他语言
    public enum Language
    {
        Chinese,
        English,
        Japanese,
    }

    public class LocalizationManager : UdonSharpBehaviour
    {
        [Header("当前使用的语言")]
        public Language currentLanguage = Language.Chinese;

        [Header("LocalizationData 资源")]
        public UnityEngine.Object localizationData;
        [HideInInspector] public string[] localizationKeys;
        [HideInInspector] public string[] chineseTranslations;
        [HideInInspector] public string[] englishTranslations;
        [HideInInspector] public string[] japaneseTranslations;

        public LocalizedText[] localizedTexts;


        /// <summary>
        /// 根据 key 获取当前语言下对应的翻译文本
        /// </summary>
        public string GetTranslation(string key)
        {
            // 查找 key 在 LocalizationKeys 数组中的索引
            int index = System.Array.IndexOf(localizationKeys, key);

            // 如果未找到 key，返回 key 本身
            if (index == -1)
            {
                Debug.LogWarning($"Localization key '{key}' not found.");
                return key;
            }

            // 根据当前语言返回对应的翻译
            switch (currentLanguage)
            {
                case Language.Chinese:
                    return chineseTranslations[index];
                case Language.English:
                    return englishTranslations[index];
                case Language.Japanese:
                    return japaneseTranslations[index];
                default:
                    Debug.LogWarning($"Unsupported language: {currentLanguage}");
                    return key;
            }
        }


        /// <summary>
        /// 切换当前语言，并可在此处添加刷新界面等逻辑
        /// </summary>
        public void SetLanguage(Language newLanguage)
        {
            if (currentLanguage == newLanguage) return;
            currentLanguage = newLanguage;

            for (int i = 0; i < localizedTexts.Length; i++)
            {
                localizedTexts[i].UpdateText();
            }
            // 此处可以调用事件或通知其他 UI 脚本更新显示
            Debug.Log("当前语言切换为: " + newLanguage);
        }

        public void SetLanguageChinese()
        {
            SetLanguage(Language.Chinese);
        }

        public void SetLanguageEnglish()
        {
            SetLanguage(Language.English);
        }

        public void SetLanguageJapanese()
        {
            SetLanguage(Language.Japanese);
        }
    }

}