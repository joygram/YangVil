using System.Collections.Generic;
using UnityEngine;

namespace CatVillage.Data
{
    [CreateAssetMenu(menuName = "CatVillage/Stage")]
    public class StageData : ScriptableObject
    {
        public string id;
        public string displayName;
        public List<EnemyVariant> enemies = new List<EnemyVariant>();
        public int enemyLevel = 1;
        public int rewardCoins = 30;
        public int rewardTreats = 1;
        public CatData firstClearBasket;
    }
}
