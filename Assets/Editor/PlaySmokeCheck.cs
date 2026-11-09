using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YangVil.Core;
using YangVil.Data;
using YangVil.UI;
using YangVil.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YangVil.EditorTools
{
    public static class PlaySmokeCheck
    {
        const string ActiveKey = "YangVil.PlaySmoke.Active";
        const string BuildAfterKey = "YangVil.PlaySmoke.BuildAfter";
        const string PendingBuildKey = "YangVil.PlaySmoke.PendingBuild";
        const string SaveDir = "Library/PlaySmokeSave";

        public static void RunThenBuildWebGL()
        {
            SessionState.SetBool(BuildAfterKey, true);
            Run();
        }

        public static void Finish(int errors)
        {
            if (errors > 0 || !SessionState.GetBool(BuildAfterKey, false))
            {
                EditorApplication.Exit(errors == 0 ? 0 : 1);
                return;
            }
            SessionState.SetBool(BuildAfterKey, false);
            SessionState.SetBool(PendingBuildKey, true);
            EditorApplication.playModeStateChanged += OnEditMode;
            EditorApplication.ExitPlaymode();
        }

        static void OnEditMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(PendingBuildKey, false)) return;
            SessionState.SetBool(PendingBuildKey, false);
            EditorApplication.playModeStateChanged -= OnEditMode;
            EditorApplication.delayCall += () =>
            {
                Debug.Log("PlaySmoke: 검증 통과 → WebGL 빌드 시작");
                var ok = BuildTool.TryBuildWebGL();
                Debug.Log($"PlaySmoke: WebGL 빌드 {(ok ? "성공" : "실패")}");
                EditorApplication.Exit(ok ? 0 : 1);
            };
        }

        public static void Run()
        {
            if (EditorUtility.scriptCompilationFailed)
            {
                Debug.LogError("PlaySmoke: 컴파일 오류 상태");
                EditorApplication.Exit(1);
                return;
            }
            AddressablesSetup.UseAssetDatabaseInPlayMode();
            if (Directory.Exists(SaveDir)) Directory.Delete(SaveDir, true);
            Directory.CreateDirectory(SaveDir);
            EditorSceneManager.OpenScene("Assets/Scenes/Intro.unity");
            SessionState.SetBool(ActiveKey, true);
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        static void Hook()
        {
            if (SessionState.GetBool(PendingBuildKey, false)) EditorApplication.playModeStateChanged += OnEditMode;
            if (!SessionState.GetBool(ActiveKey, false)) return;
            SaveSystem.DirectoryOverride = SaveDir;
            EditorApplication.playModeStateChanged += OnPlayMode;
        }

        static void OnPlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode) return;
            SessionState.SetBool(ActiveKey, false);
            new GameObject("PlaySmokeDriver").AddComponent<PlaySmokeDriver>();
        }
    }

    public class PlaySmokeDriver : MonoBehaviour
    {
        readonly List<string> _errors = new List<string>();

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Application.logMessageReceived += OnLog;
        }

        void OnLog(string message, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            if (IsEditorInternal(stack)) return;
            _errors.Add($"{type}: {message}\n{stack.Split('\n').FirstOrDefault()}");
        }

        static bool IsEditorInternal(string stack)
        {
            return !string.IsNullOrEmpty(stack) && stack.Contains("UnityEditor.") && !stack.Contains("YangVil.");
        }

        IEnumerator Start()
        {
            yield return Step("부트", () => GameBootstrap.Ready, 15);
            yield return Step("인트로 화면", () => FindAnyObjectByType<IntroScreen>() != null, 15);
            Check("인트로 버전 표시", FindAnyObjectByType<IntroScreen>().versionLabel.text.StartsWith("v"));
            if (VillageReviewCapture.PresentationEnabled)
            {
                yield return Step("인트로 가족 그림 로드", () => FindAnyObjectByType<IntroScreen>().GetComponentsInChildren<UnitSprite>().All(u => u.Image.enabled), 15);
                yield return new WaitForSecondsRealtime(0.4f);
                VillageReviewCapture.Save(null, "intro-first");
                VillageReviewCapture.Save(null, "intro-narrow", 450, 960);
            }
            GameBootstrap.Game.ResetSave();

            SceneFlow.Instance.GoVillage();
            yield return Step("마을 로드", () => FindAnyObjectByType<VillageWorld>() != null && FindAnyObjectByType<VillageHud>() != null, 30);
            yield return new WaitForSecondsRealtime(2f);
            var world = FindAnyObjectByType<VillageWorld>();
            Check("마을 건물 5종", world.transform.Cast<Transform>().Count(t => t.GetComponent<Clickable>() != null) >= 5);
            Check("마을 고양이 생성", FindObjectsByType<SpriteActor>(FindObjectsSortMode.None).Length >= 1);
            Check("고양이 프레임 로드", FindObjectsByType<SpriteActor>(FindObjectsSortMode.None).All(a => a.Ready));
            Check("가이드 안내 표시", !string.IsNullOrEmpty(FindAnyObjectByType<VillageHud>().guideLabel.text));
            Check("처음엔 쓰다듬기 권유", Guide.Next(GameBootstrap.Game).Target == GuideTarget.Pet);
            var starter = GameBootstrap.Game.Data.cats[0];
            var home = GameBootstrap.Game.House(starter.houseUid);
            Check("고양이 집 위치 판정", world.HouseAt(world.transform.Find("Dynamic").Find(home.uid).position) == home);
            Check("고양이 끌어놓기 판정", world.DropCat(starter, world.transform.Find("Dynamic").Find(home.uid).position) == MoveResult.Same);
            Check("빈 땅에 놓으면 이동 안 함", world.DropCat(starter, new Vector2(0, 100)) == null);
            var hud = FindAnyObjectByType<VillageHud>();
            hud.guideToggle.button.onClick.Invoke();
            Check("안내 접기", !hud.guideLabel.transform.parent.gameObject.activeSelf);
            hud.guideToggle.button.onClick.Invoke();
            Check("안내 다시 표시", hud.guideLabel.transform.parent.gameObject.activeSelf);
            world.BeginMove(starter);
            hud.hatchery.button.onClick.Invoke();
            Check("이사 중에도 버튼 동작", hud.panel.activeSelf && !world.Moving);
            hud.panelClose.button.onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.5f);
            hud.toast.gameObject.SetActive(false);
            VillageReviewCapture.Save(world.cam, "village-first", overview: true);
            VillageReviewCapture.Save(world.cam, "village-narrow", 450, 960, true);
            var petted = FindObjectsByType<CatWanderer>(FindObjectsSortMode.None).First(w => w.ActorReady);
            var petPosition = petted.transform.position;
            var petSprite = petted.GetComponentInChildren<SpriteActor>();
            var petHeight = petSprite.Renderer.sprite.bounds.size.y * petSprite.transform.lossyScale.y;
            petted.Pet();
            Check("고양이 쓰담 반응", petted.Petting);
            yield return null;
            yield return null;
            var body = petted.GetComponentInChildren<SpeechBubble>().transform.Find("BubbleBody").GetComponent<SpriteRenderer>();
            Check("만화 말풍선 표시", body.enabled && body.size.x > 0.3f);
            var press = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
            UnityEngine.EventSystems.ExecuteEvents.Execute(petted.gameObject, press, UnityEngine.EventSystems.ExecuteEvents.pointerDownHandler);
            yield return new WaitForSecondsRealtime(0.7f);
            Check("쓰담 후 성장 체격 유지", Mathf.Abs(petSprite.Renderer.sprite.bounds.size.y * petSprite.transform.lossyScale.y - petHeight) < 0.02f);
            Check("쓰담 후 이동 루트 위치 유지", Vector3.Distance(petted.transform.position, petPosition) < 0.01f);
            UnityEngine.EventSystems.ExecuteEvents.Execute(petted.gameObject, press, UnityEngine.EventSystems.ExecuteEvents.pointerUpHandler);
            Check("길게 누르면 고양이 카드", hud.panel.activeSelf);
            hud.panelClose.button.onClick.Invoke();
            starter.cleanliness = 10f;
            Check("더러우면 꼬질꼬질 상태", GameBootstrap.Game.Mood(starter) == CatMood.Dirty);
            GameBootstrap.Game.Wash(starter);
            Check("씻기면 상태 회복", GameBootstrap.Game.Mood(starter) != CatMood.Dirty && starter.cleanliness >= 99f);
            var rng = new System.Random(3);
            var child = Genetics.Breed(starter.genes, starter.genes, rng);
            Check("짝짓기 유전자 생성", child != null && child.size >= 0.8f && child.size <= 1.5f);
            var ghost = child.Clone();
            Genetics.ApplyOutlier(ghost, Outlier.Ghost);
            Check("아웃라이어 형질 적용", ghost.outlier == Outlier.Ghost && Genetics.OutlierName(ghost.outlier).Length > 0);
            Check("유전자대로 털 칠하기", FindObjectsByType<SpriteActor>(FindObjectsSortMode.None)
                .Any(a => a.Renderer.sprite != null && string.IsNullOrEmpty(a.Renderer.sprite.texture.name)));
            CheckCatLife(GameBootstrap.Game, starter);

            hud.adventure.button.onClick.Invoke();
            yield return Step("여정 지도 진입", () => FindAnyObjectByType<StageSelectScreen>() != null, 30);
            yield return null;
            var map = FindAnyObjectByType<StageSelectScreen>();
            Check("지도 거점 수", map.NodeCount == GameBootstrap.Game.Config.stages.Count);
            Check("지도 세로 스크롤", map.mapScroll.content == map.listContent && map.listContent.rect.height > map.mapScroll.viewport.rect.height);
            map.SelectStage(1);
            Check("잠긴 거점 안내", !map.depart.Interactable && map.detailBody.text.Contains("앞선 거점"));
            VillageReviewCapture.Save(world.cam, "journey-locked");
            map.SelectStage(0);
            Check("출발 거점 선택", map.depart.Interactable && map.SelectedIndex == 0);
            VillageReviewCapture.Save(world.cam, "journey-first");
            VillageReviewCapture.Save(world.cam, "journey-narrow", 450, 960);
            var party = GameBootstrap.Game.Data.cats.Where(c => c.inParty).ToArray();
            foreach (var c in party) c.inParty = false;
            map.SelectStage(0);
            Check("파티 없으면 출발 차단", !map.depart.Interactable);
            foreach (var c in party) c.inParty = true;
            map.SelectStage(0);
            map.depart.button.onClick.Invoke();
            yield return Step("전투 로드", () => FindAnyObjectByType<BattleWorld>() != null && FindAnyObjectByType<BattleHud>() != null, 30);
            var battle = FindAnyObjectByType<BattleWorld>();
            yield return Step("전투 상태", () => battle.State != null, 10);
            battle.ToggleSpeed();
            Check("스테이지 웨이브 3개", battle.State.WaveCount == 3);
            yield return Step("마지막 웨이브 도달", () => battle.State.Wave == battle.State.WaveCount - 1 || FindAnyObjectByType<ResultScreen>() != null, 180);
            Check("웨이브 전환 후 적 생성", battle.State.Wave == 0 || battle.State.Enemies.Count > 0);
            yield return Step("전투 종료→결과", () => FindAnyObjectByType<ResultScreen>() != null, 180);
            var result = FindAnyObjectByType<ResultScreen>();
            Check("1-1 승리", result.Victory);
            Check("결과 '다음 스테이지' 버튼", result.next.gameObject.activeSelf);

            CheckExpedition(GameBootstrap.Game);
            result.stages.button.onClick.Invoke();
            yield return Step("결과→모험 목록", () => FindAnyObjectByType<StageSelectScreen>() != null && FindAnyObjectByType<VillageWorld>() != null, 30);
            yield return null;
            map = FindAnyObjectByType<StageSelectScreen>();
            map.SelectStage(1);
            Check("승리 후 다음 거점 해금", map.depart.Interactable);
            VillageReviewCapture.Save(FindAnyObjectByType<VillageWorld>().cam, "journey-progress");
            if (VillageReviewCapture.Enabled)
            {
                var records = GameBootstrap.Game.Data.stages;
                for (var i = 0; i < GameBootstrap.Game.Config.stages.Count - 1; i++)
                {
                    var id = GameBootstrap.Game.Config.stages[i].id;
                    var record = records.Find(r => r.stageId == id);
                    if (record == null) { record = new StageRecord { stageId = id }; records.Add(record); }
                    record.stars = 3;
                }
                FindAnyObjectByType<ScreenManager>().Show<StageSelectScreen>();
                yield return new WaitForSecondsRealtime(2);
                map = FindAnyObjectByType<StageSelectScreen>();
                map.SelectStage(GameBootstrap.Game.Config.stages.Count - 1);
                map.mapScroll.verticalNormalizedPosition = 1;
                Check("보스 거점 해금", map.depart.Interactable && map.detailTitle.text.Contains("보스"));
                VillageReviewCapture.Save(FindAnyObjectByType<VillageWorld>().cam, "journey-boss");
                records.Add(new StageRecord { stageId = GameBootstrap.Game.Config.stages.Last().id, stars = 3 });
                FindAnyObjectByType<ScreenManager>().Show<StageSelectScreen>();
                yield return new WaitForSecondsRealtime(2);
                map = FindAnyObjectByType<StageSelectScreen>();
                map.mapScroll.verticalNormalizedPosition = 1;
                VillageReviewCapture.Save(FindAnyObjectByType<VillageWorld>().cam, "journey-complete");
            }

            SceneFlow.Instance.GoVillage();
            yield return Step("마을 복귀", () => FindAnyObjectByType<VillageWorld>() != null && FindAnyObjectByType<VillageHud>() != null, 30);
            yield return new WaitForSecondsRealtime(2f);
            if (VillageReviewCapture.Enabled)
            {
                var game = GameBootstrap.Game;
                game.Data.coins = 100000;
                while (game.FreeVillageSlot() >= 0) game.BuyHouse(game.Config.houses[game.Data.houses.Count % game.Config.houses.Count]);
                yield return new WaitForSecondsRealtime(2);
                world = FindAnyObjectByType<VillageWorld>();
                Check("6집 슬롯 위치 판정", game.Data.houses.All(h => world.HouseAt(world.layout.HousePosition(h.slot)) == h));
                world.cam.transform.position = new Vector3(0, -1, -10);
                world.cam.orthographicSize = 11;
                VillageReviewCapture.Save(world.cam, "village-six-houses");
                world.cam.orthographicSize = 5;
                VillageReviewCapture.Save(world.cam, "village-zoom");
            }
            if (VillageReviewCapture.PresentationEnabled) yield return CheckPresentation();
            Finish();
        }

        IEnumerator CheckPresentation()
        {
            var game = GameBootstrap.Game;
            foreach (var cat in game.Data.cats) cat.inParty = false;
            var species = game.Config.starterCat;
            var genes = Genetics.FromSpecies(species, new System.Random(17));
            genes.size = 1;
            var levels = new[] { 1, CatData.TeenLevel, CatData.AdultLevel };
            for (var i = 0; i < levels.Length; i++)
                game.Data.cats.Add(new OwnedCat { uid = $"presentation_{i}", speciesId = species.id, genes = genes.Clone(), level = levels[i],
                    inParty = true, houseUid = game.Data.houses[0].uid, careUnix = Game.Now, bondUnix = Game.Now, personality = 0 });
            SceneFlow.Instance.GoBattle(game.Config.stages[2]);
            yield return Step("혼합 성장 전투 로드", () => FindAnyObjectByType<BattleWorld>() != null && FindAnyObjectByType<BattleWorld>().State != null && FindAnyObjectByType<BattleHud>() != null, 30);
            var battle = FindAnyObjectByType<BattleWorld>();
            battle.TogglePause();
            yield return Step("혼합 성장 그림 로드", () => FindObjectsByType<SpriteActor>(FindObjectsSortMode.None).All(a => a.Ready), 15);
            yield return new WaitForSecondsRealtime(0.5f);
            var actors = FindObjectsByType<SpriteActor>(FindObjectsSortMode.None).Where(a => a.transform.parent.localPosition.x < 0).OrderBy(a => a.height).ToArray();
            Check("아기·청소년·성묘 전투 3마리", actors.Length == 3);
            if (actors.Length == 3)
            {
                var heights = actors.Select(a => a.Renderer.bounds.size.y).ToArray();
                Check("성묘 실제 표시 높이 아기의 2배", Mathf.Abs(heights[2] / heights[0] - 2) < 0.02f && heights[1] > heights[0] && heights[1] < heights[2]);
                Check("성장별 HP바가 그림 위에 위치", actors.All(a => a.transform.parent.Find("Bar").position.y > a.transform.position.y + a.VisibleTop));
                Debug.Log($"PresentationReview: 전투 성장 높이 {heights[0]:0.000}/{heights[1]:0.000}/{heights[2]:0.000}");
            }
            Check("전투 초상화 그림 표시", FindAnyObjectByType<BattleHud>().portraitRow.GetComponentsInChildren<UnitSprite>().Count(u => u.Image.enabled) == 3);
            VillageReviewCapture.Save(battle.cam, "battle-growth");
            VillageReviewCapture.Save(battle.cam, "battle-growth-narrow", 450, 960);
            var hud = FindAnyObjectByType<BattleHud>();
            battle.State.Allies[2].Gauge = Battle.BattleUnit.GaugeMax;
            battle.ToggleAutoSkill();
            hud.portraitRow.GetChild(2).GetComponent<LabeledButton>().button.onClick.Invoke();
            battle.TogglePause();
            yield return Step("수동 필살 컷인", () => hud != null && hud.cutIn.activeSelf, 20);
            yield return new WaitForSecondsRealtime(0.22f);
            VillageReviewCapture.Save(battle.cam, "battle-skill");
            SceneFlow.Instance.GoVillage();
            yield return Step("혼합 성장 마을 복귀", () => FindAnyObjectByType<VillageWorld>() != null && FindAnyObjectByType<VillageHud>() != null, 30);
            yield return new WaitForSecondsRealtime(1);
            var world = FindAnyObjectByType<VillageWorld>();
            var villagers = FindObjectsByType<CatWanderer>(FindObjectsSortMode.None).Where(w => w.name.StartsWith("presentation_")).OrderBy(w => w.name).ToArray();
            for (var i = 0; i < villagers.Length; i++)
            {
                villagers[i].enabled = false;
                villagers[i].StopAllCoroutines();
                villagers[i].Hush();
                var visual = villagers[i].GetComponentInChildren<SpriteActor>().transform.parent;
                visual.localRotation = Quaternion.identity;
                visual.localScale = Vector3.one;
                visual.localPosition = Vector3.zero;
                villagers[i].transform.localPosition = new Vector3(-1.1f + i * 1.1f, -0.6f, 0);
            }
            var villageHeights = villagers.Select(v => v.GetComponentInChildren<SpriteActor>().Renderer.bounds.size.y).ToArray();
            Debug.Log($"PresentationReview: 마을 성장 높이 {string.Join("/", villageHeights.Select(h => h.ToString("0.000")))}");
            Check("마을도 성장별 실제 높이 유지", villageHeights.Length == 3 && Mathf.Abs(villageHeights[2] / villageHeights[0] - 2) < 0.03f);
            world.cam.orthographicSize = 5;
            world.FocusOn(world.layout.HousePosition(0));
            VillageReviewCapture.Save(world.cam, "village-growth");
            var boss = game.Config.stages.Last();
            SceneFlow.Instance.GoBattle(boss);
            yield return Step("보스 전투 화면", () => FindAnyObjectByType<BattleWorld>() != null && FindAnyObjectByType<BattleWorld>().Stage == boss && FindAnyObjectByType<BattleHud>() != null, 30);
            battle = FindAnyObjectByType<BattleWorld>();
            battle.TogglePause();
            yield return Step("보스 전투 그림 로드", () => FindObjectsByType<SpriteActor>(FindObjectsSortMode.None).All(a => a.Ready), 15);
            yield return new WaitForSecondsRealtime(0.4f);
            battle.ToggleSpeed();
            battle.TogglePause();
            yield return Step("3대3 진형 등장", () => battle.State.Wave == 1 && FindObjectsByType<SpriteActor>(FindObjectsSortMode.None).Count(a => a.Ready && a.transform.parent.localPosition.x > 0 && a.transform.parent.localPosition.x < 3) == 3, 90);
            battle.TogglePause();
            VillageReviewCapture.Save(battle.cam, "battle-party");
            battle.TogglePause();
            yield return Step("실제 보스 등장", () => FindObjectsByType<SpriteActor>(FindObjectsSortMode.None).Any(a => a.Ready && battle.State.Enemies.Any(e => e.Variant.isBoss && a.transform.parent.name == e.Name)), 90);
            yield return Step("보스 입장 완료", () => FindObjectsByType<SpriteActor>(FindObjectsSortMode.None).Any(a => a.Ready && a.transform.parent.localPosition.x <= battle.enemySlots[0].x + 0.02f && battle.State.Enemies.Any(e => e.Variant.isBoss && a.transform.parent.name == e.Name)), 15);
            battle.TogglePause();
            Check("보스 고유 이름 표시", battle.State.Enemies.Any(e => e.Variant.isBoss && e.Name == GameDataBuilder.RatKingName));
            VillageReviewCapture.Save(battle.cam, "battle-boss");
            SceneFlow.Instance.GoVillage();
            yield return Step("시각 검증 후 마을 복귀", () => FindAnyObjectByType<VillageWorld>() != null && FindAnyObjectByType<VillageHud>() != null, 30);
        }

        void CheckCatLife(Game game, OwnedCat starter)
        {
            Check("고양이 성격 부여", starter.personality >= 0 && starter.personality < CatLife.PersonalityCount);
            var bond = starter.bond;
            Check("쓰담 바크", !string.IsNullOrEmpty(game.Pet(starter)) && starter.bond > bond);
            Check("쓰담 후 다음 권유로 넘어감", Guide.Next(game).Target != GuideTarget.Pet);
            var basket = game.Data.baskets[0];
            basket.hatchAtUnix = Game.Now;
            var kitten = game.Hatch(basket);
            Check("두 번째 고양이 부화", kitten != null);
            var before = game.Affinity(starter, kitten);
            var first = game.Interact(starter, kitten);
            Check("고양이끼리 상호작용", first != null && !string.IsNullOrEmpty(first.BarkA) && game.Affinity(starter, kitten) != before);
            var rel = game.Rel(starter, kitten);
            rel.affinity = -50f;
            InteractionResult fight = null;
            for (var i = 0; i < 40 && fight == null; i++)
            {
                rel.grievances[(int)Grievance.Jealousy] = 100f;
                var r = game.Interact(starter, kitten);
                if (r.Kind == Interaction.Fight) fight = r;
            }
            Check("싸움 원인 표시", fight != null && fight.Cause == Grievance.Jealousy && fight.BarkA == CatLife.CauseBark(Grievance.Jealousy));
            Check("관계 카드 갈등 원인", game.RelationSummary(starter).Contains(CatLife.Label(Grievance.Jealousy)));
            Check("아기는 짝짓기 거절", game.BreedRefusal(starter, kitten) != null);
            starter.level = kitten.level = 20;
            starter.fullness = kitten.fullness = starter.cleanliness = kitten.cleanliness = 100f;
            rel.affinity = 80f;
            Check("호감 높은 성묘 짝짓기 허용", game.BreedRefusal(starter, kitten) == null);
            starter.bond = 95f;
            var episode = game.AvailableEpisode(starter);
            Check("유대 에피소드 열림", episode != null && episode.Choices.Length > 0);
            if (episode != null)
            {
                game.ResolveEpisode(episode, starter, episode.Choices[0]);
                Check("에피소드 완료 기록", game.AvailableEpisode(starter) != episode);
            }
            starter.level = 1;
            kitten.level = 1;
        }

        void CheckExpedition(Game game)
        {
            Check("산책 보내기 시작", game.StartExpedition() == null && game.OnExpedition);
            game.Data.expedition.startUnix = Game.Now - 3600;
            var coins = game.Data.coins;
            var report = game.ReturnExpedition();
            Check("방치 모험 보상", report != null && report.Runs == 3600 / CatLife.ExpeditionRunSeconds && game.Data.coins > coins && !game.OnExpedition);
        }

        IEnumerator Step(string name, System.Func<bool> done, float timeout)
        {
            var until = Time.realtimeSinceStartup + timeout;
            while (!done())
            {
                if (Time.realtimeSinceStartup > until)
                {
                    _errors.Add($"시간 초과: {name}");
                    Finish();
                    yield break;
                }
                yield return null;
            }
            Debug.Log($"PlaySmoke: ✔ {name}");
        }

        void Check(string name, bool ok)
        {
            if (ok) Debug.Log($"PlaySmoke: ✔ {name}");
            else _errors.Add($"실패: {name}");
        }

        bool _finished;

        void Finish()
        {
            if (_finished) return;
            _finished = true;
            StopAllCoroutines();
            Application.logMessageReceived -= OnLog;
            foreach (var e in _errors) Debug.Log($"PlaySmoke: ✘ {e}");
            Debug.Log($"PlaySmoke: 결과 오류 {_errors.Count}건");
            PlaySmokeCheck.Finish(_errors.Count);
        }
    }
}
