using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LockTargetImage))]
public class LockTargetImageEditor : Editor
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
