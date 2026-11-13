using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using YangVillage.Battle;
using YangVillage.Core;

// Godot YangVillage game/scripts/ui/BattleScreen.cs 이식. screen_id = battle
// 그리기 층: 무대(배경·어둡게·전장) → HUD 바탕 → 조작 줄 → 전장 위 연출(별도 Canvas 정렬) → 번쩍·컷인.
namespace YangVillage.Ui
{
    // 전투: 왼쪽 우리 파티 · 오른쪽 적 무리가 마주 선 전장. 돌진 → 타격 → 넉백 → 복귀.
    public class BattleScreen : MonoBehaviour
    {
        const float Cell = 68;
        const float UnitHeight = 1.8f * Cell;
        const float FieldHeight = 700;
        static readonly Vector2 FieldOrigin = new Vector2(270, 380);
        static readonly Vector2[] AllySlots = { new Vector2(-2.2f, 1.2f), new Vector2(-2.65f, -1.2f), new Vector2(-2.1f, -3.6f) };
        static readonly Vector2[] EnemySlots = { new Vector2(2.2f, 1.2f), new Vector2(2.65f, -1.2f), new Vector2(2.1f, -3.6f), new Vector2(1f, -2.5f) };

        Game G => App.Game;
        Db Db => App.Db;
        BattleState _state;
        string _stageId;
        Node2 _stage, _field;
        Vector2 _stageHome;
        Vector2 _size;
        RectTransform _overlay;
        TextMeshProUGUI _title, _status;
        Image _dim, _flash;
        RectTransform _skillRow, _handRow;
        TaskCompletionSource<List<BattleCard>> _command;
        readonly List<BattleCard> _picks = new List<BattleCard>();
        readonly Dictionary<BattleCard, Button> _cardButtons = new Dictionary<BattleCard, Button>();
        bool _auto;
        Button _speedButton, _autoButton, _pauseButton;
        readonly Dictionary<BattleUnit, BattleActor> _actors = new Dictionary<BattleUnit, BattleActor>();
        readonly Dictionary<BattleUnit, (Image panel, TextMeshProUGUI label)> _panels = new Dictionary<BattleUnit, (Image, TextMeshProUGUI)>();
        readonly System.Random _rng = new System.Random();
        int _speed = 1;
        bool _paused;
        BattleBackdrop _backdrop;

        static Vector2 Slot(Vector2 slot) => new Vector2(slot.x * Cell, -slot.y * Cell);
        bool Here => this != null && isActiveAndEnabled;

        // 왼쪽 위 원점 노드 (Godot 화면 좌표).
        static RectTransform Space(Transform parent, string name, int sorting = -1)
        {
            var r = Kit.Node(name, parent);
            r.anchorMin = r.anchorMax = new Vector2(0, 1);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = Vector2.zero;
            if (sorting >= 0)
            {
                var c = r.gameObject.AddComponent<Canvas>();
                c.overrideSorting = true;
                c.sortingOrder = sorting;
            }
            return r;
        }

