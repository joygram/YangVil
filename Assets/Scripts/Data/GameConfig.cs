using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CatVillage.Data
{
    [CreateAssetMenu(menuName = "CatVillage/Game Config")]
    public class GameConfig : ScriptableObject
    {
        public const string ResourcePath = "GameConfig";

        public List<CatData> cats = new List<CatData>();
        public List<StageData> stages = new List<StageData>();
        public List<ItemData> items = new List<ItemData>();
        public CatData starterCat;
        public CatData starterBasket;

        public List<ScreenEntry> screens = new List<ScreenEntry>();
        public MemoryPolicy memory = new MemoryPolicy();

        public AssetReferenceGameObject Screen(string screenType) => screens.Find(s => s.screenType == screenType)?.prefab;

        public int basketPrice = 100;
        public int firstHatchSeconds = 30;
        public int hatchSeconds = 300;
        public int expPerLevel = 50;
        public int coinsPerLevelPerMinute = 1;
        public int coinCap = 300;
        public int maxBattleTurns = 30;
        public int fastClearTurns = 10;
        public int battleItemLimit = 3;

        public static GameConfig Load() => Resources.Load<GameConfig>(ResourcePath);

        public ItemData Item(ItemKind kind) => items.Find(i => i.kind == kind);

        public CatData Cat(string id) => cats.Find(c => c.id == id);
    }
}
