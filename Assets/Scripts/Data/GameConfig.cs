using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace YangVil.Data
{
    [CreateAssetMenu(menuName = "YangVil/Game Config")]
    public class GameConfig : ScriptableObject
    {
        public const string ResourcePath = "GameConfig";

        public List<CatData> cats = new List<CatData>();
        public List<StageData> stages = new List<StageData>();
        public List<ItemData> items = new List<ItemData>();
        public List<HouseData> houses = new List<HouseData>();
        public CatData starterCat;
        public CatData starterBasket;
        public HouseData starterHouse;

        public AssetReference villageScene;
        public AssetReference battleScene;
        public List<ScreenEntry> screens = new List<ScreenEntry>();
        public MemoryPolicy memory = new MemoryPolicy();
        public PresentationSet presentation = new PresentationSet();

        public AssetReferenceGameObject Screen(string screenType) => screens.Find(s => s.screenType == screenType)?.prefab;

        public int basketPrice = 100;
        public int hatcherySlots = 2;
        public int villageSlots = 6;
        public int firstHatchSeconds = 30;
        public int hatchSeconds = 300;
        public int breedSeconds = 600;
        public int breedPrice = 50;
        public int expPerLevel = 50;
        public int coinsPerLevelPerMinute = 1;
        public int maxBattleTurns = 30;
        public int fastClearTurns = 10;
        public int battleItemLimit = 3;

        public static GameConfig Load() => Resources.Load<GameConfig>(ResourcePath);

        public ItemData Item(ItemKind kind) => items.Find(i => i.kind == kind);

        public CatData Cat(string id) => cats.Find(c => c.id == id);

        public HouseData House(string id) => houses.Find(h => h.id == id);
    }
}
