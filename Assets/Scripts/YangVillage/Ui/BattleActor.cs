using UnityEngine;
using YangVillage.Battle;

// Godot YangVillage game/scripts/ui/BattleActor.cs·BattleBackdrop.cs 이식.
namespace YangVillage.Ui
{
    // 전장에 선 유닛 하나: 그림자 · 그림 · 머리 위 체력/게이지 · 이름 · 지목 표시.
    public class BattleActor : Node2
    {
        const float BarWidth = 84;
        const float BarHeight = 9;
        const float GaugeHeight = 6;
        static readonly Color Shadow = new Color(0.36f, 0.3f, 0.21f, 0.22f);
        static readonly Color Flashed = new Color(8, 8, 8);
        static readonly Color Pain = new Color(2.4f, 0.45f, 0.45f, 0.55f);
        static readonly Color Scorch = new Color(3f, 1.3f, 0.3f, 0.6f);
        // 지쳐 주저앉은 몸: 색은 조금만 빼고 알아볼 수 있게 둔다.
        static readonly Color Fainted = new Color(0.72f, 0.72f, 0.78f);

        public BattleUnit Unit;
        public Vector2 Home;
        public float Head;
        Sprite2 _sprite;
        Node2 _body;
        TMPro.TextMeshProUGUI _focus;
        Vector2 _baseScale;
        bool _down;
        EmoteFx _sweat, _pant;
        Tw _idle;
        Hud _hud;

        public static BattleActor Of(Transform parent, BattleUnit unit, Vector2 home, float height)
        {
            var actor = Make<BattleActor>(parent, unit.Uid);
            actor.Unit = unit;
            actor.Home = home;
            actor.Position = home;
            actor.Head = height;
            actor.ZIndex = 300 + (int)home.y;

            // 바닥 그림자·체력 막대 (Godot _Draw = 자식보다 아래).
            actor._hud = PaintNode.Of<Hud>(actor.transform);
            actor._hud.Actor = actor;

            var texture = Sprites.Frames(unit.SpriteId)[0];
            var scale = height / texture.rect.height;
            actor._baseScale = new Vector2(scale, scale);
            actor._body = Make<Node2>(actor.transform, "body");
            actor._sprite = Sprite2.Of(actor._body.transform, texture);
            actor._sprite.Scale = actor._baseScale;
            actor._sprite.Offset = new Vector2(0, -texture.rect.height / 2f);
            actor._sprite.FlipH = !unit.IsAlly;

            var icon = Sprites.ElementIcon(unit.ElementId, 26);
            if (icon != null)
            {
                icon.transform.SetParent(actor.transform, false);
                Kit.PlaceCentered(icon.Rect, new Vector2(-BarWidth / 2 - 28, -height - 28), new Vector2(26, 26));
            }
            var caption = unit.IsAlly ? App.T("ui_ally_caption", unit.Name, App.Game.StageLabel(unit.Level)) : unit.IsBoss ? App.T("ui_boss_caption", unit.Name) : unit.Name;
            var name = actor.AddLabel(caption, 14, Kit.Ink, new Vector2(-110, -height - 50), new Vector2(220, 20));
            Kit.NoWrap(name);
            Kit.Outline(name, Kit.Paper, 8);
            actor._focus = actor.AddLabel(App.T("ui_focus_mark"), 26, Kit.Bad, new Vector2(-20, -height - 84), new Vector2(40, 30));
            actor._focus.gameObject.SetActive(false);
            return actor;
        }

        // 대기: 숨 쉬듯 살짝 눌렸다 펴진다. 유닛마다 박자가 조금씩 다르다.
        void Start()
        {
            Breathe(0.55f + Random.value * 0.25f, new Vector2(1.03f, 0.96f));
        }

        void Breathe(float beat, Vector2 squash)
        {
            _idle?.Kill();
            _body.Scale = Vector2.one;
            _idle = Tw.Create(this).SetLoops()
                .Vec(() => _body.Scale, v => _body.Scale = v, squash, beat).SetTrans(Trans.Sine)
                .Vec(() => _body.Scale, v => _body.Scale = v, Vector2.one, beat).SetTrans(Trans.Sine);
        }

        class Hud : Paint
        {
            public BattleActor Actor;

            protected override void Draw()
            {
                var a = Actor;
                DrawEllipse(Vector2.zero, a.Head * 0.42f, a.Head * 0.42f * 0.28f, Shadow);
                var top = -a.Head - 26;
                Bar(top, BarHeight, (float)a.Unit.Hp / a.Unit.MaxHp, a.Unit.IsAlly ? Kit.Good : Kit.Accent);
                if (a.Unit.IsAlly) Bar(top + BarHeight + 3, GaugeHeight, (float)a.Unit.Gauge / BattleUnit.GaugeMax, Kit.Gold);
            }

