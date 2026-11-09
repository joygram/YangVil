using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CatVillage.Battle;
using CatVillage.Core;
using CatVillage.Data;
using TMPro;
using UnityEngine;

namespace CatVillage.World
{
    public class BattleWorld : MonoBehaviour
    {
        public Camera cam;
        public TMP_FontAsset font;
        public float unitHeight = 1.7f;
        public Vector2[] allySlots = { new Vector2(-1.9f, 0.6f), new Vector2(-2.5f, -1.2f), new Vector2(-1.9f, -3.0f) };
        public Vector2[] enemySlots = { new Vector2(1.9f, 0.6f), new Vector2(2.5f, -1.2f), new Vector2(1.9f, -3.0f), new Vector2(1.2f, -2.1f) };

        public event Action<string> Narrated;
        public event Action Updated;
        public event Action<BattleUnit> SkillCutIn;
        public event Action<bool, BattleReward> Finished;

        public BattleState State { get; private set; }
        public StageData Stage { get; private set; }
        public float Speed { get; private set; } = 1f;
        public bool Paused { get; private set; }

        class Actor
        {
            public BattleUnit Unit;
            public Transform Root;
            public SpriteActor Sprite;
            public WorldBar Hp;
            public WorldBar Gauge;
            public GameObject Focus;
            public Vector3 Home;
        }

        Game _game;
        AssetScope _scope;
        BattleFeel _feel;
        PresentationSet _art;
        readonly Dictionary<BattleUnit, Actor> _actors = new Dictionary<BattleUnit, Actor>();
        int _itemsUsed;
        Vector3 _camHome;

        public int ItemsLeft => _game.Config.battleItemLimit - _itemsUsed;

        void Start()
        {
            if (!GameBootstrap.Ready || SceneFlow.Instance.PendingStage == null) return;
            _game = GameBootstrap.Game;
            _scope = new AssetScope();
            _art = _game.Config.presentation;
            _feel = _art.feel;
            Stage = SceneFlow.Instance.PendingStage;
            _camHome = cam.transform.position;

            var allies = _game.Party.Select(c => BattleUnit.FromCat(_game.Species(c), c.level)).ToList();
            var enemies = Stage.enemies.Select(e => BattleUnit.FromEnemy(e, Stage.enemyLevel)).ToList();
            State = new BattleState(allies, enemies, _game.Config.maxBattleTurns, Stage.enemyLevel)
            {
                FastClearTurns = _game.Config.fastClearTurns
            };

            BuildBackground();
            for (var i = 0; i < allies.Count; i++) Spawn(allies[i], allySlots[i % allySlots.Length]);
            for (var i = 0; i < enemies.Count; i++) Spawn(enemies[i], enemySlots[i % enemySlots.Length]);

            var bgm = PresentationSet.IsSet(Stage.bgm) ? Stage.bgm : _art.bgmBattle;
            AudioHub.Instance?.PlayBgm(bgm);
            StartCoroutine(Run());
        }

        void OnDestroy()
        {
            Time.timeScale = 1f;
            _scope?.Dispose();
        }

        void BuildBackground()
        {
            var sky = WorldKit.Box(transform, "Sky", new Vector2(0, 2.5f), new Vector2(14, 9), new Color(0.62f, 0.82f, 0.95f), -100);
            WorldKit.Box(transform, "Ground", new Vector2(0, -3.8f), new Vector2(14, 6), new Color(0.55f, 0.72f, 0.42f), -99);
            var bg = PresentationSet.IsSet(Stage.background) ? Stage.background : _art.battleBackgroundFallback;
            StartCoroutine(_scope.LoadSprite(bg, new Vector2(0.5f, 0.5f), 100, s =>
            {
                sky.sprite = s;
                sky.color = Color.white;
                var h = cam.orthographicSize * 2;
                var k = h / s.bounds.size.y;
                sky.transform.localPosition = Vector3.zero;
                sky.transform.localScale = new Vector3(k, k, 1);
            }));
        }

