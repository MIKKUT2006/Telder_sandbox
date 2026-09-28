//#if UNITY_EDITOR

//using System;
//using System.IO;
//using System.Text;

//using UnityEditor;
//using UnityEditor.SceneManagement;
//using UnityEngine;


//public static class TelderV37IndependentArmPivotInstaller
//{
//    [MenuItem(
//        "Tools/Game/Apply V37 Independent Arm Position + Rotation Point"
//    )]
//    public static void Apply()
//    {
//        try
//        {
//            string path =
//                FindScriptPath(
//                    "HeldItemPoseEditorWindow"
//                );


//            if (
//                string.IsNullOrWhiteSpace(
//                    path
//                )
//            )
//            {
//                Debug.LogError(
//                    "V37: HeldItemPoseEditorWindow.cs not found."
//                );

//                return;
//            }


//            string absolute =
//                ToAbsolutePath(
//                    path
//                );


//            string source =
//                File.ReadAllText(
//                    absolute,
//                    Encoding.UTF8
//                );


//            string backup =
//                absolute
//                +
//                ".v37_backup";


//            if (!File.Exists(backup))
//            {
//                File.Copy(
//                    absolute,
//                    backup,
//                    false
//                );
//            }


//            source =
//                RemovePreviousPivotGui(
//                    source
//                );


//            source =
//                AddFields(
//                    source
//                );


//            source =
//                AddReadCall(
//                    source
//                );


//            source =
//                AddInspectorGui(
//                    source
//                );


//            source =
//                AddSaveCall(
//                    source
//                );


//            source =
//                AddPreviewReapply(
//                    source
//                );


//            source =
//                ReplacePreviewMiningOverlay(
//                    source
//                );


//            source =
//                AddHelpers(
//                    source
//                );


//            File.WriteAllText(
//                absolute,
//                source,
//                new UTF8Encoding(
//                    false
//                )
//            );


//            AssetDatabase.Refresh();


//            Debug.Log(
//                "V37: Held Item Pose Editor patched successfully."
//            );


//            EditorUtility.DisplayDialog(
//                "V37 Arm Editor",
//                "Готово.\n\n" +
//                "Теперь отдельно редактируются:\n" +
//                "• положение руки;\n" +
//                "• точка, вокруг которой рука вращается.\n\n" +
//                "Rotation Point больше НЕ двигает саму руку.",
//                "OK"
//            );
//        }
//        catch (Exception exception)
//        {
//            Debug.LogError(
//                "V37 INSTALLER FAILED:\n" +
//                exception
//            );


//            EditorUtility.DisplayDialog(
//                "V37 — ошибка",
//                exception.ToString(),
//                "OK"
//            );
//        }
//    }


//    // =====================================================
//    // REMOVE OLD CONFUSING PIVOT GUI
//    // =====================================================

//    private static string RemovePreviousPivotGui(
//        string source
//    )
//    {
//        int handLabel =
//            source.IndexOf(
//                "\"Точка ладони\"",
//                StringComparison.Ordinal
//            );


//        if (handLabel < 0)
//            return source;


//        string[] oldLabels =
//        {
//            "\"ТОЧКА, ОТНОСИТЕЛЬНО КОТОРОЙ КРУТИТСЯ РУКА\"",
//            "\"Точки вращения рук\"",
//            "\"Точки плеч / Pivot вращения\""
//        };


//        int removeStart =
//            -1;


//        for (
//            int i = 0;
//            i < oldLabels.Length;
//            i++
//        )
//        {
//            int label =
//                source.LastIndexOf(
//                    oldLabels[i],
//                    handLabel,
//                    StringComparison.Ordinal
//                );


//            if (label < 0)
//                continue;


//            int candidate =
//                source.LastIndexOf(
//                    "EditorGUILayout.LabelField(",
//                    label,
//                    StringComparison.Ordinal
//                );


//            if (
//                candidate >= 0
//                &&
//                (
//                    removeStart < 0
//                    ||
//                    candidate <
//                    removeStart
//                )
//            )
//            {
//                removeStart =
//                    candidate;
//            }
//        }


//        if (removeStart < 0)
//            return source;


