using UnityEngine;

namespace YangVil.Data
{
    public enum ItemKind { Heal, Revive, GaugeFill, Exp, HatchNow }

    [CreateAssetMenu(menuName = "YangVil/Item")]
    public class ItemData : ScriptableObject
    {
        public string id;
        public string displayName;
        public ItemKind kind;
        public int value;
        public int price;
        public bool usableInBattle;
        public UnityEngine.AddressableAssets.AssetReferenceTexture2D icon;
        public Color color = new Color(0.85f, 0.8f, 0.7f);
    }
}
