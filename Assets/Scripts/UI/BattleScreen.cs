using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CatVillage.Battle;
using CatVillage.Data;
using TMPro;
using UnityEngine;

namespace CatVillage.UI
{
    public class BattleScreen : GameScreen
    {
        public TMP_Text title;
        public TMP_Text status;
        public RectTransform enemyRow;
        public RectTransform allyRow;
        public RectTransform portraitRow;
        public RectTransform itemRow;
        public LabeledButton speed;
        public LabeledButton autoSkill;
        public LabeledButton pause;
        public UnitSlot slotPrefab;
        public LabeledButton portraitPrefab;
        public LabeledButton itemButtonPrefab;
        public DamagePopup popupPrefab;
        public float slotSpacing = 330f;

        public StageData Stage;

        BattleState _battle;
        readonly Dictionary<BattleUnit, UnitSlot> _slots = new Dictionary<BattleUnit, UnitSlot>();
        readonly Dictionary<BattleUnit, LabeledButton> _portraits = new Dictionary<BattleUnit, LabeledButton>();
        readonly List<(ItemData item, LabeledButton button)> _items = new List<(ItemData, LabeledButton)>();
        int _itemsUsed;
        bool _paused;
        float _speed = 1f;

        protected override void Build()
        {
            var allies = Game.Party.Select(c => BattleUnit.FromCat(Game.Species(c), c.level)).ToList();
            var enemies = Stage.enemies.Select(e => BattleUnit.FromEnemy(e, Stage.enemyLevel)).ToList();
            _battle = new BattleState(allies, enemies, Game.Config.maxBattleTurns, Stage.enemyLevel)
            {
                FastClearTurns = Game.Config.fastClearTurns
            };

            title.text = Stage.displayName;
            status.text = "";
            speed.OnClick(() =>
            {
                _speed = _speed > 1f ? 1f : 2f;
                speed.Text = $"{_speed:0}x";
            });
            autoSkill.OnClick(() =>
            {
                _battle.AutoSkill = !_battle.AutoSkill;
                RefreshHud();
            });
            pause.OnClick(() =>
            {
                _paused = !_paused;
                pause.Text = _paused ? "계속" : "정지";
            });

            foreach (var e in enemies) AddSlot(e);
            foreach (var a in allies)
            {
                AddSlot(a);
                var p = Instantiate(portraitPrefab, portraitRow);
                var unit = a;
                p.OnClick(() =>
                {
                    _battle.QueueSkill(unit);
                    RefreshHud();
                });
                _portraits[a] = p;
            }
            foreach (var item in Game.Config.items.Where(i => i.usableInBattle))
            {
                var b = Instantiate(itemButtonPrefab, itemRow);
                var it = item;
                b.OnClick(() => UseItem(it));
                _items.Add((item, b));
            }

            Layout(_battle.Enemies);
            Layout(_battle.Allies);
            RefreshHud();
            StartCoroutine(Run());
        }

        void AddSlot(BattleUnit u)
        {
            var slot = Instantiate(slotPrefab, u.IsAlly ? allyRow : enemyRow);
            slot.sprite.Bind(u.Sprites, u.Tint);
            slot.sprite.SetScale(u.Scale);
            slot.sprite.Face(!u.IsAlly);
            slot.nameLabel.text = u.Name;
            slot.gauge.gameObject.SetActive(u.IsAlly);
            slot.focusMarker.SetActive(false);
            slot.button.interactable = !u.IsAlly;
            slot.button.onClick.AddListener(() => ToggleFocus(u));
            _slots[u] = slot;
        }

        void Layout(List<BattleUnit> units)
        {
            var n = units.Count;
            for (var i = 0; i < n; i++)
            {
                var rt = (RectTransform)_slots[units[i]].transform;
                rt.anchoredPosition = new Vector2((i - (n - 1) / 2f) * slotSpacing, 0);
            }
        }

        void ToggleFocus(BattleUnit enemy)
        {
            if (!enemy.Alive) return;
            _battle.FocusTarget = _battle.FocusTarget == enemy ? null : enemy;
            RefreshHud();
        }

        void UseItem(ItemData item)
        {
            if (_itemsUsed >= Game.Config.battleItemLimit || Game.ItemCount(item) <= 0) return;
            var used = item.kind switch
            {
                ItemKind.Heal => _battle.UseHeal(item.value),
                ItemKind.Revive => _battle.UseRevive(item.value),
                ItemKind.GaugeFill => _battle.UseGaugeFill(),
                _ => false
            };
            if (!used) return;
            Game.ConsumeItem(item);
            _itemsUsed++;
            RefreshHud();
        }