//        int handLabelFieldStart =
//            source.LastIndexOf(
//                "EditorGUILayout.LabelField(",
//                handLabel,
//                StringComparison.Ordinal
//            );


//        if (
//            handLabelFieldStart <=
//            removeStart
//        )
//        {
//            return source;
//        }


//        return
//            source.Remove(
//                removeStart,
//                handLabelFieldStart
//                -
//                removeStart
//            );
//    }


//    // =====================================================
//    // FIELDS
//    // =====================================================

//    private static string AddFields(
//        string source
//    )
//    {
//        if (
//            source.Contains(
//                "// [TELDER-V37-INDEPENDENT-ARM-RIG]"
//            )
//        )
//        {
//            return source;
//        }


//        int animationSection =
//            source.IndexOf(
//                "// ANIMATION",
//                StringComparison.Ordinal
//            );


//        if (animationSection < 0)
//        {
//            throw new InvalidOperationException(
//                "ANIMATION section not found."
//            );
//        }


//        int lineStart =
//            source.LastIndexOf(
//                '\n',
//                animationSection
//            );


//        string fields =
//@"
//    // [TELDER-V37-INDEPENDENT-ARM-RIG]

//    // Actual arm Transform positions.
//    private Vector2 v37FrontArmPosition;

//    private Vector2 v37BackArmPosition;


//    // Independent points in each arm PARENT local space.
//    // Mining rotates the arm around these coordinates.
//    private Vector2 v37FrontRotationPoint;

//    private Vector2 v37BackRotationPoint;


//";


//        return
//            source.Insert(
//                lineStart + 1,
//                fields
//            );
//    }


//    // =====================================================
//    // PLAYER LOAD
//    // =====================================================

//    private static string AddReadCall(
//        string source
//    )
//    {
//        if (
//            source.Contains(
//                "ReadV37IndependentArmRig();"
//            )
//        )
//        {
//            return source;
//        }


//        int index =
//            source.IndexOf(
//                "ReadMiningOverlaySettings();",
//                StringComparison.Ordinal
//            );


//        if (index < 0)
//        {
//            throw new InvalidOperationException(
//                "ReadMiningOverlaySettings call not found."
//            );
//        }


//        return
//            source.Insert(
//                index,
//@"ReadV37IndependentArmRig();

//        "
//            );
//    }


//    // =====================================================
//    // GUI
//    // =====================================================

//    private static string AddInspectorGui(
//        string source
//    )
//    {
//        if (
//            source.Contains(
//                "\"Положение рук и точки вращения\""
//            )
//        )
//        {
//            return source;
//        }


//        int handText =
//            source.IndexOf(
//                "\"Точка ладони\"",
//                StringComparison.Ordinal
//            );


//        if (handText < 0)
//        {
//            throw new InvalidOperationException(
//                "'Точка ладони' section not found."
//            );
//        }


//        int handField =
//            source.LastIndexOf(
//                "EditorGUILayout.LabelField(",
//                handText,
//                StringComparison.Ordinal
//            );


//        if (handField < 0)
//        {
//            throw new InvalidOperationException(
//                "Hand LabelField start not found."
//            );
//        }


//        string gui =
//@"EditorGUILayout.LabelField(
//            ""Положение рук и точки вращения"",
//            EditorStyles.boldLabel
//        );


//        EditorGUILayout.HelpBox(
//            ""ARM POSITION двигает саму руку. ROTATION POINT двигает только математическую точку, "" +
//            ""вокруг которой выполняется mining swing. Эти параметры полностью независимы."",
//            MessageType.Info
//        );


//        EditorGUILayout.LabelField(
//            ""Передняя рука"",
//            EditorStyles.miniBoldLabel
//        );


//        Vector2 nextFrontArmPosition =
//            EditorGUILayout.Vector2Field(
//                ""Arm Position"",
//                v37FrontArmPosition
//            );


//        Vector2 nextFrontRotationPoint =
//            EditorGUILayout.Vector2Field(
//                ""Rotation Point"",
//                v37FrontRotationPoint
//            );


//        EditorGUILayout.Space(
//            5f
//        );


//        EditorGUILayout.LabelField(
//            ""Задняя рука"",
//            EditorStyles.miniBoldLabel
//        );


