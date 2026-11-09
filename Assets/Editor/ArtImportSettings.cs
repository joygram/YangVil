using UnityEditor;
using UnityEngine;

namespace CatVillage.EditorTools
{
    public class ArtImportSettings : AssetPostprocessor
    {
        public const string UiSpriteRoot = "Assets/Art/ThirdParty/KenneyPixelUI/";
        public const int UiSliceBorder = 16;

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Art/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            if (assetPath.StartsWith(UiSpriteRoot))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100;
                importer.spriteBorder = new Vector4(UiSliceBorder, UiSliceBorder, UiSliceBorder, UiSliceBorder);
            }
            else
            {
                importer.textureType = TextureImporterType.Default;
            }
        }
    }
}
