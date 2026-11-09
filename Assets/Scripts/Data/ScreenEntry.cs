using System;
using UnityEngine.AddressableAssets;

namespace YangVil.Data
{
    [Serializable]
    public class ScreenEntry
    {
        public string screenType;
        public AssetReferenceGameObject prefab;
    }
}
