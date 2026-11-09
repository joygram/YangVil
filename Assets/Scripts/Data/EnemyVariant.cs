using UnityEngine;

namespace CatVillage.Data
{
    public enum BossPattern { None, Summon, Rage, DrainGauge, ChargeBlast }

    [CreateAssetMenu(menuName = "CatVillage/Enemy Variant")]
    public class EnemyVariant : ScriptableObject
    {
        public string id;
        public EnemyBase enemyBase;
        public Element element;
        public float statMultiplier = 1f;
        public float scale = 1f;
        public bool isBoss;
        public BossPattern pattern;
        public int patternEvery = 3;
        public EnemyVariant summon;

        public string DisplayName => $"{Affinity.Label(element)} {enemyBase.displayName}";

        public Color Tint => enemyBase.baseTint * Affinity.Tint(element);

        public Stats StatsAt(int level)
        {
            var s = enemyBase.baseStats + enemyBase.growth * (level - 1);
            return s.Scaled(statMultiplier);
        }
    }
}
