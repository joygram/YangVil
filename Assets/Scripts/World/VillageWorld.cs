using System;
using System.Collections.Generic;
using System.Linq;
using CatVillage.Core;
using CatVillage.Data;
using TMPro;
using UnityEngine;

namespace CatVillage.World
{
    public class VillageWorld : MonoBehaviour
    {
        public const string Hatchery = "hatchery";
        public const string Shop = "shop";
        public const string BreedTower = "breed";
        public const string Gate = "gate";
        public const string Dex = "dex";

        public Camera cam;
        public TMP_FontAsset font;
        static readonly Rect mapBounds = new Rect(-4.6f, -12.5f, 9.2f, 21.5f);
        static readonly Vector2 houseSize = new Vector2(2.7f, 2.8f);
        const float catHeight = 1f;
        static readonly Vector2[] houseSlots =
        {
            new Vector2(-2.9f, -1.6f), new Vector2(2.9f, -1.6f),
            new Vector2(-2.9f, -5.2f), new Vector2(2.9f, -5.2f),
            new Vector2(-2.9f, -8.8f), new Vector2(2.9f, -8.8f)
        };

        public event Action<string> BuildingTapped;
        public event Action<OwnedHouse> HouseTapped;
        public event Action<OwnedCat> CatTapped;

        static readonly (string id, string label, Vector2 pos, Vector2 size, Color color)[] Buildings =
        {
            (Hatchery, "부화소", new Vector2(-1.6f, 6.6f), new Vector2(3.0f, 2.2f), new Color(0.98f, 0.85f, 0.6f)),
            (Dex, "도감", new Vector2(2.6f, 6.6f), new Vector2(2.2f, 1.8f), new Color(0.85f, 0.8f, 1f)),
            (Shop, "상점", new Vector2(-2.6f, 2.6f), new Vector2(2.4f, 2.0f), new Color(0.75f, 0.85f, 1f)),
            (BreedTower, "짝짓기 탑", new Vector2(2.6f, 2.6f), new Vector2(2.2f, 2.4f), new Color(1f, 0.75f, 0.85f)),
            (Gate, "모험 게이트", new Vector2(0f, -11.2f), new Vector2(3.6f, 1.6f), new Color(0.6f, 0.5f, 0.4f))
        };

        Game _game;
        AssetScope _scope;
        Transform _dynamic;
        readonly Dictionary<string, (Transform root, SpriteRenderer yard, TextMeshPro coins)> _houses =
            new Dictionary<string, (Transform, SpriteRenderer, TextMeshPro)>();
        OwnedCat _moving;
        readonly Dictionary<string, (OwnedCat cat, CatWanderer wanderer, Color tint, TextMeshPro bubble)> _wanderers =
            new Dictionary<string, (OwnedCat, CatWanderer, Color, TextMeshPro)>();
        float _nextMoodRefresh;
        float _nextCoinRefresh;

        public Game Game => _game;

        void Start()
        {
            if (!GameBootstrap.Ready) return;
            _game = GameBootstrap.Game;
            _scope = new AssetScope();
            BuildStatic();
            _dynamic = new GameObject("Dynamic").transform;
            _dynamic.SetParent(transform, false);
            Rebuild();
            _game.Changed += Rebuild;
            AudioHub.Instance?.PlayBgm(_game.Config.presentation.bgmVillage);
        }

        void OnDestroy()
        {
            if (_game != null) _game.Changed -= Rebuild;
            _scope?.Dispose();
        }

