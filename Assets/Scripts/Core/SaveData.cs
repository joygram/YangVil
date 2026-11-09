using System;
using System.Collections.Generic;

namespace YangVil.Core
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
        public int personality = -1;
        public float bond = 50f;
        public long bondUnix;
        public string parentA = "";
        public string parentB = "";
        public List<CatMemory> memories = new List<CatMemory>();
    }

    [Serializable]
    public class CatMemory
    {
        public string text;
        public long unix;
    }

    [Serializable]
    public class Relation
    {
        public string a;
        public string b;
        public float affinity;
        public float[] grievances = new float[4];
        public int lastCause = -1;
        public bool madeUp;
    }

    [Serializable]
    public class Expedition
    {
        public List<string> cats = new List<string>();
        public string stageId = "";
        public long startUnix;
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
        public string parentA = "";
        public string parentB = "";
        public int personality = -1;
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
        public const int CurrentVersion = 7;

        public int version = CurrentVersion;
        public int coins = 250;
        public List<OwnedCat> cats = new List<OwnedCat>();
        public List<OwnedHouse> houses = new List<OwnedHouse>();
        public List<Basket> baskets = new List<Basket>();
        public List<StageRecord> stages = new List<StageRecord>();
        public List<ItemStack> items = new List<ItemStack>();
        public List<string> discovered = new List<string>();
        public List<Relation> relations = new List<Relation>();
        public List<string> episodesSeen = new List<string>();
        public List<string> tried = new List<string>();
        public Expedition expedition = new Expedition();
        public int nextUid = 1;
    }
}
