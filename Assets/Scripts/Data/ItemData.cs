using UnityEngine;

namespace CatVillage.Data
{
    public enum ItemKind { Heal, Revive, GaugeFill, Exp, HatchNow }

    [CreateAssetMenu(menuName = "CatVillage/Item")]
    public class ItemData : ScriptableObject
    {
        public string id;
        public string displayName;
        public ItemKind kind;
        public int value;
        public int price;
        public bool usableInBattle;
    }
}
