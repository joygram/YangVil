using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace CatVillage.EditorTools
{
    [InitializeOnLoad]
    public static class GameViewPortrait
    {
        const string Label = "CatVillage 세로";
        const int Width = 1080;
        const int Height = 1920;

        static GameViewPortrait()
        {
            if (!Application.isBatchMode) EditorApplication.delayCall += Apply;
        }

        [MenuItem("CatVillage/Game View 1080x1920")]
        public static void Apply()
        {
            try
            {
                var asm = typeof(Editor).Assembly;
                var sizesType = asm.GetType("UnityEditor.GameViewSizes");
                var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
                var sizes = singleton.GetProperty("instance").GetValue(null);
                var group = sizesType.GetProperty("currentGroup").GetValue(sizes);
                var groupType = group.GetType();

                var texts = (string[])groupType.GetMethod("GetDisplayTexts").Invoke(group, null);
                var index = Array.FindIndex(texts, t => t.StartsWith(Label));
                if (index < 0)
                {
                    var sizeType = asm.GetType("UnityEditor.GameViewSize");
                    var kind = Enum.Parse(asm.GetType("UnityEditor.GameViewSizeType"), "FixedResolution");
                    var size = Activator.CreateInstance(sizeType, kind, Width, Height, Label);
                    groupType.GetMethod("AddCustomSize").Invoke(group, new[] { size });
                    sizesType.GetMethod("SaveToHDD")?.Invoke(sizes, null);
                    index = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null) - 1;
                }

                var viewType = asm.GetType("UnityEditor.GameView");
                var property = viewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var view in Resources.FindObjectsOfTypeAll(viewType).Cast<EditorWindow>())
                {
                    property.SetValue(view, index);
                    view.Repaint();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"GameViewPortrait: Game 뷰 해상도 설정 실패 {e.Message}");
            }
        }
    }
}
