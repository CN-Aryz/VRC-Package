using UnityEngine;
using UnityEditor;
using System.Collections.Generic;


namespace Aryz.Localization
{
    [CustomEditor(typeof(LocalizationManager))]
    public class LocalizationEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector(); // 保留默认的 Inspector UI

            LocalizationManager myComponent = (LocalizationManager)target;

            if (GUILayout.Button("自动设置所有文本"))
            {
                LocalizedText[] localizedTexts = FindObjectsOfType<LocalizedText>();

                foreach (LocalizedText text in localizedTexts)
                {
                    text.localizationManager = myComponent;
                }
                myComponent.localizedTexts = localizedTexts;
            }
        }
    }

}