//        Vector2 nextBackArmPosition =
//            EditorGUILayout.Vector2Field(
//                ""Arm Position"",
//                v37BackArmPosition
//            );


//        Vector2 nextBackRotationPoint =
//            EditorGUILayout.Vector2Field(
//                ""Rotation Point"",
//                v37BackRotationPoint
//            );


//        bool armPositionChanged =
//            nextFrontArmPosition !=
//            v37FrontArmPosition
//            ||
//            nextBackArmPosition !=
//            v37BackArmPosition;


//        bool rotationPointChanged =
//            nextFrontRotationPoint !=
//            v37FrontRotationPoint
//            ||
//            nextBackRotationPoint !=
//            v37BackRotationPoint;


//        if (armPositionChanged)
//        {
//            v37FrontArmPosition =
//                nextFrontArmPosition;


//            v37BackArmPosition =
//                nextBackArmPosition;


//            UpdateV37PreviewArmPositions();
//        }


//        if (rotationPointChanged)
//        {
//            v37FrontRotationPoint =
//                nextFrontRotationPoint;


//            v37BackRotationPoint =
//                nextBackRotationPoint;


//            Repaint();
//        }


//        EditorGUILayout.BeginHorizontal();


//        if (
//            GUILayout.Button(
//                ""Применить Arm Position""
//            )
//        )
//        {
//            ApplyV37ArmPositionsToScene();
//        }


//        if (
//            GUILayout.Button(
//                ""Применить Rotation Point""
//            )
//        )
//        {
//            ApplyV37RotationPointsToOverlay();
//        }


//        EditorGUILayout.EndHorizontal();


//        if (
//            GUILayout.Button(
//                ""Считать всё из Player""
//            )
//        )
//        {
//            ReadV37IndependentArmRig();

//            UpdateV37PreviewArmPositions();
//        }


//        EditorGUILayout.Space(
//            12f
//        );


//        ";


//        return
//            source.Insert(
//                handField,
//                gui
//            );
//    }


//    // =====================================================
//    // SAVE
//    // =====================================================

//    private static string AddSaveCall(
//        string source
//    )
//    {
//        int method =
//            source.IndexOf(
//                "private void SaveAll()",
//                StringComparison.Ordinal
//            );


//        if (method < 0)
//            return source;


//        int brace =
//            source.IndexOf(
//                '{',
//                method
//            );


//        int end =
//            FindMatchingBrace(
//                source,
//                brace
//            );


//        string block =
//            source.Substring(
//                method,
//                end -
//                method +
//                1
//            );


//        if (
//            block.Contains(
//                "ApplyV37ArmPositionsToScene();"
//            )
//        )
//        {
//            return source;
//        }


//        int applyHand =
//            block.IndexOf(
//                "ApplyHandPoint();",
//                StringComparison.Ordinal
//            );


//        if (applyHand < 0)
//            return source;


//        block =
//            block.Insert(
//                applyHand,
//@"ApplyV37ArmPositionsToScene();

//        ApplyV37RotationPointsToOverlay();

//        "
//            );


//        return
//            source.Remove(
//                method,
//                end -
//                method +
//                1
//            )
//            .Insert(
//                method,
//                block
//            );
//    }


//    // =====================================================
//    // PREVIEW REAPPLY AFTER ANIMATION SAMPLE
//    // =====================================================

//    private static string AddPreviewReapply(
//        string source
//    )
//    {
//        int method =
//            source.IndexOf(
//                "private void SamplePreviewAnimation()",
//                StringComparison.Ordinal
//            );


//        if (method < 0)
//            return source;


//        int brace =
//            source.IndexOf(
//                '{',
//                method
//            );


//        int end =
//            FindMatchingBrace(
//                source,
//                brace
//            );


//        string block =
//            source.Substring(
//                method,
//                end -
//                method +
//                1
//            );


//        if (
//            block.Contains(
//                "UpdateV37PreviewArmPositions();"
//            )
//        )
//        {
//            return source;
//        }


//        int handUpdate =
//            block.IndexOf(
//                "UpdatePreviewHandPoint();",
//                StringComparison.Ordinal
//            );


