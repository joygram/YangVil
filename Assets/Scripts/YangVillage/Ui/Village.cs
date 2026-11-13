using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using YangVillage.Core;

// Godot YangVillage game/scripts/ui/Village.cs (scenes/village.tscn) 이식. 씬: Assets/Scenes/CatYard.unity
namespace YangVillage.Ui
{
    // 마을: 화면 전체가 마당(고양이 배회). 위에 재화·가이드, 아래에 메뉴 줄, 나머지는 메뉴에서 여는 창.
    public class Village : MonoBehaviour
    {
        const float CatSize = 240;
        const float MenuHeight = 104;
        const float IconSize = 52;
        static readonly Vector2 Viewport = new Vector2(540, 960);

        // 모험 = Godot stage_select (미이식) → 기존 Unity 흐름 입구로 보냄.
        public string adventureScene = "Intro";
        public TMP_FontAsset font;

        Game G => App.Game;
        RectTransform _root, _yard, _popup;
        TextMeshProUGUI _coins, _treats, _guide;
        Button _hatcheryButton, _catsButton;
        Action _reopen;
        float _tick;
        bool _built;

        void Awake()
        {
            Kit.Font = font;
            if (EventSystem.current == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = Viewport;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            _root = (RectTransform)canvasGo.transform;
            Build();
        }

        void Build()
        {
            Kit.Rect(_root, Kit.Grass);
            _yard = Kit.Node("yard", _root);
            Kit.Fill(_yard);
            _yard.gameObject.AddComponent<RectMask2D>();

            var column = Kit.Column(_root, 12, 8);

            var top = Kit.Panel(column, Kit.Alpha(Kit.Paper, 0.92f), Kit.Ink, 16);
            Kit.VBox((RectTransform)top.transform, 0, 8);
            var bar = Kit.Row(top.transform, 10);
            SpriteView.Of("sprite_reward_coin", 36, bar);
            _coins = Kit.Label(bar, "", 22);
            _treats = Kit.Label(bar, "", 22);
            Kit.Expand(_coins);
            Kit.Expand(_treats);

            var guide = Kit.Panel(column, Kit.Alpha(Kit.Night, 0.88f), Kit.Gold, 14);
            Kit.VBox((RectTransform)guide.transform, 0, 10);
            _guide = Kit.Label(guide.transform, "", 18, Kit.Paper);

            Kit.Spacer(column);

            var menu = Kit.Row(column, 8);
            _hatcheryButton = MenuButton(menu, "sprite_basket", "ui_menu_hatchery", () => Open(Hatchery), false);
            MenuButton(menu, "sprite_reward_coin", "ui_menu_shop", () => Open(Shop), false);
            _catsButton = MenuButton(menu, "sprite_food_bowl", "ui_menu_cats", () => Open(Cats), false);
            MenuButton(menu, "sprite_pawprint", "ui_menu_adventure", () => SceneManager.LoadScene(adventureScene), true);

            // Godot 은 마당 크기가 정해진 뒤(deferred) 첫 갱신 — 여기선 Start 에서.
        }

        void Start()
        {
            Canvas.ForceUpdateCanvases();
            _built = true;
            Refresh();
        }

        // 메뉴 칸: 그림 위, 이름 아래. 모험만 강조색.
        Button MenuButton(Transform parent, string spriteId, string textId, Action onPress, bool primary)
        {
            var button = primary
                ? Kit.Button(parent, App.T(textId), onPress, 22)
                : Kit.Button(parent, App.T(textId), onPress, 18, Kit.Paper, Kit.Cream);
            Kit.ButtonIcon(button, Sprites.Frames(spriteId)[0], IconSize);
            ((VerticalLayoutGroup)button.GetComponent<LayoutGroup>()).childAlignment = TextAnchor.UpperCenter;
            var le = Kit.Layout(button);
            le.minHeight = le.preferredHeight = MenuHeight;
            le.flexibleWidth = primary ? 1.3f : 1f;
            le.minWidth = 0;
            le.preferredWidth = 0;
            return button;
        }

        void Update()
        {
            if (!_built) return;
            SortYard();
            _tick += Time.deltaTime;
            if (_tick < 1) return;
            _tick = 0;
            Refresh(false);
        }

        // Godot ZIndex = 발밑 y. UGUI 는 형제 순서로.
        void SortYard()
        {
            var items = _yard.Cast<Transform>().Skip(1)
                .Select(t => (t, y: t.TryGetComponent<CatWalker>(out var w) ? w.SortY : -((RectTransform)t).anchoredPosition.y))
                .OrderBy(p => p.y).ToList();
            for (var i = 0; i < items.Count; i++) items[i].t.SetSiblingIndex(i + 1);
        }

        void Say(string textId, params object[] args)
        {
            Refresh();
            if (textId != null) _guide.text = App.T(textId, args);
        }

        // 지금 할 일 한 줄.
        string Guide()
        {
            if (G.Data.baskets.Any(G.IsReady)) return "ui_guide_hatch";
            if (G.Data.cats.Count == 0) return G.Data.baskets.Count == 0 ? "ui_guide_basket" : "ui_guide_wait";
            return G.Data.cats.Any(c => c.inParty) ? "ui_guide_adventure" : "ui_guide_party";
        }

        void Refresh(bool yard = true)
        {
            _coins.text = App.T("ui_coins", G.Data.coins);
            _treats.text = App.T("ui_treats", G.Data.treats);
            _guide.text = App.T(Guide());
            Kit.ButtonText(_hatcheryButton).text = App.T("ui_menu_badge", App.T("ui_menu_hatchery"), G.Data.baskets.Count);
            Kit.ButtonText(_catsButton).text = App.T("ui_menu_badge", App.T("ui_menu_cats"), G.Data.cats.Count);
            if (yard) RefreshYard();
            if (_reopen != null && (yard || _reopen == (Action)Hatchery)) Open(_reopen);
        }

        // 메뉴 창: 제목 + 내용 + 닫기. 내용이 바뀌면 같은 창을 다시 그린다.
        Transform Sheet(string title)
        {
            if (_popup != null) Destroy(_popup.gameObject);
            _popup = Kit.Node("popup", _root);
            Kit.Fill(_popup);
            Kit.Rect(_popup, Kit.Alpha(Kit.Night, 0.6f), true);
            var panel = Kit.Panel(_popup, Kit.Paper, Kit.Ink, 16);
            var r = (RectTransform)panel.transform;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            Kit.VBox(r, 10, 14);
            var le = Kit.Layout(panel);
            le.preferredWidth = 460;
            le.layoutPriority = 2;
            var fit = panel.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            if (title != null) Kit.Label(panel.transform, title, 24);
            return panel.transform;
        }

        void Close()
        {
            if (_popup != null) Destroy(_popup.gameObject);
            _popup = null;
            _reopen = null;
        }

        void Open(Action sheet)
        {
            _reopen = sheet;
            sheet();
        }

        void Hatchery()
        {
            var body = Sheet(App.T("ui_hatchery", G.Data.baskets.Count, App.Db.ConfigInt("hatchery_slots")));
            if (G.Data.baskets.Count == 0) Kit.Label(body, App.T("ui_hatchery_empty"), 18, Kit.Muted);
            foreach (var basket in G.Data.baskets.ToList())
            {
                var row = Kit.Row(body);
                SpriteView.Of("sprite_basket", 64, row);
                var owned = basket;
                var left = basket.hatchAtUnix - G.Now;
                var label = Kit.Label(row, G.IsReady(basket) ? App.T("ui_basket_ready") : App.T("ui_basket_wait", $"{left / 60}:{left % 60:00}"), 18);
                Kit.Expand(label);
                if (G.IsReady(basket))
                {
                    var hatch = Kit.Button(row, App.T("ui_hatch"), () =>
                    {
                        var cat = G.Hatch(owned);
                        Say(cat != null ? "ui_hatched" : null, cat != null ? G.CatName(cat) : "");
                    }, 20);
                    Kit.Layout(hatch).flexibleWidth = 0;
                }
            }
            Kit.Button(body, App.T("ui_buy_basket", App.Db.ConfigInt("basket_price")), () => Say(G.BuyBasket() ? null : "ui_cant_buy"));
            Kit.Button(body, App.T("ui_close"), Close);
        }

        void Shop()
        {
            var body = Sheet(App.T("ui_menu_shop"));
            Kit.Label(body, App.T("ui_coins", G.Data.coins) + "  ·  " + App.T("ui_treats", G.Data.treats), 18, Kit.AccentDark);
            Kit.Button(body, App.T("ui_buy_basket", App.Db.ConfigInt("basket_price")), () => Say(G.BuyBasket() ? null : "ui_cant_buy"));
            Kit.Button(body, App.T("ui_buy_treat", App.Db.ConfigInt("treat_price")), () => Say(G.BuyTreat() ? null : "ui_cant_buy"));
            Kit.Button(body, App.T("ui_close"), Close);
        }

        void Cats()
        {
            var body = Sheet(App.T("ui_cat_list") + "  ·  " + App.T("ui_cats", G.Data.cats.Count));
            var scrollRect = Kit.Node("scroll", body);
            Kit.Layout(scrollRect).minHeight = Kit.Layout(scrollRect).preferredHeight = 520;
            scrollRect.gameObject.AddComponent<RectMask2D>();
            var hit = scrollRect.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            var scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30;
            var list = Kit.Node("list", scrollRect);
            list.anchorMin = new Vector2(0, 1);
            list.anchorMax = Vector2.one;
            list.pivot = new Vector2(0.5f, 1);
            list.offsetMin = list.offsetMax = Vector2.zero;
            Kit.VBox(list, 8);
            list.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = list;
            foreach (var cat in G.Data.cats.OrderByDescending(c => c.inParty).ThenByDescending(c => c.level))
            {
                var line = App.T("ui_cat_line", G.CatName(cat), cat.level, G.StageLabel(cat.level), G.ElementName(G.CatElement(cat)));
                var owned = cat;
                var button = Kit.Button(list, (cat.inParty ? App.T("ui_party_in") + "  " : "") + line, () => OpenCard(owned), 18);
                var group = button.GetComponent<VerticalLayoutGroup>();
                // Godot 버튼 아이콘 = 글 왼쪽. 가로 줄로 바꿔 끼움.
                DestroyImmediate(group);
                var h = button.gameObject.AddComponent<HorizontalLayoutGroup>();
                h.spacing = 8;
                h.padding = new RectOffset(10, 10, 4, 4);
                h.childControlWidth = h.childControlHeight = true;
                h.childForceExpandWidth = h.childForceExpandHeight = false;
                h.childAlignment = TextAnchor.MiddleLeft;
                Kit.ButtonIcon(button, Sprites.Frames(G.CatSprite(cat.catId))[0], 64);
                Kit.Expand(Kit.ButtonText(button));
                Kit.Layout(button).minHeight = 72;
            }
            Kit.Button(body, App.T("ui_close"), Close);
        }

        void RefreshYard()
        {
            foreach (Transform child in _yard) Destroy(child.gameObject);
            _yard.DetachChildren();
            var area = _yard.rect.size;
            var ground = SpriteView.Of("sprite_village_courtyard", area.y, _yard);
            Kit.Fill(ground.Rect);
            var fit = ground.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            var s = ground.Image.sprite.rect.size;
            fit.aspectRatio = s.x / s.y;
            ground.Image.preserveAspect = false;
            Prop("sprite_food_bowl", 0.3f, 0.46f, 96);
            Prop("sprite_yarn_ball", 0.66f, 0.68f, 72);

            var bounds = new Rect(area.x * 0.04f, area.y * 0.34f, area.x * 0.92f, area.y * 0.5f);
            foreach (var cat in G.Data.cats.OrderByDescending(c => c.inParty).Take(App.Db.ConfigInt("village_slots")))
            {
                var size = CatSize * App.Db.ConfigInt("growth_scale_pct_" + G.GrowthStage(cat.level)) / 100f;
                var owned = cat;
                var walker = CatWalker.Create(_yard, G.CatSprite(cat.catId), size, bounds);
                walker.Tapped = () => OpenCard(owned);
                walker.Place();
            }
        }

        void Prop(string spriteId, float x, float y, float size)
        {
            var prop = SpriteView.Of(spriteId, size, _yard);
            prop.Rect.anchorMin = prop.Rect.anchorMax = prop.Rect.pivot = new Vector2(0, 1);
            var area = _yard.rect.size;
            prop.Rect.anchoredPosition = new Vector2(area.x * x, -area.y * y);
        }

        // 고양이 카드
        void OpenCard(OwnedCat cat)
        {
            _reopen = () => OpenCard(cat);
            var body = Sheet(null);

            var portrait = SpriteView.Of(G.CatSprite(cat.catId), 180, Kit.Row(body));
            var icon = Sprites.ElementIcon(G.CatElement(cat), 48);
            if (icon != null)
            {
                icon.transform.SetParent(portrait.transform, false);
                icon.Rect.anchorMin = icon.Rect.anchorMax = icon.Rect.pivot = new Vector2(0, 1);
                icon.Rect.anchoredPosition = Vector2.zero;
                Kit.Layout(icon).ignoreLayout = true;
            }
            Kit.Label(body, App.T("ui_cat_line", G.CatName(cat), cat.level, G.StageLabel(cat.level), G.ElementName(G.CatElement(cat))), 20);
            var need = G.ExpToNext(cat);
            Kit.Label(body, need > 0 ? App.T("ui_cat_exp", cat.exp, need) : App.T("ui_cat_max"), 16, Kit.Muted);
            var note = Kit.Label(body, App.T("ui_treats", G.Data.treats), 16, Kit.AccentDark);

            Kit.Button(body, App.T("ui_feed"), () =>
            {
                if (!G.Feed(cat)) { note.text = App.T("ui_no_treats"); return; }
                Refresh();
            });
            Kit.Button(body, App.T(cat.inParty ? "ui_party_in" : "ui_party_out"), () =>
            {
                if (!G.ToggleParty(cat)) { note.text = App.T("ui_party_full", G.PartySize); return; }
                Refresh();
            });
            Kit.Button(body, App.T("ui_close"), Close);
        }
    }
}
