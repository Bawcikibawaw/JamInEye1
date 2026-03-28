#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

[CustomEditor(typeof(TilemapGroupedShadowBuilder))]
public class TilemapGroupedShadowBuilderEditor : Editor
{
    private TilemapGroupedShadowBuilder builder;

    private SerializedProperty sourceTilemapProp;
    private SerializedProperty shadowContainerProp;
    private SerializedProperty shadowContainerNameProp;
    private SerializedProperty shadowContainerLayerId;
    private SerializedProperty shadowColorProp;
    private SerializedProperty sortingLayerNameProp;
    private SerializedProperty orderInLayerProp;
    private SerializedProperty shadowMaterialProp;
    private SerializedProperty worldOffsetProp;
    private SerializedProperty horizonPointProp;
    private SerializedProperty horizonDirectionProp;
    private SerializedProperty baseRotationDegProp;
    private SerializedProperty rotationPerUnitProp;
    private SerializedProperty maxAbsRotationProp;
    private SerializedProperty useAutoRotationProp;
    private SerializedProperty mirrorXProp;
    private SerializedProperty mirrorYProp;
    private SerializedProperty shadowScaleProp;
    private SerializedProperty createColliderProp;
    private SerializedProperty colliderIsTriggerProp;
    private SerializedProperty colliderLocalOffsetProp;
    private SerializedProperty rebuildOnEnableProp;
    private SerializedProperty clearBeforeBuildProp;
    private SerializedProperty groupsProp;

    private int activeGroupIndex = -1;
    private bool paintMode;
    private bool eraseMode;
    private bool movePivotMode;
    private bool rotateGroupMode;

