using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CatVillage.Data
{
    [Serializable]
    public class AssetReferenceAudioClip : AssetReferenceT<AudioClip>
    {
        public AssetReferenceAudioClip(string guid) : base(guid) { }
    }

    [Serializable]
    public class ElementFx
    {
        public Element element;
        public AssetReferenceGameObject hit;
        public AssetReferenceGameObject skill;
    }

    [Serializable]
    public class BuildingArt
    {
        public string buildingId;
        public AssetReferenceTexture2D sprite;
    }

    [Serializable]
    public class BattleFeel
    {
        public float hitStopSeconds = 0.06f;
        public float skillHitStopSeconds = 0.12f;
        public float shakeStrength = 14f;
        public float skillShakeStrength = 28f;
        public float shakeSeconds = 0.18f;
        public float knockbackDistance = 28f;
        public float lungeRatio = 0.55f;
        public float cutInSeconds = 0.7f;
    }

    [Serializable]
    public class PresentationSet
    {
        public AssetReferenceTexture2D villageBackground;
        public AssetReferenceTexture2D basketIcon;
        public AssetReferenceTexture2D battleBackgroundFallback;
        public List<BuildingArt> buildings = new List<BuildingArt>();
        public List<ElementFx> elementFx = new List<ElementFx>();
        public AssetReferenceGameObject hatchFx;
        public AssetReferenceGameObject levelUpFx;
        public AssetReferenceAudioClip bgmVillage;
        public AssetReferenceAudioClip bgmBattle;
        public AssetReferenceAudioClip sfxHit;
        public AssetReferenceAudioClip sfxSkill;
        public AssetReferenceAudioClip sfxButton;
        public AssetReferenceAudioClip sfxCoin;
        public BattleFeel feel = new BattleFeel();

        public ElementFx Fx(Element e) => elementFx.Find(f => f.element == e);
        public AssetReferenceTexture2D Building(string id) => buildings.Find(b => b.buildingId == id)?.sprite;

        public static bool IsSet(AssetReference r) => r != null && r.RuntimeKeyIsValid();
    }
}
