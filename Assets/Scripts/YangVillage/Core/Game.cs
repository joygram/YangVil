using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Godot YangVillage game/scripts/core (SaveData·Game) 이식. 마을(고양이) 규칙만 — 전투·거점은 미이식.
namespace YangVillage.Core
{
    [Serializable]
    public class OwnedCat
    {
        public string uid;
        public string catId;
        public int level = 1;
        public int exp;
        public bool inParty;
    }

    [Serializable]
    public class Basket
    {
        public string catId;
        public long hatchAtUnix;
    }

    [Serializable]
    public class StageRecord
    {
        public string stageId;
        public int stars;
    }

    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public int coins;
        public int treats;
        public List<OwnedCat> cats = new List<OwnedCat>();
        public List<Basket> baskets = new List<Basket>();
        public List<StageRecord> stages = new List<StageRecord>();
        public List<string> discovered = new List<string>();
        public int nextUid = 1;
    }

    // 게임 규칙 한 곳. 수치·이름은 전부 Db에서 읽음.
    public class Game
    {
        public Db Db { get; }
        public SaveData Data { get; private set; }
        public Func<long> Clock { get; }

        readonly Func<string> _load;
        readonly Action<string> _save;
        readonly System.Random _rng;

        public Game(Db db, Func<string> load, Action<string> save, System.Random rng = null, Func<long> clock = null)
        {
            Clock = clock ?? (() => DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            Db = db;
            _load = load;
            _save = save;
            _rng = rng ?? new System.Random();
            var text = load();
            Data = string.IsNullOrEmpty(text) ? null : JsonUtility.FromJson<SaveData>(text);
            if (Data == null || Data.version != SaveData.CurrentVersion) NewSave();
        }

        public long Now => Clock();
        public int PartySize => Db.ConfigInt("party_size");

        void NewSave()
        {
            Data = new SaveData { coins = Db.ConfigInt("start_coins"), treats = Db.ConfigInt("start_treats") };
            AddCat(Db.Config("starter_cat_id"), true);
            Data.baskets.Add(new Basket { catId = Db.Config("starter_basket_cat_id"), hatchAtUnix = Now + Db.ConfigInt("first_hatch_seconds") });
            Commit();
        }

        public void ResetSave() => NewSave();

        public void Commit() => _save(JsonUtility.ToJson(Data));

        // 고양이
        OwnedCat AddCat(string catId, bool inParty)
        {
            Db.Row("cat", "cat_id", catId);
            var cat = new OwnedCat { uid = $"c{Data.nextUid++}", catId = catId, inParty = inParty };
            Data.cats.Add(cat);
            if (!Data.discovered.Contains(catId)) Data.discovered.Add(catId);
            return cat;
        }

        public string CatName(OwnedCat cat) => Db.Text(Db.Get("cat", "cat_id", cat.catId, "text_id"));

        public string ElementName(string elementId) => Db.Text(Db.Get("element", "element_id", elementId, "text_id"));

        public string CatSprite(string catId) => Db.Get("cat", "cat_id", catId, "sprite_id");

        public string CatElement(OwnedCat cat) => Db.Get("cat", "cat_id", cat.catId, "element_id");

        public int GrowthStage(int level) =>
            level >= Db.ConfigInt("adult_level") ? 2 : level >= Db.ConfigInt("teen_level") ? 1 : 0;

        public string StageLabel(int level) => Db.Text("growth_stage_" + GrowthStage(level));

        public IEnumerable<OwnedCat> Party => Data.cats.Where(c => c.inParty).Take(PartySize);

        public bool ToggleParty(OwnedCat cat)
        {
            if (!cat.inParty && Party.Count() >= PartySize) return false;
            cat.inParty = !cat.inParty;
            Commit();
            return true;
        }

        public int LevelCap => Db.Set.Get("level_exp").Rows.Count + 1;

        public int ExpToNext(OwnedCat cat) =>
            cat.level >= LevelCap ? 0 : Db.Int("level_exp", "level", cat.level.ToString(), "exp_to_next");

        public void GainExp(OwnedCat cat, int exp)
        {
            cat.exp += exp;
            while (cat.level < LevelCap && cat.exp >= ExpToNext(cat))
            {
                cat.exp -= ExpToNext(cat);
                cat.level++;
            }
            if (cat.level >= LevelCap) cat.exp = 0;
        }

        public bool Feed(OwnedCat cat)
        {
            if (Data.treats <= 0 || cat.level >= LevelCap) return false;
            Data.treats--;
            GainExp(cat, Db.ConfigInt("treat_exp"));
            Commit();
            return true;
        }

        public bool BuyTreat()
        {
            var price = Db.ConfigInt("treat_price");
            if (Data.coins < price) return false;
            Data.coins -= price;
            Data.treats++;
            Commit();
            return true;
        }

        // 바구니·부화
        public int FreeHatcherySlots => Db.ConfigInt("hatchery_slots") - Data.baskets.Count;

        public bool IsReady(Basket b) => Now >= b.hatchAtUnix;

        public string RollBasketCat(string basketId)
        {
            var pool = Db.Where("basket_pool", "basket_id", basketId).ToList();
            var total = pool.Sum(r => int.Parse(Db.Cell("basket_pool", r, "weight")));
            var roll = _rng.Next(total);
            var star = "";
            foreach (var r in pool)
            {
                roll -= int.Parse(Db.Cell("basket_pool", r, "weight"));
                if (roll < 0) { star = Db.Cell("basket_pool", r, "star"); break; }
            }
            var candidates = Db.Where("cat", "star", star).Where(r => Db.Cell("cat", r, "acquire_locked") == "false").ToList();
            return Db.Cell("cat", candidates[_rng.Next(candidates.Count)], "cat_id");
        }

        public bool BuyBasket()
        {
            var price = Db.ConfigInt("basket_price");
            if (FreeHatcherySlots <= 0 || Data.coins < price) return false;
            Data.coins -= price;
            Data.baskets.Add(new Basket { catId = RollBasketCat(Db.Config("shop_basket_id")), hatchAtUnix = Now + Db.ConfigInt("hatch_seconds") });
            Commit();
            return true;
        }

        public OwnedCat Hatch(Basket b)
        {
            if (!IsReady(b)) return null;
            Data.baskets.Remove(b);
            var cat = AddCat(b.catId, Party.Count() < PartySize);
            Commit();
            return cat;
        }
    }
}
