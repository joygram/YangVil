using System.Collections.Generic;

// Godot YangVillage game/scripts/core/TableCheck.cs 이식.
namespace YangVillage.Core
{
    // schema 테이블 기준 점검: 칼럼 일치·형식·키 중복·참조.
    public static class TableCheck
    {
        public const string SchemaName = "schema";

        public static List<string> Run(TableSet set)
        {
            var errors = new List<string>();
            var schema = set.Get(SchemaName);
            var columns = new Dictionary<string, List<string[]>>();
            foreach (var row in schema.Rows)
            {
                var table = schema.Get(row, "table");
                if (!columns.TryGetValue(table, out var list)) columns[table] = list = new List<string[]>();
                list.Add(row);
            }
            foreach (var table in set.All)
                if (table.Name != SchemaName && !columns.ContainsKey(table.Name))
                    errors.Add($"{table.Name}: schema 행 없음");

            foreach (var pair in columns)
            {
                if (!set.Has(pair.Key)) { errors.Add($"{pair.Key}: 테이블 파일 없음"); continue; }
                var table = set.Get(pair.Key);
                var declared = new List<string>();
                foreach (var row in pair.Value) declared.Add(schema.Get(row, "column"));
                if (string.Join(",", declared) != string.Join(",", table.Columns))
                {
                    errors.Add($"{table.Name}: 칼럼 불일치 schema[{string.Join(",", declared)}] 파일[{string.Join(",", table.Columns)}]");
                    continue;
                }
                foreach (var row in pair.Value) CheckColumn(set, schema, table, row, errors);
            }
            return errors;
        }

        static void CheckColumn(TableSet set, TsvTable schema, TsvTable table, string[] spec, List<string> errors)
        {
            var column = schema.Get(spec, "column");
            var type = schema.Get(spec, "type");
            var isKey = schema.Get(spec, "key") == "true";
            var optional = schema.Get(spec, "optional") == "true";
            var reference = schema.Get(spec, "ref");
            HashSet<string> target = null;
            if (reference.Length > 0)
            {
                var parts = reference.Split('.');
                if (!set.Has(parts[0]) || !set.Get(parts[0]).HasColumn(parts[1]))
                {
                    errors.Add($"{table.Name}.{column}: 참조 대상 없음 {reference}");
                    return;
                }
                target = new HashSet<string>();
                var other = set.Get(parts[0]);
                foreach (var row in other.Rows) target.Add(other.Get(row, parts[1]));
            }
            var seen = new HashSet<string>();
            for (int i = 0; i < table.Rows.Count; i++)
            {
                var value = table.Get(table.Rows[i], column);
                var where = $"{table.Name}.{column} {i + 2}행";
                if (value.Length == 0)
                {
                    if (!optional) errors.Add($"{where}: 빈 값");
                    continue;
                }
                if (type == "int" && !long.TryParse(value, out _)) errors.Add($"{where}: 정수 아님 {value}");
                if (type == "bool" && value != "true" && value != "false") errors.Add($"{where}: true/false 아님 {value}");
                if (isKey && !seen.Add(value)) errors.Add($"{where}: 키 중복 {value}");
                if (target != null && !target.Contains(value)) errors.Add($"{where}: 참조 끊김 {value} → {reference}");
            }
        }
    }
}
