using System;
using UnityEngine;

namespace CatVillage.Data
{
    [Serializable]
    public class SpriteSet
    {
        public Texture2D[] idle;
        public Texture2D[] attack;
        public Texture2D sheet;
        public int cellSize = 16;
        public int idleRow;
        public int attackRow = 4;
        public int framesPerRow = 6;
        public int idleFrames = 4;
        public int attackFrames = 6;
    }
}
