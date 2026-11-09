using UnityEngine;

namespace YangVil.Data
{
    [CreateAssetMenu(menuName = "YangVil/Cat")]
    public class CatData : ScriptableObject
    {
        public string id;
        public string displayName;
        public Element element;
        public Stats baseStats;
        public Stats growth;
        public string skillName;
        public float skillPower = 2.5f;
        public Color tint = Color.white;
        public CoatSpec coat = new CoatSpec();
        public SpriteSet sprites;

        public const int TeenLevel = 10;
        public const int AdultLevel = 20;
        public const float StageBonus = 1.3f;

        public Stats StatsAt(int level)
        {
            var s = baseStats + growth * (level - 1);
            return s.Scaled(Mathf.Pow(StageBonus, GrowthStage(level)));
        }

        public static int GrowthStage(int level) => level >= AdultLevel ? 2 : level >= TeenLevel ? 1 : 0;

        public static float VisualScaleAt(int level, CatGenes genes = null) => (genes == null ? 1f : genes.size) * (GrowthStage(level) switch
        {
            0 => 0.55f,
            1 => 0.8f,
            _ => 1.1f
        });

        public static string StageLabel(int level) => GrowthStage(level) switch
        {
            0 => "아기",
            1 => "청소년",
            _ => "성묘"
        };
    }
}
