using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CatVillage.Data
{
    [Serializable]
    public class WaveData
    {
        public List<EnemyVariant> enemies = new List<EnemyVariant>();
        public int levelBonus;
    }

    [CreateAssetMenu(menuName = "CatVillage/Stage")]
    public class StageData : ScriptableObject
    {
        public string id;
        public string displayName;
        public List<WaveData> waves = new List<WaveData>();
        public int enemyLevel = 1;
        public int rewardCoins = 30;
        public int rewardTreats = 1;
        public CatData firstClearBasket;
        public AssetReferenceTexture2D background;
        public AssetReferenceAudioClip bgm;

        public bool HasBoss => waves.Any(w => w.enemies.Any(e => e.isBoss));
    }
}
