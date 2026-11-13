using UnityEngine;
using UnityEngine.UI;

// Godot YangVillage game/scripts/ui/Intro.cs 이식. screen_id = intro
namespace YangVillage.Ui
{
    // 첫 화면: 제목 · 마스코트 셋. 준비가 끝나면 잠깐 보여 주고 마을로 넘어간다.
    public class Intro : MonoBehaviour
    {
        const float MascotSize = 190;

        async void Start()
        {
            var root = (RectTransform)transform;
            Kit.Root(root, Kit.Sky);
            var grass = Kit.Rect(root, Kit.Grass).rectTransform;
            grass.anchorMin = Vector2.zero;
            grass.anchorMax = new Vector2(1, 0);
            grass.pivot = new Vector2(0.5f, 0);
            grass.offsetMin = Vector2.zero;
            grass.offsetMax = new Vector2(0, 420);

            var column = Kit.Column(root, 32, 18);
            Kit.CenterColumn(column);
            Kit.Label(column, App.T("ui_title"), 84);
            Kit.Label(column, App.T("ui_tagline"), 24);

            var mascots = Kit.Row(column, 4);
            mascots.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.LowerCenter;
            var db = App.Db;
            var starter = App.Game.CatSprite(db.Config("starter_cat_id"));
            for (var stage = 2; stage >= 0; stage--)
            {
                var size = MascotSize * db.ConfigInt("growth_scale_pct_" + stage) / 100f;
                SpriteView.Of(starter, size, mascots);
            }

            Kit.Label(column, App.T("ui_version", db.Config("version")), 16, Kit.Muted);
            if (App.Capturing) return;
            await Tw.Wait(db.ConfigInt("intro_hold_ms") / 1000f);
            if (this != null) App.Go("village");
        }
    }
}