        void BuildStatic()
        {
            var art = _game.Config.presentation;
            var ground = WorldKit.Box(transform, "Ground", mapBounds.center, mapBounds.size, new Color(0.56f, 0.78f, 0.45f), -100);
            ground.gameObject.AddComponent<BoxCollider2D>().size = Vector2.one;
            var camRig = ground.gameObject.AddComponent<VillageCamera>();
            camRig.cam = cam;
            camRig.bounds = mapBounds;
            StartCoroutine(_scope.LoadSprite(art.villageBackground, new Vector2(0.5f, 0.5f), 100, s => FitBackground(ground, s)));

            WorldKit.Box(transform, "Road", new Vector2(0, -2.5f), new Vector2(1.2f, 16f), new Color(0.85f, 0.75f, 0.55f), -90);

            foreach (var b in Buildings)
            {
                var root = new GameObject(b.id).transform;
                root.SetParent(transform, false);
                root.localPosition = b.pos;
                var body = WorldKit.Box(root, "Body", Vector2.zero, b.size, b.color, 10);
                WorldKit.Label(root, font, b.label, new Vector2(0, -b.size.y / 2 - 0.35f), 4, Color.white, 50);
                var id = b.id;
                WorldKit.Clickable(root.gameObject, b.size, () => BuildingTapped?.Invoke(id));
                StartCoroutine(_scope.LoadSprite(art.Building(b.id), new Vector2(0.5f, 0.5f), 100, s => Skin(body, s, b.size)));
            }
        }

        static void FitBackground(SpriteRenderer ground, Sprite s)
        {
            var size = ground.transform.localScale;
            ground.sprite = s;
            ground.color = Color.white;
            var b = s.bounds.size;
            ground.transform.localScale = new Vector3(size.x / b.x, size.y / b.y, 1);
            ground.GetComponent<BoxCollider2D>().size = b;
        }

        static void Skin(SpriteRenderer body, Sprite s, Vector2 size)
        {
            body.sprite = s;
            body.color = Color.white;
            var b = s.bounds.size;
            var k = Mathf.Min(size.x / b.x, size.y / b.y);
            body.transform.localScale = new Vector3(k, k, 1);
        }

        void Rebuild()
        {
            foreach (Transform child in _dynamic) Destroy(child.gameObject);
            _houses.Clear();
            _wanderers.Clear();

            foreach (var house in _game.Data.houses) BuildHouse(house);

            var waiting = _game.Waiting.ToList();
            var hatchery = Buildings[0];
            var area = new Rect(hatchery.pos.x - 2.5f, hatchery.pos.y - 2.6f, 5f, 0.8f);
            foreach (var cat in waiting) SpawnCat(cat, area, _dynamic);

            if (_moving != null) Highlight(_moving);
        }

        void BuildHouse(OwnedHouse house)
        {
            var def = _game.HouseDef(house);
            var root = new GameObject(house.uid).transform;
            root.SetParent(_dynamic, false);
            root.localPosition = houseSlots[Mathf.Clamp(house.slot, 0, houseSlots.Length - 1)];

            var yard = WorldKit.Box(root, "Yard", Vector2.zero, houseSize, Color.Lerp(def.tint, Color.white, 0.55f), -50);
            var hut = WorldKit.Box(root, "Hut", new Vector2(0, houseSize.y / 2 - 0.5f), new Vector2(1.6f, 1.1f), def.tint, 5);
            StartCoroutine(_scope.LoadSprite(def.sprite, new Vector2(0.5f, 0.5f), 100, s => Skin(hut, s, new Vector2(1.8f, 1.4f))));
            WorldKit.Label(root, font, $"{def.displayName} Lv{house.level}", new Vector2(0, -houseSize.y / 2 - 0.3f), 3.2f, Color.white, 50);
            var coins = WorldKit.Label(root, font, "", new Vector2(0, houseSize.y / 2 + 0.4f), 3.6f, new Color(1f, 0.9f, 0.3f), 60);
            WorldKit.Clickable(yard.gameObject, Vector2.one, () => OnHouseTapped(house));

            var area = new Rect(-houseSize.x / 2 + 0.4f, -houseSize.y / 2 + 0.3f, houseSize.x - 0.8f, houseSize.y - 1.4f);
            foreach (var cat in _game.CatsIn(house)) SpawnCat(cat, area, root);
            _houses[house.uid] = (root, yard, coins);
            RefreshCoins(house);
        }

