using System;
using System.Linq;
using CatVillage.Core;
using CatVillage.Data;
using CatVillage.World;
using TMPro;
using UnityEngine;

namespace CatVillage.UI
{
    public class VillageHud : GameScreen
    {
        public TMP_Text wallet;
        public LabeledButton hatchery;
        public LabeledButton shop;
        public LabeledButton breed;
        public LabeledButton dex;
        public LabeledButton adventure;
        public GameObject panel;
        public TMP_Text panelTitle;
        public RectTransform panelContent;
        public LabeledButton panelClose;
        public GameObject moveBanner;
        public TMP_Text moveLabel;
        public LabeledButton moveCancel;
        public Toast toast;
        public TMP_Text guideLabel;
        public LabeledButton guideGo;
        public CatCard catCardPrefab;
        public RowCard rowCardPrefab;
        public ShopSlot shopSlotPrefab;
        public LabeledButton tabButtonPrefab;

        enum ShopTab { Basket, Item, House }

        VillageWorld _world;
        Action _render;
        OwnedCat _breedFirst;
        ShopTab _shopTab;

        protected override void Build()
        {
            _world = FindAnyObjectByType<VillageWorld>();
            _world.BuildingTapped += OnBuilding;
            _world.HouseTapped += OnHouse;
            _world.CatTapped += OnCat;
            _world.MoveAttempted += OnMoveAttempted;

            hatchery.OnClick(() => OnBuilding(VillageWorld.Hatchery));
            shop.OnClick(() => OnBuilding(VillageWorld.Shop));
            breed.OnClick(() => OnBuilding(VillageWorld.BreedTower));
            dex.OnClick(() => OnBuilding(VillageWorld.Dex));
            adventure.OnClick(() => OnBuilding(VillageWorld.Gate));
            panelClose.OnClick(Close);
            guideGo.OnClick(FollowGuide);
            moveCancel.OnClick(CancelMove);
            panel.SetActive(false);
            moveBanner.SetActive(false);
            toast.gameObject.SetActive(false);
            Game.Changed += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            VillageCamera.Locked = false;
            var game = Screens == null ? null : Screens.Game;
            if (game != null) game.Changed -= Refresh;
            if (_world == null) return;
            _world.BuildingTapped -= OnBuilding;
            _world.HouseTapped -= OnHouse;
            _world.CatTapped -= OnCat;
            _world.MoveAttempted -= OnMoveAttempted;
            _world.EndMove();
        }

        void OnHouse(OwnedHouse h) => Open($"{Game.HouseDef(h).displayName} Lv{h.level}", () => RenderHouse(h));

        void OnCat(OwnedCat c) => Open(Game.Species(c).displayName, () => RenderCats(new[] { c }));

        void OnBuilding(string id)
        {
            switch (id)
            {
                case VillageWorld.Hatchery: Open("부화소", RenderHatchery); break;
                case VillageWorld.Shop: Open("상점", RenderShop); break;
                case VillageWorld.BreedTower: _breedFirst = null; Open("짝짓기 탑", RenderBreed); break;
                case VillageWorld.Dex: Open("도감", RenderDex); break;
                case VillageWorld.Gate: Screens.Show<StageSelectScreen>(); break;
            }
        }

        void Open(string title, Action render)
        {
            if (_world.Moving) return;
            panelTitle.text = title;
            _render = render;
            panel.SetActive(true);
            VillageCamera.Locked = true;
            Refresh();
        }

        void Close()
        {
            VillageCamera.Locked = false;
            panel.SetActive(false);
            _render = null;
        }

        void Refresh()
        {
            guideLabel.text = Guide.Next(Game).Text;
            wallet.text =$"생선 코인 {Game.Data.coins}   츄르 {Game.ItemCount(Game.Config.Item(ItemKind.Exp))}   담요 {Game.ItemCount(Game.Config.Item(ItemKind.HatchNow))}";
            if (_render == null) return;
            Ui.Clear(panelContent);
            _render();
        }

        float _nextGuide;

