using UnityEngine;

namespace CatVillage.Core
{
    public static class BuildInfo
    {
        public const string ResourceName = "BuildInfo";

        static string _version;
        static string _date;

        public static string Version
        {
            get
            {
                Load();
                return _version;
            }
        }

        public static string Date
        {
            get
            {
                Load();
                return _date;
            }
        }

        public static string Label => string.IsNullOrEmpty(_date ?? Date) ? $"v{Version}" : $"v{Version} · {Date}";

        static void Load()
        {
            if (_version != null) return;
            var asset = Resources.Load<TextAsset>(ResourceName);
            var lines = asset == null ? new string[0] : asset.text.Split('\n');
            _version = lines.Length > 0 && lines[0].Trim().Length > 0 ? lines[0].Trim() : $"{Application.version}-dev";
            _date = lines.Length > 1 ? lines[1].Trim() : "";
        }
    }
}
