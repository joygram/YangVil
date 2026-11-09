using UnityEngine;

namespace YangVil.Data
{
    public enum BossPattern { None, Summon, Rage, DrainGauge, ChargeBlast }

    [CreateAssetMenu(menuName = "YangVil/Enemy Variant")]
    public class EnemyVariant : ScriptableObject
    {
        public string id;
        public string displayName;
        public EnemyBase enemyBase;
        public Element element;
        public float statMultiplier = 1f;
        public float scale = 1f;
        public bool isBoss;
        public BossPattern pattern;
        public int patternEvery = 3;
        public EnemyVariant summon;

        public string DisplayName => string.IsNullOrEmpty(displayName) ? $"{Affinity.Label(element)} {enemyBase.displayName}" : displayName;

        public Color Tint => enemyBase.baseTint * Affinity.Tint(element);

        public Stats StatsAt(int level)
        {
            var s = enemyBase.baseStats + enemyBase.growth * (level - 1);
            return s.Scaled(statMultiplier);
        }
    }
}
