using System.Collections.Generic;
using System.IO;
using System.Linq;
using CatVillage.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CatVillage.EditorTools
{
    public static class GameDataBuilder
    {
        const string DataRoot = "Assets/Data";
        const string ConfigPath = "Assets/Resources/GameConfig.asset";
        const string Kitty = "Assets/Art/ThirdParty/CuteKitty";
        const string Dog = "Assets/Art/ThirdParty/Dog/dog_brown_sheet.png";

        [MenuItem("CatVillage/Build Game Data")]
        public static void Build()
        {
            foreach (var dir in new[] { "Cats", "Enemies", "Stages", "Items", "Houses" })
                Directory.CreateDirectory($"{DataRoot}/{dir}");
            Directory.CreateDirectory("Assets/Resources");
            foreach (var dir in AddressablesSetup.ArtFolders) Directory.CreateDirectory(dir);
            Directory.CreateDirectory(AddressablesSetup.AudioFolder);
            AssetDatabase.Refresh();
            AddressablesSetup.Run();

            var cats = BuildCats();
            var variants = BuildEnemies();
            var items = BuildItems();
            var stages = BuildStages(variants, cats);
            var houses = BuildHouses();

            var config = Upsert<GameConfig>(ConfigPath);
            config.cats = cats.Values.ToList();
            config.items = items;
            config.stages = stages;
            config.houses = houses;
            config.starterCat = cats["cat_fire"];
            config.starterBasket = cats["cat_leaf"];
            config.starterHouse = houses[0];
            config.presentation ??= new PresentationSet();
            foreach (var e in System.Enum.GetValues(typeof(Element)).Cast<Element>())
                if (config.presentation.Fx(e) == null) config.presentation.elementFx.Add(new ElementFx { element = e });
            foreach (var id in new[] { "hatchery", "shop", "breed", "dex", "gate" })
                if (config.presentation.buildings.All(b => b.buildingId != id)) config.presentation.buildings.Add(new BuildingArt { buildingId = id });
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log($"GameDataBuilder: 고양이 {cats.Count}, 적 {variants.Count}, 스테이지 {stages.Count}, 아이템 {items.Count}, 집 {houses.Count}");
        }

        static List<AssetReferenceTexture2D> Frames(string action, int count) =>
            Enumerable.Range(1, count).Select(i => AddressablesSetup.Texture($"{Kitty}/kitty_{action}_0{i}.png")).ToList();

        static SpriteSet KittySet() => new SpriteSet
        {
            idle = Frames("idle", 4), walk = Frames("walk", 3), attack = Frames("attack", 6), hurt = Frames("death", 1), cellSize = 16
        };

        static SpriteSet DogSet() => new SpriteSet
        {
            sheet = AddressablesSetup.Texture(Dog), cellSize = 16, framesPerRow = 6, idleRow = 0, idleFrames = 4,
            walkRow = 1, walkFrames = 6, attackRow = 4, attackFrames = 6, hurtRow = -1
        };

        static List<HouseData> BuildHouses()
        {
            var specs = new (string id, string name, Element[] accepts, int price, Color tint)[]
            {
                ("house_fire", "모닥불 집", new[] { Element.Fire }, 0, new Color(1f, 0.55f, 0.35f)),
                ("house_water", "연못 집", new[] { Element.Water }, 150, new Color(0.45f, 0.7f, 1f)),
                ("house_leaf", "풀숲 집", new[] { Element.Leaf }, 150, new Color(0.45f, 0.85f, 0.4f)),
                ("house_wind", "바람개비 집", new[] { Element.Wind }, 200, new Color(0.7f, 0.95f, 0.9f)),
                ("house_light", "햇살 정원", new[] { Element.Sun, Element.Moon }, 300, new Color(1f, 0.9f, 0.5f)),
                ("house_forest", "숲속 오두막", new[] { Element.Fire, Element.Leaf, Element.Wind, Element.Water }, 500, new Color(0.65f, 0.5f, 0.35f))
            };
            var list = new List<HouseData>();
            foreach (var s in specs)
            {
                var h = Upsert<HouseData>($"{DataRoot}/Houses/{s.id}.asset");
                h.id = s.id;
                h.displayName = s.name;
                h.accepts = s.accepts.ToList();
                h.price = s.price;
                h.tint = s.tint;
                EditorUtility.SetDirty(h);
                list.Add(h);
            }
            return list;
        }

        static T Upsert<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static Dictionary<string, CatData> BuildCats()
        {
            var specs = new (string id, string name, Element e, Stats s, string skill)[]
            {
                ("cat_fire", "치즈", Element.Fire, new Stats(120, 22, 10, 12), "불꽃 냥펀치"),
                ("cat_water", "고등어", Element.Water, new Stats(135, 18, 12, 10), "물방울 꾹꾹이"),
                ("cat_leaf", "삼색이", Element.Leaf, new Stats(115, 20, 11, 13), "풀잎 할퀴기"),
                ("cat_wind", "턱시도", Element.Wind, new Stats(100, 21, 8, 16), "회오리 점프"),
                ("cat_sun", "크림", Element.Sun, new Stats(110, 24, 9, 11), "햇살 하품"),
                ("cat_moon", "까망이", Element.Moon, new Stats(105, 25, 8, 14), "달빛 그림자")
            };
            var result = new Dictionary<string, CatData>();
            foreach (var spec in specs)
            {
                var c = Upsert<CatData>($"{DataRoot}/Cats/{spec.id}.asset");
                c.id = spec.id;
                c.displayName = spec.name;
                c.element = spec.e;
                c.baseStats = spec.s;
                c.growth = new Stats(12, 3, 1, 1);
                c.skillName = spec.skill;
                c.skillPower = 2.5f;
                c.tint = Color.Lerp(Color.white, Affinity.Tint(spec.e), 0.55f);
                c.sprites = KittySet();
                EditorUtility.SetDirty(c);
                result[spec.id] = c;
            }
            return result;
        }

        static EnemyBase Base(string id, string name, EnemyRole role, Stats s, Stats g, SpriteSet set, Color tint, float scale)
        {
            var b = Upsert<EnemyBase>($"{DataRoot}/Enemies/base_{id}.asset");
            b.id = id;
            b.displayName = name;
            b.role = role;
            b.baseStats = s;
            b.growth = g;
            b.sprites = set;
            b.baseTint = tint;
            b.scale = scale;
            EditorUtility.SetDirty(b);
            return b;
        }

        static EnemyVariant Variant(string id, EnemyBase b, Element e, float mult = 1f, float scale = 1f,
            BossPattern pattern = BossPattern.None, EnemyVariant summon = null)
        {
            var v = Upsert<EnemyVariant>($"{DataRoot}/Enemies/{id}.asset");
            v.id = id;
            v.enemyBase = b;
            v.element = e;
            v.statMultiplier = mult;
            v.scale = scale;
            v.isBoss = pattern != BossPattern.None;
            v.pattern = pattern;
            v.patternEvery = 3;
            v.summon = summon;
            EditorUtility.SetDirty(v);
            return v;
        }

        static Dictionary<string, EnemyVariant> BuildEnemies()
        {
            var rat = Base("rat", "들쥐", EnemyRole.Swarm, new Stats(60, 12, 4, 14), new Stats(8, 2, 1, 1),
                KittySet(), new Color(0.75f, 0.75f, 0.75f), 0.7f);
            var crow = Base("crow", "까마귀", EnemyRole.Ranged, new Stats(55, 15, 3, 15), new Stats(7, 3, 1, 1),
                KittySet(), new Color(0.45f, 0.4f, 0.55f), 0.8f);
            var dog = Base("dog", "들개", EnemyRole.Tank, new Stats(110, 14, 10, 7), new Stats(14, 2, 2, 1),
                DogSet(), Color.white, 1f);
            var robot = Base("robot", "로봇 청소기", EnemyRole.Tank, new Stats(130, 16, 14, 6), new Stats(16, 3, 2, 1),
                DogSet(), new Color(0.6f, 0.65f, 0.7f), 1f);

            var v = new Dictionary<string, EnemyVariant>();
            void Add(EnemyVariant x) => v[x.id] = x;
            Add(Variant("rat_fire", rat, Element.Fire));
            Add(Variant("rat_water", rat, Element.Water));
            Add(Variant("rat_leaf", rat, Element.Leaf));
            Add(Variant("crow_wind", crow, Element.Wind));
            Add(Variant("crow_moon", crow, Element.Moon));
            Add(Variant("dog_leaf", dog, Element.Leaf));
            Add(Variant("dog_fire", dog, Element.Fire));
            Add(Variant("boss_rat_king", rat, Element.Fire, 2f, 2.1f, BossPattern.Summon, v["rat_fire"]));
            Add(Variant("boss_crow_witch", crow, Element.Moon, 2f, 1.8f, BossPattern.DrainGauge));
            Add(Variant("boss_dog_chief", dog, Element.Fire, 2f, 1.5f, BossPattern.Rage));
            Add(Variant("boss_robot", robot, Element.Water, 2f, 1.5f, BossPattern.ChargeBlast));
            return v;
        }

        static List<ItemData> BuildItems()
        {
            var specs = new (string id, string name, ItemKind kind, int value, int price, bool battle)[]
            {
                ("item_milk", "우유 한 컵", ItemKind.Heal, 30, 30, true),
                ("item_silvervine", "개다래 잎", ItemKind.Revive, 50, 80, true),
                ("item_catnip", "캣닢", ItemKind.GaugeFill, 0, 40, true),
                ("item_churu", "츄르", ItemKind.Exp, 100, 20, false),
                ("item_tuna", "고급 참치캔", ItemKind.Exp, 500, 80, false),
                ("item_blanket", "따뜻한 담요", ItemKind.HatchNow, 0, 50, false)
            };
            var list = new List<ItemData>();
            foreach (var s in specs)
            {
                var i = Upsert<ItemData>($"{DataRoot}/Items/{s.id}.asset");
                i.id = s.id;
                i.displayName = s.name;
                i.kind = s.kind;
                i.value = s.value;
                i.price = s.price;
                i.usableInBattle = s.battle;
                EditorUtility.SetDirty(i);
                list.Add(i);
            }
            return list;
        }

        static List<StageData> BuildStages(Dictionary<string, EnemyVariant> v, Dictionary<string, CatData> cats)
        {
            var specs = new (string id, string name, string[] enemies, int level, int coins, string basket)[]
            {
                ("stage_1_1", "1-1 골목 입구", new[] { "rat_leaf" }, 1, 30, "cat_water"),
                ("stage_1_2", "1-2 쓰레기통 뒤", new[] { "rat_leaf", "rat_fire" }, 2, 35, "cat_wind"),
                ("stage_1_3", "1-3 전깃줄 아래", new[] { "crow_wind", "rat_water" }, 3, 40, "cat_sun"),
                ("stage_1_4", "1-4 빈 공터", new[] { "dog_leaf", "rat_fire" }, 4, 45, "cat_moon"),
                ("stage_1_5", "1-5 공사장", new[] { "dog_fire", "crow_moon", "rat_leaf" }, 5, 50, "cat_leaf"),
                ("stage_1_6", "1-6 보스: 쥐 대왕", new[] { "boss_rat_king" }, 6, 120, "cat_fire")
            };
            var list = new List<StageData>();
            foreach (var s in specs)
            {
                var st = Upsert<StageData>($"{DataRoot}/Stages/{s.id}.asset");
                st.id = s.id;
                st.displayName = s.name;
                st.enemies = s.enemies.Select(e => v[e]).ToList();
                st.enemyLevel = s.level;
                st.rewardCoins = s.coins;
                st.rewardTreats = 2;
                st.firstClearBasket = cats[s.basket];
                EditorUtility.SetDirty(st);
                list.Add(st);
            }
            return list;
        }
    }
}
