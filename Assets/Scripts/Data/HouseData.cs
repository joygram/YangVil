using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace YangVil.Data
{
    [CreateAssetMenu(menuName = "YangVil/House")]
    public class HouseData : ScriptableObject
    {
        public string id;
        public string displayName;
        public List<Element> accepts = new List<Element>();
        public int price;
        public int[] capacity = { 2, 3, 4 };
        public int[] coinCap = { 100, 200, 300 };
        public int[] upgradeCost = { 200, 400 };
        public Color tint = Color.white;
        public AssetReferenceTexture2D sprite;

        public int MaxLevel => capacity.Length;
        public int Capacity(int level) => capacity[Mathf.Clamp(level - 1, 0, capacity.Length - 1)];
        public int CoinCap(int level) => coinCap[Mathf.Clamp(level - 1, 0, coinCap.Length - 1)];
        public int UpgradeCost(int level) => level < MaxLevel ? upgradeCost[level - 1] : -1;
        public bool Accepts(Element e) => accepts.Contains(e);

        public string AcceptLabel() => string.Join("·", accepts.ConvertAll(Affinity.Label));
    }
}
