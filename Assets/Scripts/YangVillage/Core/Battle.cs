using System;
using System.Collections.Generic;
using System.Linq;
using YangVillage.Core;

// Godot YangVillage game/scripts/battle (Affinity·BattleUnit·BattleState) 이식. 동작 동일.
namespace YangVillage.Battle
{
    // 기운 상성. element 테이블의 beats 칼럼 기준.
    public sealed class Affinity
    {
        readonly Db _db;
        readonly int _strong;
        readonly int _weak;

        public Affinity(Db db)
        {
            _db = db;
            _strong = db.ConfigInt("affinity_strong_pct");
            _weak = db.ConfigInt("affinity_weak_pct");
        }

        public bool Beats(string attacker, string defender) =>
            _db.Get("element", "element_id", attacker, "beats") == defender;

        // 서로 이기는 쌍(햇살·달빛)은 약점만 있고 반감 없음.
        public int MultiplierPct(string attacker, string defender)
        {
            if (Beats(attacker, defender)) return _strong;
            if (Beats(defender, attacker)) return _weak;
            return 100;
        }
    }

    public class BattleUnit
    {
        public const int GaugeMax = 3;

        public string Uid;
        public string Name;
        public int Level;
        public string ElementId;
        public string SpriteId;
        public string StyleId;
        public bool IsAlly;
        public bool IsBoss;
        public int MaxHp;
        public int Hp;
        public int Atk;
        public int Def;
        public int Spd;
        public int Gauge;
        public List<CardDef> Deck = new List<CardDef>();

        public bool Alive => Hp > 0;
        public bool GaugeFull => Gauge >= GaugeMax;

        public void AddGauge(int amount = 1) { Gauge = Math.Min(GaugeMax, Gauge + amount); }
    }

    public enum BattleOutcome { Ongoing, Victory, Defeat }

    public enum ActionKind { Attack, Skill }

    // 행동 카드 종류 (card 테이블 한 행).
    public class CardDef
    {
        public string Id;
        public string Name;
        public string Color;
        public int PowerPct;
        public int Hits;
        public int Gauge;
    }

    // 손패 한 장. Def 가 없으면 필살 카드.
    public class BattleCard
    {
        public BattleUnit Owner;
        public CardDef Def;
        public bool IsSkill => Def == null;
    }

    public class BattleRules
    {
        public int MaxTurns;
        public int FastClearTurns;
        public int SkillPowerPct;
        public int HandSize;
        public int PickCount;
        public int ChainBonusPct;
        public int ExtraPowerPct;
    }

    public class BattleAction
    {
        public BattleUnit Actor;
        public ActionKind Kind;
        public BattleCard Card;
        public bool Extra;
        public readonly List<(BattleUnit target, int damage, int affinityPct)> Hits = new List<(BattleUnit, int, int)>();
    }

    // 턴 = 손패에서 카드를 골라 우리 편이 차례로 행동 → 남은 적이 차례로 반격.
    public class BattleState
    {
        public const int MaxStars = 3;

        public readonly List<BattleUnit> Allies;
        public readonly List<BattleUnit> Enemies;
        public readonly List<BattleCard> Hand = new List<BattleCard>();
        public int Turn { get; private set; }
        public bool AutoSkill = true;
        public BattleUnit FocusTarget;
        public CardDef Chain { get; private set; }

        readonly Affinity _affinity;
        readonly BattleRules _rules;
        readonly Random _rng;
        readonly Queue<(BattleUnit actor, BattleCard card, bool extra)> _pending = new Queue<(BattleUnit, BattleCard, bool)>();
        readonly List<List<BattleUnit>> _waves;
        List<BattleCard> _deck = new List<BattleCard>();
        bool _dealt;

        public int Wave { get; private set; }
        public int WaveCount => _waves.Count;
        public int MaxTurns => _rules.MaxTurns;
        public int PickCount => _rules.PickCount;

        public BattleState(Affinity affinity, List<BattleUnit> allies, List<List<BattleUnit>> waves, BattleRules rules, Random rng)
        {
            _affinity = affinity;
            Allies = allies;
            _waves = waves;
            Enemies = new List<BattleUnit>(waves.Count > 0 ? waves[0] : new List<BattleUnit>());
            _rules = rules;
            _rng = rng;
        }

        bool WaveDown => Enemies.All(e => !e.Alive);

        public bool WaveCleared => WaveDown && Wave < _waves.Count - 1 && Allies.Any(a => a.Alive);

        public void AdvanceWave()
        {
            if (!WaveCleared) return;
            Wave++;
            Enemies.Clear();
            Enemies.AddRange(_waves[Wave]);
            _pending.Clear();
            FocusTarget = null;
        }

        public BattleOutcome Outcome
        {
            get
            {
                if (Allies.All(a => !a.Alive) || Turn > _rules.MaxTurns) return BattleOutcome.Defeat;
                if (WaveDown && Wave >= _waves.Count - 1) return BattleOutcome.Victory;
                return BattleOutcome.Ongoing;
            }
        }

        public bool NeedsCommand => Outcome == BattleOutcome.Ongoing && !WaveCleared && _pending.Count == 0;

