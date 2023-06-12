using UnityEditor;
using UnityEngine;
using XDPaint.Core.Materials;
using XDPaint.Editor.Utils;

namespace XDPaint.Editor
{
    [CustomPropertyDrawer(typeof(Brush))]
    public class BrushDrawer : PropertyDrawer
    {
        private float MarginBetweenFields => EditorGUIUtility.standardVerticalSpacing;
        private float SingleLineHeight => EditorGUIUtility.singleLineHeight;
        private float SingleLineHeightWithMargin => SingleLineHeight + MarginBetweenFields;

        private Brush brush;
        private Rect rect;
        private float textureHeight;
        private SerializedProperty name, texture, filter, color, size, hardness, preview, renderAngle;

        private enum PropertyType
        {
            Property,
            Slider
        }

        private Brush GetBrush(SerializedProperty property)
        {
            return PropertyDrawerUtility.GetActualObjectForSerializedProperty<Brush>(property);
        }

        private float GetMinimalBrushSize()
        {
            var minSize = BrushDrawerHelper.MinValue;
            if (texture != null && texture.objectReferenceValue != null)
            {
                var brushTexture = texture.objectReferenceValue as Texture;
                if (brushTexture != null)
                {
                    minSize = 2f / (Mathf.Max(brushTexture.width, brushTexture.height) - 3f);
                }
            }
            return minSize;
        }
        
        private void DisplayPropertyField(SerializedProperty property, string tooltip = "", PropertyType propertyType = PropertyType.Property, float min = 0f, float max = 1f)
        {
            if (propertyType == PropertyType.Property)
            {
                EditorGUI.PropertyField(rect, property, new GUIContent(property.displayName, tooltip));
            }
            else
            {
                EditorGUI.Slider(rect, property, min, max, new GUIContent(property.displayName, tooltip));
            }
            rect.y += MarginBetweenFields;
            AddToPositionY(SingleLineHeight);
        }

        private void AddToPositionY(float addY)
        {
            rect.y += addY;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            brush = GetBrush(property);
            if (brush != null)
            {
                var brushTexture = brush.RenderTexture != null ? brush.RenderTexture : brush.SourceTexture;
                if (brushTexture != null)
                {
                    var maxSize = Mathf.Max(brushTexture.width, brushTexture.height);
                    var textureSize = Mathf.Clamp(maxSize, BrushDrawerHelper.MaxBrushTextureSize, maxSize);
                    textureHeight = textureSize + BrushDrawerHelper.LineOffset * 2f;
                }
            }
            var height = property.isExpanded ? BrushDrawerHelper.PropertiesCount * SingleLineHeightWithMargin + textureHeight : SingleLineHeightWithMargin;
            return height;
        }

        public override bool CanCacheInspectorGUI(SerializedProperty property)
        {
            return false;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            rect = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), GUIContent.none);
            rect.height = SingleLineHeight;
            rect.y += MarginBetweenFields;
 
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 1;

            name = property.FindPropertyRelative("name");
            var foldoutRect = new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth, SingleLineHeightWithMargin);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, name.stringValue);
            if (property.isExpanded)
            {
                Undo.RecordObject(property.serializedObject.targetObject, "Brush Parameters");
                brush = GetBrush(property);
                rect.y += SingleLineHeightWithMargin;
                color = property.FindPropertyRelative("color");
                texture = property.FindPropertyRelative("sourceTexture");
                filter = property.FindPropertyRelative("filterMode");
                size = property.FindPropertyRelative("size");
                hardness = property.FindPropertyRelative("hardness");
                preview = property.FindPropertyRelative("preview");
                renderAngle = property.FindPropertyRelative("renderAngle");
                
                EditorGUI.BeginChangeCheck();
                EditorGUI.BeginDisabledGroup(brush.Name == BrushDrawerHelper.CustomPresetName);
                DisplayPropertyField(name, BrushDrawerHelper.NameTooltip);
                EditorGUI.EndDisabledGroup();
                if (EditorGUI.EndChangeCheck())
                {
                    brush.Name = name.stringValue;
                }
                
                EditorGUI.BeginChangeCheck();
                DisplayPropertyField(texture, BrushDrawerHelper.TextureTooltip);
                if (EditorGUI.EndChangeCheck())
                {
                    brush.SetTexture(texture.objectReferenceValue as Texture);
                    EditorUtility.SetDirty(property.serializedObject.targetObject);
                }
                
                EditorGUI.BeginChangeCheck();
                DisplayPropertyField(filter, BrushDrawerHelper.FilterTooltip);
                if (EditorGUI.EndChangeCheck())
                {
                    brush.FilterMode = (FilterMode)filter.enumValueIndex;
                }
                
                EditorGUI.BeginChangeCheck();
                DisplayPropertyField(color, BrushDrawerHelper.ColorTooltip);
                if (EditorGUI.EndChangeCheck())
                {
                    brush.SetColor(color.colorValue);
                }
                
                EditorGUI.BeginChangeCheck();
                DisplayPropertyField(size, BrushDrawerHelper.SizeTooltip, PropertyType.Slider, GetMinimalBrushSize(), BrushDrawerHelper.MaxValue);
                if (EditorGUI.EndChangeCheck())
                {
                    brush.Size = size.floatValue;
                }
                
                EditorGUI.BeginChangeCheck();
                DisplayPropertyField(hardness, BrushDrawerHelper.HardnessTooltip, PropertyType.Slider, BrushDrawerHelper.MinHardnessValue, BrushDrawerHelper.MaxHardnessValue);
                if (EditorGUI.EndChangeCheck())
                {
                    brush.Hardness = hardness.floatValue;
                }
  
                EditorGUI.BeginChangeCheck();
                DisplayPropertyField(renderAngle, BrushDrawerHelper.RenderAngleTooltip, PropertyType.Slider, BrushDrawerHelper.MinAngleValue, BrushDrawerHelper.MaxAngleValue);
                if (EditorGUI.EndChangeCheck())
                {
                    brush.RenderAngle = renderAngle.floatValue;
                }
                
                EditorGUI.BeginChangeCheck();
                DisplayPropertyField(preview, BrushDrawerHelper.PreviewTooltip);
                if (EditorGUI.EndChangeCheck())
                {
                    brush.Preview = preview.boolValue;
                }
                
                DrawTexture();
            }
            EditorGUI.indentLevel = indent;
            
            EditorGUI.EndProperty();
        }

        private void DrawTexture()
        {
            var brushTexture = brush.RenderTexture != null ? brush.RenderTexture : brush.SourceTexture;
            if (brushTexture == null)
                return;

            var width = Mathf.Clamp(brushTexture.width * brush.Size, 1f, BrushDrawerHelper.MaxBrushTextureSize);
            var height = Mathf.Clamp(brushTexture.height * brush.Size, 1f, BrushDrawerHelper.MaxBrushTextureSize);
            var textureRect = new Rect(new Vector2(rect.x + rect.width / 2f - width / 2f, rect.y + BrushDrawerHelper.LineOffset), new Vector2(width, height));
            GUI.DrawTexture(textureRect, brushTexture, ScaleMode.ScaleToFit);
            textureHeight = height + BrushDrawerHelper.LineOffset;
        }
    }
}