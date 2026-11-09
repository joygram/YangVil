using System.Collections.Generic;
using CatVillage.Core;
using CatVillage.Data;
using TMPro;
using UnityEngine;

namespace CatVillage.UI
{
    public class VillageScreen : GameScreen
    {
        enum Tab { Cats, Baskets, Shop }

        public TMP_Text wallet;
        public LabeledButton collect;
        public LabeledButton tabCats;
        public LabeledButton tabBaskets;
        public LabeledButton tabShop;
        public RectTransform listContent;
        public LabeledButton adventure;
        public Toast toast;
        public CatCard catCardPrefab;
        public RowCard rowCardPrefab;

        Tab _tab = Tab.Cats;
        readonly List<(Basket basket, RowCard card)> _basketRows = new List<(Basket, RowCard)>();

        protected override void Build()
        {
            collect.OnClick(() => toast.Show($"코인 +{Game.CollectCoins()}"));
            tabCats.OnClick(() => SetTab(Tab.Cats));
            tabBaskets.OnClick(() => SetTab(Tab.Baskets));
            tabShop.OnClick(() => SetTab(Tab.Shop));
            adventure.OnClick(() => Screens.Show<StageSelectScreen>());
            toast.gameObject.SetActive(false);
            Game.Changed += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (Screens != null) Game.Changed -= Refresh;
        }

        void SetTab(Tab tab)
        {
            _tab = tab;
            Refresh();
        }

        void Update()
        {
            collect.Text = $"코인 받기 +{Game.PendingCoins}";
            foreach (var (basket, card) in _basketRows)
            {
                var left = basket.hatchAtUnix - Game.Now;
                card.label.text = $"{Game.Config.Cat(basket.speciesId).displayName} 바구니  {Ui.Clock(left)}";
                card.action.Text = left <= 0 ? "부화!" : "담요";
            }
        }

        void Refresh()
        {
            wallet.text = $"생선 코인 {Game.Data.coins}   츄르 {Game.ItemCount(Game.Config.Item(ItemKind.Exp))}";
            tabCats.Highlight(_tab == Tab.Cats);
            tabBaskets.Highlight(_tab == Tab.Baskets);
            tabShop.Highlight(_tab == Tab.Shop);
            _basketRows.Clear();
            Ui.Clear(listContent);
            switch (_tab)
            {
                case Tab.Cats: BuildCats(); break;
                case Tab.Baskets: BuildBaskets(); break;
                default: BuildShop(); break;
            }
        }

        void BuildCats()
        {
            foreach (var cat in Game.Data.cats)
            {
                var species = Game.Species(cat);
                var card = Instantiate(catCardPrefab, listContent);
                card.portrait.Bind(species.sprites, species.tint);
                card.portrait.SetScale(0.75f + 0.125f * CatData.GrowthStage(cat.level));
                var s = species.StatsAt(cat.level);
                card.info.text =
                    $"{species.displayName} [{Affinity.Label(species.element)}] Lv{cat.level} {CatData.StageLabel(cat.level)}\n" +
                    $"HP {s.hp}  공격 {s.atk}  방어 {s.def}  속도 {s.spd}\nEXP {cat.exp}/{Game.ExpToNext(cat)}";
                var c = cat;
                card.feed.OnClick(() =>
                {
                    if (!Game.Feed(c)) toast.Show("츄르가 없어요");
                });
                card.party.Text = cat.inParty ? "파티 해제" : "파티 편성";
                card.party.Highlight(cat.inParty);
                card.party.OnClick(() =>
                {
                    if (!Game.ToggleParty(c)) toast.Show($"파티는 {Game.PartySize}마리까지예요");
                });
            }
        }

        void BuildBaskets()
        {
            if (Game.Data.baskets.Count == 0)
            {
                var empty = Instantiate(rowCardPrefab, listContent);
                empty.label.text = "바구니가 없어요. 상점이나 모험 보상으로 얻어요.";
                empty.action.gameObject.SetActive(false);
                return;
            }
            foreach (var basket in Game.Data.baskets.ToArray())
            {
                var card = Instantiate(rowCardPrefab, listContent);
                var b = basket;
                card.action.OnClick(() => OnBasket(b));
                _basketRows.Add((basket, card));
            }
        }

        void OnBasket(Basket b)
        {
            if (Game.IsReady(b)) toast.Show($"{Game.Species(Game.Hatch(b)).displayName} 탄생!");
            else if (!Game.HatchNow(b)) toast.Show("따뜻한 담요가 없어요");
        }

        void BuildShop()
        {
            var basket = Instantiate(rowCardPrefab, listContent);
            basket.label.text = $"털실 바구니 (무작위 속성)  {Game.Config.basketPrice}코인";
            basket.action.Text = "구매";
            basket.action.OnClick(() => toast.Show(Game.BuyBasket() ? "바구니 구매!" : "코인이 부족해요"));

            foreach (var item in Game.Config.items)
            {
                if (item.price <= 0) continue;
                var card = Instantiate(rowCardPrefab, listContent);
                card.label.text = $"{item.displayName} (보유 {Game.ItemCount(item)})  {item.price}코인\n{Describe(item)}";
                card.action.Text = "구매";
                var it = item;
                card.action.OnClick(() => toast.Show(Game.BuyItem(it) ? $"{it.displayName} 구매!" : "코인이 부족해요"));
            }
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
