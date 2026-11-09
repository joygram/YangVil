using UnityEditor;
using UnityEngine;

namespace YangVil.EditorTools
{
    public class ArtImportSettings : AssetPostprocessor
    {
        public const string UiSpriteRoot = "Assets/Art/ThirdParty/KenneyPixelUI/";
        public const int UiSliceBorder = 16;
        public const int PixelArtMaxSize = 256;
        public const int MaxTextureSize = 2048;

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Art/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.GetSourceTextureWidthAndHeight(out var width, out var height);
            var large = Mathf.Max(width, height) > PixelArtMaxSize;
            importer.filterMode = large ? FilterMode.Bilinear : FilterMode.Point;
            importer.maxTextureSize = MaxTextureSize;
            importer.textureCompression = large ? TextureImporterCompression.CompressedHQ : TextureImporterCompression.Uncompressed;
            importer.crunchedCompression = large;
            importer.compressionQuality = 75;

            importer.isReadable = !large && (assetPath.StartsWith("Assets/Art/ThirdParty/CuteKitty/") || assetPath.StartsWith("Assets/Art/Cats/"));

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
