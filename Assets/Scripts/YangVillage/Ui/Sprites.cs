using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Godot YangVillage Sprites·SpriteView 이식. res:// 경로 → Resources/YangVillage/ 같은 상대 경로.
namespace YangVillage.Ui
{
    // sprite 테이블 → 프레임.
    public static class Sprites
    {
        static readonly Dictionary<string, Sprite[]> Cache = new Dictionary<string, Sprite[]>();

        public static Sprite[] Frames(string spriteId)
        {
            if (Cache.TryGetValue(spriteId, out var frames)) return frames;
            var db = App.Db;
            var path = db.Get("sprite", "sprite_id", spriteId, "path").Replace("res://", "YangVillage/");
            path = path.Substring(0, path.LastIndexOf('.'));
            var sheet = Resources.Load<Texture2D>(path);
            if (sheet == null) throw new KeyNotFoundException($"그림 없음: {path}");
            var start = db.Int("sprite", "sprite_id", spriteId, "start");
            frames = new Sprite[db.Int("sprite", "sprite_id", spriteId, "count")];
            var columns = db.Int("sprite", "sprite_id", spriteId, "columns");
            var rows = db.Int("sprite", "sprite_id", spriteId, "rows");
            var cell = new Vector2((float)sheet.width / columns, (float)sheet.height / rows);
            for (var i = 0; i < frames.Length; i++)
            {
                var index = start + i;
                var x = index % columns * cell.x;
                var yTop = index / columns * cell.y;
                frames[i] = Sprite.Create(sheet, new Rect(x, sheet.height - yTop - cell.y, cell.x, cell.y), new Vector2(0.5f, 0.5f), 100);
            }
            return Cache[spriteId] = frames;
        }

        public static Color ElementColor(string elementId) => Kit.Hex(App.Db.Get("element", "element_id", elementId, "color"));

        // 기운 표식. 그림 없는 기운은 null.
        public static SpriteView ElementIcon(string elementId, float size)
        {
            var spriteId = App.Db.Get("element", "element_id", elementId, "sprite_id");
            return spriteId == "" ? null : SpriteView.Of(spriteId, size);
        }
    }

    // sprite 한 개를 프레임 순서대로 보여 줌.
    public class SpriteView : MonoBehaviour
    {
        const float FrameSeconds = 0.15f;
        Sprite[] _frames = new Sprite[0];
        float _time;
        bool _loop = true;
        public Image Image { get; private set; }

        public RectTransform Rect => (RectTransform)transform;

        public static SpriteView Of(string spriteId, float size, Transform parent = null)
        {
            var go = new GameObject(spriteId, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
            var view = go.AddComponent<SpriteView>();
            view.Rect.sizeDelta = new Vector2(size, size);
            var le = go.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = size;
            le.minHeight = le.preferredHeight = size;
            view.Play(spriteId);
            return view;
        }

        void EnsureImage()
        {
            if (Image != null) return;
            Image = TryGetComponent<Image>(out var img) ? img : gameObject.AddComponent<Image>();
            Image.preserveAspect = true;
            Image.raycastTarget = false;
        }

        public void Play(string spriteId, bool loop = true)
        {
            EnsureImage();
            _frames = Sprites.Frames(spriteId);
            _loop = loop;
            _time = 0;
            Image.sprite = _frames[0];
        }

        void Update()
        {
            if (_frames.Length < 2) return;
            _time += Time.deltaTime;
            var index = (int)(_time / FrameSeconds);
            Image.sprite = _frames[_loop ? index % _frames.Length : Mathf.Min(index, _frames.Length - 1)];
        }
    }
}
