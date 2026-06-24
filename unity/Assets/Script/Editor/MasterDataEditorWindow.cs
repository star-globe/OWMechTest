using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AdvancedGears.Editor
{
    /// <summary>
    /// マスタデータをスプレッドシート風に一覧表示し、CSV出力できるエディタウィンドウ。
    /// WarGame > Master Data Viewer から開く。
    /// </summary>
    public class MasterDataEditorWindow : EditorWindow
    {
        private Type[] masterTypes;

        private int selectedTypeIndex;
        private Vector2 scrollPos;
        private List<ScriptableObject> records = new List<ScriptableObject>();
        private List<FieldInfo> columns = new List<FieldInfo>();

        [MenuItem("WarGame/Master Data Viewer")]
        public static void ShowWindow()
        {
            GetWindow<MasterDataEditorWindow>("Master Data Viewer");
        }

        // IDBasedMasterSettings を実装した ScriptableObject サブクラスを自動検索
        private static Type[] FindMasterTypes() => AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
            .Where(t => !t.IsAbstract
                     && typeof(ScriptableObject).IsAssignableFrom(t)
                     && typeof(IDBasedMasterSettings).IsAssignableFrom(t))
            .OrderBy(t => t.Name)
            .ToArray();

        private void OnEnable()
        {
            masterTypes = FindMasterTypes();
            Refresh();
        }

        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.Space(4);
            DrawTable();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            var typeNames = masterTypes.Select(t => t.Name).ToArray();
            EditorGUI.BeginChangeCheck();
            selectedTypeIndex = EditorGUILayout.Popup(selectedTypeIndex, typeNames,
                GUILayout.Width(220));
            if (EditorGUI.EndChangeCheck())
                Refresh();

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
                Refresh();

            if (GUILayout.Button("Export CSV", EditorStyles.toolbarButton, GUILayout.Width(90)))
                ExportCsv();

            EditorGUILayout.EndHorizontal();
        }

        private void Refresh()
        {
            var type = masterTypes[selectedTypeIndex];
            var loaded = Resources.LoadAll(string.Empty, type);
            records = loaded.Cast<ScriptableObject>()
                            .OrderBy(r => r.name)
                            .ToList();
            columns = MasterCsvIO.GetExportableFields(type);
            Repaint();
        }

        private void DrawTable()
        {
            if (records.Count == 0)
            {
                EditorGUILayout.HelpBox("レコードが見つかりません。Resources フォルダを確認してください。",
                    MessageType.Info);
                return;
            }

            const float labelW = 160f;
            const float colW   = 130f;

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // ヘッダー行
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Asset Name", EditorStyles.boldLabel, GUILayout.Width(labelW));
            foreach (var col in columns)
                EditorGUILayout.LabelField(col.Name, EditorStyles.boldLabel, GUILayout.Width(colW));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(string.Empty, GUI.skin.horizontalSlider);

            // データ行
            foreach (var record in records)
            {
                EditorGUILayout.BeginHorizontal();

                // アセット名をクリックするとインスペクタに表示
                if (GUILayout.Button(record.name, EditorStyles.linkLabel, GUILayout.Width(labelW)))
                    Selection.activeObject = record;

                foreach (var col in columns)
                {
                    var val = col.GetValue(record)?.ToString() ?? "";
                    EditorGUILayout.LabelField(val, GUILayout.Width(colW));
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField($"総レコード数: {records.Count}", EditorStyles.miniLabel);
        }

        private void ExportCsv()
        {
            var type = masterTypes[selectedTypeIndex];
            var path = EditorUtility.SaveFilePanel("CSV エクスポート", "Assets", type.Name, "csv");
            if (string.IsNullOrEmpty(path))
                return;

            MasterCsvIO.Export(records, columns, path);
            Debug.Log($"[MasterDataViewer] {records.Count} 件を出力しました: {path}");
        }
    }
}
