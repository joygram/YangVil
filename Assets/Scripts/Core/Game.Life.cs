using System;
using System.Collections.Generic;
using System.Linq;
using YangVil.Data;

namespace YangVil.Core
{
    public class ExpeditionReport
    {
        public int Runs;
        public int Coins;
        public int Treats;
        public int Exp;
        public long Seconds;
        public string Bark;
    }

    public partial class Game
    {
        public Personality PersonalityOf(OwnedCat cat) => (Personality)Math.Max(0, cat.personality);

        public OwnedCat Cat(string uid) => Data.cats.Find(c => c.uid == uid);

        public void SaveQuiet() => SaveSystem.Save(Data);

        public bool HasTried(string key) => Data.tried.Contains(key);

        public void MarkTried(string key)
        {
            if (!Data.tried.Contains(key)) Data.tried.Add(key);
        }

        public void Remember(OwnedCat cat, string text)
        {
            cat.memories.Insert(0, new CatMemory { text = text, unix = Now });
            if (cat.memories.Count > CatLife.MemoryLimit) cat.memories.RemoveRange(CatLife.MemoryLimit, cat.memories.Count - CatLife.MemoryLimit);
        }

        public void AddBond(OwnedCat cat, float amount)
        {
            DecayBond(cat);
            cat.bond = Math.Clamp(cat.bond + amount, 0f, 100f);
        }

        void DecayBond(OwnedCat cat)
        {
            if (cat.bondUnix <= 0) cat.bondUnix = Now;
            var hours = Math.Max(0, Now - cat.bondUnix) / 3600f;
            cat.bondUnix = Now;
            if (cat.bond <= CatLife.BondFloor) return;
            cat.bond = Math.Max(CatLife.BondFloor, cat.bond - hours * CatLife.BondDecayPerHour);
        }

        public Relation Rel(OwnedCat x, OwnedCat y)
        {
            var (a, b) = string.CompareOrdinal(x.uid, y.uid) < 0 ? (x.uid, y.uid) : (y.uid, x.uid);
            var found = Data.relations.Find(x2 => x2.a == a && x2.b == b);
            if (found != null) return found;
            found = new Relation { a = a, b = b };
            Data.relations.Add(found);
            return found;
        }

        public float Affinity(OwnedCat x, OwnedCat y) => Rel(x, y).affinity;

        public List<Relation> Relations(OwnedCat cat) => Data.relations.Where(r => r.a == cat.uid || r.b == cat.uid).ToList();

        public OwnedCat Other(Relation r, OwnedCat cat) => Cat(r.a == cat.uid ? r.b : r.a);

        public OwnedCat BestFriend(OwnedCat cat) =>
            Relations(cat).Where(r => r.affinity > 0).OrderByDescending(r => r.affinity).Select(r => Other(r, cat)).FirstOrDefault(c => c != null);

        public OwnedCat Rival(OwnedCat cat) =>
            Relations(cat).Where(r => r.affinity < 0).OrderBy(r => r.affinity).Select(r => Other(r, cat)).FirstOrDefault(c => c != null);

        public IEnumerable<OwnedCat> Housemates(OwnedCat cat) =>
            Data.cats.Where(c => c != cat && c.houseUid == cat.houseUid && !Away(c));

        static void Shift(Relation r, float delta) => r.affinity = Math.Clamp(r.affinity + delta, -100f, 100f);

        void OnFavored(OwnedCat cat, float bond, Grievance cause, Func<OwnedCat, bool> envious)
        {
            AddBond(cat, bond);
            foreach (var other in Housemates(cat).Where(envious))
                Rel(cat, other).grievances[(int)cause] += 10f + other.bond / 10f;
        }

        public string Pet(OwnedCat cat)
        {
            OnFavored(cat, 3f, Grievance.Jealousy, o => o.bond >= 40f);
            MarkTried(Tried.Pet);
            SaveQuiet();
            return CatLife.Bark(PersonalityOf(cat), "pet");
        }

