using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace AdvancedGears.Editor
{
    public static class MasterCsvIO
    {
        public static void Export(IList<ScriptableObject> records, IList<FieldInfo> fields, string path)
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", fields.Select(f => f.Name)));
            foreach (var r in records)
            {
                var values = fields.Select(f => EscapeCsv(f.GetValue(r)?.ToString() ?? ""));
                sb.AppendLine(string.Join(",", values));
            }
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(withBom: true));
        }

        public static List<FieldInfo> GetExportableFields(Type type)
        {
            return type
                .GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public)
                .Where(f => f.GetCustomAttribute<SerializeField>() != null || f.IsPublic)
                .Where(f => IsPrimitive(f.FieldType))
                .ToList();
        }

        private static bool IsPrimitive(Type t)
        {
            return t == typeof(int) || t == typeof(float) || t == typeof(string)
                || t == typeof(bool) || t.IsEnum;
        }

        private static string EscapeCsv(string value)
        {
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
                return $"\"{value.Replace("\"", "\"\"")}\"";
            return value;
        }
    }
}