        void RefreshHud()
        {
            foreach (var (u, slot) in _slots)
            {
                slot.hp.Set((float)u.Hp / u.MaxHp);
                slot.gauge.Set((float)u.Gauge / BattleUnit.GaugeMax);
                slot.sprite.Image.color = u.Alive ? u.Tint : new Color(0.3f, 0.3f, 0.3f, 0.5f);
                slot.focusMarker.SetActive(_battle.FocusTarget == u);
            }
            foreach (var (u, p) in _portraits)
            {
                var ready = u.Alive && u.GaugeFull;
                p.Interactable = ready && !_battle.AutoSkill;
                p.Highlight(ready);
                var tag = _battle.IsSkillQueued(u) ? "예약됨" : ready ? "필살 준비!" : $"게이지 {u.Gauge}/{BattleUnit.GaugeMax}";
                p.Text = $"{u.Name}\n{tag}";
            }
            autoSkill.Text = _battle.AutoSkill ? "자동필살 ON" : "자동필살 OFF";
            autoSkill.Highlight(_battle.AutoSkill);
            var left = Game.Config.battleItemLimit - _itemsUsed;
            foreach (var (item, b) in _items)
            {
                b.Text = $"{item.displayName} x{Game.ItemCount(item)}";
                b.Interactable = left > 0 && Game.ItemCount(item) > 0;
            }
        }

        float Wait(float seconds) => seconds / _speed;

        IEnumerator Run()
        {
            yield return new WaitForSeconds(0.6f);
            while (_battle.Outcome == BattleOutcome.Ongoing)
            {
                while (_paused) yield return null;
                var action = _battle.Step();
                if (action == null) break;
                yield return Animate(action);
                RefreshHud();
                yield return new WaitForSeconds(Wait(0.25f));
            }
            yield return new WaitForSeconds(0.8f);
            Finish();
        }

        static string Narrate(BattleAction a) => a.Kind switch
        {
            ActionKind.Skill => $"{a.Actor.Name}의 {a.Actor.SkillName}!",
            ActionKind.Summon => $"{a.Actor.Name}이(가) 부하를 불렀다!",
            ActionKind.Rage => $"{a.Actor.Name}이(가) 분노했다!",
            ActionKind.Drain => $"{a.Actor.Name}의 깃털 폭풍! 게이지가 줄었다",
            ActionKind.Charge => $"{a.Actor.Name}이(가) 힘을 모은다… (지금 공격하면 2배)",
            ActionKind.Blast => $"{a.Actor.Name}의 전체 공격!",
            _ => $"{a.Actor.Name}의 공격"
        };

        IEnumerator Animate(BattleAction action)
        {
            var actor = _slots[action.Actor];
            status.text = Narrate(action);

            if (action.Spawned != null)
            {
                AddSlot(action.Spawned);
                Layout(_battle.Enemies);
            }

            if (action.Hits.Count > 0)
            {
                actor.sprite.PlayAttack();
                var actorRt = (RectTransform)actor.sprite.transform;
                var target = _slots[action.Hits[0].target].transform;
                var toward = (Vector2)actorRt.parent.InverseTransformPoint(target.position);
                StartCoroutine(actor.sprite.Lunge(toward, Wait(0.35f)));
                yield return new WaitForSeconds(Wait(0.2f));
                foreach (var (t, dmg, aff) in action.Hits) Popup(_slots[t], dmg, aff, action.Kind == ActionKind.Skill);
            }
            else
            {
                StartCoroutine(actor.sprite.Flash(Ui.Good, Wait(0.3f)));
            }
            yield return new WaitForSeconds(Wait(0.45f));
        }

        void Popup(UnitSlot slot, int dmg, float aff, bool skill)
        {
            StartCoroutine(slot.sprite.Flash(Color.white, Wait(0.12f)));
            var tag = aff > 1f ? " 효과 굉장!" : aff < 1f ? " 별로…" : "";
            var color = aff > 1f ? Ui.Good : aff < 1f ? Ui.Muted : Color.white;
            var popup = Instantiate(popupPrefab, slot.popupAnchor);
            popup.Play($"-{dmg}{tag}", color, skill ? 1.3f : 1f, _speed);
        }

        void Finish()
        {
            var victory = _battle.Outcome == BattleOutcome.Victory;
            var reward = victory ? Game.ApplyVictory(Stage, _battle.Stars()) : null;
            if (!victory) Game.Commit();
            Screens.Show<ResultScreen>(r =>
            {
                r.Stage = Stage;
                r.Victory = victory;
                r.Reward = reward;
            });
        }
    }
}
