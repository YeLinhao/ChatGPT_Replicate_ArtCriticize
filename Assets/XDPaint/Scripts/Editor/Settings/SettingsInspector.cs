using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using XDPaint.Core;
using XDPaint.Tools;

namespace XDPaint.Editor
{
    [CustomEditor(typeof(Settings))]
    public class SettingsInspector : UnityEditor.Editor
    {
        private Settings settings;
        private SerializedProperty defaultBrushProperty;
        private SerializedProperty defaultCircleBrushProperty;
        private SerializedProperty isVRModeProperty;
        private SerializedProperty pressureEnabledProperty;
        private SerializedProperty checkCanvasRaycastsProperty;
        private SerializedProperty brushDuplicatePartWidthProperty;
        private SerializedProperty pixelPerUnitProperty;
        private SerializedProperty containerGameObjectNameProperty;
        
        void OnEnable()
        {
            settings = (Settings)target;
            defaultBrushProperty = serializedObject.FindProperty("DefaultBrush");
            defaultCircleBrushProperty = serializedObject.FindProperty("DefaultCircleBrush");
            isVRModeProperty = serializedObject.FindProperty("IsVRMode");
            pressureEnabledProperty = serializedObject.FindProperty("PressureEnabled");
            checkCanvasRaycastsProperty = serializedObject.FindProperty("CheckCanvasRaycasts");
            brushDuplicatePartWidthProperty = serializedObject.FindProperty("BrushDuplicatePartWidth");
            pixelPerUnitProperty = serializedObject.FindProperty("PixelPerUnit");
            containerGameObjectNameProperty = serializedObject.FindProperty("ContainerGameObjectName");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(defaultBrushProperty, new GUIContent("Default Brush"));
            EditorGUILayout.PropertyField(defaultCircleBrushProperty, new GUIContent("Default Circle Brush"));
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(isVRModeProperty, new GUIContent("Is VR Mode"));
            if (EditorGUI.EndChangeCheck())
            {
                var group = EditorUserBuildSettings.selectedBuildTargetGroup;
                var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
                var allDefines = defines.Split(';').ToList();
                if (isVRModeProperty.boolValue)
                {
                    allDefines.AddRange(Constants.Defines.VREnabled.Except(allDefines));
                }
                else
                {
                    for (var i = allDefines.Count - 1; i >= 0; i--)
                    {
                        if (Constants.Defines.VREnabled.Contains(allDefines[i]))
                        {
                            allDefines.RemoveAt(i);
                        }
                    }
                }
                PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", allDefines.ToArray()));
            }
            EditorGUILayout.PropertyField(pressureEnabledProperty, new GUIContent("Pressure Enabled"));
            EditorGUILayout.PropertyField(checkCanvasRaycastsProperty, new GUIContent("Check Canvas Raycasts"));
            EditorGUILayout.PropertyField(brushDuplicatePartWidthProperty, new GUIContent("Brush Duplicate Part Width"));
            EditorGUILayout.PropertyField(pixelPerUnitProperty, new GUIContent("Pixel per Unit"));
            EditorGUILayout.PropertyField(containerGameObjectNameProperty, new GUIContent("Container GameObject Name"));
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}