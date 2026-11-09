using CatVillage.Core;
using CatVillage.Data;
using TMPro;

namespace CatVillage.UI
{
    public class ResultScreen : GameScreen
    {
        public TMP_Text title;
        public TMP_Text body;
        public LabeledButton retry;
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
            retry.OnClick(() => SceneFlow.Instance.GoBattle(Stage));
            home.OnClick(() => SceneFlow.Instance.GoVillage());
        }
    }
}
