using System;
using System.Collections.Generic;

namespace CatVillage.Core
{
    [Serializable]
    public class OwnedCat
    {
        public string uid;
        public string speciesId;
        public int level = 1;
        public int exp;
        public bool inParty;
    }

    [Serializable]
    public class Basket
    {
        public string speciesId;
        public long hatchAtUnix;
    }

    [Serializable]
    public class StageRecord
    {
        public string stageId;
        public int stars;
    }

    [Serializable]
    public class ItemStack
    {
        public string itemId;
        public int count;
    }

    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public int coins = 100;
        public List<OwnedCat> cats = new List<OwnedCat>();
        public List<Basket> baskets = new List<Basket>();
        public List<StageRecord> stages = new List<StageRecord>();
        public List<ItemStack> items = new List<ItemStack>();
        public long lastCollectUnix;
        public int nextUid = 1;
    }
}
