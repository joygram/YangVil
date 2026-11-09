using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace YangVil.EditorTools
{
    [InitializeOnLoad]
    public static class DynamicFontReset
    {
        static DynamicFontReset()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode) ResetAll();
            };
        }

        [MenuItem("YangVil/Reset Dynamic Fonts")]
        public static void ResetAll()
        {
            var count = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { "Assets/Art" }))
            {
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (font == null || font.atlasPopulationMode != AtlasPopulationMode.Dynamic) continue;
                font.ClearFontAssetData(true);
                EditorUtility.SetDirty(font);
                count++;
            }
            if (count == 0) return;
            AssetDatabase.SaveAssets();
            Debug.Log($"DynamicFontReset: 동적 폰트 {count}개 비움");
        }
    }
}