        void Update()
        {
            if (Screens == null) return;
            if (_render == RenderHatchery) RefreshTimers();
            if (Time.unscaledTime < _nextGuide) return;
            _nextGuide = Time.unscaledTime + 1f;
            guideLabel.text = Guide.Next(Game).Text;
        }

        void FollowGuide()
        {
            var step = Guide.Next(Game);
            switch (step.Target)
            {
                case GuideTarget.Hatchery: OnBuilding(VillageWorld.Hatchery); break;
                case GuideTarget.Shop: OnBuilding(VillageWorld.Shop); break;
                case GuideTarget.Collect: toast.Show($"코인 +{Game.CollectAndSave(step.House)}"); break;
                case GuideTarget.Party: Open("내 고양이", () => RenderCats(Game.Data.cats)); break;
                case GuideTarget.Adventure: OnBuilding(VillageWorld.Gate); break;
            }
        }

        RowCard Row(string label, string action, Action onClick)
        {
            var row = Instantiate(rowCardPrefab, panelContent);
            row.label.text = label;
            if (action == null) row.action.gameObject.SetActive(false);
            else
            {
                row.action.Text = action;
                row.action.OnClick(onClick);
            }
            return row;
        }

        void RenderHatchery()
        {
            Row($"부화 슬롯 {Game.Data.baskets.Count}/{Game.Config.hatcherySlots}", null, null);
            foreach (var b in Game.Data.baskets.ToArray())
            {
                var basket = b;
                var row = Row("", "", () =>
                {
                    if (Game.IsReady(basket)) toast.Show($"{Game.Species(Game.Hatch(basket)).displayName} 탄생! 집으로 옮겨 주세요");
                    else if (!Game.HatchNow(basket)) toast.Show("따뜻한 담요가 없어요");
                });
                row.name = $"basket:{Game.Data.baskets.IndexOf(basket)}";
            }
            var waiting = Game.Waiting.ToList();
            Row(waiting.Count > 0 ? $"집 없는 고양이 {waiting.Count}마리 → 이동을 눌러 집으로" : "집 없는 고양이 없음", null, null);
            RenderCats(waiting);
            RefreshTimers();
        }

        void RefreshTimers()
        {
            foreach (var row in panelContent.GetComponentsInChildren<RowCard>())
            {
                if (!row.name.StartsWith("basket:")) continue;
                var i = int.Parse(row.name.Substring(7));
                if (i >= Game.Data.baskets.Count) continue;
                var b = Game.Data.baskets[i];
                var left = b.hatchAtUnix - Game.Now;
                row.label.text = $"{Game.Config.Cat(b.speciesId).displayName} 바구니  {Ui.Clock(left)}";
                row.action.Text = left <= 0 ? "부화!" : "담요";
            }
        }

        void RenderHouse(OwnedHouse h)
        {
            var def = Game.HouseDef(h);
            var cats = Game.CatsIn(h).ToList();
            Row($"받는 속성: {def.AcceptLabel()}\n정원 {cats.Count}/{def.Capacity(h.level)} · 코인 {Game.Pending(h)}/{def.CoinCap(h.level)}",
                "코인 받기", () => toast.Show($"코인 +{Game.CollectAndSave(h)}"));
            var cost = def.UpgradeCost(h.level);
            if (cost > 0)
                Row($"업그레이드 Lv{h.level + 1}: 정원 {def.Capacity(h.level + 1)} · 상한 {def.CoinCap(h.level + 1)}  ({cost}코인)", "업그레이드",
                    () => toast.Show(Game.UpgradeHouse(h) ? "업그레이드 완료!" : "코인이 부족해요"));
            RenderCats(cats);
        }

