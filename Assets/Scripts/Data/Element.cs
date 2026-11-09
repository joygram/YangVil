using UnityEngine;

namespace YangVil.Data
{
    public enum Element { Fire, Water, Leaf, Wind, Sun, Moon }

    public static class Affinity
    {
        public const float Strong = 1.5f;
        public const float Weak = 0.75f;

        public static float Multiplier(Element attacker, Element defender)
        {
            if (Beats(attacker, defender)) return Strong;
            if (Beats(defender, attacker) && !IsLightPair(attacker, defender)) return Weak;
            return 1f;
        }

        public static bool Beats(Element a, Element b)
        {
            if (IsLightPair(a, b)) return a != b;
            return (a, b) switch
            {
                (Element.Fire, Element.Leaf) => true,
                (Element.Leaf, Element.Wind) => true,
                (Element.Wind, Element.Water) => true,
                (Element.Water, Element.Fire) => true,
                _ => false
            };
        }

        static bool IsLightPair(Element a, Element b)
        {
            return (a == Element.Sun || a == Element.Moon) && (b == Element.Sun || b == Element.Moon);
        }

        public static string Label(Element e) => e switch
        {
            Element.Fire => "불꽃",
            Element.Water => "물방울",
            Element.Leaf => "풀잎",
            Element.Wind => "바람",
            Element.Sun => "햇살",
            _ => "달빛"
        };

        public static Color Tint(Element e) => e switch
        {
            Element.Fire => new Color(1f, 0.55f, 0.35f),
            Element.Water => new Color(0.45f, 0.7f, 1f),
            Element.Leaf => new Color(0.5f, 0.9f, 0.45f),
            Element.Wind => new Color(0.75f, 0.95f, 0.9f),
            Element.Sun => new Color(1f, 0.92f, 0.45f),
            _ => new Color(0.7f, 0.6f, 1f)
        };
    }
}