        void Start()
        {
            var root = (RectTransform)transform;
            Kit.Root(root, Kit.Grass);
            _stageId = App.StageId;
            _auto = Environment.GetCommandLineArgs().Contains("--auto");
            _state = G.BuildBattle(_stageId);
            Canvas.ForceUpdateCanvases();
            _size = root.rect.size;

            var world = Space(root, "world");
            _stage = Node2.Make<Node2>(world, "stage");
            _backdrop = PaintNode.Of<BattleBackdrop>(_stage.transform);
            _backdrop.Area = _size;
            _backdrop.FieldHeight = FieldHeight;
            _backdrop.Picture = Sprites.Frames(Db.Config("battle_backdrop_sprite_id"))[0];
            _backdrop.Layout();
            _dim = Kit.Node("dim", _stage.transform).gameObject.AddComponent<Image>();
            var dimRect = _dim.rectTransform;
            dimRect.anchorMin = dimRect.anchorMax = new Vector2(0.5f, 0.5f);
            dimRect.pivot = new Vector2(0, 1);
            dimRect.anchoredPosition = new Vector2(-_size.x / 4, _size.y / 4);
            dimRect.sizeDelta = _size * 1.5f;
            _dim.color = FxMath.A(Kit.Night, 0);
            _dim.raycastTarget = false;
            // 전장(배우·연출)은 Godot z_index 대로 HUD 위에 그린다.
            _field = Node2.Make<Node2>(_stage.transform, "field");
            _field.Position = FieldOrigin;
            var fieldCanvas = _field.gameObject.AddComponent<Canvas>();
            fieldCanvas.overrideSorting = true;
            fieldCanvas.sortingOrder = 1;
            var shade = PaintNode.Of<BattleBackdrop>(Space(root, "hud_shade"));
            shade.Area = _size;
            shade.FieldHeight = FieldHeight;
            shade.Redraw();

            var touch = Kit.Rect(root, Color.clear, true);
            touch.gameObject.AddComponent<Touch>().Pressed = OnFieldInput;

            var column = Kit.Column(root, 12, 6);
            _title = Kit.Label(column, "", 20, Kit.Paper);
            var statusPanel = Kit.Card(column);
            statusPanel.GetComponent<Image>().raycastTarget = false;
            _status = Kit.Label(statusPanel, App.T("ui_battle_start"), 17);
            Kit.Spacer(column);

            _skillRow = Kit.Row(column, 6);
            Kit.Layout(_skillRow).minHeight = 52;
            _handRow = Kit.Row(column, 6);
            Kit.Layout(_handRow).minHeight = 124;

            var panels = Kit.Row(column, 6);
            foreach (var ally in _state.Allies)
            {
                var panel = Kit.Panel(panels, Kit.Paper, Kit.Ink, 16);
                Kit.VBox(panel.rectTransform, 0, 10);
                Kit.Expand(panel);
                var label = Kit.Label(panel.transform, "", 13);
                _panels[ally] = (panel, label);
            }

            var controls = Kit.Row(column, 6);
            _speedButton = Kit.Button(controls, "", () =>
            {
                _speed = _speed > 1 ? 1 : Db.ConfigInt("battle_fast_speed");
                ApplyTime();
            }, 16);
            _autoButton = Kit.Button(controls, "", () =>
            {
                _auto = !_auto;
                if (_auto && _command != null) Finish(_state.AutoPick());
                RefreshAll();
            }, 16);
            _pauseButton = Kit.Button(controls, "", () =>
            {
                _paused = !_paused;
                ApplyTime();
            }, 16);
            Kit.Button(controls, App.T("ui_retreat"), () => App.Go("stage_select"), 16);

            _overlay = Space(root, "overlay", 2);
            _flash = Kit.Node("flash", _overlay).gameObject.AddComponent<Image>();
            _flash.rectTransform.anchorMin = _flash.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _flash.rectTransform.pivot = new Vector2(0, 1);
            _flash.rectTransform.anchoredPosition = Vector2.zero;
            _flash.rectTransform.sizeDelta = _size;
            _flash.color = FxMath.A(Color.white, 0);
            _flash.raycastTarget = false;

            for (var i = 0; i < _state.Allies.Count; i++) Spawn(_state.Allies[i], AllySlots[i % AllySlots.Length]);
            SpawnWave();
            RefreshAll();
            Run();
        }

        void OnDestroy() => Time.timeScale = 1;

        void ApplyTime()
        {
            Time.timeScale = _paused ? 0 : _speed;
            RefreshAll();
        }

        // 배치
        BattleActor Spawn(BattleUnit unit, Vector2 slot)
        {
            // 멀리(위) 선 유닛은 작게, 가까이(아래) 선 유닛은 크게.
            var near = Mathf.InverseLerp(AllySlots.Max(s => s.y), AllySlots.Min(s => s.y), slot.y);
            var depth = Mathf.Lerp(Db.ConfigInt("depth_far_pct"), Db.ConfigInt("depth_near_pct"), near) / 100f;
            var height = UnitHeight * depth * Db.ConfigInt(unit.IsBoss ? "boss_scale_pct" : unit.IsAlly ? "ally_scale_pct" : "enemy_scale_pct") / 100f;
            var actor = BattleActor.Of(_field.transform, unit, Slot(slot), height);
            _actors[unit] = actor;
            return actor;
        }

