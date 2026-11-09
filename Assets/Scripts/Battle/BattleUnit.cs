using CatVillage.Data;
using UnityEngine;

namespace CatVillage.Battle
{
    public class BattleUnit
    {
        public const int GaugeMax = 3;

        public string Name;
        public Element Element;
        public bool IsAlly;
        public int MaxHp;
        public int Hp;
        public int Atk;
        public int Def;
        public int Spd;
        public int Gauge;
        public float SkillPower = 2.5f;
        public string SkillName = "필살기";
        public SpriteSet Sprites;
        public Color Tint = Color.white;
        public float Scale = 1f;
        public EnemyVariant Variant;
        public bool Raged;

        public bool Alive => Hp > 0;
        public bool GaugeFull => Gauge >= GaugeMax;

        public CatGenes Genes;

        public static BattleUnit FromCat(CatData species, int level, CatGenes genes = null, string name = null)
        {
            var s = species.StatsAt(level);
            return new BattleUnit
            {
                Genes = genes,
                Name = name ?? species.displayName,
                Element = species.element,
                IsAlly = true,
                MaxHp = s.hp, Hp = s.hp, Atk = s.atk, Def = s.def, Spd = s.spd,
                SkillPower = species.skillPower,
                SkillName = species.skillName,
                Sprites = species.sprites,
                Tint = species.tint,
                Scale = (genes?.size ?? 1f) * (0.75f + 0.125f * CatData.GrowthStage(level))
            };
        }

        public static BattleUnit FromEnemy(EnemyVariant v, int level)
        {
            var s = v.StatsAt(level);
            return new BattleUnit
            {
                Name = v.DisplayName,
                Element = v.element,
                IsAlly = false,
                MaxHp = s.hp, Hp = s.hp, Atk = s.atk, Def = s.def, Spd = s.spd,
                Sprites = v.enemyBase.sprites,
                Tint = v.Tint,
                Scale = v.enemyBase.scale * v.scale,
                Variant = v
            };
        }

        public void AddGauge() => Gauge = Mathf.Min(GaugeMax, Gauge + 1);
    }
}
