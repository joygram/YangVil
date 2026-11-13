using System.IO;
using System.Linq;
using UnityEngine;
using YangVillage.Core;

// Godot YangVillage game/scripts/ui/App.cs 이식. 테이블 = Resources/YangVillage/Tables (scripts/sync_godot_village.py).
namespace YangVillage.Ui
{
    public static class App
    {
        const string TableFolder = "YangVillage/Tables";
        static Game _game;

        static string SavePath => Path.Combine(Application.persistentDataPath, "yangvillage_save.json");

        public static Game Game => _game ??= new Game(new Db(LoadTables()), Load, Save);
        public static Db Db => Game.Db;

        public static string T(string textId, params object[] args) => args.Length == 0 ? Db.Text(textId) : Db.Text(textId, args);

        static TableSet LoadTables()
        {
            var assets = Resources.LoadAll<TextAsset>(TableFolder).ToDictionary(a => a.name, a => a.text);
            return TableSet.Load(assets.Keys, name => assets[name]);
        }

        static string Load() => File.Exists(SavePath) ? File.ReadAllText(SavePath) : null;

        static void Save(string text) => File.WriteAllText(SavePath, text);
    }
}
