using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using XDPaint.Tools.Triangles;

namespace XDPaint.Editor
{
    public class TrianglesDataWindow : EditorWindow
    {
        private PaintManager paintManager;

        public void SetPaintManager(PaintManager newPaintManager)
        {
            paintManager = newPaintManager;
            TrianglesData.OnUpdate = progress =>
            {
                if (EditorUtility.DisplayCancelableProgressBar("Updating", "Updating triangles data, please wait...", progress))
                {
                    TrianglesData.Break();
                    paintManager.ClearTrianglesNeighborsData();
                };
            };
            TrianglesData.OnFinish = EditorUtility.ClearProgressBar;
        }

        void OnGUI()
        {
            EditorGUI.BeginDisabledGroup(true);
            GUILayout.TextArea(string.Empty, GUI.skin.horizontalSlider, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUI.EndDisabledGroup();
            GUILayout.Label("Press 'Fill triangles data' to fill mesh triangles data.", EditorStyles.label);
            GUILayout.Label("Note, that it may take a few minutes.", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);
            GUILayout.TextArea(string.Empty, GUI.skin.horizontalSlider, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUI.EndDisabledGroup();
            if (GUILayout.Button("Fill triangles data"))
            {
                if (Selection.activeGameObject != null)
                {
                    Selection.activeGameObject.TryGetComponent<PaintManager>(out var selectedPaintManager);
                    if (selectedPaintManager != null)
                    {
                        paintManager = selectedPaintManager;
                    }
                }
                else
                {
                    Debug.LogWarning("Selected GameObject is null.");
                    return;
                }
                if (paintManager == null)
                {
                    Debug.LogWarning("Can't find PaintManager in Selected GameObject.");
                    return;
                }
                paintManager.FillTrianglesData();
                if (!Application.isPlaying)
                {
                    EditorUtility.SetDirty(paintManager);
                    EditorSceneManager.MarkSceneDirty(paintManager.gameObject.scene);
                }
                Close();
            }
        }
    }
}