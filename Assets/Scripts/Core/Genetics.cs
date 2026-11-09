using YangVil.Data;
using UnityEngine;

namespace YangVil.Core
{
    public static class Genetics
    {
        public const float OutlierChance = 0.04f;
        public const float PatternMutation = 0.12f;
        public const float ColorJitter = 0.06f;

        public static CatGenes FromSpecies(CatData species, System.Random rng)
        {
            var c = species.coat;
            return new CatGenes
            {
                pattern = c.pattern,
                light = Jitter(c.light, rng),
                dark = Jitter(c.dark, rng),
                accent = Jitter(c.accent, rng),
                accent2 = Jitter(c.accent2, rng),
                size = 0.92f + (float)rng.NextDouble() * 0.16f,
                seed = rng.Next()
            };
        }

        public static CatGenes Breed(CatGenes a, CatGenes b, System.Random rng)
        {
            var child = new CatGenes
            {
                pattern = rng.NextDouble() < PatternMutation ? (CoatPattern)rng.Next(4) : rng.Next(2) == 0 ? a.pattern : b.pattern,
                light = Jitter(Mix(a.light, b.light, rng), rng),
                dark = Jitter(Mix(a.dark, b.dark, rng), rng),
                accent = rng.Next(2) == 0 ? a.accent : b.accent,
                accent2 = rng.Next(2) == 0 ? a.accent2 : b.accent2,
                size = Mathf.Clamp((a.size + b.size) / 2f + ((float)rng.NextDouble() - 0.5f) * 0.12f, 0.8f, 1.2f),
                seed = rng.Next()
            };
            if (rng.NextDouble() < OutlierChance) ApplyOutlier(child, (Outlier)(1 + rng.Next(4)));
            return child;
        }

        public static void ApplyOutlier(CatGenes g, Outlier outlier)
        {
            g.outlier = outlier;
            switch (outlier)
            {
                case Outlier.Grandpa:
                    g.pattern = CoatPattern.Solid;
                    g.light = new Color(0.86f, 0.85f, 0.82f);
                    g.dark = new Color(0.45f, 0.44f, 0.42f);
                    break;
                case Outlier.Ghost:
                    g.light = new Color(0.92f, 0.95f, 1f);
                    g.dark = new Color(0.7f, 0.75f, 0.9f);
                    break;
                case Outlier.Giant:
                    g.size = 1.45f;
                    break;
            }
        }

        public static string OutlierName(Outlier o) => o switch
        {
            Outlier.Grandpa => "콧수염 할배냥",
            Outlier.Rainbow => "무지개냥",
            Outlier.Ghost => "유령냥",
            Outlier.Giant => "왕냥이",
            _ => ""
        };

        public static string Describe(CatGenes g)
        {
            var pattern = g.pattern switch
            {
                CoatPattern.Tabby => "줄무늬",
                CoatPattern.Tuxedo => "턱시도",
                CoatPattern.Calico => "삼색",
                _ => "단색"
            };
            var o = OutlierName(g.outlier);
            return string.IsNullOrEmpty(o) ? $"{pattern} · 몸집 {g.size:0.00}" : $"★{o}★ {pattern} · 몸집 {g.size:0.00}";
        }

        static Color Mix(Color a, Color b, System.Random rng)
        {
            var t = (float)rng.NextDouble();
            return Color.Lerp(a, b, t);
        }

        static Color Jitter(Color c, System.Random rng)
        {
            float J() => ((float)rng.NextDouble() - 0.5f) * 2f * ColorJitter;
            return new Color(Mathf.Clamp01(c.r + J()), Mathf.Clamp01(c.g + J()), Mathf.Clamp01(c.b + J()), c.a);
        }
    }
}
