using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CatVillage.Battle;
using CatVillage.Core;
using CatVillage.Data;
using CatVillage.World;
using TMPro;
using UnityEngine;

namespace CatVillage.UI
{
    public class BattleHud : GameScreen
    {
        public TMP_Text title;
        public TMP_Text status;
        public RectTransform portraitRow;
        public RectTransform itemRow;
        public LabeledButton speed;
        public LabeledButton autoSkill;
        public LabeledButton pause;
        public LabeledButton portraitPrefab;
        public LabeledButton itemButtonPrefab;
        public GameObject cutIn;
        public TMP_Text cutInLabel;

        BattleWorld _world;
        readonly Dictionary<BattleUnit, LabeledButton> _portraits = new Dictionary<BattleUnit, LabeledButton>();
        readonly List<(ItemData item, LabeledButton button)> _items = new List<(ItemData, LabeledButton)>();

        protected override void Build()
        {
            _world = FindAnyObjectByType<BattleWorld>();
            StartCoroutine(Bind());
        }

        IEnumerator Bind()
        {
            while (_world.State == null) yield return null;
            OnWave(_world.State.Wave + 1, _world.State.WaveCount);
            _world.WaveStarted += OnWave;
            status.text = "";
            cutIn.SetActive(false);
            speed.OnClick(_world.ToggleSpeed);
            autoSkill.OnClick(_world.ToggleAutoSkill);
            pause.OnClick(_world.TogglePause);

            foreach (var ally in _world.State.Allies)
            {
                var p = Instantiate(portraitPrefab, portraitRow);
                var unit = ally;
                p.OnClick(() => _world.QueueSkill(unit));
                _portraits[ally] = p;
            }
            foreach (var item in Game.Config.items.Where(i => i.usableInBattle))
            {
                var b = Instantiate(itemButtonPrefab, itemRow);
                var it = item;
                b.OnClick(() => _world.UseItem(it));
                _items.Add((item, b));
            }

            _world.Narrated += OnNarrated;
            _world.Updated += Refresh;
            _world.SkillCutIn += OnCutIn;
            _world.Finished += OnFinished;
            Refresh();
        }

        void OnDestroy()
        {
            if (_world == null) return;
            _world.WaveStarted -= OnWave;
            _world.Narrated -= OnNarrated;
            _world.Updated -= Refresh;
            _world.SkillCutIn -= OnCutIn;
            _world.Finished -= OnFinished;
        }

        void OnNarrated(string text) => status.text = text;

        void OnWave(int wave, int total) => title.text = $"{_world.Stage.displayName}  WAVE {wave}/{total}";

        void OnCutIn(BattleUnit unit) => StartCoroutine(CutIn(unit));

        IEnumerator CutIn(BattleUnit unit)
        {
            cutInLabel.text = $"{unit.Name}\n{unit.SkillName}!";
            cutIn.SetActive(true);
            var rt = (RectTransform)cutIn.transform;
            var seconds = Game.Config.presentation.feel.cutInSeconds;
            for (var t = 0f; t < 1f; t += Time.unscaledDeltaTime / seconds)
            {
                rt.anchoredPosition = new Vector2(Mathf.Lerp(-1200, 0, Mathf.Min(1, t * 4)) + Mathf.Max(0, t - 0.75f) * 4 * 1200, rt.anchoredPosition.y);
                yield return null;
            }
            cutIn.SetActive(false);
        }

        void Refresh()
        {
            var state = _world.State;
            foreach (var (u, p) in _portraits)
            {
                var ready = u.Alive && u.GaugeFull;
                p.Interactable = ready && !state.AutoSkill;
                p.Highlight(ready);
                var tag = !u.Alive ? "쓰러짐" : state.IsSkillQueued(u) ? "예약됨" : ready ? "필살 준비!" : $"게이지 {u.Gauge}/{BattleUnit.GaugeMax}";
                p.Text = $"{u.Name}\n{tag}";
            }
            autoSkill.Text = state.AutoSkill ? "자동필살 ON" : "자동필살 OFF";
            autoSkill.Highlight(state.AutoSkill);
            speed.Text = $"{_world.Speed:0}x";
            pause.Text = _world.Paused ? "계속" : "정지";
            foreach (var (item, b) in _items)
            {
                b.Text = $"{item.displayName} x{Game.ItemCount(item)}";
                b.Interactable = _world.ItemsLeft > 0 && Game.ItemCount(item) > 0;
            }
        }

        void OnFinished(bool victory, BattleReward reward)
        {
            var stage = _world.Stage;
            Screens.Show<ResultScreen>(r =>
            {
                r.Stage = stage;
                r.Victory = victory;
                r.Reward = reward;
            });
        }
    }
}
