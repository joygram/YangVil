using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YangVillage.Core;

// Godot YangVillage game/scripts/ui/StageSelect.cs·MapCanvas.cs 이식. screen_id = stage_select
namespace YangVillage.Ui
{
    // 모험 지도: 굽은 길 위 거점 노드 + 아래 상세 카드.
    public class StageSelect : MonoBehaviour
    {
        const float StepY = 130;
        const float HeaderY = 70;
        const float NodeSize = 84;
        const float BossNodeSize = 112;
        const float Swing = 150;

        Game G => App.Game;
        List<string[]> _stages;
        int _selected;
        TextMeshProUGUI _progress, _title, _detail;
        Button _depart;
        readonly List<RectTransform> _nodes = new List<RectTransform>();

        string Id(int index) => App.Db.Cell("stage", _stages[index], "stage_id");
        string Region(int index) => App.Db.Cell("stage", _stages[index], "region_id");

        void Start()
        {
            var root = (RectTransform)transform;
            Kit.Root(root, Kit.Grass);
            _stages = G.Stages;
            var open = Enumerable.Range(0, _stages.Count).Count(G.IsUnlocked);
            _selected = Math.Max(0, open - 1);

            var column = Kit.Column(root, 14, 8);
            _progress = Kit.Label(column, "", 20);

            // 스크롤 영역 (세로만).
            var scrollRect = Kit.Node("scroll", column);
            Kit.Layout(scrollRect).flexibleHeight = 1;
            scrollRect.gameObject.AddComponent<RectMask2D>();
            scrollRect.gameObject.AddComponent<Image>().color = Color.clear;
            var scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30;
            var map = Kit.Node("map", scrollRect);
            map.anchorMin = new Vector2(0, 1);
            map.anchorMax = Vector2.one;
            map.pivot = new Vector2(0.5f, 1);
            scroll.content = map;
            var road = PaintNode.Of<MapRoad>(map);
            var roadNode = road.GetComponent<Node2>();
            roadNode.AnchorTopLeft();
            road.OpenCount = open;

            var width = root.rect.width - 28;
            var y = 20f;
            var selectedY = 0f;
            for (var i = 0; i < _stages.Count; i++)
            {
                if (i == 0 || Region(i) != Region(i - 1))
                {
                    var header = Kit.Label(map, App.T(App.Db.Get("region", "region_id", Region(i), "text_id")), 26);
                    TopLeft(header.rectTransform, new Vector2(0, y), new Vector2(width, HeaderY - 10));
                    y += HeaderY;
                }
                var last = i == _stages.Count - 1 || Region(i + 1) != Region(i);
                var size = last ? BossNodeSize : NodeSize;
                var center = new Vector2(width / 2 + Mathf.Sin(i * 1.15f) * Swing, y + BossNodeSize / 2);
                road.Points.Add(center);
                if (i == _selected) selectedY = y;
                Node(map, i, center, size, last);
                y += StepY;
            }
            map.offsetMin = new Vector2(0, -(y + 20));
            map.offsetMax = Vector2.zero;
            road.Redraw();

            var card = Kit.Card(column);
            _title = Kit.Label(card, "", 24);
            _detail = Kit.Label(card, "", 17);
            _depart = Kit.Button(card, "", Depart, 26);
            Kit.Button(column, App.T("ui_home"), () => App.Go("village"), 18);

            Select(_selected);
            Canvas.ForceUpdateCanvases();
            var view = scrollRect.rect.height;
            var range = map.rect.height - view;
            if (range > 0) scroll.verticalNormalizedPosition = 1 - Mathf.Clamp01(Math.Max(0, selectedY - 200) / range);
        }

        // Godot Control: 왼쪽 위 기준 자리.
        static void TopLeft(RectTransform r, Vector2 position, Vector2 size)
        {
            r.anchorMin = r.anchorMax = new Vector2(0, 1);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = size;
            r.anchoredPosition = new Vector2(position.x + size.x / 2, -(position.y + size.y / 2));
        }