//        if (handUpdate < 0)
//            return source;


//        block =
//            block.Insert(
//                handUpdate,
//@"UpdateV37PreviewArmPositions();

//        "
//            );


//        return
//            source.Remove(
//                method,
//                end -
//                method +
//                1
//            )
//            .Insert(
//                method,
//                block
//            );
//    }


//    // =====================================================
//    // PREVIEW MINING
//    // =====================================================

//    private static string ReplacePreviewMiningOverlay(
//        string source
//    )
//    {
//        int method =
//            source.IndexOf(
//                "private void ApplyPreviewMiningOverlay()",
//                StringComparison.Ordinal
//            );


//        if (method < 0)
//        {
//            throw new InvalidOperationException(
//                "ApplyPreviewMiningOverlay() not found."
//            );
//        }


//        int brace =
//            source.IndexOf(
//                '{',
//                method
//            );


//        int end =
//            FindMatchingBrace(
//                source,
//                brace
//            );


//        string replacement =
//@"private void ApplyPreviewMiningOverlay()
//    {
//        if (
//            !previewMining
//            ||
//            previewFrontArmPivot ==
//            null
//        )
//        {
//            return;
//        }


//        float phase;


//        if (autoMiningSwing)
//        {
//            phase =
//                Mathf.PingPong(
//                    (float)
//                    EditorApplication.timeSinceStartup
//                    *
//                    miningSwingSpeed,
//                    1f
//                );
//        }
//        else
//        {
//            phase =
//                manualMiningPhase;
//        }


//        phase =
//            phase
//            *
//            phase
//            *
//            (
//                3f
//                -
//                2f
//                *
//                phase
//            );


//        float frontAngle =
//            Mathf.Lerp(
//                frontArmBackAngle,
//                frontArmForwardAngle,
//                phase
//            );


//        float backAngle =
//            Mathf.Lerp(
//                backArmBackAngle,
//                backArmForwardAngle,
//                phase
//            );


//        RotateV37PreviewArmAroundPoint(
//            previewFrontArmPivot,
//            v37FrontRotationPoint,
//            frontAngle
//        );


//        RotateV37PreviewArmAroundPoint(
//            previewBackArmPivot,
//            v37BackRotationPoint,
//            backAngle
//        );
//    }";


//        return
//            source.Remove(
//                method,
//                end -
//                method +
//                1
//            )
//            .Insert(
//                method,
//                replacement
//            );
//    }


//    // =====================================================
//    // HELPERS
//    // =====================================================

//    private static string AddHelpers(
//        string source
//    )
//    {
//        if (
//            source.Contains(
//                "private void ReadV37IndependentArmRig()"
//            )
//        )
//        {
//            return source;
//        }


//        int insert =
//            source.IndexOf(
//                "private void ApplyHandPoint()",
//                StringComparison.Ordinal
//            );


//        if (insert < 0)
//        {
//            throw new InvalidOperationException(
//                "ApplyHandPoint() insertion point not found."
//            );
//        }


//        string helpers =
//@"private void ReadV37IndependentArmRig()
//    {
//        if (sourceFrontArmPivot != null)
//        {
//            v37FrontArmPosition =
//                new Vector2(
//                    sourceFrontArmPivot.localPosition.x,
//                    sourceFrontArmPivot.localPosition.y
//                );
//        }


//        if (sourceBackArmPivot != null)
//        {
//            v37BackArmPosition =
//                new Vector2(
//                    sourceBackArmPivot.localPosition.x,
//                    sourceBackArmPivot.localPosition.y
//                );
//        }


//        if (player == null)
//            return;


//        ArmMiningOverlayController overlay =
//            player.GetComponent<
//                ArmMiningOverlayController
//            >();


//        if (overlay == null)
//            return;


//        SerializedObject serialized =
//            new SerializedObject(
//                overlay
//            );


//        SerializedProperty frontPoint =
//            serialized.FindProperty(
//                ""frontRotationPointLocal""
//            );


//        SerializedProperty backPoint =
//            serialized.FindProperty(
//                ""backRotationPointLocal""
//            );


