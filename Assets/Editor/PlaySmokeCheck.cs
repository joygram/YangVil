using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CatVillage.Core;
using CatVillage.UI;
using CatVillage.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatVillage.EditorTools
{
    public static class PlaySmokeCheck
    {
        const string ActiveKey = "CatVillage.PlaySmoke.Active";
        const string BuildAfterKey = "CatVillage.PlaySmoke.BuildAfter";
        const string PendingBuildKey = "CatVillage.PlaySmoke.PendingBuild";
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
            return !string.IsNullOrEmpty(stack) && stack.Contains("UnityEditor.") && !stack.Contains("CatVillage.");
        }

        IEnumerator Start()
        {
            yield return Step("부트", () => GameBootstrap.Ready, 15);
            GameBootstrap.Game.ResetSave();

            SceneFlow.Instance.GoVillage();
            yield return Step("마을 로드", () => FindAnyObjectByType<VillageWorld>() != null && FindAnyObjectByType<VillageHud>() != null, 30);
            yield return new WaitForSecondsRealtime(2f);
            var world = FindAnyObjectByType<VillageWorld>();
            Check("마을 건물 5종", world.transform.Cast<Transform>().Count(t => t.GetComponent<Clickable>() != null) >= 5);
            Check("마을 고양이 생성", FindObjectsByType<SpriteActor>(FindObjectsSortMode.None).Length >= 1);
            Check("고양이 프레임 로드", FindObjectsByType<SpriteActor>(FindObjectsSortMode.None).All(a => a.Ready));
            Check("가이드 안내 표시", !string.IsNullOrEmpty(FindAnyObjectByType<VillageHud>().guideLabel.text));

            SceneFlow.Instance.GoBattle(GameBootstrap.Game.Config.stages[0]);
            yield return Step("전투 로드", () => FindAnyObjectByType<BattleWorld>() != null && FindAnyObjectByType<BattleHud>() != null, 30);
            var battle = FindAnyObjectByType<BattleWorld>();
            yield return Step("전투 상태", () => battle.State != null, 10);
            battle.ToggleSpeed();
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
