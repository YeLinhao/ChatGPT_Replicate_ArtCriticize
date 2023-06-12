using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using XDPaint.Core.PaintObject.Base;
using XDPaint.Tools.Image.Base;
using XDPaint.Utils;

namespace XDPaint.Editor.Utils
{
    public class ToolSettingsDrawer
    {
        public PropertyInfo DrawSettings(IPaintTool tool)
        {
            if (!Application.isPlaying || tool == null)
                return null;

            var type = tool.GetType();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            PropertyInfo changedProperty = null;
            foreach (var property in properties)
            {
                var allAttributes = property.GetCustomAttributes(true);
                var hasAttribute = allAttributes.FirstOrDefault(x => x is PaintToolPropertyAttribute) != null;
                if (property.PropertyType == typeof(bool) && hasAttribute)
                {
                    var result = Convert.ToBoolean(property.GetValue(tool));
                    var name = property.Name.ToCamelCaseWithSpace();
                    EditorGUI.BeginChangeCheck();
                    result = EditorGUI.Toggle(EditorGUILayout.GetControlRect(), name, result);
                    if (EditorGUI.EndChangeCheck())
                    {
                        property.SetValue(tool, result);
                        changedProperty = property;
                    }
                }

                if (property.PropertyType == typeof(float) && hasAttribute)
                {
                    var result = Convert.ToSingle(property.GetValue(tool));
                    var name = property.Name.ToCamelCaseWithSpace();
                    var rangeMin = 0f;
                    var rangeMax = 1f;
                    var hasRangeAttribute = false;
                    foreach (var attribute in allAttributes)
                    {
                        if (attribute is PaintToolRangeAttribute range)
                        {
                            hasRangeAttribute = true;
                            rangeMin = range.Min;
                            rangeMax = range.Max;
                            break;
                        }
                    }
                    EditorGUI.BeginChangeCheck();
                    result = hasRangeAttribute
                        ? EditorGUI.Slider(EditorGUILayout.GetControlRect(), name, result, rangeMin, rangeMax)
                        : EditorGUI.FloatField(EditorGUILayout.GetControlRect(), name, result);
                    if (EditorGUI.EndChangeCheck())
                    {
                        property.SetValue(tool, result);
                        changedProperty = property;
                    }
                }

                if (property.PropertyType == typeof(int) && hasAttribute)
                {
                    var result = Convert.ToInt32(property.GetValue(tool));
                    var name = property.Name.ToCamelCaseWithSpace();
                    var rangeMin = 0;
                    var rangeMax = 1;
                    var hasRangeAttribute = false;
                    foreach (var attribute in allAttributes)
                    {
                        if (attribute is PaintToolRangeAttribute range)
                        {
                            hasRangeAttribute = true;
                            rangeMin = (int)range.Min;
                            rangeMax = (int)range.Max;
                            break;
                        }
                    }
                    EditorGUI.BeginChangeCheck();
                    result = hasRangeAttribute
                        ? EditorGUI.IntSlider(EditorGUILayout.GetControlRect(), name, result, rangeMin, rangeMax)
                        : EditorGUI.IntField(EditorGUILayout.GetControlRect(), name, result);
                    if (EditorGUI.EndChangeCheck())
                    {
                        property.SetValue(tool, result);
                        changedProperty = property;
                    }
                }
            }
            return changedProperty;
        }
    }
}