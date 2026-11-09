using System;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;

namespace YangVil.Data
{
    [Serializable]
    public class SpriteSet
    {
        public List<AssetReferenceTexture2D> idle = new List<AssetReferenceTexture2D>();
        public List<AssetReferenceTexture2D> walk = new List<AssetReferenceTexture2D>();
        public List<AssetReferenceTexture2D> attack = new List<AssetReferenceTexture2D>();
        public List<AssetReferenceTexture2D> hurt = new List<AssetReferenceTexture2D>();
        public AssetReferenceTexture2D sheet;
        public int cellSize = 16;
        public int framesPerRow = 6;
        public int idleRow;
        public int idleFrames = 4;
        public int walkRow = 1;
        public int walkFrames = 6;
        public int attackRow = 4;
        public int attackFrames = 6;
        public int hurtRow = -1;
        public int hurtFrames;
        public float fps = 8f;

        public bool UsesSheet => sheet != null && sheet.RuntimeKeyIsValid();
    }
}
