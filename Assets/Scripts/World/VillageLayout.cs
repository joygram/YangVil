using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace YangVil.World
{
    [Serializable]
    public class VillageBuildingPlacement
    {
        public string id;
        public string label;
        public Vector2 position;
        public Vector2 size;
        public VillageMotif motif;
        public VillageBuildingPlacement(string id, string label, Vector2 position, Vector2 size, VillageMotif motif)
        { this.id = id; this.label = label; this.position = position; this.size = size; this.motif = motif; }
    }

    [CreateAssetMenu(menuName = "YangVil/Village Layout")]
    public class VillageLayout : ScriptableObject
    {
        public AssetReferenceGameObject sceneryPrefab;
        public Rect bounds = new Rect(-6.2f, -12.5f, 12.4f, 23f);
        public Vector2 cameraStart = new Vector2(0, 2.3f);
        public float cameraSize = 7.4f;
        public Vector2 gardenSize = new Vector2(3.5f, 3.2f);
        public Rect waitingArea = new Rect(-3.5f, 3.2f, 2.8f, 0.9f);
        public Vector2 plaza = new Vector2(0, 2.1f);
        public Vector2[] houseSlots =
        {
            new Vector2(-2.6f, -0.7f), new Vector2(2.6f, -2.5f),
            new Vector2(-2.8f, -4.8f), new Vector2(2.7f, -6.5f),
            new Vector2(-2.6f, -8.6f), new Vector2(2.6f, -10.2f)
        };
        public Vector2[] path =
        {
            new Vector2(-1.8f, 6), new Vector2(0, 3), new Vector2(0.6f, 1),
            new Vector2(-0.3f, -2), new Vector2(0.6f, -5), new Vector2(-0.2f, -8), new Vector2(0, -11.4f)
        };
        public VillageBuildingPlacement[] buildings =
        {
            new VillageBuildingPlacement(VillageWorld.Hatchery, "바구니 부화소", new Vector2(-2.2f, 5.5f), new Vector2(3.2f, 3.2f), VillageMotif.Basket),
            new VillageBuildingPlacement(VillageWorld.Dex, "고양이 도서관", new Vector2(2.1f, 6.3f), new Vector2(2.7f, 2.7f), VillageMotif.Book),
            new VillageBuildingPlacement(VillageWorld.Shop, "생선 잡화점", new Vector2(-3.4f, 2.2f), new Vector2(2.6f, 2.6f), VillageMotif.Shop),
            new VillageBuildingPlacement(VillageWorld.BreedTower, "인연의 정원", new Vector2(3.3f, 2.6f), new Vector2(2.5f, 3.1f), VillageMotif.Tower),
            new VillageBuildingPlacement(VillageWorld.Gate, "숲길 모험", new Vector2(0, -11.4f), new Vector2(3.3f, 2.9f), VillageMotif.Gate)
        };
        public Vector2 HousePosition(int slot) => houseSlots[Mathf.Clamp(slot, 0, houseSlots.Length - 1)];
        public float OverviewSize(float aspect)
        {
            var halfWidth = 0f;
            foreach (var b in buildings) halfWidth = Mathf.Max(halfWidth, Mathf.Abs(b.position.x) + b.size.x / 2 + 0.2f);
            return Mathf.Max(cameraSize, halfWidth / Mathf.Max(0.1f, aspect));
        }
        public Rect WanderArea => new Rect(-gardenSize.x / 2 + 0.45f, -gardenSize.y / 2 + 0.6f, gardenSize.x - 0.9f, gardenSize.y - 1.7f);
    }
}
