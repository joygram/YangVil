using System.IO;
using YangVil.Data;
using YangVil.World;
using UnityEditor;
using UnityEngine;

namespace YangVil.EditorTools
{
    public static class EnemySilhouetteArt
    {
        const string Folder = "Assets/Art/Enemies/ForestCritters";

        public static void Bake()
        {
            Directory.CreateDirectory(Folder);
            Sheet("rat", VillageMotif.Mouse);
            Sheet("crow", VillageMotif.Crow);
            AssetDatabase.Refresh();
            AddressablesSetup.Run();
            Bind("rat"); Bind("crow");
            GameDataBuilder.ApplyBossPresentation();
            AssetDatabase.SaveAssets();
            Debug.Log("EnemySilhouetteArtBuilder: 들쥐·까마귀 전용 시트 연결");
        }

        static void Sheet(string id, VillageMotif motif)
        {
            var path = $"{Folder}/{id}_sheet.png";
            if (File.Exists(path)) { AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate); return; }
            var source = VillageIllustration.Draw(motif);
            var sheet = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            sheet.SetPixels(new Color[128 * 128]);
            for (var row = 0; row < 4; row++)
            for (var frame = 0; frame < 4; frame++)
            for (var y = 0; y < 32; y++)
            for (var x = 0; x < 32; x++)
            {
                var lift = row == 1 ? frame % 2 : 0;
                var push = row == 2 && frame == 1 ? 2 : 0;
                var sx = x - push; var sy = y - lift;
                if (sx < 0 || sx >= 32 || sy < 0 || sy >= 32) continue;
                var color = source.GetPixel(sx * 6 + 3, sy * 6 + 3);
                sheet.SetPixel(frame * 32 + x, (3 - row) * 32 + y, color);
            }
            sheet.Apply(); File.WriteAllBytes(path, sheet.EncodeToPNG());
            Object.DestroyImmediate(source); Object.DestroyImmediate(sheet);
            AssetDatabase.ImportAsset(path);
        }

        static void Bind(string id)
        {
            var data = AssetDatabase.LoadAssetAtPath<EnemyBase>($"Assets/Data/Enemies/base_{id}.asset");
            var path = $"{Folder}/{id}_sheet.png";
            var current = data.sprites.UsesSheet ? data.sprites.sheet : data.sprites.idle.Count > 0 ? data.sprites.idle[0] : null;
            var previous = current == null ? "" : AssetDatabase.GUIDToAssetPath(current.AssetGUID);
            if (previous != path && !previous.StartsWith("Assets/Art/ThirdParty/CuteKitty/")) return;
            data.sprites = new SpriteSet { sheet = AddressablesSetup.Texture(path), cellSize = 32, framesPerRow = 4,
                idleRow = 0, idleFrames = 4, walkRow = 1, walkFrames = 4, attackRow = 2, attackFrames = 4, hurtRow = 3, hurtFrames = 4, fps = 6 };
            EditorUtility.SetDirty(data);
        }
    }
}
