using System.Linq;
using UnityEngine;

// Godot YangVillage game/scripts/ui/Result.cs 이식. screen_id = result
namespace YangVillage.Ui
{
    // 전투 결과: 별 · 보상 · 다음 행동.
    public class Result : MonoBehaviour
    {
        void Start()
        {
            var root = (RectTransform)transform;
            var game = App.Game;
            var won = App.Won && App.Reward != null;
            Kit.Root(root, won ? Kit.Sky : Kit.Night);
            var column = Kit.Column(root, 28, 16);
            Kit.CenterColumn(column);

            Kit.Label(column, App.T(won ? "ui_victory" : "ui_defeat"), 72, won ? Kit.Ink : Kit.Paper);
            var holder = Kit.Row(column);
            var mascot = SpriteView.Of(game.CatSprite(game.Party.Select(c => c.catId).DefaultIfEmpty(game.Db.Config("starter_cat_id")).First()), 190, holder);
            if (!won) mascot.Image.color = Kit.Down;

            var body = Kit.Card(column, 10);
            if (won)
            {
                var reward = App.Reward;
                Kit.Label(body, Kit.Stars(reward.stars), 48, Kit.AccentDark);
                var text = App.T("ui_result_reward", game.StageName(App.StageId), reward.coins, reward.treats);
                if (reward.basketCatId != null) text += App.T("ui_result_basket", App.T(game.Db.Get("cat", "cat_id", reward.basketCatId, "text_id")));
                Kit.Label(body, text, 22);
                if (reward.basketCatId != null) Kit.Label(body, App.T("ui_result_basket_hint"), 16, Kit.Muted);
            }
            else
            {
                Kit.Label(body, App.T("ui_result_defeat"), 22);
                Kit.Label(body, App.T("ui_result_defeat_hint"), 16, Kit.Muted);
            }

            var next = won ? game.NextStageId(App.StageId) : null;
            if (next != null)
                Kit.Button(column, App.T("ui_next", game.StageName(next)), () =>
                {
                    App.StageId = next;
                    App.Go("battle");
                }, 24);
            Kit.Button(column, App.T("ui_retry"), () => App.Go("battle"));
            var nav = Kit.Row(column);
            Kit.Button(nav, App.T("ui_stages"), () => App.Go("stage_select"));
            Kit.Button(nav, App.T("ui_home"), () => App.Go("village"));
        }
    }
}
