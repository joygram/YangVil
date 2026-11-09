using System.Linq;
using CatVillage.Data;

namespace CatVillage.Core
{
    public enum GuideTarget { None, Hatchery, Shop, ShopHouse, Collect, Party, Adventure }

    public readonly struct GuideStep
    {
        public readonly string Text;
        public readonly GuideTarget Target;
        public readonly OwnedHouse House;

        public GuideStep(string text, GuideTarget target, OwnedHouse house = null)
        {
            Text = text;
            Target = target;
            House = house;
        }
    }

    public static class Guide
    {
        public const int CollectThreshold = 30;

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

            var rich = data.houses.FirstOrDefault(h => game.Pending(h) >= CollectThreshold);
            if (rich != null)
                return new GuideStep($"{game.HouseDef(rich).displayName}에 코인 {game.Pending(rich)}개가 쌓였어요 → 받기", GuideTarget.Collect, rich);

            if (game.Party.Count() < Game.PartySize && data.cats.Count > game.Party.Count())
                return new GuideStep("파티에 빈자리가 있어요 → 고양이를 눌러 '파티 편성'", GuideTarget.Party);

            var next = NextStage(game);
            if (next != null)
                return new GuideStep($"모험 {next.displayName}에 도전해요", GuideTarget.Adventure);

            if (data.baskets.Count == 0)
                return new GuideStep($"새 고양이를 맞이해요 → 상점에서 털실 바구니 ({game.Config.basketPrice}코인)", GuideTarget.Shop);

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
