using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(Sound))]
public class SoundDrawer : PropertyDrawer
{
    private bool parentSettings = false;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Indent level for nested fields
        int indent = EditorGUI.indentLevel;

        // Draw the foldout for the Sound object
        property.isExpanded = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), property.isExpanded, label);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            float yOffset = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // Get all relevant properties
            SerializedProperty nameProp = property.FindPropertyRelative("name");
            SerializedProperty clipProp = property.FindPropertyRelative("clip");
            SerializedProperty volumeProp = property.FindPropertyRelative("volume");
            SerializedProperty volumeVarianceProp = property.FindPropertyRelative("volumeVariance");
            SerializedProperty pitchProp = property.FindPropertyRelative("pitch");
            SerializedProperty pitchVarianceProp = property.FindPropertyRelative("pitchVariance");
            SerializedProperty loopProp = property.FindPropertyRelative("loop");
            SerializedProperty playOnAwakeProp = property.FindPropertyRelative("playOnAwake");
            SerializedProperty mixerGroupProp = property.FindPropertyRelative("mixerGroup");
            SerializedProperty onDifferentGOProp = property.FindPropertyRelative("onDifferentGO");
            SerializedProperty attachedToObjectProp = property.FindPropertyRelative("attachedObject");
            SerializedProperty attachOnPlayer = property.FindPropertyRelative("attachedOnPlayer");
            SerializedProperty minDistanceProp = property.FindPropertyRelative("minDistance");
            SerializedProperty maxDistanceProp = property.FindPropertyRelative("maxDistance");
            SerializedProperty spatialBlendProp = property.FindPropertyRelative("spatialBlend");

            // Draw standard fields
            EditorGUI.PropertyField(new Rect(position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight), nameProp);
            yOffset += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            EditorGUI.PropertyField(new Rect(position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight), clipProp);
            yOffset += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            EditorGUI.PropertyField(new Rect(position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight), volumeProp);
            yOffset += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            EditorGUI.PropertyField(new Rect(position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight), volumeVarianceProp);
            yOffset += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            EditorGUI.PropertyField(new Rect(position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight), pitchProp);
            yOffset += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            EditorGUI.PropertyField(new Rect(position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight), pitchVarianceProp);
            yOffset += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            EditorGUI.PropertyField(new Rect(position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight), loopProp);
            yOffset += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            EditorGUI.PropertyField(new Rect(position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight), playOnAwakeProp);
            yOffset += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            EditorGUI.PropertyField(new Rect(position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight), mixerGroupProp);
            yOffset += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // Parent Settings Foldout
            parentSettings = EditorGUI.Foldout(new Rect(position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight), parentSettings, "Parent Settings");
            yOffset += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            if (parentSettings)
            {
                // Draw "onDifferentGO" property
                EditorGUI.PropertyField(new Rect(position.x + 15, position.y + yOffset, position.width - 15, EditorGUIUtility.singleLineHeight), onDifferentGOProp);
                yOffset += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                if (onDifferentGOProp.boolValue)
                {
                    EditorGUI.PropertyField(new Rect(position.x + 15, position.y + yOffset, position.width - 15, EditorGUIUtility.singleLineHeight), attachOnPlayer);
                    yOffset += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    if (!attachOnPlayer.boolValue)
                    {
                        EditorGUI.PropertyField(new Rect(position.x + 15, position.y + yOffset, position.width - 15, EditorGUIUtility.singleLineHeight), attachedToObjectProp);
                        yOffset += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    }
                    

                    

                    // 3D Blend Options
                    if (attachedToObjectProp.objectReferenceValue != null)
                    {
                        EditorGUI.LabelField(new Rect(position.x + 15, position.y + yOffset, position.width - 15, EditorGUIUtility.singleLineHeight), "3D Blend Options");
                        yOffset += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                        EditorGUI.Slider(new Rect(position.x + 15, position.y + yOffset, position.width - 15, EditorGUIUtility.singleLineHeight), minDistanceProp, 0f, 100f, new GUIContent("Min Distance"));
                        yOffset += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                        EditorGUI.Slider(new Rect(position.x + 15, position.y + yOffset, position.width - 15, EditorGUIUtility.singleLineHeight), maxDistanceProp, 0f, 500f, new GUIContent("Max Distance"));
                        yOffset += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                        EditorGUI.Slider(new Rect(position.x + 15, position.y + yOffset, position.width - 15, EditorGUIUtility.singleLineHeight), spatialBlendProp, 0f, 1f, new GUIContent("Spatial Blend"));
                        yOffset += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    }
                }
            }

            EditorGUI.indentLevel = indent; // Reset indent level
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight; // For foldout

        if (property.isExpanded)
        {
            // Standard fields
            height += 9 * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);

            // Parent settings foldout
            if (parentSettings)
            {
                height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                SerializedProperty onDifferentGOProp = property.FindPropertyRelative("onDifferentGO");
                if (onDifferentGOProp.boolValue)
                {
                    height += 2 * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);

                    SerializedProperty attachedToObjectProp = property.FindPropertyRelative("attachedObject");
                    if (attachedToObjectProp.objectReferenceValue != null)
                    {
                        // Add space for 3D blend options
                        height += 4 * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
                    }
                }
            }
        }

        return height + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // Extra spacing between elements
    }
}