        void Spawn(BattleUnit u, Vector2 slot)
        {
            var root = new GameObject(u.Name).transform;
            root.SetParent(transform, false);
            root.localPosition = new Vector3(slot.x, slot.y, slot.y * 0.01f);

            var spriteGo = new GameObject("Sprite");
            spriteGo.transform.SetParent(root, false);
            var sprite = spriteGo.AddComponent<SpriteActor>();
            sprite.SetTint(u.Tint);
            sprite.SetHeight(unitHeight * u.Scale);
            sprite.Face(u.IsAlly);
            sprite.Renderer.sortingOrder = 100 - Mathf.RoundToInt(slot.y * 10);
            StartCoroutine(_scope.LoadFrames(u.Sprites, sprite.SetFrames));

            var top = unitHeight * u.Scale + 0.25f;
            var hp = WorldBar.Create(root, new Vector2(0, top), new Vector2(1.2f, 0.14f), u.IsAlly ? new Color(0.35f, 0.9f, 0.35f) : new Color(0.95f, 0.35f, 0.3f), 200);
            WorldBar gauge = null;
            if (u.IsAlly) gauge = WorldBar.Create(root, new Vector2(0, top - 0.2f), new Vector2(1.2f, 0.08f), new Color(1f, 0.85f, 0.2f), 200);
            WorldKit.Label(root, font, u.Name, new Vector2(0, top + 0.3f), 2.6f, Color.white, 210);
            var focus = WorldKit.Label(root, font, "▼", new Vector2(0, top + 0.75f), 5f, Color.red, 210).gameObject;
            focus.SetActive(false);

            if (!u.IsAlly)
            {
                var unit = u;
                var c = WorldKit.Clickable(root.gameObject, new Vector2(1.4f, unitHeight * u.Scale), () => ToggleFocus(unit));
                c.GetComponent<BoxCollider2D>().offset = new Vector2(0, unitHeight * u.Scale / 2);
            }

            _actors[u] = new Actor { Unit = u, Root = root, Sprite = sprite, Hp = hp, Gauge = gauge, Focus = focus, Home = root.localPosition };
            RefreshActor(_actors[u]);
        }

        void RefreshActor(Actor a)
        {
            a.Hp.Set((float)a.Unit.Hp / a.Unit.MaxHp);
            a.Gauge?.Set((float)a.Unit.Gauge / BattleUnit.GaugeMax);
            a.Focus.SetActive(State.FocusTarget == a.Unit);
            if (!a.Unit.Alive) a.Sprite.SetDead(true);
        }

        void RefreshAll()
        {
            foreach (var a in _actors.Values) RefreshActor(a);
            Updated?.Invoke();
        }

        public void ToggleFocus(BattleUnit enemy)
        {
            if (!enemy.Alive) return;
            State.FocusTarget = State.FocusTarget == enemy ? null : enemy;
            RefreshAll();
        }

        public void QueueSkill(BattleUnit ally)
        {
            State.QueueSkill(ally);
            RefreshAll();
        }

        public void ToggleAutoSkill()
        {
            State.AutoSkill = !State.AutoSkill;
            RefreshAll();
        }

        public void ToggleSpeed()
        {
            Speed = Speed > 1f ? 1f : 2f;
            if (!Paused) Time.timeScale = Speed;
            Updated?.Invoke();
        }

        public void TogglePause()
        {
            Paused = !Paused;
            Time.timeScale = Paused ? 0f : Speed;
            Updated?.Invoke();
        }

        public bool UseItem(ItemData item)
        {
            if (ItemsLeft <= 0 || _game.ItemCount(item) <= 0) return false;
            var used = item.kind switch
            {
                ItemKind.Heal => State.UseHeal(item.value),
                ItemKind.Revive => State.UseRevive(item.value),
                ItemKind.GaugeFill => State.UseGaugeFill(),
                _ => false
            };
            if (!used) return false;
            _game.ConsumeItem(item);
            _itemsUsed++;
            foreach (var a in _actors.Values)
                if (a.Unit.IsAlly && a.Unit.Alive) a.Sprite.SetDead(false);
            RefreshAll();
            return true;
        }

        IEnumerator Run()
        {
            Time.timeScale = Speed;
            yield return new WaitForSeconds(0.8f);
            while (State.Outcome == BattleOutcome.Ongoing)
            {
                var action = State.Step();
                if (action == null) break;
                yield return Animate(action);
                RefreshAll();
                yield return new WaitForSeconds(0.2f);
            }
            yield return new WaitForSeconds(0.8f);
            Time.timeScale = 1f;
            var victory = State.Outcome == BattleOutcome.Victory;
            var reward = victory ? _game.ApplyVictory(Stage, State.Stars()) : null;
            if (!victory) _game.Commit();
            Finished?.Invoke(victory, reward);
        }

        static string Narrate(BattleAction a) => a.Kind switch
        {
            ActionKind.Skill => $"{a.Actor.Name}의 {a.Actor.SkillName}!",
            ActionKind.Summon => $"{a.Actor.Name}이(가) 부하를 불렀다!",
            ActionKind.Rage => $"{a.Actor.Name}이(가) 분노했다!",
            ActionKind.Drain => $"{a.Actor.Name}의 깃털 폭풍!",
            ActionKind.Charge => $"{a.Actor.Name}이(가) 힘을 모은다…",
            ActionKind.Blast => $"{a.Actor.Name}의 전체 공격!",
            _ => $"{a.Actor.Name}의 공격"
        };

