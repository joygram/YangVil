using System.Collections.Generic;
using System.Linq;
using CatVillage.Data;
using UnityEngine;

namespace CatVillage.Battle
{
    public enum BattleOutcome { Ongoing, Victory, Defeat }

    public enum ActionKind { Attack, Skill, Summon, Rage, Drain, Charge, Blast, Heal, Gauge }

    public class BattleAction
    {
        public BattleUnit Actor;
        public ActionKind Kind;
        public readonly List<(BattleUnit target, int damage, float affinity)> Hits = new List<(BattleUnit, int, float)>();
        public BattleUnit Spawned;
    }

    public class BattleState
    {
        public readonly List<BattleUnit> Allies;
        public readonly List<BattleUnit> Enemies;
        public readonly int MaxTurns;
        public readonly int EnemyLevel;
        public int Turn { get; private set; }
        public bool AutoSkill = true;
        public BattleUnit FocusTarget;

        readonly Queue<BattleUnit> _order = new Queue<BattleUnit>();
        readonly HashSet<BattleUnit> _queuedSkills = new HashSet<BattleUnit>();
        readonly Dictionary<BattleUnit, int> _charging = new Dictionary<BattleUnit, int>();

        public BattleState(List<BattleUnit> allies, List<BattleUnit> enemies, int maxTurns, int enemyLevel)
        {
            Allies = allies;
            Enemies = enemies;
            MaxTurns = maxTurns;
            EnemyLevel = enemyLevel;
        }

        public BattleOutcome Outcome
        {
            get
            {
                if (Enemies.All(e => !e.Alive)) return BattleOutcome.Victory;
                if (Allies.All(a => !a.Alive) || Turn >= MaxTurns) return BattleOutcome.Defeat;
                return BattleOutcome.Ongoing;
            }
        }

        public IEnumerable<BattleUnit> AllUnits => Allies.Concat(Enemies);

        public BattleUnit PeekNext()
        {
            Refill();
            return _order.Count > 0 ? _order.Peek() : null;
        }

        public IEnumerable<BattleUnit> UpcomingOrder => _order.Where(u => u.Alive);

        public void QueueSkill(BattleUnit ally)
        {
            if (ally.IsAlly && ally.Alive && ally.GaugeFull) _queuedSkills.Add(ally);
        }

        public bool IsSkillQueued(BattleUnit u) => _queuedSkills.Contains(u);

        public BattleAction Step()
        {
            if (Outcome != BattleOutcome.Ongoing) return null;
            Refill();
            var actor = _order.Dequeue();
            while (!actor.Alive)
            {
                Refill();
                actor = _order.Dequeue();
            }
            Turn++;
            return actor.IsAlly ? AllyAct(actor) : EnemyAct(actor);
        }

        void Refill()
        {
            if (_order.Any(u => u.Alive)) return;
            _order.Clear();
            foreach (var u in AllUnits.Where(u => u.Alive).OrderByDescending(u => u.Spd).ThenBy(u => u.IsAlly ? 0 : 1))
                _order.Enqueue(u);
        }

        BattleAction AllyAct(BattleUnit actor)
        {
            var target = PickEnemyTarget(actor);
            var useSkill = actor.GaugeFull && (AutoSkill || _queuedSkills.Contains(actor));
            _queuedSkills.Remove(actor);
            var action = new BattleAction { Actor = actor, Kind = useSkill ? ActionKind.Skill : ActionKind.Attack };
            if (useSkill)
            {
                actor.Gauge = 0;
                Hit(action, actor, target, actor.SkillPower);
            }
            else
            {
                Hit(action, actor, target, 1f);
                actor.AddGauge();
            }
            return action;
        }

        BattleAction EnemyAct(BattleUnit actor)
        {
            var v = actor.Variant;
            if (v != null && v.isBoss) return BossAct(actor, v);
            var action = new BattleAction { Actor = actor, Kind = ActionKind.Attack };
            Hit(action, actor, LowestHp(Allies), 1f);
            return action;
        }