        // 손패를 채운다. 덱이 모자라면 살아 있는 우리 편 카드로 다시 섞는다.
        public void Deal()
        {
            if (_dealt) return;
            _deck.RemoveAll(c => !c.Owner.Alive);
            if (_deck.Count < _rules.HandSize)
            {
                _deck = Allies.Where(a => a.Alive).SelectMany(a => a.Deck.Select(d => new BattleCard { Owner = a, Def = d })).ToList();
                for (var i = _deck.Count - 1; i > 0; i--)
                {
                    var j = _rng.Next(i + 1);
                    (_deck[i], _deck[j]) = (_deck[j], _deck[i]);
                }
            }
            var take = Math.Min(_rules.HandSize, _deck.Count);
            Hand.Clear();
            Hand.AddRange(_deck.GetRange(0, take));
            _deck.RemoveRange(0, take);
            _dealt = true;
        }

        public List<BattleCard> SkillCards() =>
            Allies.Where(a => a.Alive && a.GaugeFull).Select(a => new BattleCard { Owner = a }).ToList();

        public List<BattleCard> AutoPick()
        {
            Deal();
            var picks = new List<BattleCard>();
            if (AutoSkill) picks.AddRange(SkillCards().Take(_rules.PickCount));
            var need = _rules.PickCount - picks.Count;
            var chain = picks.Count == 0 ? Hand.GroupBy(c => c.Def).FirstOrDefault(g => g.Count() >= need) : null;
            picks.AddRange((chain?.AsEnumerable() ?? Hand.OrderByDescending(c => c.Def.PowerPct * c.Def.Hits)).Take(need));
            return picks;
        }

        public void Commit(IReadOnlyList<BattleCard> picks)
        {
            if (!NeedsCommand) return;
            Deal();
            var valid = picks.Distinct().Where(p => p.Owner.Alive && (p.IsSkill ? p.Owner.GaugeFull : Hand.Contains(p))).Take(_rules.PickCount).ToList();
            if (valid.Count == 0) valid = AutoPick();
            Turn++;
            _dealt = false;
            if (Outcome != BattleOutcome.Ongoing) return;

            var full = valid.Count == _rules.PickCount;
            Chain = full && valid.All(p => !p.IsSkill && p.Def == valid[0].Def) ? valid[0].Def : null;
            foreach (var pick in valid) _pending.Enqueue((pick.Owner, pick, false));
            if (full && valid.All(p => p.Owner == valid[0].Owner)) _pending.Enqueue((valid[0].Owner, null, true));
            foreach (var enemy in Enemies.Where(e => e.Alive).OrderByDescending(e => e.Spd)) _pending.Enqueue((enemy, null, false));
        }

        public BattleAction Step()
        {
            if (Outcome != BattleOutcome.Ongoing || WaveCleared) return null;
            if (_pending.Count == 0) Commit(AutoPick());
            while (_pending.Count > 0)
            {
                var (actor, card, extra) = _pending.Dequeue();
                if (!actor.Alive) continue;
                if (WaveDown)
                {
                    _pending.Clear();
                    break;
                }
                return actor.IsAlly ? AllyAct(actor, card, extra) : EnemyAct(actor);
            }
            return null;
        }

        BattleAction AllyAct(BattleUnit actor, BattleCard card, bool extra)
        {
            var skill = card != null && card.IsSkill;
            var action = new BattleAction { Actor = actor, Card = card, Extra = extra, Kind = skill ? ActionKind.Skill : ActionKind.Attack };
            var chain = Chain != null ? _rules.ChainBonusPct : 0;
            if (skill)
            {
                actor.Gauge = 0;
                Hit(action, actor, PickEnemyTarget(actor), _rules.SkillPowerPct);
            }
            else if (extra) Hit(action, actor, PickEnemyTarget(actor), _rules.ExtraPowerPct + chain);
            else
            {
                for (var i = 0; i < card.Def.Hits; i++) Hit(action, actor, PickEnemyTarget(actor), card.Def.PowerPct + chain);
                actor.AddGauge(card.Def.Gauge);
            }
            return action;
        }

        BattleAction EnemyAct(BattleUnit actor)
        {
            var action = new BattleAction { Actor = actor, Kind = ActionKind.Attack };
            Hit(action, actor, LowestHp(Allies), 100);
            return action;
        }

        BattleUnit PickEnemyTarget(BattleUnit actor)
        {
            if (FocusTarget != null && FocusTarget.Alive) return FocusTarget;
            FocusTarget = null;
            var alive = Enemies.Where(e => e.Alive).ToList();
            var strong = alive.Where(e => _affinity.Beats(actor.ElementId, e.ElementId)).ToList();
            return LowestHp(strong.Count > 0 ? strong : alive);
        }

        static BattleUnit LowestHp(IEnumerable<BattleUnit> units) =>
            units.Where(u => u.Alive).OrderBy(u => u.Hp).FirstOrDefault();

        void Hit(BattleAction action, BattleUnit attacker, BattleUnit target, int powerPct)
        {
            if (target == null) return;
            var aff = _affinity.MultiplierPct(attacker.ElementId, target.ElementId);
            var raw = attacker.Atk * (powerPct / 100.0) * (aff / 100.0) - target.Def / 2.0;
            var dmg = Math.Max(1, (int)Math.Round(raw));
            target.Hp = Math.Max(0, target.Hp - dmg);
            if (target.IsAlly && target.Alive) target.AddGauge();
            action.Hits.Add((target, dmg, aff));
        }

        public int Stars()
        {
            if (Outcome != BattleOutcome.Victory) return 0;
            var stars = 1;
            if (Allies.All(a => a.Alive)) stars++;
            if (Turn <= _rules.FastClearTurns) stars++;
            return stars;
        }
    }
}