        void RenderCats(System.Collections.Generic.IEnumerable<OwnedCat> cats)
        {
            foreach (var cat in cats)
            {
                var species = Game.Species(cat);
                var card = Instantiate(catCardPrefab, panelContent);
                BindUnit(card.portrait, species.sprites, species.tint, cat.genes);
                var s = species.StatsAt(cat.level);
                var home = Game.House(cat.houseUid);
                card.info.text =
                    $"{Game.DisplayName(cat)} [{Affinity.Label(species.element)}] Lv{cat.level} {CatData.StageLabel(cat.level)}\n" +
                    $"{Genetics.Describe(cat.genes)}\n" +
                    $"HP {s.hp} 공격 {s.atk} 방어 {s.def} 속도 {s.spd}\n" +
                    $"EXP {cat.exp}/{Game.ExpToNext(cat)} · {(home != null ? Game.HouseDef(home).displayName : "부화소 대기")}\n" +
                    $"{CatCare.Label(Game.Mood(cat))} · 배 {cat.fullness:0} · 청결 {cat.cleanliness:0}";
                var c = cat;
                card.feed.OnClick(() => { if (!Game.Feed(c)) toast.Show("츄르가 없어요"); });
                card.wash.OnClick(() =>
                {
                    Game.Wash(c);
                    toast.Show($"{Game.Species(c).displayName} 깨끗해졌어요!");
                });
                card.party.Text = cat.inParty ? "파티 해제" : "파티 편성";
                card.party.Highlight(cat.inParty);
                card.party.OnClick(() => { if (!Game.ToggleParty(c)) toast.Show($"파티는 {Game.PartySize}마리까지예요"); });
                card.move.OnClick(() => BeginMove(c));
            }
        }

        void BeginMove(OwnedCat cat)
        {
            Close();
            _world.BeginMove(cat);
            moveLabel.text = $"{Game.Species(cat).displayName}을(를) 옮길 집을 누르세요 (초록 = 가능)";
            moveBanner.SetActive(true);
        }

        void CancelMove()
        {
            _world.EndMove();
            moveBanner.SetActive(false);
        }

        void OnMoveAttempted(MoveResult result)
        {
            switch (result)
            {
                case MoveResult.Moved:
                    moveBanner.SetActive(false);
                    toast.Show("이사 완료!");
                    break;
                case MoveResult.WrongElement: toast.Show("이 집은 속성이 안 맞아요"); break;
                case MoveResult.Full: toast.Show("집이 꽉 찼어요"); break;
                default: toast.Show("이미 이 집에 살아요"); break;
            }
        }

        void RenderShop()
        {
            var tabs = new GameObject("ShopTabs", typeof(RectTransform)).GetComponent<RectTransform>();
            tabs.SetParent(panelContent, false);
            var row = tabs.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            row.spacing = 10;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = true;
            tabs.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 90;
            Tab(tabs, "바구니", ShopTab.Basket);
            Tab(tabs, "아이템", ShopTab.Item);
            Tab(tabs, "집", ShopTab.House);

            var grid = new GameObject("ShopGrid", typeof(RectTransform)).GetComponent<RectTransform>();
            grid.SetParent(panelContent, false);
            var layout = grid.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>();
            layout.cellSize = new Vector2(290, 400);
            layout.spacing = new Vector2(16, 16);
            layout.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 3;
            layout.childAlignment = TextAnchor.UpperCenter;

            switch (_shopTab)
            {
                case ShopTab.Basket:
                    Slot(grid, "털실 바구니", "무작위 속성 고양이", $"부화 슬롯 {Game.Data.baskets.Count}/{Game.Config.hatcherySlots}",
                        Game.Config.basketPrice, new Color(0.95f, 0.8f, 0.6f), Game.Config.presentation.basketIcon,
                        () => Game.BuyBasket() ? "바구니를 부화소에 넣었어요" : Game.FreeHatcherySlots <= 0 ? "부화 슬롯이 꽉 찼어요" : "코인이 부족해요");
                    break;
                case ShopTab.Item:
                    foreach (var item in Game.Config.items.Where(i => i.price > 0))
                    {
                        var it = item;
                        Slot(grid, item.displayName, Describe(item), $"보유 {Game.ItemCount(item)}", item.price, item.color, item.icon,
                            () => Game.BuyItem(it) ? $"{it.displayName} 구매!" : "코인이 부족해요");
                    }
                    break;
                default:
                    foreach (var house in Game.Config.houses.Where(h => h.price > 0))
                    {
                        var hd = house;
                        var count = Game.Data.houses.Count(h => h.houseId == hd.id);
                        Slot(grid, house.displayName, $"받는 속성: {house.AcceptLabel()}", count > 0 ? $"보유 {count}" : "", house.price, house.tint, house.sprite,
                            () => Game.BuyHouse(hd) ? $"{hd.displayName} 완성!" : Game.FreeVillageSlot() < 0 ? "빈 땅이 없어요" : "코인이 부족해요");
                    }
                    break;
            }
        }