        BattleAction BossAct(BattleUnit boss, EnemyVariant v)
        {
            if (_charging.TryGetValue(boss, out var left))
            {
                if (left > 1)
                {
                    _charging[boss] = left - 1;
                    return new BattleAction { Actor = boss, Kind = ActionKind.Charge };
                }
                _charging.Remove(boss);
                var blast = new BattleAction { Actor = boss, Kind = ActionKind.Blast };
                foreach (var a in Allies.Where(a => a.Alive)) Hit(blast, boss, a, 1.2f);
                return blast;
            }

            var patternTurn = v.patternEvery > 0 && Turn % v.patternEvery == 0;
            switch (v.pattern)
            {
                case BossPattern.Summon when patternTurn && v.summon != null && Enemies.Count(e => e.Alive) < 3:
                    var minion = BattleUnit.FromEnemy(v.summon, EnemyLevel);
                    Enemies.Add(minion);
                    return new BattleAction { Actor = boss, Kind = ActionKind.Summon, Spawned = minion };
                case BossPattern.Rage when !boss.Raged && boss.Hp * 2 <= boss.MaxHp:
                    boss.Raged = true;
                    boss.Atk = (int)(boss.Atk * 1.5f);
                    boss.Spd = (int)(boss.Spd * 1.3f);
                    return new BattleAction { Actor = boss, Kind = ActionKind.Rage };
                case BossPattern.DrainGauge when patternTurn:
                    foreach (var a in Allies) a.Gauge = Mathf.Max(0, a.Gauge - 1);
                    var drain = new BattleAction { Actor = boss, Kind = ActionKind.Drain };
                    foreach (var a in Allies.Where(a => a.Alive)) Hit(drain, boss, a, 0.6f);
                    return drain;
                case BossPattern.ChargeBlast when patternTurn:
                    _charging[boss] = 2;
                    return new BattleAction { Actor = boss, Kind = ActionKind.Charge };
            }

            var action = new BattleAction { Actor = boss, Kind = ActionKind.Attack };
            Hit(action, boss, LowestHp(Allies), 1f);
            return action;
        }

        public bool IsCharging(BattleUnit u) => _charging.ContainsKey(u);

        BattleUnit PickEnemyTarget(BattleUnit actor)
        {
            if (FocusTarget != null && FocusTarget.Alive) return FocusTarget;
            FocusTarget = null;
            var alive = Enemies.Where(e => e.Alive).ToList();
            var strong = alive.Where(e => Affinity.Beats(actor.Element, e.Element)).ToList();
            return LowestHp(strong.Count > 0 ? strong : alive);
        }

        static BattleUnit LowestHp(IEnumerable<BattleUnit> units) =>
            units.Where(u => u.Alive).OrderBy(u => u.Hp).FirstOrDefault();

        void Hit(BattleAction action, BattleUnit attacker, BattleUnit target, float power)
        {
            if (target == null) return;
            var aff = Affinity.Multiplier(attacker.Element, target.Element);
            var taken = IsCharging(target) ? 2f : 1f;
            var dmg = Mathf.Max(1, Mathf.RoundToInt((attacker.Atk * power * aff - target.Def / 2f) * taken));
            target.Hp = Mathf.Max(0, target.Hp - dmg);
            if (target.IsAlly && target.Alive) target.AddGauge();
            action.Hits.Add((target, dmg, aff));
        }

        public int Stars()
        {
            if (Outcome != BattleOutcome.Victory) return 0;
            var stars = 1;
            if (Allies.All(a => a.Alive)) stars++;
            if (Turn <= FastClearTurns) stars++;
            return stars;
        }

        public int FastClearTurns = 10;

        public bool UseHeal(int percent)
        {
            var target = Allies.Where(a => a.Alive).OrderBy(a => (float)a.Hp / a.MaxHp).FirstOrDefault();
            if (target == null) return false;
            target.Hp = Mathf.Min(target.MaxHp, target.Hp + target.MaxHp * percent / 100);
            return true;
        }

        public bool UseRevive(int percent)
        {
            var target = Allies.FirstOrDefault(a => !a.Alive);
            if (target == null) return false;
            target.Hp = Mathf.Max(1, target.MaxHp * percent / 100);
            return true;
        }

        public bool UseGaugeFill()
        {
            var target = Allies.Where(a => a.Alive && !a.GaugeFull).OrderBy(a => a.Gauge).FirstOrDefault();
            if (target == null) return false;
            target.Gauge = BattleUnit.GaugeMax;
            return true;
        }
    }
}