            void Bar(float y, float height, float ratio, Color fill)
            {
                var frame = new Rect(-BarWidth / 2, y, BarWidth, height);
                DrawRect(new Rect(frame.x - 2, frame.y - 2, frame.width + 4, frame.height + 4), Kit.Ink);
                DrawRect(frame, Kit.Cream);
                DrawRect(new Rect(frame.position, new Vector2(BarWidth * Mathf.Clamp01(ratio), height)), fill);
            }
        }

        // 등 쪽(-1 = 왼쪽).
        int Side => Unit.IsAlly ? -1 : 1;

        // 한마디: 고양이마다 정해진 줄이 말풍선으로 떴다 사라진다.
        void Bark(string prefix)
        {
            if (!Unit.IsAlly) return;
            var pick = 0;
            foreach (var letter in Unit.Name) pick += letter;
            var bubbleNode = Make<Node2>(transform, "bubble");
            bubbleNode.ZIndex = 20;
            var panel = Kit.Panel(bubbleNode.transform, Kit.Paper, Kit.Ink, 14);
            var r = panel.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 1);
            r.anchoredPosition = new Vector2(0, Head + 104);
            Kit.VBox(r, 0, 6);
            Kit.Layout(panel).preferredWidth = 140;
            Kit.Layout(panel).layoutPriority = 2;
            var fit = panel.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            fit.horizontalFit = fit.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            Kit.Label(panel.transform, App.T(prefix + pick % App.Db.ConfigInt("bark_variants")), 16, Kit.Ink);
            var group = bubbleNode.gameObject.AddComponent<CanvasGroup>();
            Tw.Create(bubbleNode).Interval(1.1f)
                .Float(() => group.alpha, v => group.alpha = v, 0f, 0.3f)
                .Callback(bubbleNode.Free);
        }

        public Rect HitBox => new Rect(Position + new Vector2(-Head * 0.5f, -Head), new Vector2(Head, Head));

        public void Refresh(bool focused)
        {
            _focus.gameObject.SetActive(focused && Unit.Alive);
            if (!Unit.Alive && !_down)
            {
                _down = true;
                _sprite.Modulate = Fainted;
                Tw.Create(_sprite).Float(() => _sprite.Rotation, v => _sprite.Rotation = v, (Unit.IsAlly ? -90 : 90) * Mathf.Deg2Rad, 0.25f).SetTrans(Trans.Bounce).SetEase(Ease.Out);
                if (_sweat != null) Destroy(_sweat.gameObject);
                _sweat = null;
                if (_pant != null) Destroy(_pant.gameObject);
                _idle?.Kill();
                _body.Scale = Vector2.one;
                EmoteFx.Of(transform, EmoteFx.Kind.Dizzy, Head * 0.3f, new Vector2(0, -Head * 0.75f));
                Bark("ui_bark_down_");
            }
            else if (Unit.Alive && _sweat == null && Unit.Hp * 100 < Unit.MaxHp * App.Db.ConfigInt("low_hp_pct"))
            {
                _sweat = EmoteFx.Of(transform, EmoteFx.Kind.Sweat, Head * 0.3f, new Vector2(Side * Head * 0.42f, -Head * 0.8f));
                // 헐떡임: 숨이 가빠지고 입 앞으로 김이 나온다.
                _pant = EmoteFx.Of(transform, EmoteFx.Kind.Pant, Head * 0.3f, new Vector2(-Side * Head * 0.38f, -Head * 0.5f), -Side);
                Breathe(0.13f, new Vector2(1.07f, 0.9f));
            }
            _hud.Redraw();
        }

        Tw Tween() => Tw.Create(this);

        // 돌진 직전: 몸을 낮춰 힘을 모은다.
        public void Crouch()
        {
            Tween()
                .Vec(() => _sprite.Scale, v => _sprite.Scale = v, _baseScale * new Vector2(1.15f, 0.82f), 0.09f)
                .Vec(() => _sprite.Scale, v => _sprite.Scale = v, _baseScale * new Vector2(0.92f, 1.1f), 0.05f)
                .Vec(() => _sprite.Scale, v => _sprite.Scale = v, _baseScale, 0.1f);
        }

        float SpriteY { get => _sprite.Position.y; set => _sprite.Position = new Vector2(_sprite.Position.x, value); }

