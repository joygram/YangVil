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
            GameBootstrap.Game.ResetSave();

            SceneFlow.Instance.GoVillage();
            yield return Step("마을 로드", () => FindAnyObjectByType<VillageWorld>() != null && FindAnyObjectByType<VillageHud>() != null, 30);
            yield return new WaitForSecondsRealtime(2f);
            var world = FindAnyObjectByType<VillageWorld>();
            Check("마을 건물 5종", world.transform.Cast<Transform>().Count(t => t.GetComponent<Clickable>() != null) >= 5);
            Check("마을 고양이 생성", FindObjectsByType<SpriteActor>(FindObjectsSortMode.None).Length >= 1);
            Check("고양이 프레임 로드", FindObjectsByType<SpriteActor>(FindObjectsSortMode.None).All(a => a.Ready));
            Check("가이드 안내 표시", !string.IsNullOrEmpty(FindAnyObjectByType<VillageHud>().guideLabel.text));
            var starter = GameBootstrap.Game.Data.cats[0];
            var home = GameBootstrap.Game.House(starter.houseUid);
            Check("고양이 집 위치 판정", world.HouseAt(world.transform.Find("Dynamic").Find(home.uid).position) == home);
            Check("고양이 끌어놓기 판정", world.DropCat(starter, world.transform.Find("Dynamic").Find(home.uid).position) == MoveResult.Same);
            Check("빈 땅에 놓으면 이동 안 함", world.DropCat(starter, new Vector2(0, 100)) == null);
            var hud = FindAnyObjectByType<VillageHud>();
            world.BeginMove(starter);
            hud.hatchery.button.onClick.Invoke();
            Check("이사 중에도 버튼 동작", hud.panel.activeSelf && !world.Moving);
            hud.panelClose.button.onClick.Invoke();
            var petted = FindObjectsByType<CatWanderer>(FindObjectsSortMode.None).First(w => w.GetComponent<SpriteActor>().Ready);
            petted.Pet();
            Check("고양이 쓰담 반응", petted.Petting);
            var press = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
            UnityEngine.EventSystems.ExecuteEvents.Execute(petted.gameObject, press, UnityEngine.EventSystems.ExecuteEvents.pointerDownHandler);
            yield return new WaitForSecondsRealtime(0.7f);
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

            SceneFlow.Instance.GoBattle(GameBootstrap.Game.Config.stages[0]);
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

            result.stages.button.onClick.Invoke();
            yield return Step("결과→모험 목록", () => FindAnyObjectByType<StageSelectScreen>() != null && FindAnyObjectByType<VillageWorld>() != null, 30);

            SceneFlow.Instance.GoVillage();
            yield return Step("마을 복귀", () => FindAnyObjectByType<VillageWorld>() != null && FindAnyObjectByType<VillageHud>() != null, 30);
            yield return new WaitForSecondsRealtime(2f);
            Finish();
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