        void SpawnCat(OwnedCat cat, Rect area, Transform parent)
        {
            var species = _game.Species(cat);
            var go = new GameObject(cat.uid);
            go.transform.SetParent(parent, false);
            var actor = go.AddComponent<SpriteActor>();
            actor.SetTint(species.tint);
            actor.SetHeight(catHeight * cat.genes.size * (0.75f + 0.125f * CatData.GrowthStage(cat.level)));
            var wanderer = go.AddComponent<CatWanderer>();
            wanderer.Init(actor, area);
            var bubble = WorldKit.Label(go.transform, font, "", new Vector2(0, 1.25f), 2.4f, Color.white, 600);
            wanderer.SetMood(_game.Mood(cat), species.tint, bubble);
            _wanderers[cat.uid] = (cat, wanderer, species.tint, bubble);
            var c = cat;
            WorldKit.Clickable(go, new Vector2(1f, 1f), () => CatTapped?.Invoke(c)).transform.GetComponent<BoxCollider2D>().offset = new Vector2(0, 0.5f);
            var drag = go.AddComponent<CatDrag>();
            drag.cam = cam;
            drag.Began = () => Highlight(c);
            drag.Hovered = p => HoverDrop(c, p);
            drag.Dropped = p => DropCat(c, p);
            StartCoroutine(_scope.LoadCatFrames(species.sprites, cat.genes, actor.SetFrames));
        }

        void HoverDrop(OwnedCat cat, Vector2 worldPoint)
        {
            Highlight(cat);
            var house = HouseAt(worldPoint);
            if (house == null || !_houses.TryGetValue(house.uid, out var v)) return;
            v.yard.color = _game.CanMoveTo(cat, house) ? new Color(0.35f, 1f, 0.35f) : new Color(1f, 0.45f, 0.4f);
        }

        public OwnedHouse HouseAt(Vector2 worldPoint)
        {
            foreach (var house in _game.Data.houses)
            {
                if (!_houses.TryGetValue(house.uid, out var v)) continue;
                var center = (Vector2)v.root.position;
                if (new Rect(center - houseSize / 2, houseSize).Contains(worldPoint)) return house;
            }
            return null;
        }

        public MoveResult? DropCat(OwnedCat cat, Vector2 worldPoint)
        {
            EndMove();
            var house = HouseAt(worldPoint);
            if (house == null) return null;
            var result = _game.MoveCat(cat, house);
            MoveAttempted?.Invoke(result);
            return result;
        }

        void OnHouseTapped(OwnedHouse house)
        {
            if (_moving != null)
            {
                var result = _game.MoveCat(_moving, house);
                if (result == MoveResult.Moved) EndMove();
                MoveAttempted?.Invoke(result);
                return;
            }
            HouseTapped?.Invoke(house);
        }

        public event Action<MoveResult> MoveAttempted;

        public void BeginMove(OwnedCat cat)
        {
            _moving = cat;
            Highlight(cat);
        }

        public void EndMove()
        {
            _moving = null;
            if (_game == null) return;
            foreach (var house in _game.Data.houses)
                if (_houses.TryGetValue(house.uid, out var v))
                    v.yard.color = Color.Lerp(_game.HouseDef(house).tint, Color.white, 0.55f);
        }

        public bool Moving => _moving != null;

        void Highlight(OwnedCat cat)
        {
            foreach (var house in _game.Data.houses)
            {
                if (!_houses.TryGetValue(house.uid, out var v)) continue;
                v.yard.color = _game.CanMoveTo(cat, house) ? new Color(0.6f, 1f, 0.55f) : new Color(0.5f, 0.5f, 0.5f);
            }
        }

        void RefreshCoins(OwnedHouse house)
        {
            if (!_houses.TryGetValue(house.uid, out var v)) return;
            var n = _game.Pending(house);
            v.coins.text = n > 0 ? $"● {n}" : "";
        }

        void Update()
        {
            if (_game == null || Time.time < _nextCoinRefresh) return;
            _nextCoinRefresh = Time.time + 1f;
            foreach (var house in _game.Data.houses) RefreshCoins(house);
            if (Time.time < _nextMoodRefresh) return;
            _nextMoodRefresh = Time.time + 20f;
            foreach (var (cat, wanderer, tint, bubble) in _wanderers.Values)
                if (wanderer != null) wanderer.SetMood(_game.Mood(cat), tint, bubble);
        }

        public void FocusOn(Vector2 position)
        {
            var p = cam.transform.position;
            cam.transform.position = new Vector3(position.x, position.y, p.z);
        }
    }
}
