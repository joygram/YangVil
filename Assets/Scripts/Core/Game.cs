using System;
using System.Collections.Generic;
using System.Linq;
using CatVillage.Data;

namespace CatVillage.Core
{
    public class Game
    {
        public const int PartySize = 3;

        public GameConfig Config { get; }
        public SaveData Data { get; private set; }
        public event Action Changed;

        public Game(GameConfig config)
        {
            Config = config;
            Data = SaveSystem.Load() ?? NewSave();
        }

        public static long Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        SaveData NewSave()
        {
            var data = new SaveData { lastCollectUnix = Now };
            Data = data;
            AddCat(Config.starterCat, true);
            data.baskets.Add(new Basket { speciesId = Config.starterBasket.id, hatchAtUnix = Now + Config.firstHatchSeconds });
            AddItem(Config.Item(ItemKind.Heal), 2);
            AddItem(Config.Item(ItemKind.Exp), 3);
            SaveSystem.Save(data);
            return data;
        }

        public void ResetSave()
        {
            SaveSystem.Delete();
            Data = NewSave();
            Commit();
        }

        public void Commit()
        {
            SaveSystem.Save(Data);
            Changed?.Invoke();
        }

        public CatData Species(OwnedCat cat) => Config.Cat(cat.speciesId);

        public IEnumerable<OwnedCat> Party => Data.cats.Where(c => c.inParty).Take(PartySize);

        OwnedCat AddCat(CatData species, bool inParty)
        {
            var cat = new OwnedCat { uid = $"c{Data.nextUid++}", speciesId = species.id, inParty = inParty };
            Data.cats.Add(cat);
            return cat;
        }

        public bool IsReady(Basket b) => Now >= b.hatchAtUnix;

        public OwnedCat Hatch(Basket b)
        {
            if (!IsReady(b)) return null;
            Data.baskets.Remove(b);
            var cat = AddCat(Config.Cat(b.speciesId), Party.Count() < PartySize);
            Commit();
            return cat;
        }

        public void AddBasket(CatData species)
        {
            Data.baskets.Add(new Basket { speciesId = species.id, hatchAtUnix = Now + Config.hatchSeconds });
        }

        public bool BuyBasket()
        {
            if (Data.coins < Config.basketPrice) return false;
            Data.coins -= Config.basketPrice;
            var pick = Config.cats[UnityEngine.Random.Range(0, Config.cats.Count)];
            AddBasket(pick);
            Commit();
            return true;
        }

        public bool BuyItem(ItemData item)
        {
            if (Data.coins < item.price) return false;
            Data.coins -= item.price;
            AddItem(item, 1);
            Commit();
            return true;
        }

        public int ItemCount(ItemData item) => Data.items.Find(s => s.itemId == item.id)?.count ?? 0;

        public void AddItem(ItemData item, int n)
        {
            if (item == null) return;
            var stack = Data.items.Find(s => s.itemId == item.id);
            if (stack == null) Data.items.Add(stack = new ItemStack { itemId = item.id });
            stack.count += n;
        }

        public bool ConsumeItem(ItemData item)
        {
            var stack = Data.items.Find(s => s.itemId == item.id);
            if (stack == null || stack.count <= 0) return false;
            stack.count--;
            return true;
        }

        public bool Feed(OwnedCat cat)
        {
            var treat = Config.Item(ItemKind.Exp);
            if (!ConsumeItem(treat)) return false;
            GainExp(cat, treat.value);
            Commit();
            return true;
        }

        public bool HatchNow(Basket b)
        {
            if (!ConsumeItem(Config.Item(ItemKind.HatchNow))) return false;
            b.hatchAtUnix = Now;
            Commit();
            return true;
        }

        public int ExpToNext(OwnedCat cat) => cat.level * Config.expPerLevel;

        public void GainExp(OwnedCat cat, int exp)
        {
            cat.exp += exp;
            while (cat.exp >= ExpToNext(cat))
            {
                cat.exp -= ExpToNext(cat);
                cat.level++;
            }
        }

        public bool ToggleParty(OwnedCat cat)
        {
            if (!cat.inParty && Party.Count() >= PartySize) return false;
            cat.inParty = !cat.inParty;
            Commit();
            return true;
        }

        public int PendingCoins
        {
            get
            {
                var minutes = (Now - Data.lastCollectUnix) / 60f;
                var rate = Data.cats.Sum(c => c.level) * Config.coinsPerLevelPerMinute;
                return Math.Min(Config.coinCap, (int)(minutes * rate));
            }
        }

        public int CollectCoins()
        {
            var n = PendingCoins;
            Data.coins += n;
            Data.lastCollectUnix = Now;
            Commit();
            return n;
        }

        public int Stars(StageData stage) => Data.stages.Find(r => r.stageId == stage.id)?.stars ?? 0;

        public bool IsUnlocked(int stageIndex) =>
            stageIndex == 0 || Stars(Config.stages[stageIndex - 1]) > 0;

        public BattleReward ApplyVictory(StageData stage, int stars)
        {
            var record = Data.stages.Find(r => r.stageId == stage.id);
            var firstClear = record == null;
            if (firstClear) Data.stages.Add(record = new StageRecord { stageId = stage.id });
            record.stars = Math.Max(record.stars, stars);

            var reward = new BattleReward { coins = stage.rewardCoins, treats = stage.rewardTreats, stars = stars };
            Data.coins += reward.coins;
            AddItem(Config.Item(ItemKind.Exp), reward.treats);
            foreach (var cat in Party) GainExp(cat, stage.enemyLevel * 20);
            if (firstClear && stage.firstClearBasket != null)
            {
                AddBasket(stage.firstClearBasket);
                reward.basket = stage.firstClearBasket;
            }
            Commit();
            return reward;
        }
    }

    public class BattleReward
    {
        public int coins;
        public int treats;
        public int stars;
        public CatData basket;
    }
}
