using System;
using UnityEngine;

namespace YangVil.Data
{
    public enum CoatPattern { Solid, Tabby, Tuxedo, Calico }

    public enum Outlier { None, Grandpa, Rainbow, Ghost, Giant }

    [Serializable]
    public class CoatSpec
    {
        public CoatPattern pattern = CoatPattern.Solid;
        public Color light = new Color(1f, 0.8f, 0.5f);
        public Color dark = new Color(0.7f, 0.4f, 0.15f);
        public Color accent = Color.white;
        public Color accent2 = Color.black;
    }

    [Serializable]
    public class CatGenes
    {
        public CoatPattern pattern = CoatPattern.Solid;
        public Color light = Color.white;
        public Color dark = Color.gray;
        public Color accent = Color.white;
        public Color accent2 = Color.black;
        public float size = 1f;
        public Outlier outlier = Outlier.None;
        public int seed;

        public CatGenes Clone() => (CatGenes)MemberwiseClone();

        public string Key => $"{pattern}|{ColorUtility.ToHtmlStringRGB(light)}|{ColorUtility.ToHtmlStringRGB(dark)}|" +
                             $"{ColorUtility.ToHtmlStringRGB(accent)}|{ColorUtility.ToHtmlStringRGB(accent2)}|{outlier}|{seed}";
    }
}