        // 뛰어서 이동: 포물선으로 떴다가 착지하며 눌린다.
        public void Hop(float seconds, float height)
        {
            Tween()
                .Float(() => SpriteY, v => SpriteY = v, -height, seconds / 2).SetTrans(Trans.Quad).SetEase(Ease.Out)
                .Float(() => SpriteY, v => SpriteY = v, 0f, seconds / 2).SetTrans(Trans.Quad).SetEase(Ease.In)
                .Vec(() => _sprite.Scale, v => _sprite.Scale = v, _baseScale * new Vector2(1.14f, 0.86f), 0.05f)
                .Vec(() => _sprite.Scale, v => _sprite.Scale = v, _baseScale, 0.1f);
        }

        // 걸어서 이동: 정해진 시간 동안 총총 뛴다.
        public void Walk(float seconds)
        {
            const float stride = 0.16f;
            Tween().SetLoops(Mathf.Max(1, (int)(seconds / stride)))
                .Float(() => SpriteY, v => SpriteY = v, -Head * 0.1f, stride / 2)
                .Float(() => SpriteY, v => SpriteY = v, 0f, stride / 2);
        }

        // 돌진 잔상.
        public Sprite2 Ghost(Transform parent, Vector2 at)
        {
            var g = Sprite2.Of(parent, _sprite.Texture);
            g.Scale = _baseScale;
            g.Offset = _sprite.Offset;
            g.FlipH = _sprite.FlipH;
            g.Position = at;
            g.ZIndex = ZIndex - 1;
            g.Modulate = new Color(1, 1, 1, 0.4f);
            return g;
        }

        // 내지르기: 뒤로 당겼다가 대상 쪽으로 튕겨 나가고 돌아온다. 타격은 StrikeLead 뒤에 맞춘다.
        public const float StrikeLead = 0.11f;

        public void Strike(Vector2 aim, float reach)
        {
            var lean = (aim.x >= 0 ? 16 : -16) * Mathf.Deg2Rad;
            Tween()
                .Vec(() => _body.Position, v => _body.Position = v, -aim * reach * 0.5f, 0.07f).SetTrans(Trans.Quad).SetEase(Ease.Out)
                .Parallel().Float(() => _sprite.Rotation, v => _sprite.Rotation = v, -lean * 0.6f, 0.07f)
                .Vec(() => _body.Position, v => _body.Position = v, aim * reach, 0.04f)
                .Parallel().Float(() => _sprite.Rotation, v => _sprite.Rotation = v, lean, 0.04f)
                .Parallel().Vec(() => _sprite.Scale, v => _sprite.Scale = v, _baseScale * new Vector2(1.2f, 0.88f), 0.04f)
                .Interval(0.05f)
                .Vec(() => _body.Position, v => _body.Position = v, Vector2.zero, 0.14f).SetTrans(Trans.Quad).SetEase(Ease.Out)
                .Parallel().Float(() => _sprite.Rotation, v => _sprite.Rotation = v, 0f, 0.14f)
                .Parallel().Vec(() => _sprite.Scale, v => _sprite.Scale = v, _baseScale, 0.14f);
        }

        // 쏘는 반동: 대상 반대쪽으로 밀렸다 돌아온다.
        public void Recoil(Vector2 aim, float reach)
        {
            Tween()
                .Vec(() => _body.Position, v => _body.Position = v, -aim * reach, 0.04f)
                .Parallel().Vec(() => _sprite.Scale, v => _sprite.Scale = v, _baseScale * new Vector2(0.9f, 1.1f), 0.04f)
                .Vec(() => _body.Position, v => _body.Position = v, Vector2.zero, 0.16f).SetTrans(Trans.Quad).SetEase(Ease.Out)
                .Parallel().Vec(() => _sprite.Scale, v => _sprite.Scale = v, _baseScale, 0.16f);
        }

