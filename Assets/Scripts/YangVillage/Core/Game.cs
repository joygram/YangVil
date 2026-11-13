using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using YangVillage.Battle;

// Godot YangVillage game/scripts/core (SaveData·Game) 이식. 동작 동일.
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

        // 거점
        public List<string[]> Stages => Db.Set.Get("stage").Rows
            .OrderBy(r => Db.Int("region", "region_id", Db.Cell("stage", r, "region_id"), "act"))
            .ThenBy(r => int.Parse(Db.Cell("stage", r, "order"))).ToList();

        public string StageName(string stageId) => Db.Text(Db.Get("stage", "stage_id", stageId, "text_id"));

        public int Stars(string stageId) => Data.stages.Find(r => r.stageId == stageId)?.stars ?? 0;

        public bool IsUnlocked(int index) => index == 0 || Stars(Db.Cell("stage", Stages[index - 1], "stage_id")) > 0;

        public string NextStageId(string stageId)
        {
            var list = Stages;
            var i = list.FindIndex(r => Db.Cell("stage", r, "stage_id") == stageId) + 1;
            return i > 0 && i < list.Count && IsUnlocked(i) ? Db.Cell("stage", list[i], "stage_id") : null;
        }

        public int RewardCoins(string stageId) =>
            Db.ConfigInt("reward_coin_base") + Db.Int("stage", "stage_id", stageId, "level") * Db.ConfigInt("reward_coin_per_level");

        // 전투
        public BattleUnit UnitFromCat(OwnedCat cat)
        {
            var row = Db.Row("cat", "cat_id", cat.catId);
            var role = Db.Cell("cat", row, "role_id");
            var tier = Db.Cell("cat", row, "stat_tier");
            var stat = Db.Where("cat_stat", "role_id", role).First(r => Db.Cell("cat_stat", r, "stat_tier") == tier);
            int At(string name) => int.Parse(Db.Cell("cat_stat", stat, "base_" + name)) + int.Parse(Db.Cell("cat_stat", stat, "growth_" + name)) * (cat.level - 1);
            var mult = Math.Pow(Db.ConfigInt("growth_stage_mult_pct") / 100.0, GrowthStage(cat.level));
            var hp = (int)(At("hp") * mult);
            return new BattleUnit
            {
                Uid = cat.uid, Name = CatName(cat), Level = cat.level, ElementId = Db.Cell("cat", row, "element_id"), SpriteId = Db.Cell("cat", row, "sprite_id"), IsAlly = true,
                MaxHp = hp, Hp = hp, Atk = (int)(At("atk") * mult), Def = (int)(At("def") * mult),
                Spd = Db.Int("role", "role_id", role, "spd"), Deck = DeckOf(role), StyleId = Db.Get("role", "role_id", role, "attack_style_id")
            };
        }

        readonly Dictionary<string, CardDef> _cards = new Dictionary<string, CardDef>();

        CardDef Card(string cardId)
        {
            if (_cards.TryGetValue(cardId, out var card)) return card;
            return _cards[cardId] = new CardDef
            {
                Id = cardId, Name = Db.Text(Db.Get("card", "card_id", cardId, "text_id")), Color = Db.Get("card", "card_id", cardId, "color"),
                PowerPct = Db.Int("card", "card_id", cardId, "power_pct"), Hits = Db.Int("card", "card_id", cardId, "hits"), Gauge = Db.Int("card", "card_id", cardId, "gauge")
            };
        }

        List<CardDef> DeckOf(string role)
        {
            var deck = new List<CardDef>();
            foreach (var row in Db.Where("role_card", "role_id", role))
                for (var i = int.Parse(Db.Cell("role_card", row, "count")); i > 0; i--)
                    deck.Add(Card(Db.Cell("role_card", row, "card_id")));
            return deck;
        }

        BattleUnit UnitFromEnemy(string[] row)
        {
            var enemyId = Db.Cell("stage_enemy", row, "enemy_id");
            var boss = Db.Cell("stage_enemy", row, "is_boss") == "true";
            var hp = int.Parse(Db.Cell("stage_enemy", row, "hp"));
            // 적마다 제 그림이 있으면 그것을, 아직 없으면 지역 공용 그림을 쓴다.
            var sprite = Db.Get("enemy", "enemy_id", enemyId, "sprite_id");
            if (sprite == "") sprite = Db.Get("region", "region_id", Db.Get("enemy", "enemy_id", enemyId, "region_id"), boss ? "boss_sprite_id" : "sprite_id");
            return new BattleUnit
            {
                Uid = enemyId, Name = Db.Text(Db.Get("enemy", "enemy_id", enemyId, "text_id")),
                Level = int.Parse(Db.Cell("stage_enemy", row, "level")),
                ElementId = Db.Get("enemy", "enemy_id", enemyId, "element_id"), IsBoss = boss,
                SpriteId = sprite,
                MaxHp = hp, Hp = hp, Atk = int.Parse(Db.Cell("stage_enemy", row, "atk")), Def = int.Parse(Db.Cell("stage_enemy", row, "def")),
                Spd = Db.ConfigInt(boss ? "boss_spd" : "enemy_spd"), StyleId = Db.Config(boss ? "boss_attack_style_id" : "enemy_attack_style_id")
            };
        }

        public BattleState BuildBattle(string stageId)
        {
            var size = Db.ConfigInt("wave_size");
            var rows = Db.Where("stage_enemy", "stage_id", stageId)
                .OrderBy(r => Db.Cell("stage_enemy", r, "is_boss") == "true" ? 1 : 0)
                .ThenBy(r => int.Parse(Db.Cell("stage_enemy", r, "slot"))).ToList();
            var waves = new List<List<BattleUnit>>();
            for (var i = 0; i < rows.Count; i += size)
                waves.Add(rows.Skip(i).Take(size).Select(UnitFromEnemy).ToList());
            return new BattleState(new Affinity(Db), Party.Select(UnitFromCat).ToList(), waves,
                new BattleRules
                {
                    MaxTurns = Db.ConfigInt("max_battle_turns"), FastClearTurns = Db.ConfigInt("fast_clear_turns"), SkillPowerPct = Db.ConfigInt("skill_power_pct"),
                    HandSize = Db.ConfigInt("hand_size"), PickCount = Db.ConfigInt("pick_count"), ChainBonusPct = Db.ConfigInt("chain_bonus_pct"), ExtraPowerPct = Db.ConfigInt("extra_power_pct")
                }, _rng);
        }

        public BattleReward ApplyVictory(string stageId, int stars)
        {
            var record = Data.stages.Find(r => r.stageId == stageId);
            var firstClear = record == null;
            if (firstClear) Data.stages.Add(record = new StageRecord { stageId = stageId });
            record.stars = Math.Max(record.stars, stars);

            var reward = new BattleReward { coins = RewardCoins(stageId), treats = Db.ConfigInt("reward_treats"), stars = stars };
            Data.coins += reward.coins;
            Data.treats += reward.treats;
            var exp = Db.Int("stage", "stage_id", stageId, "level") * Db.ConfigInt("reward_exp_per_level");
            foreach (var cat in Party) GainExp(cat, exp);
            if (firstClear && FreeHatcherySlots > 0)
            {
                reward.basketCatId = RollBasketCat(Db.Config("stage_basket_id"));
                Data.baskets.Add(new Basket { catId = reward.basketCatId, hatchAtUnix = Now + Db.ConfigInt("hatch_seconds") });
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
        public string basketCatId;
    }
}
