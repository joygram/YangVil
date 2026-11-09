using System.IO;
using YangVil.Data;
using UnityEditor;
using UnityEngine;

namespace YangVil.EditorTools
{
    public static class SpeechBubbleArt
    {
        public const string Folder = "Assets/Art/Speech";
        public const string BodyPath = Folder + "/speech_body.png";
        public const string TailPath = Folder + "/speech_tail.png";
        const int BodyPx = 32;
        const int Radius = 10;
        const int Line = 2;
        const int TailPx = 16;

        [MenuItem("YangVil/Bake Speech Bubble")]
        public static void Bake()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/Resources/GameConfig.asset");
            var ink = config.presentation.speech.ink;
            Directory.CreateDirectory(Folder);
            if (!File.Exists(BodyPath)) File.WriteAllBytes(BodyPath, Body(ink).EncodeToPNG());
            if (!File.Exists(TailPath)) File.WriteAllBytes(TailPath, Tail(ink).EncodeToPNG());
            AssetDatabase.ImportAsset(BodyPath);
            AssetDatabase.ImportAsset(TailPath);
            AddressablesSetup.Run();
            config.presentation.speech.body = AddressablesSetup.Texture(BodyPath);
            config.presentation.speech.tail = AddressablesSetup.Texture(TailPath);
            config.presentation.speech.sliceBorder = Radius + Line;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log($"Builder: 말풍선 슬롯 → {BodyPath}, {TailPath}");
        }

        static Texture2D Body(Color ink)
        {
            var t = new Texture2D(BodyPx, BodyPx, TextureFormat.RGBA32, false);
            for (var y = 0; y < BodyPx; y++)
            for (var x = 0; x < BodyPx; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                var core = new Vector2(Mathf.Clamp(p.x, Radius, BodyPx - Radius), Mathf.Clamp(p.y, Radius, BodyPx - Radius));
                var d = Radius - Vector2.Distance(p, core);
                t.SetPixel(x, y, d < 0 ? Color.clear : d < Line ? ink : Color.white);
            }
            t.Apply();
            return t;
        }

        static Texture2D Tail(Color ink)
        {
            var t = new Texture2D(TailPx, TailPx, TextureFormat.RGBA32, false);
            for (var y = 0; y < TailPx; y++)
            {
                var width = (y + 1) * 0.55f;
                for (var x = 0; x < TailPx; x++)
                {
                    var px = x + 0.5f;
                    var inside = px <= width;
                    var edge = px > width - Line || x < Line;
                    var top = y >= TailPx - Line;
                    t.SetPixel(x, y, !inside ? Color.clear : edge && !top ? ink : Color.white);
                }
            }
            t.Apply();
            return t;
        }
    }
}
