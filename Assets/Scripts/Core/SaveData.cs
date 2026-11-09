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
        public string houseUid = "";
        public float fullness = 100f;
        public float cleanliness = 100f;
        public long careUnix;
        public Data.CatGenes genes = new Data.CatGenes();
    }

    [Serializable]
    public class OwnedHouse
    {
        public string uid;
        public string houseId;
        public int level = 1;
        public int slot;
        public long lastCollectUnix;
    }

    [Serializable]
    public class Basket
    {
        public string speciesId;
        public long hatchAtUnix;
        public bool hasGenes;
        public Data.CatGenes genes = new Data.CatGenes();
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
        public const int CurrentVersion = 5;

        public int version = CurrentVersion;
        public int coins = 250;
        public List<OwnedCat> cats = new List<OwnedCat>();
        public List<OwnedHouse> houses = new List<OwnedHouse>();
        public List<Basket> baskets = new List<Basket>();
        public List<StageRecord> stages = new List<StageRecord>();
        public List<ItemStack> items = new List<ItemStack>();
        public List<string> discovered = new List<string>();
        public int nextUid = 1;
    }
}