    private void OnEnable()
    {
        builder = (TilemapGroupedShadowBuilder)target;

        sourceTilemapProp = serializedObject.FindProperty("sourceTilemap");
        shadowContainerProp = serializedObject.FindProperty("shadowContainer");
        shadowContainerNameProp = serializedObject.FindProperty("shadowContainerName");
        shadowContainerLayerId = serializedObject.FindProperty("shadowContainerLayerId");
        shadowColorProp = serializedObject.FindProperty("shadowColor");
        sortingLayerNameProp = serializedObject.FindProperty("sortingLayerName");
        orderInLayerProp = serializedObject.FindProperty("orderInLayer");
        shadowMaterialProp = serializedObject.FindProperty("shadowMaterial");
        worldOffsetProp = serializedObject.FindProperty("worldOffset");
        horizonPointProp = serializedObject.FindProperty("horizonPoint");
        horizonDirectionProp = serializedObject.FindProperty("horizonDirection");
        baseRotationDegProp = serializedObject.FindProperty("baseRotationDeg");
        rotationPerUnitProp = serializedObject.FindProperty("rotationPerUnit");
        maxAbsRotationProp = serializedObject.FindProperty("maxAbsRotation");
        useAutoRotationProp = serializedObject.FindProperty("useAutoRotation");
        mirrorXProp = serializedObject.FindProperty("mirrorX");
        mirrorYProp = serializedObject.FindProperty("mirrorY");
        shadowScaleProp = serializedObject.FindProperty("shadowScale");
        createColliderProp = serializedObject.FindProperty("createCollider");
        colliderIsTriggerProp = serializedObject.FindProperty("colliderIsTrigger");
        colliderLocalOffsetProp = serializedObject.FindProperty("colliderLocalOffset");
        rebuildOnEnableProp = serializedObject.FindProperty("rebuildOnEnable");
        clearBeforeBuildProp = serializedObject.FindProperty("clearBeforeBuild");
        groupsProp = serializedObject.FindProperty("groups");

        SceneView.duringSceneGui += DuringSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= DuringSceneGUI;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawMainSettings();
        EditorGUILayout.Space(10f);
        DrawGroupsSection();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawMainSettings()
    {
        EditorGUILayout.LabelField("Source", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(sourceTilemapProp);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Container", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(shadowContainerProp);
        EditorGUILayout.PropertyField(shadowContainerNameProp);
        EditorGUILayout.PropertyField(shadowContainerLayerId);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Visual", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(shadowColorProp);
        EditorGUILayout.PropertyField(sortingLayerNameProp);
        EditorGUILayout.PropertyField(orderInLayerProp);
        EditorGUILayout.PropertyField(shadowMaterialProp);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Placement", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(worldOffsetProp);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Auto Rotation", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(useAutoRotationProp);
        EditorGUILayout.PropertyField(horizonPointProp);
        EditorGUILayout.PropertyField(horizonDirectionProp);
        EditorGUILayout.PropertyField(baseRotationDegProp);
        EditorGUILayout.PropertyField(rotationPerUnitProp);
        EditorGUILayout.PropertyField(maxAbsRotationProp);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Shadow Shape", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(mirrorXProp);
        EditorGUILayout.PropertyField(mirrorYProp);
        EditorGUILayout.PropertyField(shadowScaleProp);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Collider", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(createColliderProp);
        if (createColliderProp.boolValue)
        {
            EditorGUILayout.PropertyField(colliderIsTriggerProp);
            EditorGUILayout.PropertyField(colliderLocalOffsetProp);
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Build", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(rebuildOnEnableProp);
        EditorGUILayout.PropertyField(clearBeforeBuildProp);

        EditorGUILayout.Space(8);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Rebuild"))
            {
                serializedObject.ApplyModifiedProperties();
                Undo.RecordObject(builder, "Rebuild Shadows");
                builder.Rebuild();
                EditorUtility.SetDirty(builder);
            }

            if (GUILayout.Button("Refresh Roots"))
            {
                serializedObject.ApplyModifiedProperties();
                Undo.RecordObject(builder, "Refresh Shadow Roots");
                builder.RefreshGroupRootsOnly();
                EditorUtility.SetDirty(builder);
            }

            if (GUILayout.Button("Clear Shadows"))
            {
                serializedObject.ApplyModifiedProperties();
                Undo.RecordObject(builder, "Clear Shadows");
                builder.ClearShadows();
                EditorUtility.SetDirty(builder);
            }
        }
    }

    private void DrawGroupsSection()
    {
        EditorGUILayout.LabelField("Groups", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Add Group"))
            {
                Undo.RecordObject(builder, "Add Group");
                builder.AddGroup();
                EditorUtility.SetDirty(builder);
                serializedObject.Update();
                activeGroupIndex = builder.Groups.Count - 1;
            }

            GUI.enabled = activeGroupIndex >= 0 && activeGroupIndex < builder.Groups.Count;
            if (GUILayout.Button("Remove Active"))
            {
                Undo.RecordObject(builder, "Remove Group");
                builder.RemoveGroupAt(activeGroupIndex);
                EditorUtility.SetDirty(builder);
                serializedObject.Update();
                activeGroupIndex = Mathf.Clamp(activeGroupIndex - 1, -1, builder.Groups.Count - 1);
                StopModes();
            }
            GUI.enabled = true;
        }

        EditorGUILayout.Space(4f);

        for (int i = 0; i < groupsProp.arraySize; i++)
        {
            SerializedProperty groupProp = groupsProp.GetArrayElementAtIndex(i);
            SerializedProperty nameProp = groupProp.FindPropertyRelative("name");
            SerializedProperty colorProp = groupProp.FindPropertyRelative("gizmoColor");
            SerializedProperty enabledProp = groupProp.FindPropertyRelative("enabled");
            SerializedProperty cellsProp = groupProp.FindPropertyRelative("cells");
            SerializedProperty pivotModeProp = groupProp.FindPropertyRelative("pivotMode");
            SerializedProperty pivotOffsetProp = groupProp.FindPropertyRelative("pivotOffset");
            SerializedProperty manualPivotProp = groupProp.FindPropertyRelative("manualPivotWorldPosition");
            SerializedProperty manualEulerProp = groupProp.FindPropertyRelative("manualEulerRotation");

            GUI.backgroundColor = activeGroupIndex == i ? new Color(0.75f, 0.9f, 1f) : Color.white;
            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUI.backgroundColor = Color.white;

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Toggle(activeGroupIndex == i, "Active", "Button", GUILayout.Width(60)))
                        activeGroupIndex = i;

                    EditorGUILayout.PropertyField(nameProp, GUIContent.none);
                    EditorGUILayout.PropertyField(enabledProp, GUIContent.none, GUILayout.Width(20));
                    EditorGUILayout.PropertyField(colorProp, GUIContent.none, GUILayout.Width(50));
                }

                EditorGUILayout.LabelField($"Tiles: {cellsProp.arraySize}");

                EditorGUILayout.PropertyField(pivotModeProp);
                EditorGUILayout.PropertyField(pivotOffsetProp);

                if ((TilemapGroupedShadowBuilder.GroupPivotMode)pivotModeProp.enumValueIndex ==
                    TilemapGroupedShadowBuilder.GroupPivotMode.ManualWorldPosition)
                {
                    EditorGUILayout.PropertyField(manualPivotProp);
                }

                EditorGUILayout.PropertyField(manualEulerProp, new GUIContent("Custom Euler Rotation"));

                float autoAngle = builder.GetGroupAutoAngle(i);
                Vector3 euler = manualEulerProp.vector3Value;
                Vector3 finalEuler = euler;
                finalEuler.z += autoAngle;

                EditorGUILayout.LabelField(
                    "Computed",
                    $"Auto Z: {autoAngle:F2}°   Final Euler: ({finalEuler.x:F2}, {finalEuler.y:F2}, {finalEuler.z:F2})"
                );

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Paint Tiles"))
                    {
                        activeGroupIndex = i;
                        paintMode = true;
                        eraseMode = false;
                        movePivotMode = false;
                        rotateGroupMode = false;
                        SceneView.RepaintAll();
                    }

                    if (GUILayout.Button("Erase Tiles"))
                    {
                        activeGroupIndex = i;
                        paintMode = false;
                        eraseMode = true;
                        movePivotMode = false;
                        rotateGroupMode = false;
                        SceneView.RepaintAll();
                    }

                    if (GUILayout.Button("Move Pivot"))
                    {
                        activeGroupIndex = i;
                        paintMode = false;
                        eraseMode = false;
                        movePivotMode = true;
                        rotateGroupMode = false;
                        SceneView.RepaintAll();
                    }

                    if (GUILayout.Button("Rotate Group"))
                    {
                        activeGroupIndex = i;
                        paintMode = false;
                        eraseMode = false;
                        movePivotMode = false;
                        rotateGroupMode = true;
                        SceneView.RepaintAll();
                    }

                    if (GUILayout.Button("Stop"))
                    {
                        StopModes();
                        SceneView.RepaintAll();
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Rebuild Group"))
                    {
                        serializedObject.ApplyModifiedProperties();
                        Undo.RecordObject(builder, "Rebuild Group");
                        builder.Rebuild();
                        EditorUtility.SetDirty(builder);
                    }

                    if (GUILayout.Button("Clear Group Tiles"))
                    {
                        Undo.RecordObject(builder, "Clear Group Tiles");
                        cellsProp.ClearArray();
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(builder);
                    }

                    if (GUILayout.Button("Reset Rotation"))
                    {
                        Undo.RecordObject(builder, "Reset Group Rotation");
                        manualEulerProp.vector3Value = Vector3.zero;
                        serializedObject.ApplyModifiedProperties();
                        builder.RefreshGroupRootsOnly();
                        EditorUtility.SetDirty(builder);
                    }
                }
            }
        }