        void Tab(Transform parent, string label, ShopTab tab)
        {
            var b = Instantiate(tabButtonPrefab, parent);
            b.Text = label;
            b.Highlight(_shopTab == tab);
            b.OnClick(() =>
            {
                _shopTab = tab;
                Refresh();
            });
        }

        void Slot(Transform grid, string name, string detail, string owned, int price, Color color,
            UnityEngine.AddressableAssets.AssetReferenceTexture2D icon, Func<string> buy)
        {
            var slot = Instantiate(shopSlotPrefab, grid);
            slot.title.text = name;
            slot.detail.text = detail;
            slot.owned.text = owned;
            slot.buy.Text = $"{price} 코인";
            slot.buy.Interactable = Game.Data.coins >= price;
            slot.SetPlaceholder(name, color);
            if (PresentationSet.IsSet(icon)) StartCoroutine(Assets.LoadSprite(icon, new Vector2(0.5f, 0.5f), 100, slot.SetIcon));
            slot.buy.OnClick(() => toast.Show(buy()));
        }

        void RenderBreed()
        {
            Row(_breedFirst == null
                    ? $"첫 번째 고양이를 고르세요 ({Game.Config.breedPrice}코인, {Game.Config.breedSeconds / 60}분)"
                    : $"{Game.Species(_breedFirst).displayName} + 두 번째 고양이를 고르세요", null, null);
            foreach (var cat in Game.Data.cats)
            {
                if (cat == _breedFirst) continue;
                var c = cat;
                Row($"{Game.DisplayName(cat)} Lv{cat.level} [{Affinity.Label(Game.Species(cat).element)}]\n{Genetics.Describe(cat.genes)}", "선택", () =>
                {
                    if (_breedFirst == null)
                    {
                        _breedFirst = c;
                        Refresh();
                        return;
                    }
                    var ok = Game.Breed(_breedFirst, c);
                    toast.Show(ok ? "바구니가 부화소에 들어갔어요" : Game.FreeHatcherySlots <= 0 ? "부화 슬롯이 꽉 찼어요" : "코인이 부족해요");
                    _breedFirst = null;
                    Refresh();
                });
            }
        }

        void RenderDex()
        {
            var found = Game.Config.cats.Count(s => Game.Data.discovered.Contains(s.id));
            Row($"수집 {found}/{Game.Config.cats.Count}", null, null);
            foreach (var species in Game.Config.cats)
            {
                var known = Game.Data.discovered.Contains(species.id);
                Row(known ? $"{species.displayName} [{Affinity.Label(species.element)}] · {species.skillName}" : "??? (미발견)", null, null);
            }
            var outliers = new[] { Outlier.Grandpa, Outlier.Rainbow, Outlier.Ghost, Outlier.Giant };
            Row($"특이 개체 {outliers.Count(o => Game.Data.discovered.Contains($"outlier:{o}"))}/{outliers.Length} (짝짓기로 낮은 확률 탄생)", null, null);
            foreach (var o in outliers)
                Row(Game.Data.discovered.Contains($"outlier:{o}") ? $"★ {Genetics.OutlierName(o)}" : "★ ??? (미발견)", null, null);
        }

        public static string Describe(ItemData item) => item.kind switch
        {
            ItemKind.Heal => $"전투: 가장 아픈 아군 HP {item.value}% 회복",
            ItemKind.Revive => $"전투: 쓰러진 아군 HP {item.value}%로 부활",
            ItemKind.GaugeFill => "전투: 아군 1마리 필살 게이지 가득",
            ItemKind.Exp => $"육성: EXP +{item.value}",
            _ => "육성: 부화 즉시 완료"
        };
    }
}
