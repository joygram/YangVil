using System.IO;
using UnityEngine;

namespace CatVillage.Core
{
    public static class SaveSystem
    {
        public static string DirectoryOverride;

        static string FilePath => Path.Combine(string.IsNullOrEmpty(DirectoryOverride) ? Application.persistentDataPath : DirectoryOverride, "save.json");

        public static SaveData Load()
        {
            if (!File.Exists(FilePath)) return null;
            try
            {
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
                return data != null && data.version >= 1 && data.version <= SaveData.CurrentVersion ? data : null;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"SaveSystem: 저장 파일을 읽지 못해 새로 시작합니다. {e.Message}");
                File.Copy(FilePath, FilePath + ".bak", true);
                return null;
            }
        }

        public static void Save(SaveData data)
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(data));
            WebGLSync();
        }

        public static void Delete()
        {
            if (File.Exists(FilePath)) File.Delete(FilePath);
            WebGLSync();
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        static extern void SyncFileSystem();
        static void WebGLSync() => SyncFileSystem();
#else
        static void WebGLSync() { }
#endif
    }
}