        public InteractionResult Interact(OwnedCat x, OwnedCat y)
        {
            var r = Rel(x, y);
            for (var i = 0; i < r.grievances.Length; i++) r.grievances[i] *= 0.97f;
            var home = House(x.houseUid);
            if (home != null && x.houseUid == y.houseUid && CatsIn(home).Count() >= HouseDef(home).Capacity(home.level))
                r.grievances[(int)Grievance.Territory] += 4f;

            var worst = Enumerable.Range(0, r.grievances.Length).OrderByDescending(i => r.grievances[i]).First();
            var cause = (Grievance)worst;
            var result = r.grievances[worst] >= CatLife.FightGrievance && r.affinity < 20f && _rng.NextDouble() < 0.5
                ? Fight(x, y, r, cause)
                : r.grievances[worst] >= CatLife.HissGrievance
                    ? new InteractionResult { Kind = Interaction.Hiss, Cause = cause, BarkA = CatLife.CauseBark(cause) }
                    : r.lastCause >= 0 && !r.madeUp && r.affinity >= -10f && _rng.NextDouble() < 0.35
                        ? MakeUp(x, y, r)
                        : Approach(x, y, r);
            Shift(r, CatLife.AffinityGain(result.Kind));
            SaveQuiet();
            return result;
        }

        InteractionResult Fight(OwnedCat x, OwnedCat y, Relation r, Grievance cause)
        {
            r.grievances[(int)cause] = 0f;
            r.lastCause = (int)cause;
            r.madeUp = false;
            x.cleanliness = Math.Max(0f, x.cleanliness - 15f);
            y.cleanliness = Math.Max(0f, y.cleanliness - 15f);
            Remember(x, $"{DisplayName(y)}와 싸웠다 ({CatLife.Label(cause)})");
            Remember(y, $"{DisplayName(x)}와 싸웠다 ({CatLife.Label(cause)})");
            return new InteractionResult { Kind = Interaction.Fight, Cause = cause, BarkA = CatLife.CauseBark(cause), BarkB = "하악!" };
        }

        InteractionResult MakeUp(OwnedCat x, OwnedCat y, Relation r)
        {
            r.madeUp = true;
            Remember(x, $"{DisplayName(y)}와 화해했다");
            Remember(y, $"{DisplayName(x)}와 화해했다");
            return new InteractionResult { Kind = Interaction.MakeUp, BarkA = CatLife.Bark(PersonalityOf(x), "makeup", DisplayName(y)) };
        }

        InteractionResult Approach(OwnedCat x, OwnedCat y, Relation r)
        {
            var want = CatLife.Pick(PersonalityOf(x), r.affinity, _rng);
            var key = want == Interaction.Groom ? "groom" : want == Interaction.Nap ? "nap" : "chase";
            var bark = CatLife.Bark(PersonalityOf(x), key, DisplayName(y));
            if (_rng.NextDouble() < CatLife.AcceptChance(PersonalityOf(y), r.affinity))
                return new InteractionResult { Kind = want, BarkA = bark };
            if (want == Interaction.Nap) r.grievances[(int)Grievance.Bed] += 12f;
            return new InteractionResult
            {
                Kind = Interaction.Reject, BarkA = CatLife.Bark(PersonalityOf(x), "rejected", DisplayName(y)),
                BarkB = CatLife.Bark(PersonalityOf(y), "refuse")
            };
        }

        public string BreedRefusal(OwnedCat a, OwnedCat b)
        {
            if (a == b) return "같은 고양이예요";
            if (CatData.GrowthStage(a.level) < 2 || CatData.GrowthStage(b.level) < 2) return "둘 다 성묘여야 해요";
            if (Away(a) || Away(b)) return "산책 중인 고양이가 있어요";
            if (CatCare.ProductionRate(a) < 1f || CatCare.ProductionRate(b) < 1f) return "둘 다 기분이 좋아야 해요 (밥·청결)";
            var affinity = Affinity(a, b);
            if (affinity < CatLife.BreedAffinity)
                return $"{CatLife.Bark(PersonalityOf(a), "dateNo", DisplayName(b))} (호감 {affinity:0}/{CatLife.BreedAffinity:0})";
            return null;
        }

        public IEnumerable<(OwnedCat a, OwnedCat b, float affinity)> BreedCandidates() =>
            Data.cats.SelectMany(a => Data.cats.Where(b => string.CompareOrdinal(a.uid, b.uid) < 0).Select(b => (a, b, Affinity(a, b))))
                .OrderByDescending(p => p.Item3);

        void LinkParents(OwnedCat child, Basket b)
        {
            child.parentA = b.parentA;
            child.parentB = b.parentB;
            foreach (var uid in new[] { b.parentA, b.parentB })
            {
                var parent = Cat(uid);
                if (parent == null) continue;
                Shift(Rel(child, parent), CatLife.ParentAffinity);
                Remember(parent, $"아기 {DisplayName(child)}가 태어났다");
            }
        }

        public bool Away(OwnedCat cat) => Data.expedition != null && Data.expedition.cats.Contains(cat.uid);

