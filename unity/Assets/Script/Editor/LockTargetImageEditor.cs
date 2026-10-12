using UnityEditor;
using UnityEngine;

namespace AdvancedGears
{
    [CustomEditor(typeof(LockTargetImage))]
    public class LockTargetImageEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();

            if (GUILayout.Button("Apply Color"))
            {
                var lockTargetImage = (LockTargetImage)target;
                lockTargetImage.ApplyColor();
                lockTargetImage.ApplyTextBackGroundColor();
                EditorUtility.SetDirty(target);
            }
        }
    }
}
