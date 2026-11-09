using System;
using UnityEngine.AddressableAssets;

namespace CatVillage.Data
{
    [Serializable]
    public class ScreenEntry
    {
        public string screenType;
        public AssetReferenceGameObject prefab;
    }
}
