using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using YangVillage.Core;

// Godot YangVillage game/scripts/ui/App.cs 이식.
// 테이블 = Resources/YangVillage/Tables (scripts/sync_godot_village.py 로 Godot 정본에서 복사).
// 화면 전환: Godot 은 화면마다 .tscn, Unity 는 한 씬(YangVillage.unity) 안에서 화면 루트를 갈아 끼움. screen 테이블의 screen_id 그대로.
namespace YangVillage.Ui
{
    public static class App
    {
        const string TableFolder = "YangVillage/Tables";
        static Game _game;
        static TableSet _tables;

        static string SavePath => Path.Combine(Application.persistentDataPath, "yangvillage_save.json");

        public static TableSet Tables => _tables ??= LoadTables();
        public static Game Game => _game ??= new Game(new Db(Tables), Load, Save);
        public static Db Db => Game.Db;

        public static string StageId;
        public static bool Won;
        public static BattleReward Reward;

        // 화면 갈무리 중: 화면이 스스로 넘어가지 않는다.
        public static bool Capturing;

        public static string T(string textId, params object[] args) => args.Length == 0 ? Db.Text(textId) : Db.Text(textId, args);

        // screen_id → 화면 컴포넌트.
        static readonly Dictionary<string, Type> Screens = new Dictionary<string, Type>
        {
            { "intro", typeof(Intro) },
            { "village", typeof(Village) },
            { "stage_select", typeof(StageSelect) },
            { "battle", typeof(BattleScreen) },
            { "result", typeof(Result) },
        };

        public static RectTransform Root;
        static GameObject _current;

        public static void Go(string screenId)
        {
            Db.Row("screen", "screen_id", screenId);
            if (_current != null) UnityEngine.Object.Destroy(_current);
            Time.timeScale = 1;
            var r = Kit.Node(screenId, Root);
            Kit.Fill(r);
            _current = r.gameObject;
            r.gameObject.AddComponent(Screens[screenId]);
        }

        public static IEnumerable<string> ScreenIds => Screens.Keys;

        static TableSet LoadTables()
        {
            var assets = Resources.LoadAll<TextAsset>(TableFolder).ToDictionary(a => a.name, a => a.text);
            return TableSet.Load(assets.Keys.OrderBy(n => n, StringComparer.Ordinal), name => assets[name]);
        }

        static string Load() => File.Exists(SavePath) ? File.ReadAllText(SavePath) : null;

        static void Save(string text) => File.WriteAllText(SavePath, text);
    }
}