        // 맞음: 하얗게 번쩍 + 맞은 방향으로 밀렸다 돌아옴. 아픈 잔영이 뒤로 밀려 나가고, 뜨거운 공격이면 불길이 남는다.
        public void Hurt(Vector2 blow, float push, bool burns = false)
        {
            var echo = Sprite2.Of(transform, _sprite.Texture);
            echo.Scale = _baseScale;
            echo.Offset = _sprite.Offset;
            echo.FlipH = _sprite.FlipH;
            echo.Rotation = _sprite.Rotation;
            echo.ZIndex = -1;
            echo.Modulate = burns ? Scorch : Pain;
            Tw.Create(echo)
                .Vec(() => echo.Position, v => echo.Position = v, blow * push * 2.4f, 0.34f).SetTrans(Trans.Quad).SetEase(Ease.Out)
                .Parallel().Vec(() => echo.Scale, v => echo.Scale = v, _baseScale * 1.12f, 0.34f)
                .Parallel().Float(() => echo.Alpha, v => echo.Alpha = v, 0f, 0.34f)
                .Callback(echo.Free);
            if (burns) EmoteFx.Of(transform, EmoteFx.Kind.Flame, Head, Vector2.zero);
            _sprite.Modulate = Flashed;
            var tween = Tween()
                .Vec(() => _body.Position, v => _body.Position = v, blow * push, 0.04f)
                .Parallel().Vec(() => _sprite.Scale, v => _sprite.Scale = v, _baseScale * new Vector2(0.82f, 1.14f), 0.04f)
                .Parallel().Float(() => _sprite.Rotation, v => _sprite.Rotation = v, (blow.x >= 0 ? 10 : -10) * Mathf.Deg2Rad, 0.04f)
                .Col(() => _sprite.Modulate, v => _sprite.Modulate = v, Unit.Alive ? Color.white : Fainted, 0.08f);
            if (Unit.Alive)
            {
                EmoteFx.Of(transform, EmoteFx.Kind.Hurt, Head * 0.2f, new Vector2(Side * Head * 0.3f, -Head * 0.9f));
                if (Random.Range(0, 100) < App.Db.ConfigInt("bark_hurt_pct")) Bark("ui_bark_hurt_");
            }
            tween.Vec(() => _body.Position, v => _body.Position = v, Vector2.zero, 0.14f).SetTrans(Trans.Quad).SetEase(Ease.Out)
                .Parallel().Vec(() => _sprite.Scale, v => _sprite.Scale = v, _baseScale, 0.14f);
            if (Unit.Alive) tween.Parallel().Float(() => _sprite.Rotation, v => _sprite.Rotation = v, 0f, 0.14f);
        }

        // 힘 모으기처럼 대상 없는 행동.
        public void Glow()
        {
            _sprite.Modulate = Kit.Gold;
            Tween().Col(() => _sprite.Modulate, v => _sprite.Modulate = v, Color.white, 0.3f);
        }
    }

    // 전투 배경 그림(무대와 함께 확대·흔들림) 또는 그 위에 고정으로 덮는 HUD 바탕.
    public class BattleBackdrop : Paint
    {
        const float Bleed = 40;
        static readonly Color Clear = new Color(Kit.Night.r, Kit.Night.g, Kit.Night.b, 0);
        static readonly Color TopShade = new Color(Kit.Night.r, Kit.Night.g, Kit.Night.b, 0.7f);

        public Vector2 Area;
        public float FieldHeight;
        public Sprite Picture;
        float _scroll;
        readonly System.Collections.Generic.List<UnityEngine.UI.Image> _tiles = new System.Collections.Generic.List<UnityEngine.UI.Image>();

        // 전진한 거리: 그림이 그만큼 뒤로 흐른다.
        public float Scroll
        {
            get => _scroll;
            set { _scroll = value; Layout(); }
        }

        void Fade(float top, float bottom, Color from, Color to) =>
            DrawPolygon(new[] { new Vector2(0, top), new Vector2(Area.x, top), new Vector2(Area.x, bottom), new Vector2(0, bottom) }, new[] { from, from, to, to });

        protected override void Draw()
        {
            if (Picture != null) return;
            Fade(0, FieldHeight * 0.16f, TopShade, Clear);
            Fade(FieldHeight * 0.9f, FieldHeight, Clear, Kit.Night);
            DrawRect(new Rect(0, FieldHeight, Area.x, Area.y - FieldHeight), Kit.Night);
        }

        // 그림은 Image 타일로: 흔들려도 가장자리가 비지 않게 사방으로 넘치게, 이음매가 맞게 한 장 걸러 뒤집는다.
        public void Layout()
        {
            if (Picture == null) return;
            var height = FieldHeight + Bleed * 2;
            var width = height * Picture.rect.width / Picture.rect.height;
            var start = (Area.x - width) / 2 - _scroll;
            var first = Mathf.FloorToInt((-Bleed - start) / width);
            var i = 0;
            for (var n = first; start + n * width < Area.x + Bleed; n++, i++)
            {
                if (i >= _tiles.Count)
                {
                    var r = Kit.Node("tile", transform);
                    r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                    r.pivot = new Vector2(0, 1);
                    var img = r.gameObject.AddComponent<UnityEngine.UI.Image>();
                    img.sprite = Picture;
                    img.raycastTarget = false;
                    _tiles.Add(img);
                }
                var tile = _tiles[i];
                tile.gameObject.SetActive(true);
                var left = start + n * width;
                var mirrored = n % 2 != 0;
                var rt = tile.rectTransform;
                rt.sizeDelta = new Vector2(width, height);
                rt.localScale = new Vector3(mirrored ? -1 : 1, 1, 1);
                rt.anchoredPosition = new Vector2(mirrored ? left + width : left, Bleed);
            }
            for (; i < _tiles.Count; i++) _tiles[i].gameObject.SetActive(false);
        }
    }
}
