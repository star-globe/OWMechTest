using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(CanvasRenderer))]
public class RingImage : Image
{
    [SerializeField, Range(0, 1.0f)]
    float radius = 1.0f;

    [SerializeField, Range(0, 1.0f)]
    float width = 0.1f;

    protected override void Start()
    {
        base.Start();
        SetFields();
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        SetFields();
    }

    private void SetFields()
    {
        this.material.SetColor("_Color", this.color);
        this.material.SetFloat("_Radius", radius / 2);
        this.material.SetFloat("_Width", width / 2);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(RingImage))]
public class RingImageEditor : UnityEditor.UI.ImageEditor
{
    private SerializedProperty _radiusProperty;
    private SerializedProperty _widthProperty;

    protected override void OnEnable()
    {
        base.OnEnable();
        _radiusProperty = serializedObject.FindProperty("radius");
        _widthProperty = serializedObject.FindProperty("width");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        serializedObject.Update();

        EditorGUILayout.PropertyField(_radiusProperty);
        EditorGUILayout.PropertyField(_widthProperty);

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
