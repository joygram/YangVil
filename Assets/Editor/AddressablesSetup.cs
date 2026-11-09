using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CatVillage.EditorTools
{
    public static class AddressablesSetup
    {
        public const string UiGroup = "UI";
        public const string CharacterGroup = "Characters";

        static readonly string[] CharacterFolders =
        {
            "Assets/Art/ThirdParty/CuteKitty", "Assets/Art/ThirdParty/Dog", "Assets/Art/Cats", "Assets/Art/Enemies"
        };

        [MenuItem("CatVillage/Setup Addressables")]
        public static AddressableAssetSettings Run()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            settings.BuildRemoteCatalog = false;
            Group(settings, UiGroup, BundledAssetGroupSchema.BundlePackingMode.PackTogether);
            var characters = Group(settings, CharacterGroup, BundledAssetGroupSchema.BundlePackingMode.PackTogetherByLabel);
            foreach (var folder in CharacterFolders)
            {
                if (!AssetDatabase.IsValidFolder(folder)) continue;
                foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
                    Add(settings, characters, guid);
            }
            AssetDatabase.SaveAssets();
            return settings;
        }

        static AddressableAssetGroup Group(AddressableAssetSettings settings, string name, BundledAssetGroupSchema.BundlePackingMode mode)
        {
            var group = settings.FindGroup(name) ?? settings.CreateGroup(name, false, false, true, null,
                typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            schema.BundleMode = mode;
            schema.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZ4;
            schema.UseAssetBundleCrc = false;
            schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
            schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
            EditorUtility.SetDirty(group);
            return group;
        }

        static AddressableAssetEntry Add(AddressableAssetSettings settings, AddressableAssetGroup group, string guid)
        {
            var entry = settings.CreateOrMoveEntry(guid, group, false, false);
            var path = AssetDatabase.GUIDToAssetPath(guid);
            entry.address = path;
            if (group.Name == CharacterGroup)
            {
                entry.labels.Clear();
                entry.SetLabel(System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(path)), true, true);
            }
            return entry;
        }

        public static AssetReferenceTexture2D Texture(string path)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid)) return null;
            if (settings.FindAssetEntry(guid) == null) Add(settings, settings.FindGroup(CharacterGroup), guid);
            return new AssetReferenceTexture2D(guid);
        }

        public static AssetReferenceGameObject Prefab(GameObject prefab)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(prefab));
            Add(settings, settings.FindGroup(UiGroup), guid);
            return new AssetReferenceGameObject(guid);
        }

        [MenuItem("CatVillage/Build Addressables")]
        public static bool BuildContent()
        {
            Run();
            AddressableAssetSettings.CleanPlayerContent();
            AddressableAssetSettings.BuildPlayerContent(out var result);
            if (!string.IsNullOrEmpty(result.Error))
            {
                Debug.LogError($"AddressablesSetup: 빌드 실패 {result.Error}");
                return false;
            }
            var bundles = result.FileRegistry.GetFilePaths().Count(p => p.EndsWith(".bundle"));
            Debug.Log($"AddressablesSetup: 번들 {bundles}개 빌드");
            return true;
        }
    }
}
