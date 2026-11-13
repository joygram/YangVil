using System;
using System.Collections.Generic;

// Godot YangVillage game/scripts/core (TsvTable·TableSet·Db) 이식. 동작 동일.
namespace YangVillage.Core
{
    // TSV 한 장. 첫 줄 = 칼럼 이름, 값은 전부 문자열로 보관.
    public sealed class TsvTable
    {
        public string Name { get; }
        public string[] Columns { get; }
        public List<string[]> Rows { get; } = new List<string[]>();
        readonly Dictionary<string, int> _index = new Dictionary<string, int>();

        TsvTable(string name, string[] columns)
        {
            Name = name;
            Columns = columns;
            for (int i = 0; i < columns.Length; i++) _index[columns[i]] = i;
        }

        public static TsvTable Parse(string name, string text)
        {
            var lines = text.Replace("\r", "").Split('\n');
            var table = new TsvTable(name, lines[0].Split('\t'));
            for (int i = 1; i < lines.Length; i++)
            {
                if (lines[i].Length == 0) continue;
                var cells = lines[i].Split('\t');
                if (cells.Length != table.Columns.Length)
                    throw new FormatException($"{name}: {i + 1}행 칸 수 {cells.Length} != {table.Columns.Length}");
                table.Rows.Add(cells);
            }
            return table;
        }

        public bool HasColumn(string column) => _index.ContainsKey(column);

        public string Get(string[] row, string column)
        {
            if (!_index.TryGetValue(column, out var i))
                throw new KeyNotFoundException($"{Name}: 칼럼 없음 {column}");
            return row[i];
        }
    }

    // 정본 테이블 묶음. 읽기 함수는 바깥에서 받음 (엔진·콘솔 공용).
    public sealed class TableSet
    {
        readonly Dictionary<string, TsvTable> _tables = new Dictionary<string, TsvTable>();

        public IEnumerable<TsvTable> All => _tables.Values;

        public static TableSet Load(IEnumerable<string> names, Func<string, string> readText)
        {
            var set = new TableSet();
            foreach (var name in names) set._tables[name] = TsvTable.Parse(name, readText(name));
            return set;
        }

        public bool Has(string name) => _tables.ContainsKey(name);

        public TsvTable Get(string name)
        {
            if (!_tables.TryGetValue(name, out var table))
                throw new KeyNotFoundException($"테이블 없음: {name}");
            return table;
        }
    }

    // 정본 테이블 조회. 값이 없으면 예외로 멈춤.
    public sealed class Db
    {
        public TableSet Set { get; }
        readonly Dictionary<string, Dictionary<string, string[]>> _index = new Dictionary<string, Dictionary<string, string[]>>();

        public Db(TableSet set) { Set = set; }

        public string[] Row(string table, string keyColumn, string key)
        {
            var id = table + "." + keyColumn;
            if (!_index.TryGetValue(id, out var map))
            {
                map = new Dictionary<string, string[]>();
                var t = Set.Get(table);
                foreach (var row in t.Rows) map[t.Get(row, keyColumn)] = row;
                _index[id] = map;
            }
            if (!map.TryGetValue(key, out var found))
                throw new KeyNotFoundException($"{table}: {keyColumn}={key} 없음");
            return found;
        }

        public string Get(string table, string keyColumn, string key, string column) =>
            Set.Get(table).Get(Row(table, keyColumn, key), column);

        public int Int(string table, string keyColumn, string key, string column) =>
            int.Parse(Get(table, keyColumn, key, column));

        public IEnumerable<string[]> Where(string table, string column, string value)
        {
            var t = Set.Get(table);
            foreach (var row in t.Rows)
                if (t.Get(row, column) == value) yield return row;
        }

        public string Cell(string table, string[] row, string column) => Set.Get(table).Get(row, column);

        public string Text(string textId) => Get("text", "text_id", textId, "ko").Replace("\\n", "\n");

        public string Text(string textId, params object[] args) => string.Format(Text(textId), args);

        public string Config(string key) => Get("config", "key", key, "value");

        public int ConfigInt(string key) => int.Parse(Config(key));
    }
}
