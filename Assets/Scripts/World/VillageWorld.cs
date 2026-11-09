using System;
using System.Collections.Generic;
using System.Linq;
using YangVil.Core;
using YangVil.Data;
using TMPro;
using UnityEngine;

namespace YangVil.World
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

            var waiting = _game.Waiting.Where(c => !_game.Away(c)).ToList();
            var area = new Rect(-4.2f, 3.9f, 3.8f, 1.1f);
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
            foreach (var cat in _game.CatsIn(house).Where(c => !_game.Away(c))) SpawnCat(cat, area, root);
            _houses[house.uid] = (root, yard, coins);
            RefreshCoins(house);
        }

        void SpawnCat(OwnedCat cat, Rect area, Transform parent)
        {
            var species = _game.Species(cat);
            var absent = Core.Game.Now - cat.bondUnix;
            var go = new GameObject(cat.uid);
            go.transform.SetParent(parent, false);
            var actor = go.AddComponent<SpriteActor>();
            actor.SetTint(species.tint);
            actor.SetHeight(catHeight * cat.genes.size * (0.75f + 0.125f * CatData.GrowthStage(cat.level)));
            var wanderer = go.AddComponent<CatWanderer>();
            wanderer.Init(actor, area);
            var bubble = WorldKit.Label(go.transform, font, "", new Vector2(0, 1.35f), 2.4f, Color.white, 600);
            bubble.gameObject.AddComponent<SpeechBubble>();
            wanderer.SetMood(_game.Mood(cat), species.tint, bubble);
            _wanderers[cat.uid] = (cat, wanderer, species.tint, bubble);
            var c = cat;
            var drag = go.AddComponent<CatDrag>();
            WorldKit.Clickable(go, new Vector2(1f, 1f), () => { if (!drag.TakeLongPress()) PetCat(c, wanderer); })
                .transform.GetComponent<BoxCollider2D>().offset = new Vector2(0, 0.5f);
            drag.cam = cam;
            drag.LongPressed = () => CatTapped?.Invoke(c);
            drag.Began = () => Highlight(c);
            drag.Hovered = p => HoverDrop(c, p);
            drag.Dropped = p => DropCat(c, p);
            StartCoroutine(_scope.LoadCatFrames(species.sprites, cat.genes, actor.SetFrames));
            if (absent >= CatLife.WelcomeAfterSeconds)
                wanderer.TryAmbientBark(CatLife.Bark(_game.PersonalityOf(cat), "welcome"));
        }

        public void PetCat(OwnedCat cat, CatWanderer wanderer)
        {
            if (wanderer.Busy) return;
            wanderer.Pet();
            wanderer.Say(_game.Pet(cat), 2f);
        }

        public const float InteractionInterval = 5f;
        float _nextInteraction;

        void TickInteractions()
        {
            if (Time.time < _nextInteraction) return;
            _nextInteraction = Time.time + InteractionInterval;
            var pairs = _wanderers.Values
                .Where(w => w.wanderer != null && !w.wanderer.Busy && w.wanderer.GetComponent<SpriteActor>().Ready && !string.IsNullOrEmpty(w.cat.houseUid))
                .GroupBy(w => w.cat.houseUid)
                .Where(g => g.Count() >= 2)
                .ToList();
            if (pairs.Count == 0) return;
            var group = pairs[UnityEngine.Random.Range(0, pairs.Count)].OrderBy(_ => UnityEngine.Random.value).Take(2).ToList();
            Interact(group[0].cat, group[1].cat);
        }

        public InteractionResult Interact(OwnedCat a, OwnedCat b)
        {
            if (!_wanderers.TryGetValue(a.uid, out var wa) || !_wanderers.TryGetValue(b.uid, out var wb)) return null;
            var result = _game.Interact(a, b);
            _nextInteraction = Time.time + InteractionLock;
            wa.wanderer.StartCoroutine(PlayInteraction(wa.wanderer, wb.wanderer, result));
            return result;
        }

        const float InteractionLock = 12f;
        const float ReplyDelay = 1.8f;

        static System.Collections.IEnumerator Reply(CatWanderer speaker, CatWanderer listener, string text)
        {
            yield return new WaitForSeconds(ReplyDelay);
            if (speaker == null || listener == null) yield break;
            speaker.Hush();
            listener.Say(text);
        }

        void EndInteraction() => _nextInteraction = Time.time + InteractionInterval;

        System.Collections.IEnumerator PlayInteraction(CatWanderer a, CatWanderer b, InteractionResult r)
        {
            var side = a.Spot.x <= b.Spot.x ? -0.55f : 0.55f;
            var meet = b.Spot + new Vector2(side, 0f);
            if (r.Kind == Interaction.Reject || r.Kind == Interaction.Hiss) meet = b.Spot + new Vector2(side * 1.8f, 0f);
            yield return a.WalkTo(meet);
            if (a == null || b == null) yield break;
            a.Face(b.Spot);
            b.Face(a.Spot);
            a.Say(r.BarkA);
            switch (r.Kind)
            {
                case Interaction.Groom:
                case Interaction.MakeUp:
                    a.Hearts(2);
                    b.Hearts(2);
                    yield return a.Hop(0.08f, 1.2f, 4);
                    break;
                case Interaction.Nap:
                    yield return Reply(a, b, "Zzz…");
                    yield return new WaitForSeconds(1.5f);
                    break;
                case Interaction.Chase:
                    StartCoroutine(b.Hop(0.2f, 0.6f, 2));
                    yield return b.WalkTo(b.Spot + new Vector2(-side * 1.6f, 0.3f));
                    yield return a.WalkTo(b.Spot + new Vector2(side, 0f));
                    break;
                case Interaction.Reject:
                    yield return Reply(a, b, r.BarkB);
                    yield return b.WalkTo(b.Spot + new Vector2(-side * 1.5f, 0f));
                    break;
                case Interaction.Hiss:
                    yield return Reply(a, b, "하악!");
                    yield return b.Hop(0.12f, 0.5f, 3);
                    break;
                case Interaction.Fight:
                    yield return Reply(a, b, r.BarkB);
                    yield return a.WalkTo(b.Spot + new Vector2(side * 0.3f, 0f));
                    StartCoroutine(b.Scuffle(1.6f));
                    yield return a.Scuffle(1.6f);
                    break;
            }
            yield return new WaitForSeconds(0.6f);
            if (a != null) a.Release();
            if (b != null) b.Release(2.5f);
            EndInteraction();
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

        public void Date(OwnedCat a, OwnedCat b)
        {
            var wa = WandererOf(a);
            var wb = WandererOf(b);
            if (wa == null || wb == null || wa.transform.parent != wb.transform.parent)
            {
                if (wa != null) wa.Hearts(4);
                if (wb != null) wb.Hearts(4);
                return;
            }
            wa.StartCoroutine(PlayDate(wa, wb, _game.PersonalityOf(a), _game.DisplayName(b)));
        }

        System.Collections.IEnumerator PlayDate(CatWanderer a, CatWanderer b, Personality p, string partner)
        {
            yield return a.WalkTo(b.Spot + new Vector2(-0.55f, 0f));
            a.Say(CatLife.Bark(p, "date", partner));
            var area = a.Area;
            for (var i = 0; i < 2 && a != null && b != null; i++)
            {
                var spot = new Vector2(UnityEngine.Random.Range(area.xMin + 0.6f, area.xMax), UnityEngine.Random.Range(area.yMin, area.yMax));
                b.StartCoroutine(b.WalkTo(spot));
                yield return a.WalkTo(spot + new Vector2(-0.55f, 0f));
            }
            if (a == null || b == null) yield break;
            a.Hearts(4);
            b.Hearts(4);
            yield return a.Hop(0.08f, 1.2f, 4);
            a.Release();
            b.Release();
        }

        public CatWanderer WandererOf(OwnedCat cat) => _wanderers.TryGetValue(cat.uid, out var w) ? w.wanderer : null;

        void Update()
        {
            if (_game == null) return;
            TickInteractions();
            if (Time.time < _nextCoinRefresh) return;
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
