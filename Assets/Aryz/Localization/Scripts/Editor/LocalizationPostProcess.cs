using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Aryz.Localization
{
    public class LocalizationPostProcess : MonoBehaviour
    {
        [PostProcessScene(-10)]
        public static void OnPostProcessScene()
        {
            List<LocalizationManager> managers =
                FindObjectsByType<LocalizationManager>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList();

            foreach (LocalizationManager manager in managers)
            {
                if (!manager.localizationData) continue;

                List<string> localizationKeys = new List<string>();
                List<string> chineseTranslations = new List<string>();
                List<string> englishTranslations = new List<string>();
                List<string> japaneseTranslations = new List<string>();

                foreach (LocalizationEntry entry in ((LocalizationData)manager.localizationData).entries)
                {
                    localizationKeys.Add(entry.key);
                    chineseTranslations.Add(entry.chinese);
                    englishTranslations.Add(entry.english);
                    japaneseTranslations.Add(entry.japanese);
                }


                manager.localizationKeys = localizationKeys.ToArray();
                manager.chineseTranslations = chineseTranslations.ToArray();
                manager.englishTranslations = englishTranslations.ToArray();
                manager.japaneseTranslations = japaneseTranslations.ToArray();


                manager.localizationData = null;
            }
        }
    }
}