        public bool OnExpedition => Data.expedition != null && Data.expedition.cats.Count > 0;

        public StageData BestClearedStage() => Config.stages.LastOrDefault(s => Stars(s) > 0);

        public string StartExpedition()
        {
            if (OnExpedition) return "이미 산책 중이에요";
            var stage = BestClearedStage();
            if (stage == null) return "먼저 스테이지를 하나 클리어해 주세요";
            var party = Party.ToList();
            if (party.Count == 0) return "파티에 고양이가 없어요";
            Data.expedition = new Expedition { cats = party.Select(c => c.uid).ToList(), stageId = stage.id, startUnix = Now };
            MarkTried(Tried.Stroll);
            Commit();
            return null;
        }

        public long ExpeditionSeconds => OnExpedition ? Math.Min(CatLife.ExpeditionCapSeconds, Now - Data.expedition.startUnix) : 0;

        public ExpeditionReport ReturnExpedition()
        {
            if (!OnExpedition) return null;
            var stage = Config.stages.Find(s => s.id == Data.expedition.stageId) ?? BestClearedStage();
            var cats = Data.expedition.cats.Select(Cat).Where(c => c != null).ToList();
            var seconds = ExpeditionSeconds;
            var runs = (int)(seconds / CatLife.ExpeditionRunSeconds);
            var care = cats.Count == 0 ? 1f : cats.Average(c => { Care(c); return CatCare.ProductionRate(c); });
            var eff = CatLife.ExpeditionEfficiency * care;
            var report = new ExpeditionReport
            {
                Runs = runs, Seconds = seconds,
                Coins = (int)(runs * stage.rewardCoins * eff),
                Treats = (int)(runs * stage.rewardTreats * eff / 3f),
                Exp = (int)(runs * stage.enemyLevel * 20 * eff)
            };
            Data.coins += report.Coins;
            AddItem(Config.Item(ItemKind.Exp), report.Treats);
            Data.expedition = new Expedition();
            foreach (var cat in cats)
            {
                GainExp(cat, report.Exp);
                if (runs > 0) Remember(cat, $"{stage.displayName}에서 {runs}번 싸웠다");
            }
            report.Bark = cats.Count > 0 ? CatLife.Bark(PersonalityOf(cats[0]), "expedition") : "";
            Commit();
            return report;
        }

        static string Seen(Episode e, OwnedCat c) => $"{e.Id}:{c.uid}";

        public Episode AvailableEpisode(OwnedCat cat)
        {
            Care(cat);
            return CatLife.Episodes.FirstOrDefault(e => !Data.episodesSeen.Contains(Seen(e, cat)) && e.Ready(this, cat));
        }

        public string EpisodeText(string line, OwnedCat cat)
        {
            var friend = BestFriend(cat) ?? Cat(cat.parentA) ?? Rival(cat);
            return line.Replace("{0}", DisplayName(cat)).Replace("{1}", friend != null ? DisplayName(friend) : "친구");
        }

        public string ResolveEpisode(Episode e, OwnedCat cat, EpisodeChoice choice)
        {
            Data.episodesSeen.Add(Seen(e, cat));
            MarkTried(Tried.Story);
            AddBond(cat, choice.Bond);
            var friend = BestFriend(cat) ?? Cat(cat.parentA);
            if (friend != null && choice.FriendAffinity != 0) Shift(Rel(cat, friend), choice.FriendAffinity);
            Data.coins += choice.Coins;
            Remember(cat, $"'{e.Title}' 이야기");
            Commit();
            return EpisodeText(choice.After, cat);
        }

        public string RelationSummary(OwnedCat cat)
        {
            var friend = BestFriend(cat);
            var rival = Rival(cat);
            var conflict = Relations(cat).Where(r => r.lastCause >= 0 && !r.madeUp).OrderBy(r => r.affinity).FirstOrDefault();
            var parts = new List<string>
            {
                friend != null ? $"친구 {DisplayName(friend)}({Affinity(cat, friend):0})" : "친구 없음",
                rival != null ? $"앙숙 {DisplayName(rival)}({Affinity(cat, rival):0})" : "앙숙 없음"
            };
            var line = string.Join(" · ", parts);
            if (conflict != null && Other(conflict, cat) is { } foe)
                line += $"\n최근 갈등: {DisplayName(foe)}와 {CatLife.Label((Grievance)conflict.lastCause)} → {CatLife.Fix((Grievance)conflict.lastCause)}";
            return line;
        }
    }
}