        void Node(RectTransform map, int index, Vector2 center, float size, bool last)
        {
            var unlocked = G.IsUnlocked(index);
            var stars = G.Stars(Id(index));
            var node = Kit.Node("stage", map);
            TopLeft(node, center - new Vector2(size, size) / 2, new Vector2(size, size));
            var img = node.gameObject.AddComponent<Image>();
            img.sprite = Sprites.Frames(!unlocked ? "sprite_map_locked" : stars > 0 ? "sprite_map_complete" : "sprite_map_current")[0];
            img.preserveAspect = true;
            var button = node.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            var captured = index;
            button.onClick.AddListener(() => Select(captured));

            var mark = Kit.Label(node, App.T("ui_stage_number", index + 1), 16, Kit.Paper);
            Kit.Outline(mark, Kit.Ink, 6);
            Child(mark.rectTransform, new Vector2(-size * 0.2f, -6), new Vector2(size * 0.5f, 22));
            if (last)
            {
                var flag = SpriteView.Of("sprite_flag", size * 0.6f, node);
                Child(flag.Rect, new Vector2(size * 0.6f, -size * 0.35f), new Vector2(size * 0.6f, size * 0.6f));
            }
            if (stars > 0)
            {
                var label = Kit.Label(node, Kit.Stars(stars), 18, Kit.AccentDark);
                Child(label.rectTransform, new Vector2(-20, size - 6), new Vector2(size + 40, 22));
            }
            _nodes.Add(node);
        }

        // 노드 안 Godot 자식 자리 (노드 왼쪽 위 기준).
        static void Child(RectTransform r, Vector2 position, Vector2 size)
        {
            r.anchorMin = r.anchorMax = new Vector2(0, 1);
            r.pivot = new Vector2(0, 1);
            r.sizeDelta = size;
            r.anchoredPosition = new Vector2(position.x, -position.y);
            Kit.Layout(r).ignoreLayout = true;
        }

        void Select(int index)
        {
            _selected = index;
            for (var i = 0; i < _nodes.Count; i++) _nodes[i].localScale = i == index ? new Vector3(1.12f, 1.12f, 1) : Vector3.one;
            var db = App.Db;
            var id = Id(index);
            var cleared = _stages.Count(r => G.Stars(db.Cell("stage", r, "stage_id")) > 0);
            _progress.text = App.T("ui_stage_progress", App.T(db.Get("region", "region_id", Region(index), "text_id")), cleared, _stages.Count);

            if (!G.IsUnlocked(index))
            {
                _title.text = App.T("ui_stage_locked");
                _detail.text = App.T("ui_stage_locked_detail");
                Kit.ButtonText(_depart).text = App.T("ui_depart_locked");
                Kit.SetDisabled(_depart, true);
                return;
            }
            var enemies = db.Where("stage_enemy", "stage_id", id).Count();
            var waves = (enemies + db.ConfigInt("wave_size") - 1) / db.ConfigInt("wave_size");
            var boss = App.T(db.Get("enemy", "enemy_id", db.Cell("stage", _stages[index], "boss_enemy_id"), "text_id"));
            _title.text = G.StageName(id) + "  " + Kit.Stars(G.Stars(id));
            _detail.text = App.T("ui_stage_detail", waves, db.Cell("stage", _stages[index], "level"), boss, G.RewardCoins(id), db.ConfigInt("reward_treats"))
                + (G.Stars(id) == 0 ? App.T("ui_first_basket") : "");
            var ready = G.Party.Any();
            Kit.ButtonText(_depart).text = App.T(ready ? "ui_depart" : "ui_depart_no_party");
            Kit.SetDisabled(_depart, !ready);
        }

        void Depart()
        {
            App.StageId = Id(_selected);
            App.Go("battle");
        }
    }

    // 모험 지도의 굽은 길.
    public class MapRoad : Paint
    {
        const float RoadWidth = 18;
        public readonly List<Vector2> Points = new List<Vector2>();
        public int OpenCount;

        protected override void Draw()
        {
            for (var i = 0; i + 1 < Points.Count; i++)
            {
                DrawLine(Points[i], Points[i + 1], Kit.Ink, RoadWidth + 6);
                DrawLine(Points[i], Points[i + 1], i + 1 < OpenCount ? Kit.Cream : Kit.Muted, RoadWidth);
            }
        }
    }
}
