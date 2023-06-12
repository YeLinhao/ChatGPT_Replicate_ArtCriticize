using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Scripting.Python;
[CustomEditor(typeof(PythonManager))]
public class PythonManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Launch Python Script Positive", GUILayout.Height(35)))
        {
           
            string path = Application.dataPath + "/Python/main_positive.py";
            PythonRunner.RunFile(path);
            
        }

        if (GUILayout.Button("Launch Python Script Neutral", GUILayout.Height(35)))
        {

            string path = Application.dataPath + "/Python/main_neutral.py";
            PythonRunner.RunFile(path);

        }

        if (GUILayout.Button("Launch Python Script Negative", GUILayout.Height(35)))
        {

            string path = Application.dataPath + "/Python/main_negative.py";
            PythonRunner.RunFile(path);

        }
    }
}
