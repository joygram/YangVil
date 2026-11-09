using System;
using System.Collections.Generic;
using System.Linq;
using CatVillage.Data;

namespace CatVillage.Core
{
    public enum MoveResult { Moved, WrongElement, Full, Same }

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
            if (Data.version < SaveData.CurrentVersion) Upgrade();
        }

        void Upgrade()
        {
            Data.cats.RemoveAll(c => Config.Cat(c.speciesId) == null);
            Data.houses.RemoveAll(h => Config.House(h.houseId) == null);
            if (Data.houses.Count == 0) AddHouse(Config.starterHouse, 0);
            foreach (var cat in Data.cats)
            {
                if (cat.genes == null || cat.genes.seed == 0) cat.genes = Genetics.FromSpecies(Species(cat), _rng);
                if (cat.careUnix == 0) cat.careUnix = Now;
                if (Data.houses.All(h => h.uid != cat.houseUid)) cat.houseUid = Data.houses[0].uid;
            }
            foreach (var b in Data.baskets)
                if (b.genes == null) b.genes = new CatGenes();
            Data.version = SaveData.CurrentVersion;
            SaveSystem.Save(Data);
        }

        public static long Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        SaveData NewSave()
        {
            var data = new SaveData();
            Data = data;
            var house = AddHouse(Config.starterHouse, 0);
            var cat = AddCat(Config.starterCat, true);
            cat.houseUid = house.uid;
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

        readonly System.Random _rng = new System.Random();

        OwnedCat AddCat(CatData species, bool inParty, CatGenes genes = null)
        {
            var cat = new OwnedCat
            {
                uid = $"c{Data.nextUid++}", speciesId = species.id, inParty = inParty, careUnix = Now,
                genes = genes ?? Genetics.FromSpecies(species, _rng)
            };
            Data.cats.Add(cat);
            if (!Data.discovered.Contains(species.id)) Data.discovered.Add(species.id);
            var outlier = $"outlier:{cat.genes.outlier}";
            if (cat.genes.outlier != Outlier.None && !Data.discovered.Contains(outlier)) Data.discovered.Add(outlier);
            return cat;
        }

        public string DisplayName(OwnedCat cat)
        {
            var o = Genetics.OutlierName(cat.genes.outlier);
            return string.IsNullOrEmpty(o) ? Species(cat).displayName : $"{o}({Species(cat).displayName})";
        }

        public HouseData HouseDef(OwnedHouse h) => Config.House(h.houseId);

        public OwnedHouse House(string uid) => Data.houses.Find(h => h.uid == uid);

        public IEnumerable<OwnedCat> CatsIn(OwnedHouse h) => Data.cats.Where(c => c.houseUid == h.uid);

        public IEnumerable<OwnedCat> Waiting => Data.cats.Where(c => string.IsNullOrEmpty(c.houseUid));

        OwnedHouse AddHouse(HouseData def, int slot)
        {
            var house = new OwnedHouse { uid = $"h{Data.nextUid++}", houseId = def.id, slot = slot, lastCollectUnix = Now };
            Data.houses.Add(house);
            return house;
        }

        public int FreeVillageSlot()
        {
            for (var i = 0; i < Config.villageSlots; i++)
                if (Data.houses.All(h => h.slot != i)) return i;
            return -1;
        }

        public bool BuyHouse(HouseData def)
        {
            var slot = FreeVillageSlot();
            if (slot < 0 || Data.coins < def.price) return false;
            Data.coins -= def.price;
            AddHouse(def, slot);
            Commit();
            return true;
        }

        public bool UpgradeHouse(OwnedHouse h)
        {
            var cost = HouseDef(h).UpgradeCost(h.level);
            if (cost < 0 || Data.coins < cost) return false;
            Collect(h);
            Data.coins -= cost;
            h.level++;
            Commit();
            return true;
        }

        public bool CanMoveTo(OwnedCat cat, OwnedHouse h) => Check(cat, h) == MoveResult.Moved;

        MoveResult Check(OwnedCat cat, OwnedHouse h)
        {
            if (cat.houseUid == h.uid) return MoveResult.Same;
            if (!HouseDef(h).Accepts(Species(cat).element)) return MoveResult.WrongElement;
            if (CatsIn(h).Count() >= HouseDef(h).Capacity(h.level)) return MoveResult.Full;
            return MoveResult.Moved;
        }

        public MoveResult MoveCat(OwnedCat cat, OwnedHouse h)
        {
            var result = Check(cat, h);
            if (result != MoveResult.Moved) return result;
            var from = House(cat.houseUid);
            if (from != null) Collect(from);
            Collect(h);
            cat.houseUid = h.uid;
            Commit();
            return result;
        }

        public bool IsReady(Basket b) => Now >= b.hatchAtUnix;

        public int FreeHatcherySlots => Config.hatcherySlots - Data.baskets.Count;

        public OwnedCat Hatch(Basket b)
        {
            if (!IsReady(b)) return null;
            Data.baskets.Remove(b);
            var cat = AddCat(Config.Cat(b.speciesId), Party.Count() < PartySize, b.hasGenes ? b.genes : null);
            Commit();
            return cat;
        }

        public void AddBasket(CatData species, int seconds = -1, CatGenes genes = null)
        {
            Data.baskets.Add(new Basket
            {
                speciesId = species.id, hatchAtUnix = Now + (seconds < 0 ? Config.hatchSeconds : seconds),
                hasGenes = genes != null, genes = genes ?? new CatGenes()
            });
        }

        public bool BuyBasket()
        {
            if (FreeHatcherySlots <= 0 || Data.coins < Config.basketPrice) return false;
            Data.coins -= Config.basketPrice;
            AddBasket(Config.cats[UnityEngine.Random.Range(0, Config.cats.Count)]);
            Commit();
            return true;
        }

        public bool Breed(OwnedCat a, OwnedCat b)
        {
            if (a == b || FreeHatcherySlots <= 0 || Data.coins < Config.breedPrice) return false;
            Data.coins -= Config.breedPrice;
            AddBasket(_rng.Next(2) == 0 ? Species(a) : Species(b), Config.breedSeconds, Genetics.Breed(a.genes, b.genes, _rng));
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

        public int ItemCount(ItemData item) => item == null ? 0 : Data.items.Find(s => s.itemId == item.id)?.count ?? 0;

        public void AddItem(ItemData item, int n)
        {
            if (item == null) return;
            var stack = Data.items.Find(s => s.itemId == item.id);
            if (stack == null) Data.items.Add(stack = new ItemStack { itemId = item.id });
            stack.count += n;
        }

        public bool ConsumeItem(ItemData item)
        {
            var stack = item == null ? null : Data.items.Find(s => s.itemId == item.id);
            if (stack == null || stack.count <= 0) return false;
            stack.count--;
            return true;
        }

        public bool Feed(OwnedCat cat)
        {
            var treat = Config.Item(ItemKind.Exp);
            if (!ConsumeItem(treat)) return false;
            var house = House(cat.houseUid);
            if (house != null) Collect(house);
            Care(cat);
            GainExp(cat, treat.value);
            cat.fullness = Math.Min(100f, cat.fullness + CatCare.FeedAmount);
            Commit();
            return true;
        }

        public void Care(OwnedCat cat) => CatCare.Decay(cat, Now);

        public CatMood Mood(OwnedCat cat)
        {
            Care(cat);
            return CatCare.Mood(cat);
        }

        public void Wash(OwnedCat cat)
        {
            var house = House(cat.houseUid);
            if (house != null) Collect(house);
            Care(cat);
            cat.cleanliness = 100f;
            Commit();
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

        public int Pending(OwnedHouse h)
        {
            var minutes = (Now - h.lastCollectUnix) / 60f;
            var rate = CatsIn(h).Sum(c => c.level * CatCare.ProductionRate(c)) * Config.coinsPerLevelPerMinute;
            return Math.Min(HouseDef(h).CoinCap(h.level), (int)(minutes * rate));
        }

        public int Collect(OwnedHouse h)
        {
            var n = Pending(h);
            Data.coins += n;
            h.lastCollectUnix = Now;
            return n;
        }

        public int CollectAndSave(OwnedHouse h)
        {
            var n = Collect(h);
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
