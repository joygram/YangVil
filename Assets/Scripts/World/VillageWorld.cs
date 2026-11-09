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
        public VillageLayout layout;
        Vector2 houseSize => layout.gardenSize;
        const float catHeight = 1f;

        public event Action<string> BuildingTapped;
        public event Action<OwnedHouse> HouseTapped;
        public event Action<OwnedCat> CatTapped;

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
            if (AudioHub.Instance != null) AudioHub.Instance.PlayBgm(_game.Config.presentation.bgmVillage);
        }

        void OnDestroy()
        {
            if (_game != null) _game.Changed -= Rebuild;
            SpeechBubble.ClearArt();
            _scope?.Dispose();
        }

        void BuildStatic()
        {
            var art = _game.Config.presentation;
            var ground = WorldKit.Box(transform, "Ground", layout.bounds.center, layout.bounds.size, VillageIllustration.Grass, -100);
            ground.gameObject.AddComponent<BoxCollider2D>().size = Vector2.one;
            var camRig = ground.gameObject.AddComponent<VillageCamera>();
            camRig.cam = cam;
            camRig.bounds = layout.bounds;
            cam.transform.position = new Vector3(layout.cameraStart.x, layout.cameraStart.y, -10);
            cam.orthographicSize = layout.OverviewSize(cam.aspect);
            StartCoroutine(_scope.LoadSprite(art.villageBackground, new Vector2(0.5f, 0.5f), 100, s => FitBackground(ground, s)));

            var speech = art.speech;
            StartCoroutine(_scope.LoadSlicedSprite(speech.body, new Vector2(0.5f, 0.5f), speech.pixelsPerUnit, speech.sliceBorder,
                s => SpeechBubble.UseArt(speech, s, null)));
            StartCoroutine(_scope.LoadSprite(speech.tail, new Vector2(0.5f, 1f), speech.pixelsPerUnit, s => SpeechBubble.UseArt(speech, null, s)));

            BuildGroundLayout();
            if (PresentationSet.IsSet(layout.sceneryPrefab)) StartCoroutine(_scope.Instantiate(layout.sceneryPrefab, transform, _ => { }));
            else BuildLandscape();
            foreach (var b in layout.buildings)
            {
                var root = new GameObject(b.id).transform;
                root.SetParent(transform, false);
                root.localPosition = b.position;
                var body = Picture(root, "Body", b.motif, Vector2.zero, b.size, 10);
                Nameplate(root, b.label, new Vector2(0, -b.size.y * 0.34f), 2.1f);
                var id = b.id;
                WorldKit.Clickable(root.gameObject, b.size, () => BuildingTapped?.Invoke(id));
                StartCoroutine(_scope.LoadSprite(art.Building(b.id), new Vector2(0.5f, 0.5f), 100, s => Skin(body, s, b.size)));
            }
        }

        public static SpriteRenderer Picture(Transform parent, string name, VillageMotif motif, Vector2 position, Vector2 size, int order, Color? tint = null)
        {
            var r = WorldKit.Box(parent, name, position, size, tint ?? Color.white, order);
            r.sprite = VillageIllustration.Get(motif);
            r.transform.localScale = new Vector3(size.x / r.sprite.bounds.size.x, size.y / r.sprite.bounds.size.y, 1);
            return r;
        }

        void Nameplate(Transform parent, string title, Vector2 pos, float width)
        {
            Picture(parent, "Nameplate", VillageMotif.Panel, pos, new Vector2(width, 0.38f), 48);
            var text = WorldKit.Label(parent, font, title, pos, 2.25f, VillageIllustration.Ink, 50);
            text.outlineWidth = 0;
        }

        void Path(Vector2 a, Vector2 b, float width)
        {
            var count = Mathf.CeilToInt(Vector2.Distance(a, b) / 0.22f);
            for (var i = 0; i <= count; i++)
            {
                var at = Vector2.Lerp(a, b, i / (float)Mathf.Max(1, count));
                Picture(transform, "PathEdge", VillageMotif.Disc, at + Vector2.down * 0.05f, new Vector2(width + 0.12f, width * 0.69f), -91, VillageIllustration.Hex(0xa9b781));
                Picture(transform, "SandPath", VillageMotif.Disc, at, new Vector2(width, width * 0.65f), -90, VillageIllustration.Hex(0xead8b0));
            }
        }

        void BuildGroundLayout()
        {
            for (var i = 1; i < layout.path.Length; i++) Path(layout.path[i - 1], layout.path[i], 1.25f);
            foreach (var b in layout.buildings) Path(b.position + Vector2.down * b.size.y * 0.3f, NearestPath(b.position), 0.72f);
            foreach (var spot in layout.houseSlots) Path(spot + Vector2.down * 0.75f, NearestPath(spot), 0.72f);
            Picture(transform, "PlazaRim", VillageMotif.Disc, layout.plaza, new Vector2(3.1f, 2.15f), -89, VillageIllustration.Hex(0xc4b68f));
            Picture(transform, "Plaza", VillageMotif.Disc, layout.plaza + Vector2.up * 0.06f, new Vector2(2.95f, 2.0f), -88, VillageIllustration.Cream);
            Picture(transform, "YarnLandmark", VillageMotif.Yarn, layout.plaza + Vector2.up * 0.2f, Vector2.one * 1.35f, -60);
            foreach (var pos in layout.houseSlots)
                Picture(transform, "UnbuiltMeadow", VillageMotif.Disc, pos, houseSize, -85, VillageIllustration.Hex(0xc3d59b));
        }

        public void BuildLandscape()
        {
            Picture(transform, "Pond", VillageMotif.Pond, new Vector2(3.7f, -0.4f), new Vector2(2.6f, 1.7f), -78);
            for (var i = 0; i < 13; i++)
            {
                var y = layout.bounds.yMin + 0.7f + i * 1.8f;
                foreach (var side in new[] { -1f, 1f })
                {
                    var x = side * (5.35f + 0.3f * Mathf.Sin(i * 2.7f));
                    Picture(transform, "BorderTree", VillageMotif.Tree, new Vector2(x, y), Vector2.one * (2.2f + 0.3f * Mathf.Cos(i)), -65);
                    if (i % 2 == 0) Picture(transform, "BorderFlowers", VillageMotif.Flowers, new Vector2(x - side * 0.7f, y - 0.7f), Vector2.one * 0.85f, -64);
                }
            }
            for (var i = 0; i < 42; i++)
            {
                var x = Mathf.Sin(i * 7.13f) * 4.8f;
                var y = layout.bounds.yMin + 1 + Mathf.Repeat(i * 3.17f, layout.bounds.height - 2);
                Picture(transform, "MeadowFlowers", VillageMotif.Flowers, new Vector2(x, y), Vector2.one * 0.38f, -94);
            }
        }

        Vector2 NearestPath(Vector2 position)
        {
            var best = layout.path[0];
            var distance = float.MaxValue;
            for (var i = 1; i < layout.path.Length; i++)
            {
                var a = layout.path[i - 1];
                var d = layout.path[i] - a;
                var point = a + d * Mathf.Clamp01(Vector2.Dot(position - a, d) / Mathf.Max(0.001f, d.sqrMagnitude));
                if ((position - point).sqrMagnitude >= distance) continue;
                best = point; distance = (position - point).sqrMagnitude;
            }
            return best;
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
            var area = layout.waitingArea;
            foreach (var cat in waiting) SpawnCat(cat, area, _dynamic);

            if (_moving != null) Highlight(_moving);
        }

        void BuildHouse(OwnedHouse house)
        {
            var def = _game.HouseDef(house);
            var root = new GameObject(house.uid).transform;
            root.SetParent(_dynamic, false);
            root.localPosition = layout.HousePosition(house.slot);

            Picture(root, "GardenBorder", VillageMotif.Disc, Vector2.down * 0.07f, houseSize + Vector2.one * 0.14f, -52, VillageIllustration.Hex(0x91a77a));
            var yard = Picture(root, "Yard", VillageMotif.Disc, Vector2.zero, houseSize, -50, GardenTint(def.tint));
            var hut = Picture(root, "Hut", VillageMotif.House, new Vector2(0, houseSize.y / 2 - 0.45f), new Vector2(2.25f, 2.25f), 5, Color.Lerp(def.tint, Color.white, 0.8f));
            StartCoroutine(_scope.LoadSprite(def.sprite, new Vector2(0.5f, 0.5f), 100, s => Skin(hut, s, new Vector2(2.25f, 2.25f))));
            Nameplate(root, $"{def.displayName} Lv{house.level}", new Vector2(0, -houseSize.y / 2 + 0.14f), 2.5f);
            Picture(root, "FoodBowl", VillageMotif.Bowl, new Vector2(1.05f, -0.55f), Vector2.one * 0.52f, -2);
            Picture(root, "FlowerBed", VillageMotif.Flowers, new Vector2(-1.2f, 0.5f), Vector2.one * 0.85f, -3);
            var coins = WorldKit.Label(root, font, "", new Vector2(0.85f, houseSize.y / 2 + 0.45f), 2.7f, VillageIllustration.Cream, 60);
            WorldKit.Clickable(root.gameObject, houseSize, () => OnHouseTapped(house));

            var area = layout.WanderArea;
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
            var visual = new GameObject("Visual").transform;
            visual.SetParent(go.transform, false);
            var image = new GameObject("Sprite");
            image.transform.SetParent(visual, false);
            var actor = image.AddComponent<SpriteActor>();
            actor.Renderer.sortingOrder = 20;
            actor.SetTint(species.tint);
            actor.SetHeight(catHeight * CatData.VisualScaleAt(cat.level, cat.genes));
            var wanderer = go.AddComponent<CatWanderer>();
            wanderer.Init(actor, area);
            var bubble = WorldKit.Label(go.transform, font, "", new Vector2(0, actor.height + 0.3f), 2.4f, Color.white, 600);
            bubble.gameObject.AddComponent<SpeechBubble>();
            wanderer.SetMood(_game.Mood(cat), species.tint, bubble);
            _wanderers[cat.uid] = (cat, wanderer, species.tint, bubble);
            var c = cat;
            var drag = go.AddComponent<CatDrag>();
            var hitHeight = Mathf.Max(0.8f, actor.height);
            WorldKit.Clickable(go, new Vector2(1f, hitHeight), () => { if (!drag.TakeLongPress()) PetCat(c, wanderer); })
                .transform.GetComponent<BoxCollider2D>().offset = new Vector2(0, hitHeight / 2);
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
                .Where(w => w.wanderer != null && !w.wanderer.Busy && w.wanderer.ActorReady && !string.IsNullOrEmpty(w.cat.houseUid))
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
                {
                    v.yard.color = GardenTint(_game.HouseDef(house).tint);
                    RefreshCoins(house);
                }
        }

        public bool Moving => _moving != null;

        static Color GardenTint(Color tint) => Color.Lerp(VillageIllustration.Grass, tint, 0.15f);

        void Highlight(OwnedCat cat)
        {
            foreach (var house in _game.Data.houses)
            {
                if (!_houses.TryGetValue(house.uid, out var v)) continue;
                var allowed = _game.CanMoveTo(cat, house);
                v.yard.color = allowed ? new Color(0.78f, 0.9f, 0.55f) : new Color(0.65f, 0.64f, 0.57f);
                v.coins.text = allowed ? "↓ 이 집으로" : "× 이동 불가";
            }
        }

        void RefreshCoins(OwnedHouse house)
        {
            if (!_houses.TryGetValue(house.uid, out var v)) return;
            if (_moving != null) return;
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