//        if (frontPoint != null)
//        {
//            v37FrontRotationPoint =
//                frontPoint.vector2Value;
//        }
//        else
//        {
//            v37FrontRotationPoint =
//                v37FrontArmPosition;
//        }


//        if (backPoint != null)
//        {
//            v37BackRotationPoint =
//                backPoint.vector2Value;
//        }
//        else
//        {
//            v37BackRotationPoint =
//                v37BackArmPosition;
//        }
//    }


//    private void UpdateV37PreviewArmPositions()
//    {
//        SetArmPosition(
//            previewFrontArmPivot,
//            v37FrontArmPosition
//        );


//        SetArmPosition(
//            previewBackArmPivot,
//            v37BackArmPosition
//        );


//        Repaint();
//    }


//    private static void SetArmPosition(
//        Transform arm,
//        Vector2 position
//    )
//    {
//        if (arm == null)
//            return;


//        Vector3 local =
//            arm.localPosition;


//        local.x =
//            position.x;


//        local.y =
//            position.y;


//        arm.localPosition =
//            local;
//    }


//    private void ApplyV37ArmPositionsToScene()
//    {
//        ApplyArmPositionToScene(
//            sourceFrontArmPivot,
//            v37FrontArmPosition,
//            ""Front Arm Position""
//        );


//        ApplyArmPositionToScene(
//            sourceBackArmPivot,
//            v37BackArmPosition,
//            ""Back Arm Position""
//        );


//        UpdateV37PreviewArmPositions();
//    }


//    private static void ApplyArmPositionToScene(
//        Transform arm,
//        Vector2 position,
//        string undoName
//    )
//    {
//        if (arm == null)
//            return;


//        Undo.RecordObject(
//            arm,
//            undoName
//        );


//        Vector3 local =
//            arm.localPosition;


//        local.x =
//            position.x;


//        local.y =
//            position.y;


//        arm.localPosition =
//            local;


//        EditorUtility.SetDirty(
//            arm
//        );


//        PrefabUtility
//            .RecordPrefabInstancePropertyModifications(
//                arm
//            );


//        if (
//            arm.gameObject.scene.IsValid()
//        )
//        {
//            EditorSceneManager.MarkSceneDirty(
//                arm.gameObject.scene
//            );
//        }
//    }


//    private void ApplyV37RotationPointsToOverlay()
//    {
//        if (player == null)
//            return;


//        ArmMiningOverlayController overlay =
//            player.GetComponent<
//                ArmMiningOverlayController
//            >();


//        if (overlay == null)
//        {
//            EditorUtility.DisplayDialog(
//                ""Held Item Pose"",
//                ""ArmMiningOverlayController не найден на Player."",
//                ""OK""
//            );

//            return;
//        }


//        Undo.RecordObject(
//            overlay,
//            ""Arm Rotation Points""
//        );


//        SerializedObject serialized =
//            new SerializedObject(
//                overlay
//            );


//        SerializedProperty frontPoint =
//            serialized.FindProperty(
//                ""frontRotationPointLocal""
//            );


//        SerializedProperty backPoint =
//            serialized.FindProperty(
//                ""backRotationPointLocal""
//            );


//        SerializedProperty initialized =
//            serialized.FindProperty(
//                ""rotationPointsInitialized""
//            );


//        if (
//            frontPoint == null
//            ||
//            backPoint == null
//        )
//        {
//            EditorUtility.DisplayDialog(
//                ""Held Item Pose"",
//                ""Установи новый ArmMiningOverlayController.cs из V37: "" +
//                ""в нём должны быть frontRotationPointLocal/backRotationPointLocal."",
//                ""OK""
//            );

//            return;
//        }


//        frontPoint.vector2Value =
//            v37FrontRotationPoint;


//        backPoint.vector2Value =
//            v37BackRotationPoint;


//        if (initialized != null)
//        {
//            initialized.boolValue =
//                true;
//        }


//        serialized.ApplyModifiedProperties();


//        EditorUtility.SetDirty(
//            overlay
//        );


//        PrefabUtility
//            .RecordPrefabInstancePropertyModifications(
//                overlay
//            );


