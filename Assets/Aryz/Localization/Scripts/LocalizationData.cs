
using System.Collections.Generic;
using UnityEngine;

namespace Aryz.Localization
{
    [CreateAssetMenu(fileName = "LocalizationData", menuName = "Localization/LocalizationData")]
    public class LocalizationData : ScriptableObject
    {
        // 使用数组存储每个条目（内部可以用列表也可以用数组）
        public List<LocalizationEntry> entries = new List<LocalizationEntry>();
    }

    [System.Serializable]
    public class LocalizationEntry
    {
        public string key;
        public string chinese;
        public string english;
        // 如需支持更多语言，可继续添加字段
        public string japanese;
    }
}