        EditorGUILayout.HelpBox(
            "Paint Tiles: assign tiles to the active group.\n" +
            "Move Pivot: drag the group's pivot in Scene view.\n" +
            "Rotate Group: rotate the generated shadow root around that pivot.",
            MessageType.Info
        );
    }

    private void StopModes()
    {
        paintMode = false;
        eraseMode = false;
        movePivotMode = false;
        rotateGroupMode = false;
    }

    private void DuringSceneGUI(SceneView sceneView)
    {
        if (builder == null)
            return;

        Tilemap tilemap = builder.SourceTilemap;
        if (tilemap == null)
            return;

        serializedObject.Update();

        DrawScenePreview(tilemap);

        Event e = Event.current;

        if (paintMode || eraseMode)
        {
            HandleTilePaintErase(tilemap, e);
        }

        if (activeGroupIndex >= 0 && activeGroupIndex < builder.Groups.Count)
        {
            DrawPivotAndRotationHandles();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void HandleTilePaintErase(Tilemap tilemap, Event e)
    {
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            Vector3Int cell = tilemap.WorldToCell(ray.origin);

            if (!tilemap.HasTile(cell))
                return;

            Undo.RecordObject(builder, eraseMode ? "Erase Tile From Group" : "Paint Tile To Group");

            if (eraseMode)
                builder.RemoveCellFromGroup(activeGroupIndex, cell);
            else
                builder.AddCellToGroup(activeGroupIndex, cell, true);

            EditorUtility.SetDirty(builder);
            e.Use();
        }
    }

    private void DrawPivotAndRotationHandles()
    {
        SerializedProperty groupProp = groupsProp.GetArrayElementAtIndex(activeGroupIndex);
        SerializedProperty pivotModeProp = groupProp.FindPropertyRelative("pivotMode");
        SerializedProperty manualPivotProp = groupProp.FindPropertyRelative("manualPivotWorldPosition");
        SerializedProperty manualEulerProp = groupProp.FindPropertyRelative("manualEulerRotation");

        Vector2 pivot = builder.GetGroupPivotWorld(activeGroupIndex);

        Handles.color = Color.yellow;
        float size = HandleUtility.GetHandleSize(pivot) * 0.12f;
        Handles.DrawSolidDisc(pivot, Vector3.forward, size);

        if (movePivotMode)
        {
            EditorGUI.BeginChangeCheck();
            Vector3 newPos = Handles.PositionHandle(pivot, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(builder, "Move Group Pivot");

                if ((TilemapGroupedShadowBuilder.GroupPivotMode)pivotModeProp.enumValueIndex !=
                    TilemapGroupedShadowBuilder.GroupPivotMode.ManualWorldPosition)
                {
                    pivotModeProp.enumValueIndex = (int)TilemapGroupedShadowBuilder.GroupPivotMode.ManualWorldPosition;
                }

                manualPivotProp.vector2Value = new Vector2(newPos.x, newPos.y);
                serializedObject.ApplyModifiedProperties();

                builder.RefreshGroupRootsOnly();
                EditorUtility.SetDirty(builder);
            }
        }

        if (rotateGroupMode)
        {
            float autoAngle = builder.GetGroupAutoAngle(activeGroupIndex);

            Vector3 manualEuler = manualEulerProp.vector3Value;
            Vector3 startEuler = manualEuler;
            startEuler.z += autoAngle;

            Quaternion startRot = Quaternion.Euler(startEuler);

            EditorGUI.BeginChangeCheck();
            Quaternion newRot = Handles.RotationHandle(startRot, pivot);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(builder, "Rotate Group Shadow");

                Vector3 newEuler = newRot.eulerAngles;
                newEuler.x = NormalizeAngle(newEuler.x);
                newEuler.y = NormalizeAngle(newEuler.y);
                newEuler.z = NormalizeAngle(newEuler.z);

                Vector3 updatedManual = manualEulerProp.vector3Value;
                updatedManual.x = newEuler.x;
                updatedManual.y = newEuler.y;
                updatedManual.z = newEuler.z - autoAngle;

                manualEulerProp.vector3Value = updatedManual;

                serializedObject.ApplyModifiedProperties();
                builder.RefreshGroupRootsOnly();
                EditorUtility.SetDirty(builder);
            }
        }
    }

    private void DrawScenePreview(Tilemap tilemap)
    {
        for (int g = 0; g < builder.Groups.Count; g++)
        {
            var group = builder.Groups[g];
            if (group == null || group.cells == null)
                continue;

            Handles.color = group.gizmoColor;

            for (int i = 0; i < group.cells.Count; i++)
            {
                Vector3Int cell = group.cells[i];
                if (!tilemap.HasTile(cell))
                    continue;

                Vector3 center = tilemap.GetCellCenterWorld(cell);
                Vector3 size = tilemap.layoutGrid.cellSize;

                Vector3 a = center + new Vector3(-size.x, -size.y, 0f) * 0.5f;
                Vector3 b = center + new Vector3(-size.x, size.y, 0f) * 0.5f;
                Vector3 c = center + new Vector3(size.x, size.y, 0f) * 0.5f;
                Vector3 d = center + new Vector3(size.x, -size.y, 0f) * 0.5f;

                Handles.DrawAAPolyLine(2f, a, b, c, d, a);
            }

            Vector2 pivot = builder.GetGroupPivotWorld(g);
            Handles.DrawSolidDisc(pivot, Vector3.forward, HandleUtility.GetHandleSize(pivot) * 0.05f);
        }

        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(10, 10, 360, 90), "Grouped Shadow Tools", GUI.skin.window);
        GUILayout.Label($"Active Group: {activeGroupIndex}");
        GUILayout.Label(paintMode ? "Mode: Paint Tiles" :
                        eraseMode ? "Mode: Erase Tiles" :
                        movePivotMode ? "Mode: Move Pivot" :
                        rotateGroupMode ? "Mode: Rotate Group" :
                        "Mode: None");
        GUILayout.EndArea();
        Handles.EndGUI();
    }

    private float NormalizeAngle(float degrees)
    {
        degrees %= 360f;
        if (degrees > 180f) degrees -= 360f;
        return degrees;
    }
}
#endif