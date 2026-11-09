using System.Linq;
using YangVil.Data;

namespace YangVil.Core
{
    public enum GuideTarget { None, Hatchery, Shop, ShopHouse, Collect, Party, Adventure, Pet, Card, Breed }

    public static class Tried
    {
        public const string Pet = "pet";
        public const string Card = "card";
        public const string Feed = "feed";
        public const string Story = "story";
        public const string Stroll = "stroll";
        public const string Breed = "breed";
    }

    public readonly struct GuideStep
    {
        public readonly string Text;
        public readonly GuideTarget Target;
        public readonly OwnedHouse House;
        public readonly OwnedCat Cat;

        public GuideStep(string text, GuideTarget target, OwnedHouse house = null, OwnedCat cat = null)
        {
            Text = text;
            Target = target;
            House = house;
            Cat = cat;
        }
    }

    public static class Guide
    {
        public const int CollectThreshold = 30;
        public const long StrollReturnSeconds = 30 * 60;

        public static GuideStep Next(Game game)
        {
            var data = game.Data;

            var ready = data.baskets.FirstOrDefault(game.IsReady);
            if (ready != null)
                return new GuideStep($"{game.Config.Cat(ready.speciesId).displayName} 바구니가 부화할 준비가 됐어요 → 부화소", GuideTarget.Hatchery);

            foreach (var cat in game.Waiting)
            {
                var species = game.Species(cat);
                if (data.houses.Any(h => game.CanMoveTo(cat, h)))
                    return new GuideStep($"부화소 앞 {species.displayName}을(를) 끌어서 초록색 집에 놓아요", GuideTarget.Hatchery);
                var house = game.Config.houses.FirstOrDefault(h => h.price > 0 && h.Accepts(species.element));
                if (house != null)
                    return new GuideStep($"{species.displayName}({Affinity.Label(species.element)})이 살 {house.displayName}을 지어요 ({house.price}코인) → 상점", GuideTarget.ShopHouse);
            }

            var home = data.cats.FirstOrDefault(c => !string.IsNullOrEmpty(c.houseUid) && !game.Away(c));
            if (home != null && !game.HasTried(Tried.Pet))
                return new GuideStep($"마당의 {game.DisplayName(home)}을(를) 톡 눌러 쓰다듬어 봐요 → 말을 걸어와요", GuideTarget.Pet, cat: home);
            if (home != null && !game.HasTried(Tried.Card))
                return new GuideStep($"{game.DisplayName(home)}을(를) 꾹 길게 눌러 봐요 → 성격·친구·기억 카드", GuideTarget.Card, cat: home);
            if (home != null && !game.HasTried(Tried.Feed) && game.ItemCount(game.Config.Item(ItemKind.Exp)) > 0)
                return new GuideStep($"{game.DisplayName(home)}에게 츄르를 줘 봐요 → 배부르고 유대가 쌓여요", GuideTarget.Card, cat: home);

            var storyCat = data.cats.FirstOrDefault(c => !game.Away(c) && game.AvailableEpisode(c) != null);
            if (storyCat != null)
                return new GuideStep($"{game.DisplayName(storyCat)}에게 이야기가 생겼어요 → 카드에서 '보기'", GuideTarget.Card, cat: storyCat);

            var feud = data.relations.FirstOrDefault(r => r.lastCause >= 0 && !r.madeUp && game.Cat(r.a) != null && game.Cat(r.b) != null);
            if (feud != null)
            {
                var a = game.Cat(feud.a);
                var cause = (Grievance)feud.lastCause;
                return new GuideStep($"{game.DisplayName(a)}와 {game.DisplayName(game.Cat(feud.b))}가 {CatLife.Label(cause)}로 싸웠어요 → {CatLife.Fix(cause)}",
                    GuideTarget.Card, cat: a);
            }

            var rich = data.houses.FirstOrDefault(h => game.Pending(h) >= CollectThreshold);
            if (rich != null)
                return new GuideStep($"{game.HouseDef(rich).displayName}에 코인 {game.Pending(rich)}개가 쌓였어요 → 받기", GuideTarget.Collect, rich);

            if (game.OnExpedition && game.ExpeditionSeconds >= StrollReturnSeconds)
                return new GuideStep($"산책 간 파티가 {game.ExpeditionSeconds / 60}분째 싸우는 중 → 모험에서 데려오기", GuideTarget.Adventure);

            if (!game.OnExpedition && game.Party.Count() < Game.PartySize && data.cats.Count(c => !game.Away(c)) > game.Party.Count())
                return new GuideStep("파티에 빈자리가 있어요 → 고양이를 눌러 '파티 편성'", GuideTarget.Party);

            var pair = game.BreedCandidates().FirstOrDefault(p => game.BreedRefusal(p.a, p.b) == null);
            if (pair.a != null && !game.HasTried(Tried.Breed))
                return new GuideStep($"{game.DisplayName(pair.a)}와 {game.DisplayName(pair.b)}가 서로 좋아해요 → 짝짓기 탑에서 소개", GuideTarget.Breed);

            var next = NextStage(game);
            if (next != null && !game.OnExpedition)
                return new GuideStep($"모험 {next.displayName}에 도전해요", GuideTarget.Adventure);

            if (!game.HasTried(Tried.Stroll) && game.BestClearedStage() != null && !game.OnExpedition)
                return new GuideStep("자리를 비울 땐 파티를 산책 보내요 → 모험 목록 맨 위", GuideTarget.Adventure);

            if (data.baskets.Count == 0)
                return new GuideStep($"새 고양이를 맞이해요 → 상점에서 털실 바구니 ({game.Config.basketPrice}코인)", GuideTarget.Shop);

            if (game.OnExpedition)
                return new GuideStep("파티가 산책 중이에요 → 남은 고양이들과 놀아 주세요", GuideTarget.None);

            return new GuideStep("츄르로 고양이를 키워 별 3개에 도전해요", GuideTarget.Adventure);
        }

        public static StageData NextStage(Game game)
        {
            var stages = game.Config.stages;
            for (var i = 0; i < stages.Count; i++)
                if (game.IsUnlocked(i) && game.Stars(stages[i]) == 0) return stages[i];
            return null;
        }
    }
}
