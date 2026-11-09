using UnityEngine;

namespace YangVil.Data
{
    public enum EnemyRole { Swarm, Ranged, Tank }

    [CreateAssetMenu(menuName = "YangVil/Enemy Base")]
    public class EnemyBase : ScriptableObject
    {
        public string id;
        public string displayName;
        public EnemyRole role;
        public Stats baseStats;
        public Stats growth;
        public SpriteSet sprites;
        public Color baseTint = Color.white;
        public float scale = 1f;
    }
}
