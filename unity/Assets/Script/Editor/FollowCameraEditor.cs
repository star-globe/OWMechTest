using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AdvancedGears
{
    /// <summary>
    /// FollowCamera のインスペクター。換算後の値を表示し、Play 中に調整した値をプレハブへ書き戻す
    /// </summary>
    [CustomEditor(typeof(FollowCamera))]
    public class FollowCameraEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var followCamera = (FollowCamera)target;

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.FloatField("遊び（m に換算）", followCamera.DeadZoneDistance);
                EditorGUILayout.Vector3Field("カメラの目標位置（ローカル）", followCamera.CameraLocalOffset);
            }

            if (!Application.isPlaying)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Play 中の変更は停止すると失われます。残す値は「プレハブに反映」で保存してください（参照のフィールドは反映しません）。\n「カメラリセット」は、注視点とカメラの位置の値をプレハブの値に戻し、カメラをすぐに初期の配置へ移します。", MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("カメラリセット"))
                {
                    followCamera.ResetCamera();
                }

                if (GUILayout.Button("プレハブに反映"))
                {
                    // 反映した値を、以降のカメラリセットで戻す初期値にする
                    if (ApplyToPrefab(followCamera))
                        followCamera.CaptureInitialValues();
                }
            }
        }

        /// <summary>
        /// Play 中のインスタンスの値（参照以外）を、元のプレハブの同じ位置にある FollowCamera へ書き込んで保存する。
        /// プレイヤーは Resources から Instantiate されるため、プレハブとの接続が無い。FollowCamera を持つプレハブを探して対応付ける
        /// </summary>
        static bool ApplyToPrefab(FollowCamera instance)
        {
            var relativePath = GetRelativePath(instance.transform);
            var rootName = StripCloneSuffix(instance.transform.root.name);

            var candidates = FindPrefabPaths(relativePath);
            if (candidates.Count == 0)
            {
                Debug.LogErrorFormat("[FollowCameraEditor] 反映先のプレハブが見つかりません。FollowCamera の位置: {0}", relativePath);
                return false;
            }

            // 候補が複数あるときは、ルートの名前が一致するものを使う
            var path = candidates.Count == 1
                ? candidates[0]
                : candidates.FirstOrDefault(p => System.IO.Path.GetFileNameWithoutExtension(p) == rootName);
            if (path == null)
            {
                Debug.LogErrorFormat("[FollowCameraEditor] 反映先のプレハブを 1 つに決められません。候補: {0}", string.Join(", ", candidates));
                return false;
            }

            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var dest = FindByRelativePath(root.transform, relativePath)?.GetComponent<FollowCamera>();
                if (dest == null)
                {
                    Debug.LogErrorFormat("[FollowCameraEditor] プレハブ内に FollowCamera が見つかりません。{0} : {1}", path, relativePath);
                    return false;
                }

                var copied = CopyValues(instance, dest);
                PrefabUtility.SaveAsPrefabAsset(root, path, out var success);
                if (!success)
                {
                    Debug.LogErrorFormat("[FollowCameraEditor] {0} の保存に失敗しました", path);
                    return false;
                }

                Debug.LogFormat("[FollowCameraEditor] {0} に反映しました: {1}", path, string.Join(", ", copied));
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>参照（Transform や ScriptableObject）以外のシリアライズ値をコピーする。コピーしたフィールド名を返す</summary>
        static List<string> CopyValues(FollowCamera source, FollowCamera dest)
        {
            var copied = new List<string>();
            var src = new SerializedObject(source);
            var dst = new SerializedObject(dest);

            var prop = src.GetIterator();
            var enterChildren = true;
            while (prop.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (prop.name == "m_Script" || prop.propertyType == SerializedPropertyType.ObjectReference)
                    continue;

                dst.CopyFromSerializedProperty(prop);
                copied.Add(prop.name);
            }

            dst.ApplyModifiedPropertiesWithoutUndo();
            return copied;
        }

        /// <summary>FollowCamera を持ち、ルートからの位置が同じプレハブのパス</summary>
        static List<string> FindPrefabPaths(string relativePath)
        {
            var paths = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                    continue;

                var found = FindByRelativePath(prefab.transform, relativePath);
                if (found != null && found.GetComponent<FollowCamera>() != null)
                    paths.Add(path);
            }

            return paths;
        }

        static string GetRelativePath(Transform t)
        {
            var names = new List<string>();
            while (t.parent != null)
            {
                names.Insert(0, t.name);
                t = t.parent;
            }

            return string.Join("/", names);
        }

        static Transform FindByRelativePath(Transform root, string relativePath)
        {
            return string.IsNullOrEmpty(relativePath) ? root : root.Find(relativePath);
        }

        static string StripCloneSuffix(string name)
        {
            const string suffix = "(Clone)";
            return name.EndsWith(suffix) ? name.Substring(0, name.Length - suffix.Length).TrimEnd() : name;
        }
    }
}
