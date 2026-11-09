using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace YangVil.EditorTools
{
    public static class AddressablesSetup
    {
        public const string UiGroup = "UI";
        public const string CharacterGroup = "Characters";
        public const string SceneGroup = "Scenes";
        public const string ArtGroup = "WorldArt";
        public const string AudioGroup = "Audio";
        const string FontAssetPath = "Assets/Art/UI/Fonts/Galmuri11 SDF.asset";

        static readonly string[] CharacterFolders =
        {
            "Assets/Art/ThirdParty/CuteKitty", "Assets/Art/ThirdParty/Dog", "Assets/Art/Cats", "Assets/Art/Enemies"
        };

        public static readonly string[] ArtFolders =
        {
            "Assets/Art/Backgrounds", "Assets/Art/Buildings", "Assets/Art/FX", "Assets/Art/Speech"
        };

        public const string AudioFolder = "Assets/Audio";

        [MenuItem("YangVil/Setup Addressables")]
        public static AddressableAssetSettings Run()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            settings.BuildRemoteCatalog = false;
            UseAssetDatabaseInPlayMode(settings);
            var ui = Group(settings, UiGroup, BundledAssetGroupSchema.BundlePackingMode.PackTogether);
            var characters = Group(settings, CharacterGroup, BundledAssetGroupSchema.BundlePackingMode.PackTogetherByLabel);
            Group(settings, SceneGroup, BundledAssetGroupSchema.BundlePackingMode.PackSeparately);
            var art = Group(settings, ArtGroup, BundledAssetGroupSchema.BundlePackingMode.PackTogetherByLabel);
            var audio = Group(settings, AudioGroup, BundledAssetGroupSchema.BundlePackingMode.PackSeparately);

            var font = AssetDatabase.AssetPathToGUID(FontAssetPath);
            if (!string.IsNullOrEmpty(font)) Add(settings, ui, font);

            foreach (var folder in CharacterFolders) AddFolder(settings, characters, folder, "t:Texture2D");
            foreach (var folder in ArtFolders) AddFolder(settings, art, folder, "t:Texture2D t:GameObject");
            AddFolder(settings, audio, AudioFolder, "t:AudioClip");
            AssetDatabase.SaveAssets();
            return settings;
        }

        public static void UseAssetDatabaseInPlayMode(AddressableAssetSettings settings = null)
        {
            settings ??= AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;
            var fast = settings.DataBuilders.FindIndex(b => b != null && b.GetType().Name == "BuildScriptFastMode");
            if (fast >= 0 && settings.ActivePlayModeDataBuilderIndex != fast) settings.ActivePlayModeDataBuilderIndex = fast;
        }

        static void AddFolder(AddressableAssetSettings settings, AddressableAssetGroup group, string folder, string filter)
        {
            if (!AssetDatabase.IsValidFolder(folder)) return;
            foreach (var guid in AssetDatabase.FindAssets(filter, new[] { folder }))
                Add(settings, group, guid);
        }

        public static AssetReference Scene(string path)
        {
            var settings = Run();
            var guid = AssetDatabase.AssetPathToGUID(path);
            Add(settings, settings.FindGroup(SceneGroup), guid);
            return new AssetReference(guid);
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
            if (group.Name == CharacterGroup || group.Name == ArtGroup)
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

        [MenuItem("YangVil/Build Addressables")]
        static void BuildContentMenu() => BuildContent(false);

        [MenuItem("YangVil/Build Addressables (Clean)")]
        static void BuildContentCleanMenu() => BuildContent(true);

        public static bool BuildContent(bool clean)
        {
            Run();
            if (clean) AddressableAssetSettings.CleanPlayerContent();
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