        IEnumerator Animate(BattleAction action)
        {
            var actor = _actors[action.Actor];
            var skill = action.Kind == ActionKind.Skill;
            Narrated?.Invoke(Narrate(action));

            if (action.Spawned != null)
            {
                var idx = State.Enemies.IndexOf(action.Spawned);
                Spawn(action.Spawned, enemySlots[idx % enemySlots.Length]);
            }

            if (action.Hits.Count == 0)
            {
                yield return actor.Sprite.Flash(new Color(1f, 0.9f, 0.4f), 0.3f);
                yield break;
            }

            if (skill)
            {
                SkillCutIn?.Invoke(action.Actor);
                AudioHub.Instance?.PlaySfx(_art.sfxSkill);
                yield return new WaitForSeconds(_feel.cutInSeconds);
            }

            var single = action.Hits.Count == 1;
            var target = _actors[action.Hits[0].target];
            var dest = single ? Vector3.Lerp(actor.Home, target.Home, _feel.lungeRatio) : actor.Home + new Vector3(action.Actor.IsAlly ? 0.8f : -0.8f, 0, 0);
            yield return Move(actor.Root, actor.Home, dest, 0.14f);

            actor.Sprite.Play(ActorPose.Attack);
            yield return new WaitForSeconds(0.08f);

            foreach (var (t, dmg, aff) in action.Hits) Impact(action.Actor, _actors[t], dmg, aff, skill);
            yield return HitStop(skill ? _feel.skillHitStopSeconds : _feel.hitStopSeconds);
            StartCoroutine(Shake(skill ? _feel.skillShakeStrength : _feel.shakeStrength));

            yield return new WaitForSeconds(0.15f);
            yield return Move(actor.Root, actor.Root.localPosition, actor.Home, 0.16f);
        }

        void Impact(BattleUnit attacker, Actor target, int dmg, float aff, bool skill)
        {
            AudioHub.Instance?.PlaySfx(_art.sfxHit);
            var fx = _art.Fx(attacker.Element);
            var fxRef = fx == null ? null : skill && PresentationSet.IsSet(fx.skill) ? fx.skill : fx.hit;
            var at = target.Root.position + new Vector3(0, unitHeight * target.Unit.Scale * 0.5f, -0.1f);
            StartCoroutine(_scope.Spawn(fxRef, at, transform, 2f));
            StartCoroutine(target.Sprite.Flash(Color.white, 0.08f));
            target.Sprite.Play(ActorPose.Hurt);
            StartCoroutine(Knockback(target, target.Unit.IsAlly ? -1 : 1));
            Popup(target, dmg, aff, skill);
            RefreshActor(target);
        }

        IEnumerator Knockback(Actor a, int dir)
        {
            var push = a.Home + new Vector3(dir * _feel.knockbackDistance / 100f, 0, 0);
            yield return Move(a.Root, a.Home, push, 0.06f);
            yield return Move(a.Root, push, a.Home, 0.12f);
        }

        static IEnumerator Move(Transform t, Vector3 from, Vector3 to, float seconds)
        {
            for (var k = 0f; k < 1f; k += Time.deltaTime / seconds)
            {
                t.localPosition = Vector3.Lerp(from, to, k);
                yield return null;
            }
            t.localPosition = to;
        }

        IEnumerator HitStop(float seconds)
        {
            if (seconds <= 0 || Paused) yield break;
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(seconds);
            Time.timeScale = Paused ? 0f : Speed;
        }

        IEnumerator Shake(float strength)
        {
            var amount = strength / 100f;
            for (var t = 0f; t < _feel.shakeSeconds; t += Time.unscaledDeltaTime)
            {
                var fade = 1 - t / _feel.shakeSeconds;
                cam.transform.position = _camHome + (Vector3)(UnityEngine.Random.insideUnitCircle * amount * fade);
                yield return null;
            }
            cam.transform.position = _camHome;
        }

        void Popup(Actor target, int dmg, float aff, bool skill)
        {
            var tag = aff > 1f ? "\n효과 굉장!" : aff < 1f ? "\n별로…" : "";
            var color = aff > 1f ? new Color(1f, 0.85f, 0.2f) : aff < 1f ? new Color(0.75f, 0.75f, 0.75f) : Color.white;
            var label = WorldKit.Label(transform, font, $"-{dmg}{tag}", (Vector2)target.Root.localPosition + new Vector2(0, unitHeight * target.Unit.Scale + 0.4f), skill ? 6f : 4.5f, color, 300);
            StartCoroutine(Rise(label));
        }

        static IEnumerator Rise(TextMeshPro label)
        {
            var start = label.transform.localPosition;
            for (var t = 0f; t < 1f; t += Time.deltaTime / 0.8f)
            {
                label.transform.localPosition = start + new Vector3(0, t * 0.9f, 0);
                label.alpha = 1 - t * t;
                yield return null;
            }
            Destroy(label.gameObject);
        }
    }
}
