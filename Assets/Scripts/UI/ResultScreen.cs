using CatVillage.Core;
using CatVillage.Data;
using TMPro;

namespace CatVillage.UI
{
    public class ResultScreen : GameScreen
    {
        public TMP_Text title;
        public TMP_Text body;
        public TMP_Text hint;
        public LabeledButton next;
        public LabeledButton retry;
        public LabeledButton stages;
        public LabeledButton home;

        public StageData Stage;
        public bool Victory;
        public BattleReward Reward;

        protected override void Build()
        {
            title.text = Victory ? "승리!" : "패배…";
            title.color = Victory ? Ui.Ink : Ui.Muted;
            body.text = Victory
                ? $"{Ui.Stars(Reward.stars)}\n\n생선 코인 +{Reward.coins}\n츄르 +{Reward.treats}" +
                  (Reward.basket != null ? $"\n{Reward.basket.displayName} 바구니 획득!" : "")
                : "고양이들을 더 키워서 다시 도전해요\n속성 상성도 확인해 보세요";

            var nextStage = NextStage();
            next.gameObject.SetActive(nextStage != null);
            if (nextStage != null)
            {
                next.Text = $"다음: {nextStage.displayName}";
                next.OnClick(() => SceneFlow.Instance.GoBattle(nextStage));
            }
            hint.text = Reward?.basket != null
                ? $"{Reward.basket.displayName} 바구니는 부화소에 있어요. 마을에서 부화시켜 맞는 집으로 옮겨요."
                : Victory ? "" : "마을에서 츄르로 레벨을 올리거나 상성 유리한 고양이를 편성해요.";

            retry.OnClick(() => SceneFlow.Instance.GoBattle(Stage));
            stages.OnClick(() => SceneFlow.Instance.GoVillage(true));
            home.OnClick(() => SceneFlow.Instance.GoVillage());
        }

        StageData NextStage()
        {
            if (!Victory) return null;
            var list = Game.Config.stages;
            var i = list.IndexOf(Stage) + 1;
            return i > 0 && i < list.Count && Game.IsUnlocked(i) ? list[i] : null;
        }
    }
}
