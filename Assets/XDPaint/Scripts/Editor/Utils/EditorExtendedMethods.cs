using System;
using UnityEditor;
using UnityEngine;

namespace XDPaint.Editor.Utils
{
    public static class EditorExtendedMethods
    {
        public static T GetInstance<T>(this SerializedProperty property) where T : class 
        {
            T obj = null;
            try
            {
                obj = PropertyDrawerUtility.GetActualObjectForSerializedProperty<T>(property);
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
            return obj;
        }
    }
}