        void SpawnWave()
        {
            for (var i = 0; i < _state.Enemies.Count; i++) Spawn(_state.Enemies[i], EnemySlots[i % EnemySlots.Length]);
            _title.text = App.T("ui_battle_wave", G.StageName(_stageId), _state.Wave + 1, _state.WaveCount);
        }

        class Touch : MonoBehaviour, IPointerClickHandler
        {
            public Action<Vector2> Pressed;

            public void OnPointerClick(PointerEventData e)
            {
                if (e.button != PointerEventData.InputButton.Left) return;
                var rect = (RectTransform)transform;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, e.position, e.pressEventCamera, out var local)) return;
                Pressed?.Invoke(new Vector2(local.x - rect.rect.xMin, rect.rect.yMax - local.y));
            }
        }

        void OnFieldInput(Vector2 click)
        {
            var at = click - FieldOrigin;
            var hit = _actors.Values.FirstOrDefault(a => !a.Unit.IsAlly && a.Unit.Alive && a.HitBox.Contains(at));
            if (hit == null) return;
            _state.FocusTarget = _state.FocusTarget == hit.Unit ? null : hit.Unit;
            RefreshAll();
        }

        void RefreshAll()
        {
            foreach (var actor in _actors.Values) actor.Refresh(_state.FocusTarget == actor.Unit);
            foreach (var pair in _panels)
            {
                var unit = pair.Key;
                var (panel, label) = pair.Value;
                var ready = unit.Alive && unit.GaugeFull;
                var tag = !unit.Alive ? App.T("ui_tag_down") : ready ? App.T("ui_tag_ready") : App.T("ui_tag_gauge", unit.Gauge, BattleUnit.GaugeMax);
                label.text = App.T("ui_status", unit.Name, unit.Hp, unit.MaxHp, tag);
                panel.sprite = ready ? Kit.BoxSprite(Kit.Gold, Kit.Ink, 12) : Kit.BoxSprite(Kit.Paper, Kit.Ink, 16);
                Kit.Modulate(panel, unit.Alive ? Color.white : Kit.Down);
            }
            Kit.ButtonText(_speedButton).text = App.T("ui_speed", _speed);
            Kit.ButtonText(_autoButton).text = App.T(_auto ? "ui_auto_on" : "ui_auto_off");
            Kit.ButtonText(_pauseButton).text = App.T(_paused ? "ui_resume" : "ui_pause");
        }

        // 카드 고르기: 손패에서 정해진 장수를 고르면 바로 행동이 시작된다.
        Task<List<BattleCard>> Command()
        {
            _state.Deal();
            _command = new TaskCompletionSource<List<BattleCard>>();
            _picks.Clear();
            foreach (var card in _state.SkillCards()) CardButton(_skillRow, card, 15, new Vector2(150, 52));
            foreach (var card in _state.Hand) CardButton(_handRow, card, 14, new Vector2(0, 124));
            UpdateCommand();
            return _command.Task;
        }

        void CardButton(Transform row, BattleCard card, int fontSize, Vector2 minSize)
        {
            var color = card.IsSkill ? Kit.Night : Kit.Hex(card.Def.Color);
            var button = Kit.Button(row, "", () => Toggle(card), fontSize);
            Kit.Restyle(button, color, card.IsSkill ? Kit.Gold : Kit.Ink, 12);
            var le = Kit.Layout(button);
            le.minWidth = minSize.x;
            le.minHeight = minSize.y;
            le.flexibleWidth = card.IsSkill ? 0 : 1;
            if (card.IsSkill) le.preferredWidth = minSize.x;
            else
            {
                le.preferredWidth = 0;
                Kit.ButtonIcon(button, Sprites.Frames(card.Owner.SpriteId)[0], 64);
                button.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.UpperCenter;
            }
            Kit.ButtonText(button).color = card.IsSkill ? Kit.Gold : Kit.Paper;
            _cardButtons[card] = button;
        }

        void Toggle(BattleCard card)
        {
            if (_command == null) return;
            if (!_picks.Remove(card)) _picks.Add(card);
            UpdateCommand();
            if (_picks.Count >= Math.Min(_state.PickCount, _cardButtons.Count)) Finish(_picks.ToList());
        }

        void UpdateCommand()
        {
            foreach (var pair in _cardButtons)
            {
                var card = pair.Key;
                var button = pair.Value;
                var order = _picks.IndexOf(card);
                var name = card.IsSkill ? App.T("ui_card_skill", card.Owner.Name) : card.Def.Name;
                Kit.ButtonText(button).text = order < 0 ? name : App.T("ui_card_picked", order + 1, name);
                Kit.Modulate(button, order < 0 ? Color.white : new Color(0.6f, 0.6f, 0.6f));
            }
            _status.text = App.T("ui_command", _state.PickCount, _picks.Count);
        }

        void Finish(List<BattleCard> picks)
        {
            foreach (var button in _cardButtons.Values) Destroy(button.gameObject);
            _cardButtons.Clear();
            var command = _command;
            _command = null;
            command?.TrySetResult(picks);
        }

        void Dim(bool on) =>
            Tw.Create(_dim).Float(() => _dim.color.a, v => _dim.color = FxMath.A(_dim.color, v), on ? Db.ConfigInt("skill_dim_pct") / 100f : 0f, 0.15f);

        // 진행
        async Task<bool> Wait(float seconds, bool realTime = false)
        {
            await Tw.Wait(seconds, realTime);
            return Here;
        }

        async Task<bool> Move(Node2 node, Vector2 to, float seconds, Ease ease)
        {
            var tween = Tw.Create(node).Vec(() => node.Position, v => node.Position = v, to, seconds).SetTrans(Trans.Quad).SetEase(ease);
            await tween.Finished;
            return Here;
        }

        async void Run()
        {
            Time.timeScale = _speed;
            if (!await Advance()) return;
            while (_state.Outcome == BattleOutcome.Ongoing)
            {
                if (_state.WaveCleared)
                {
                    if (!await NextWave()) return;
                    continue;
                }
                if (_state.NeedsCommand)
                {
                    var picks = _auto ? _state.AutoPick() : await Command();
                    if (!Here) return;
                    _state.Commit(picks);
                    if (_state.Chain != null)
                    {
                        _status.text = App.T("ui_log_chain", _state.Chain.Name);
                        if (!await Wait(0.5f)) return;
                    }
                }
                var action = _state.Step();
                if (action == null) continue;
                if (!await Animate(action)) return;
                RefreshAll();
                if (!await Wait(0.2f)) return;
            }
            if (!await Wait(0.8f)) return;
            App.Won = _state.Outcome == BattleOutcome.Victory;
            App.Reward = App.Won ? G.ApplyVictory(_stageId, _state.Stars()) : null;
            App.Go("result");
        }

        async Task<bool> NextWave()
        {
            if (!await Wait(0.6f)) return false;
            _state.AdvanceWave();
            _status.text = App.T("ui_log_wave", _state.Wave + 1, _state.WaveCount);
            SpawnWave();
            if (!await Advance()) return false;
            foreach (var passed in _actors.Keys.Where(u => !u.IsAlly && !_state.Enemies.Contains(u)).ToList())
            {
                _actors[passed].Free();
                _actors.Remove(passed);
            }
            RefreshAll();
            return await Wait(0.3f);
        }

        static Vector2 Lerp(Vector2 a, Vector2 b, float t) => a + (b - a) * t;
        static float Angle(Vector2 v) => Mathf.Atan2(v.y, v.x);

        async Task<bool> Animate(BattleAction action)
        {
            var actor = _actors[action.Actor];
            var skill = action.Kind == ActionKind.Skill;
            _status.text = skill ? App.T("ui_log_skill", action.Actor.Name) : action.Extra ? App.T("ui_log_extra", action.Actor.Name)
                : action.Card != null ? App.T("ui_log_card", action.Actor.Name, action.Card.Def.Name) : App.T("ui_log_card", action.Actor.Name, App.T("ui_attack"));
            if (action.Hits.Count == 0)
            {
                actor.Glow();
                return await Wait(0.3f);
            }
            if (skill)
            {
                Dim(true);
                var seconds = Db.ConfigInt("cut_in_ms") / 1000f;
                SkillCutIn.Play(_overlay, _size, App.T("ui_cut_in", action.Actor.Name), Sprites.ElementColor(action.Actor.ElementId), seconds);
                if (!await Wait(seconds)) return false;
                // 기운 모으기: 발밑으로 고리가 조여들며 몸이 빛난다.
                Gather(action.Actor, actor);
                actor.Glow();
                if (!await Wait(0.32f)) return false;
            }

            var target = _actors[action.Hits[0].target];
            var side = action.Actor.IsAlly ? 1 : -1;
            var single = action.Hits.All(h => h.target == action.Hits[0].target);
            // 한 대상이면 바로 앞까지 붙고, 여럿이면 한 걸음만 나선다.
            var reach = single
                ? target.Home + new Vector2(-side * target.Head * Db.ConfigInt("reach_gap_pct") / 100f, 1)
                : actor.Home + new Vector2(side * Cell, 0);

            var moves = Style(action.Actor, "moves");
            actor.Crouch();
            if (!await Wait(0.14f)) return false;
            if (moves)
            {
                Puff(actor.Home);
                for (var i = 1; i <= 3; i++) Trail(actor, Lerp(actor.Home, reach, i / 4f), i * 0.025f);
                actor.ZIndex += 400;
                if (!await Move(actor, reach, 0.12f, Ease.In)) return false;
            }
            Zoom(single ? ((moves ? reach : target.Home) + target.Home) / 2 : Vector2.zero, Db.ConfigInt(skill ? "skill_zoom_pct" : "zoom_pct") / 100f, 0.08f);

            for (var i = 0; i < action.Hits.Count; i++)
            {
                var (unit, damage, affinityPct) = action.Hits[i];
                var struck = _actors[unit];
                var last = i == action.Hits.Count - 1;
                if (i > 0 && !await Wait(Db.ConfigInt("hit_gap_ms") / 1000f)) return false;
                var aim = (Chest(struck) - Chest(actor)).normalized;
                if (skill && i == 0 && !await Flurry(actor, struck, aim, moves)) return false;
                if (moves)
                {
                    // 당겼다 내지르는 끝에 타격을 맞춘다.
                    actor.Strike(aim, actor.Head * Db.ConfigInt("strike_reach_pct") / 100f);
                    if (!await Wait(BattleActor.StrikeLead)) return false;
                }
                else if (!await Launch(actor, struck, aim)) return false;
                if (Style(action.Actor, "slash") || skill) Slash(actor, struck, aim, moves, i % 2 == 0 ? 1 : -1, skill ? 1.25f : 1);
                Impact(action.Actor, struck, damage, affinityPct, skill || action.Extra, aim);
                var heavy = skill && last;
                Shake(Db.ConfigInt(heavy ? "skill_shake_px" : "shake_px"), aim);
                if (!await HitStop(heavy ? "skill_hit_stop_ms" : "hit_stop_ms")) return false;
            }

            if (!await Wait(Db.ConfigInt("shake_ms") / 1000f + 0.06f)) return false;
            Zoom(Vector2.zero, 1, 0.16f);
            if (moves)
            {
                actor.Hop(0.24f, actor.Head * 0.45f);
                if (!await Move(actor, actor.Home, 0.24f, Ease.Out)) return false;
                Puff(actor.Home);
                actor.ZIndex -= 400;
            }
            if (skill) Dim(false);
            return true;
        }

        bool Style(BattleUnit unit, string column) => Db.Get("attack_style", "attack_style_id", unit.StyleId, column) == "true";

        bool Burns(BattleUnit unit) => Db.Get("element", "element_id", unit.ElementId, "burns") == "true";

        static Color Tint(BattleUnit unit) => Color.Lerp(Sprites.ElementColor(unit.ElementId), Color.white, 0.3f);

        // 원거리: 쏘는 직업은 몸에서 곧장, 주문 직업은 발밑에 고리를 띄우고 대상 머리 위에서 떨어뜨린다.
        async Task<bool> Launch(BattleActor actor, BattleActor target, Vector2 aim)
        {
            var cast = Style(actor.Unit, "cast");
            var hit = Chest(target);
            var from = cast ? hit + new Vector2(-Mathf.Sign(aim.x) * target.Head * 1.3f, -target.Head * 2.6f) : Chest(actor) + aim * actor.Head * 0.4f;
            if (cast)
            {
                Gather(actor.Unit, actor);
                if (!await Wait(0.2f)) return false;
            }
            actor.Recoil(aim, actor.Head * 0.22f);
            var texture = Sprites.Frames(Db.Get("element", "element_id", actor.Unit.ElementId, "shot_sprite_id"))[0];
            var size = target.Head * (cast ? 0.9f : 0.6f) / texture.rect.height;
            // 그림은 머리가 오른쪽이다. 왼쪽으로 날 때는 돌린 뒤 위아래를 뒤집어 바로 세운다.
            var fly = hit - from;
            Sprite2 Shot(Vector2 at, float scale, int z)
            {
                var s = Sprite2.Of(_field.transform, texture);
                s.Position = at;
                s.Scale = Vector2.one * size * scale;
                s.Rotation = Angle(fly);
                s.FlipV = fly.x < 0;
                s.ZIndex = z;
                return s;
            }
            var shot = Shot(from, 1, 1500);
            const float flight = 0.14f;
            for (var i = 1; i <= 4; i++)
            {
                var tail = Shot(Lerp(from, hit, i / 5f), 0.8f, 1490);
                tail.Alpha = 0;
                Tw.Create(tail).Interval(flight * i / 5)
                    .Float(() => tail.Alpha, v => tail.Alpha = v, 0.45f, 0.01f)
                    .Float(() => tail.Alpha, v => tail.Alpha = v, 0f, 0.12f)
                    .Callback(tail.Free);
            }
            var here = await Move(shot, hit, flight, Ease.In);
            shot.Free();
            return here;
        }

        static Vector2 Chest(BattleActor actor) => actor.Position + new Vector2(0, -actor.Head * 0.5f);

        // 베기 궤적: 붙어서 칠 때는 때리는 쪽 몸을 축으로, 멀리서 칠 때는 대상 바로 앞을 축으로 대상을 가로지른다.
        void Slash(BattleActor actor, BattleActor target, Vector2 aim, bool moves, int sweep, float scale)
        {
            var pivot = moves ? Chest(actor) : Chest(target) - aim * target.Head;
            SlashFx.Play(_field.transform, pivot, ((Chest(target) - pivot).magnitude + target.Head * 0.25f) * scale, Angle(aim), sweep, Tint(actor.Unit));
        }

        async Task<bool> HitStop(string key)
        {
            Time.timeScale = 0;
            if (!await Wait(Db.ConfigInt(key) / 1000f, true)) return false;
            Time.timeScale = _paused ? 0 : _speed;
            return true;
        }

        // 필살 난타: 본타 앞에 빠른 타격을 여러 번 꽂는다. 타마다 내지르고, 밀리고, 짧게 멈춘다.
        async Task<bool> Flurry(BattleActor actor, BattleActor target, Vector2 aim, bool moves)
        {
            for (var i = 0; i < Db.ConfigInt("skill_flurry_hits"); i++)
            {
                if (moves)
                {
                    actor.Strike(aim, actor.Head * Db.ConfigInt("strike_reach_pct") / 200f);
                    if (!await Wait(BattleActor.StrikeLead)) return false;
                }
                var jitter = new Vector2((float)(_rng.NextDouble() * 2 - 1), (float)(_rng.NextDouble() * 2 - 1)) * target.Head * 0.2f;
                Slash(actor, target, aim, moves, i % 2 == 0 ? 1 : -1, 0.9f + i % 3 * 0.1f);
                SparkFx.Play(_field.transform, Chest(target) + jitter, target.Head * 0.7f, Tint(actor.Unit), 0.16f);
                target.Hurt(aim, Db.ConfigInt("knockback_px") * 0.5f, Burns(actor.Unit));
                Shake(Db.ConfigInt("shake_px") / 2, aim);
                if (!await HitStop("flurry_hit_stop_ms")) return false;
                if (!moves && !await Wait(0.06f)) return false;
            }
            return true;
        }

        void Gather(BattleUnit unit, BattleActor actor)
        {
            var texture = Sprites.Frames(Db.Get("element", "element_id", unit.ElementId, "skill_sprite_id"))[0];
            var size = actor.Head * 2.2f / texture.rect.height;
            var ring = Sprite2.Of(_field.transform, texture);
            ring.Position = actor.Home;
            ring.Scale = new Vector2(size, size * 0.4f);
            ring.ZIndex = actor.ZIndex - 1;
            ring.Alpha = 0;
            Tw.Create(ring)
                .Float(() => ring.Alpha, v => ring.Alpha = v, 1f, 0.08f)
                .Parallel().Vec(() => ring.Scale, v => ring.Scale = v, new Vector2(size, size * 0.4f) * 0.45f, 0.3f).SetTrans(Trans.Quad).SetEase(Ease.In)
                .Float(() => ring.Alpha, v => ring.Alpha = v, 0f, 0.08f)
                .Callback(ring.Free);
        }

        // 무대 확대: 전장 좌표의 한 점을 중심으로 당겨 잡는다.
        void Zoom(Vector2 fieldPoint, float scale, float seconds)
        {
            var pivot = FieldOrigin + fieldPoint;
            _stageHome = pivot * (1 - scale);
            Tw.Create(_stage).SetParallel()
                .Vec(() => _stage.Scale, v => _stage.Scale = v, Vector2.one * scale, seconds).SetTrans(Trans.Quad).SetEase(Ease.Out)
                .Vec(() => _stage.Position, v => _stage.Position = v, _stageHome, seconds).SetTrans(Trans.Quad).SetEase(Ease.Out);
        }

        void Puff(Vector2 at)
        {
            var texture = Sprites.Frames(Db.Config("dust_sprite_id"))[0];
            var grow = Cell * 0.9f / texture.rect.width;
            var dust = Sprite2.Of(_field.transform, texture);
            dust.Position = at;
            dust.Scale = Vector2.one * grow * 0.4f;
            dust.Modulate = FxMath.A(Kit.Cream, 0.7f);
            dust.ZIndex = 200;
            Tw.Create(dust)
                .Vec(() => dust.Scale, v => dust.Scale = v, Vector2.one * grow, 0.25f).SetEase(Ease.Out)
                .Parallel().Float(() => dust.Alpha, v => dust.Alpha = v, 0f, 0.25f)
                .Callback(dust.Free);
        }

        void Trail(BattleActor actor, Vector2 at, float delay)
        {
            var ghost = actor.Ghost(_field.transform, at);
            ghost.Image.enabled = false;
            Tw.Create(ghost).Interval(delay)
                .Callback(() => ghost.Image.enabled = true)
                .Float(() => ghost.Alpha, v => ghost.Alpha = v, 0f, 0.18f)
                .Callback(ghost.Free);
        }

        // 등장: 화면 밖에서 총총 걸어 들어온다.
        // 전진: 아군은 제자리에서 걷고 땅이 뒤로 흐른다. 쓰러진 적은 땅과 함께 뒤로 밀려나고 새 적은 앞에서 다가온다.
        async Task<bool> Advance()
        {
            var distance = Db.ConfigInt("advance_px");
            var seconds = Db.ConfigInt("advance_ms") / 1000f;
            var flow = Tw.Create(this).SetParallel().SetDefaultTrans(Trans.Sine).SetDefaultEase(Ease.InOut);
            flow.Method(x => _backdrop.Scroll = x, _backdrop.Scroll, _backdrop.Scroll + distance, seconds);
            foreach (var actor in _actors.Values)
            {
                if (actor.Unit.IsAlly)
                {
                    if (actor.Unit.Alive) actor.Walk(seconds);
                    continue;
                }
                if (actor.Unit.Alive) actor.Position = actor.Home + new Vector2(distance, 0);
                var a = actor;
                flow.Float(() => a.Position.x, v => a.Position = new Vector2(v, a.Position.y), actor.Position.x - distance, seconds);
            }
            await flow.Finished;
            return Here;
        }

        void Impact(BattleUnit attacker, BattleActor target, int damage, int affinityPct, bool skill, Vector2 blow)
        {
            var at = target.Home + new Vector2(0, -target.Head * 0.5f);
            Burst(attacker, "hit_sprite_id", at, target.Head * 1.1f, 0);
            SparkFx.Play(_field.transform, at, target.Head * (skill ? 1.5f : 0.9f), Tint(attacker));
            if (skill)
            {
                // 필살: 큰 고리 + 주변에 잇따라 터지는 불꽃 + 화면 번쩍.
                Burst(attacker, "skill_sprite_id", at, target.Head * 2.4f, 0);
                for (var i = 0; i < 4; i++)
                {
                    var spread = new Vector2((float)(_rng.NextDouble() * 2 - 1), (float)(_rng.NextDouble() * 2 - 1)) * target.Head * 0.6f;
                    Burst(attacker, "hit_sprite_id", at + spread, target.Head * 0.8f, 0.05f + i * 0.05f);
                }
                _flash.color = FxMath.A(Color.Lerp(Sprites.ElementColor(attacker.ElementId), Color.white, 0.6f), 0.7f);
                Tw.Create(_flash).Float(() => _flash.color.a, v => _flash.color = FxMath.A(_flash.color, v), 0f, 0.3f);
            }

            target.Hurt(blow, Db.ConfigInt("knockback_px") * (skill ? 1.6f : 1), Burns(attacker));

            var tag = affinityPct > 100 ? "\n" + App.T("ui_pop_strong") : affinityPct < 100 ? "\n" + App.T("ui_pop_weak") : "";
            var color = affinityPct > 100 ? Kit.Gold : affinityPct < 100 ? Kit.Muted : Kit.Paper;
            // 숫자 상자 (220x80) 가운데를 축으로 커졌다 줄며 떠오른다.
            var pop = Node2.Make<Node2>(_field.transform, "pop");
            pop.ZIndex = 2000;
            pop.Position = target.Home + new Vector2(0, -target.Head - 70);
            var label = pop.AddLabel(App.T("ui_pop_damage", damage) + tag, skill ? 40 : 30, color, new Vector2(-110, -40), new Vector2(220, 80));
            Kit.Outline(label, Kit.Ink, 8);
            var group = pop.gameObject.AddComponent<CanvasGroup>();
            pop.Scale = Vector2.one * 1.9f;
            Tw.Create(pop)
                .Vec(() => pop.Scale, v => pop.Scale = v, Vector2.one, 0.14f).SetTrans(Trans.Back).SetEase(Ease.Out)
                .Float(() => pop.Position.y, v => pop.Position = new Vector2(pop.Position.x, v), pop.Position.y - 0.9f * Cell, 0.8f)
                .Parallel().Float(() => group.alpha, v => group.alpha = v, 0f, 0.8f).SetEase(Ease.In).SetTrans(Trans.Quad)
                .Callback(pop.Free);
            target.Refresh(_state.FocusTarget == target.Unit);
        }

        void Burst(BattleUnit attacker, string column, Vector2 at, float size, float delay)
        {
            var texture = Sprites.Frames(Db.Get("element", "element_id", attacker.ElementId, column))[0];
            var grow = size / texture.rect.height;
            var fx = Sprite2.Of(_field.transform, texture);
            fx.Position = at;
            fx.Scale = Vector2.one * grow * 0.3f;
            fx.ZIndex = 1500;
            fx.Alpha = 0;
            Tw.Create(fx).Interval(delay)
                .Float(() => fx.Alpha, v => fx.Alpha = v, 1f, 0.01f)
                .Vec(() => fx.Scale, v => fx.Scale = v, Vector2.one * grow, 0.14f).SetTrans(Trans.Back).SetEase(Ease.Out)
                .Parallel().Float(() => fx.Rotation, v => fx.Rotation = v, 0.5f, 0.3f)
                .Float(() => fx.Alpha, v => fx.Alpha = v, 0f, 0.2f)
                .Callback(fx.Free);
        }

        // 흔들림은 때린 방향을 따라 오간다.
        void Shake(int strength, Vector2 axis)
        {
            const int steps = 6;
            var step = Db.ConfigInt("shake_ms") / 1000f / steps;
            var tween = Tw.Create(_stage);
            for (var i = 0; i < steps; i++)
            {
                var to = _stageHome + axis * strength * (1 - i / (float)steps) * (i % 2 == 0 ? 1 : -1);
                tween.Vec(() => _stage.Position, v => _stage.Position = v, to, step);
            }
            tween.Vec(() => _stage.Position, v => _stage.Position = v, _stageHome, step);
        }
    }
}
