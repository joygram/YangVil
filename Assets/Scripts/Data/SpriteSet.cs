using System;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;

namespace CatVillage.Data
{
    [Serializable]
    public class SpriteSet
    {
        public List<AssetReferenceTexture2D> idle = new List<AssetReferenceTexture2D>();
        public List<AssetReferenceTexture2D> attack = new List<AssetReferenceTexture2D>();
        public AssetReferenceTexture2D sheet;
        public int cellSize = 16;
        public int idleRow;
        public int attackRow = 4;
        public int framesPerRow = 6;
        public int idleFrames = 4;
        public int attackFrames = 6;

        public bool UsesSheet => sheet != null && sheet.RuntimeKeyIsValid();
    }
}
