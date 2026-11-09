using System.Collections.Generic;
using CatVillage.Data;
using UnityEngine;

namespace CatVillage.Core
{
    public static class SpriteLibrary
    {
        static readonly Dictionary<(Texture2D, Rect), Sprite> Cache = new Dictionary<(Texture2D, Rect), Sprite>();

        public static Sprite[] Idle(SpriteSet set) =>
            set.sheet != null ? FromSheet(set, set.idleRow, set.idleFrames) : FromFrames(set.idle);

        public static Sprite[] Attack(SpriteSet set)
        {
            var frames = set.sheet != null ? FromSheet(set, set.attackRow, set.attackFrames) : FromFrames(set.attack);
            return frames.Length > 0 ? frames : Idle(set);
        }

        static Sprite[] FromFrames(Texture2D[] frames)
        {
            if (frames == null) return new Sprite[0];
            var result = new List<Sprite>();
            foreach (var t in frames)
                if (t != null) result.Add(Make(t, new Rect(0, 0, t.width, t.height)));
            return result.ToArray();
        }

        static Sprite[] FromSheet(SpriteSet set, int row, int count)
        {
            var t = set.sheet;
            var cell = set.cellSize;
            var rows = t.height / cell;
            var n = Mathf.Min(count, set.framesPerRow);
            var result = new Sprite[n];
            for (var i = 0; i < n; i++)
            {
                var y = (rows - 1 - row) * cell;
                result[i] = Make(t, new Rect(i * cell, y, cell, cell));
            }
            return result;
        }

        static Sprite Make(Texture2D t, Rect rect)
        {
            if (Cache.TryGetValue((t, rect), out var s)) return s;
            s = Sprite.Create(t, rect, new Vector2(0.5f, 0f), rect.width);
            Cache[(t, rect)] = s;
            return s;
        }
    }
}