//        if (
//            overlay.gameObject.scene.IsValid()
//        )
//        {
//            EditorSceneManager.MarkSceneDirty(
//                overlay.gameObject.scene
//            );
//        }
//    }


//    private static void RotateV37PreviewArmAroundPoint(
//        Transform arm,
//        Vector2 rotationPoint,
//        float angle
//    )
//    {
//        if (arm == null)
//            return;


//        Vector3 basePosition =
//            arm.localPosition;


//        Vector3 point =
//            new Vector3(
//                rotationPoint.x,
//                rotationPoint.y,
//                basePosition.z
//            );


//        Quaternion delta =
//            Quaternion.Euler(
//                0f,
//                0f,
//                angle
//            );


//        arm.localPosition =
//            point
//            +
//            delta
//            *
//            (
//                basePosition
//                -
//                point
//            );


//        arm.localRotation =
//            arm.localRotation
//            *
//            delta;
//    }


//    ";


//        return
//            source.Insert(
//                insert,
//                helpers
//            );
//    }


//    // =====================================================
//    // FILE / PARSER
//    // =====================================================

//    private static string FindScriptPath(
//        string className
//    )
//    {
//        string[] guids =
//            AssetDatabase.FindAssets(
//                className
//                +
//                " t:MonoScript"
//            );


//        for (
//            int i = 0;
//            i < guids.Length;
//            i++
//        )
//        {
//            string path =
//                AssetDatabase.GUIDToAssetPath(
//                    guids[i]
//                );


//            if (
//                string.Equals(
//                    Path.GetFileNameWithoutExtension(
//                        path
//                    ),
//                    className,
//                    StringComparison.OrdinalIgnoreCase
//                )
//            )
//            {
//                return path;
//            }
//        }


//        return null;
//    }


//    private static string ToAbsolutePath(
//        string assetPath
//    )
//    {
//        string projectRoot =
//            Directory
//                .GetParent(
//                    Application.dataPath
//                )
//                .FullName;


//        return
//            Path.Combine(
//                projectRoot,
//                assetPath
//            );
//    }


//    private static int FindMatchingBrace(
//        string source,
//        int openingBrace
//    )
//    {
//        int depth =
//            0;


//        bool inString =
//            false;


//        bool inChar =
//            false;


//        bool escape =
//            false;


//        bool lineComment =
//            false;


//        bool blockComment =
//            false;


//        for (
//            int i = openingBrace;
//            i < source.Length;
//            i++
//        )
//        {
//            char c =
//                source[i];


//            char next =
//                i + 1 <
//                source.Length
//                    ? source[i + 1]
//                    : '\0';


//            if (lineComment)
//            {
//                if (c == '\n')
//                    lineComment = false;

//                continue;
//            }


//            if (blockComment)
//            {
//                if (
//                    c == '*'
//                    &&
//                    next == '/'
//                )
//                {
//                    blockComment = false;
//                    i++;
//                }

//                continue;
//            }


//            if (inString)
//            {
//                if (escape)
//                {
//                    escape = false;
//                    continue;
//                }


//                if (c == '\\')
//                {
//                    escape = true;
//                    continue;
//                }


//                if (c == '"')
//                    inString = false;

//                continue;
//            }


//            if (inChar)
//            {
//                if (escape)
//                {
//                    escape = false;
//                    continue;
//                }


//                if (c == '\\')
//                {
//                    escape = true;
//                    continue;
//                }


//                if (c == '\'')
//                    inChar = false;

//                continue;
//            }


//            if (
//                c == '/'
//                &&
//                next == '/'
//            )
//            {
//                lineComment = true;
//                i++;
//                continue;
//            }


//            if (
//                c == '/'
//                &&
//                next == '*'
//            )
//            {
//                blockComment = true;
//                i++;
//                continue;
//            }


//            if (c == '"')
//            {
//                inString = true;
//                continue;
//            }


//            if (c == '\'')
//            {
//                inChar = true;
//                continue;
//            }


//            if (c == '{')
//            {
//                depth++;
//            }
//            else if (c == '}')
//            {
//                depth--;


//                if (depth == 0)
//                    return i;
//            }
//        }


//        throw new InvalidOperationException(
//            ""Matching brace not found.""
//        );
//    }
//}

//#endif
