#if UNITY_EDITOR

using System;
using System.IO;
using System.Text;

using UnityEditor;
using UnityEngine;


public static class TelderHeldItemPoseElbowFixInstaller
{
    private const string MenuPath =
        "Tools/Game/Fix Held Item Pose Editor + Elbow Pivot";


    [MenuItem(MenuPath)]
    public static void Apply()
    {
        try
        {
            string editorPath =
                FindScriptPath(
                    "HeldItemPoseEditorWindow"
                );


            string overlayPath =
                FindScriptPath(
                    "ArmMiningOverlayController"
                );


            if (
                string.IsNullOrWhiteSpace(
                    editorPath
                )
            )
            {
                throw new InvalidOperationException(
                    "HeldItemPoseEditorWindow.cs not found."
                );
            }


            if (
                string.IsNullOrWhiteSpace(
                    overlayPath
                )
            )
            {
                throw new InvalidOperationException(
                    "ArmMiningOverlayController.cs not found."
                );
            }


            BackupOnce(
                editorPath,
                ".elbow_ui_backup"
            );


            BackupOnce(
                overlayPath,
                ".elbow_runtime_backup"
            );


            ReplaceArmOverlay(
                overlayPath
            );


            PatchEditor(
                editorPath
            );


            TelderHeldPoseHandleInjector.Apply();


            AssetDatabase.Refresh();


            Debug.Log(
                "TELDER: Held Item Pose UI + real elbow pivot installed."
            );


            EditorUtility.DisplayDialog(
                "Held Item Pose",
                "Готово.\n\n" +
                "Теперь мах вычисляется вокруг локальной точки локтя, " +
                "а не просто вокруг центра Transform.\n\n" +
                "В preview точку можно перетаскивать мышью.",
                "OK"
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "TELDER HELD POSE FIX FAILED:\n" +
                exception
            );


            EditorUtility.DisplayDialog(
                "Held Item Pose — ошибка",
                exception.ToString(),
                "OK"
            );
        }
    }


    // =====================================================
    // ARM OVERLAY REPLACEMENT
    // =====================================================

    private static void ReplaceArmOverlay(
        string assetPath
    )
    {
        string replacementPath =
            Path.Combine(
                Application.dataPath,
                "GameData",
                "Editor",
                "TelderHeldPoseFixData",
                "ArmMiningOverlayController.txt"
            );


        if (
            !File.Exists(
                replacementPath
            )
        )
        {
            throw new FileNotFoundException(
                "Arm replacement source not found.",
                replacementPath
            );
        }


        File.WriteAllText(
            ToAbsolutePath(
                assetPath
            ),
            File.ReadAllText(
                replacementPath,
                Encoding.UTF8
            ),
            new UTF8Encoding(
                false
            )
        );
    }


    // =====================================================
    // EDITOR PATCH
    // =====================================================

    private static void PatchEditor(
        string assetPath
    )
    {
        string absolute =
            ToAbsolutePath(
                assetPath
            );


        string source =
            File.ReadAllText(
                absolute,
                Encoding.UTF8
            );


        // Remove older experimental pivot sections by replacing the complete
        // DrawInspector() method. Old unused fields may remain harmlessly.
        EnsureFields(
            ref source
        );


        source =
            ReplaceMethod(
                source,
                "private void ReadMiningOverlaySettings()",
                ReadMiningSettingsMethod
            );


        source =
            ReplaceMethod(
                source,
                "private void OnGUI()",
                OnGuiMethod
            );


        source =
            ReplaceMethod(
                source,
                "private void DrawInspector()",
                DrawInspectorMethod
            );


        source =
            ReplaceMethod(
                source,
                "private void SamplePreviewAnimation()",
                SamplePreviewAnimationMethod
            );


        source =
            ReplaceMethod(
                source,
                "private void ApplyPreviewMiningOverlay()",
                ApplyPreviewMiningMethod
            );


        InjectPreviewDefaultCapture(
            ref source
        );


        InjectSaveCall(
            ref source
        );


        if (
            !source.Contains(
                "private void DrawRotationPointControls()"
            )
        )
        {
            int insert =
                source.IndexOf(
                    "private void DrawAnimationControls()",
                    StringComparison.Ordinal
                );


            if (insert < 0)
            {
                throw new InvalidOperationException(
                    "DrawAnimationControls insertion point not found."
                );
            }


            source =
                source.Insert(
                    insert,
                    ExtraEditorMethods +
                    "\n\n    "
                );
        }


        File.WriteAllText(
            absolute,
            source,
            new UTF8Encoding(
                false
            )
        );
    }


    private static void EnsureFields(
        ref string source
    )
    {
        if (
            source.Contains(
                "// [TELDER-ELBOW-PIVOT-UI-V1]"
            )
        )
        {
            return;
        }


        int anchor =
            source.IndexOf(
                "// =====================================================\n    // ANIMATION",
                StringComparison.Ordinal
            );


        if (anchor < 0)
        {
            throw new InvalidOperationException(
                "ANIMATION fields anchor not found."
            );
        }


        string fields =
@"// [TELDER-ELBOW-PIVOT-UI-V1]

    private Vector2 frontRotationPivotLocal =
        Vector2.zero;

    private Vector2 backRotationPivotLocal =
        Vector2.zero;


    private bool foldRig =
        false;

    private bool foldAnimation =
        false;

    private bool foldArm =
        true;

    private bool foldItem =
        true;

    private bool foldPreview =
        false;


    private bool draggingFrontRotationPoint;

    private bool draggingBackRotationPoint;


    private Vector3 previewFrontDefaultLocalPosition;

    private Quaternion previewFrontDefaultLocalRotation;

    private Vector3 previewBackDefaultLocalPosition;

    private Quaternion previewBackDefaultLocalRotation;


    ";


        source =
            source.Insert(
                anchor,
                fields
            );
    }


    private static void InjectPreviewDefaultCapture(
        ref string source
    )
    {
        if (
            source.Contains(
                "CapturePreviewArmDefaults();"
            )
        )
        {
            return;
        }


        int methodStart =
            source.IndexOf(
                "private void RebuildPreview()",
                StringComparison.Ordinal
            );


        if (methodStart < 0)
        {
            throw new InvalidOperationException(
                "RebuildPreview() not found."
            );
        }


        int braceStart =
            source.IndexOf(
                '{',
                methodStart
            );


        int braceEnd =
            FindMatchingBrace(
                source,
                braceStart
            );


        string method =
            source.Substring(
                methodStart,
                braceEnd -
                methodStart +
                1
            );


        int createItem =
            method.LastIndexOf(
                "CreatePreviewHeldItem();",
                StringComparison.Ordinal
            );


        if (createItem < 0)
        {
            throw new InvalidOperationException(
                "CreatePreviewHeldItem() call not found in RebuildPreview."
            );
        }


        method =
            method.Insert(
                createItem,
@"CapturePreviewArmDefaults();


        "
            );


        source =
            source.Remove(
                methodStart,
                braceEnd -
                methodStart +
                1
            )
            .Insert(
                methodStart,
                method
            );
    }


    private static void InjectSaveCall(
        ref string source
    )
    {
        int methodStart =
            source.IndexOf(
                "private void SaveAll()",
                StringComparison.Ordinal
            );


        if (methodStart < 0)
            return;


        int braceStart =
            source.IndexOf(
                '{',
                methodStart
            );


        int braceEnd =
            FindMatchingBrace(
                source,
                braceStart
            );


        string method =
            source.Substring(
                methodStart,
                braceEnd -
                methodStart +
                1
            );


        if (
            method.Contains(
                "ApplyRotationPointSettings();"
            )
        )
        {
            return;
        }


        int applyHand =
            method.IndexOf(
                "ApplyHandPoint();",
                StringComparison.Ordinal
            );


        if (applyHand >= 0)
        {
            method =
                method.Insert(
                    applyHand,
@"ApplyRotationPointSettings();

        "
                );
        }
        else
        {
            method =
                method.Insert(
                    1,
@"


        ApplyRotationPointSettings();
"
                );
        }


        source =
            source.Remove(
                methodStart,
                braceEnd -
                methodStart +
                1
            )
            .Insert(
                methodStart,
                method
            );
    }


    // =====================================================
    // REPLACEMENT METHODS
    // =====================================================

    private const string ReadMiningSettingsMethod = @"
    private void ReadMiningOverlaySettings()
    {
        if (player == null)
            return;


        ArmMiningOverlayController overlay =
            player.GetComponent<
                ArmMiningOverlayController
            >();


        if (overlay == null)
            return;


        SerializedObject serialized =
            new SerializedObject(
                overlay
            );


        ReadFloat(
            serialized,
            ""swingSpeed"",
            ref miningSwingSpeed
        );


        ReadFloat(
            serialized,
            ""frontArmBackAngle"",
            ref frontArmBackAngle
        );


        ReadFloat(
            serialized,
            ""frontArmForwardAngle"",
            ref frontArmForwardAngle
        );


        ReadFloat(
            serialized,
            ""backArmBackAngle"",
            ref backArmBackAngle
        );


        ReadFloat(
            serialized,
            ""backArmForwardAngle"",
            ref backArmForwardAngle
        );


        SerializedProperty frontPivot =
            serialized.FindProperty(
                ""frontRotationPivotLocal""
            );


        if (frontPivot != null)
        {
            frontRotationPivotLocal =
                frontPivot.vector2Value;
        }


        SerializedProperty backPivot =
            serialized.FindProperty(
                ""backRotationPivotLocal""
            );


        if (backPivot != null)
        {
            backRotationPivotLocal =
                backPivot.vector2Value;
        }
    }";


    private const string OnGuiMethod = @"
    private void OnGUI()
    {
        DrawToolbar();


        EditorGUILayout.Space(
            4f
        );


        float availableWidth =
            Mathf.Max(
                680f,
                position.width -
                8f
            );


        float availableHeight =
            Mathf.Max(
                300f,
                position.height -
                58f
            );


        Rect full =
            GUILayoutUtility.GetRect(
                availableWidth,
                availableHeight,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true)
            );


        full.width =
            Mathf.Max(
                1f,
                full.width
            );


        full.height =
            Mathf.Max(
                1f,
                full.height
            );


        // A readable inspector instead of the old narrow technical column.
        float inspectorWidth =
            Mathf.Clamp(
                full.width *
                0.36f,
                400f,
                455f
            );


        Rect previewRect =
            new Rect(
                full.x,
                full.y,
                Mathf.Max(
                    260f,
                    full.width -
                    inspectorWidth -
                    8f
                ),
                full.height
            );


        Rect inspectorRect =
            new Rect(
                previewRect.xMax +
                8f,
                full.y,
                inspectorWidth,
                full.height
            );


        DrawAnimatedPreview(
            previewRect
        );


        GUILayout.BeginArea(
            inspectorRect
        );


        scroll =
            EditorGUILayout.BeginScrollView(
                scroll
            );


        DrawInspector();


        EditorGUILayout.EndScrollView();


        GUILayout.EndArea();
    }";


    private const string DrawInspectorMethod = @"
    private void DrawInspector()
    {
        GUILayout.Space(
            4f
        );


        EditorGUILayout.LabelField(
            ""Held Item Pose"",
            EditorStyles.largeLabel
        );


        EditorGUILayout.LabelField(
            string.IsNullOrWhiteSpace(
                itemId
            )
                ? ""Предмет не выбран""
                : itemId,
            EditorStyles.miniLabel
        );


        GUILayout.Space(
            6f
        );


        foldRig =
            DrawSectionHeader(
                ""Риг"",
                foldRig
            );


        if (foldRig)
        {
            EditorGUILayout.BeginVertical(
                EditorStyles.helpBox
            );


            using (
                new EditorGUI.DisabledScope(
                    true
                )
            )
            {
                EditorGUILayout.ObjectField(
                    ""Animator"",
                    sourceAnimator,
                    typeof(Animator),
                    true
                );


                EditorGUILayout.ObjectField(
                    ""Front Arm"",
                    sourceFrontArmPivot,
                    typeof(Transform),
                    true
                );


                EditorGUILayout.ObjectField(
                    ""Back Arm"",
                    sourceBackArmPivot,
                    typeof(Transform),
                    true
                );


                EditorGUILayout.ObjectField(
                    ""Hand Point"",
                    sourceHandPoint,
                    typeof(Transform),
                    true
                );
            }


            EditorGUILayout.EndVertical();
        }


        foldAnimation =
            DrawSectionHeader(
                ""Анимация"",
                foldAnimation
            );


        if (foldAnimation)
        {
            EditorGUILayout.BeginVertical(
                EditorStyles.helpBox
            );


            DrawAnimationControls();


            EditorGUILayout.EndVertical();
        }


        foldArm =
            DrawSectionHeader(
                ""Рука / мах"",
                foldArm
            );


        if (foldArm)
        {
            EditorGUILayout.BeginVertical(
                EditorStyles.helpBox
            );


            DrawRotationPointControls();


            EditorGUILayout.EndVertical();
        }


        foldItem =
            DrawSectionHeader(
                ""Предмет в руке"",
                foldItem
            );


        if (foldItem)
        {
            EditorGUILayout.BeginVertical(
                EditorStyles.helpBox
            );


            EditorGUILayout.LabelField(
                ""Точка ладони"",
                EditorStyles.boldLabel
            );


            Vector2 nextHand =
                EditorGUILayout.Vector2Field(
                    ""Hand Point"",
                    handLocalPosition
                );


            if (
                nextHand !=
                handLocalPosition
            )
            {
                handLocalPosition =
                    nextHand;


                UpdatePreviewHandPoint();
            }


            if (
                GUILayout.Button(
                    ""Применить Hand Point""
                )
            )
            {
                ApplyHandPoint();
            }


            GUILayout.Space(
                8f
            );


            EditorGUILayout.LabelField(
                ""Положение предмета"",
                EditorStyles.boldLabel
            );


            EditorGUI.BeginChangeCheck();


            heldOffsetX =
                EditorGUILayout.Slider(
                    ""Offset X"",
                    heldOffsetX,
                    -1.5f,
                    1.5f
                );


            heldOffsetY =
                EditorGUILayout.Slider(
                    ""Offset Y"",
                    heldOffsetY,
                    -1.5f,
                    1.5f
                );


            heldScale =
                EditorGUILayout.Slider(
                    ""Scale"",
                    heldScale,
                    0.05f,
                    4f
                );


            heldRotation =
                EditorGUILayout.Slider(
                    ""Rotation"",
                    heldRotation,
                    -180f,
                    180f
                );


            if (
                EditorGUI.EndChangeCheck()
            )
            {
                UpdatePreviewHeldItemPose();
            }


            if (
                !string.IsNullOrWhiteSpace(
                    itemJsonPath
                )
            )
            {
                EditorGUILayout.LabelField(
                    Path.GetFileName(
                        itemJsonPath
                    ),
                    EditorStyles.miniLabel
                );
            }


            EditorGUILayout.EndVertical();
        }


        foldPreview =
            DrawSectionHeader(
                ""Preview"",
                foldPreview
            );


        if (foldPreview)
        {
            EditorGUILayout.BeginVertical(
                EditorStyles.helpBox
            );


            previewZoom =
                EditorGUILayout.Slider(
                    ""Zoom"",
                    previewZoom,
                    0.65f,
                    2.5f
                );


            mirrorPreview =
                EditorGUILayout.Toggle(
                    ""Mirror X"",
                    mirrorPreview
                );


            if (
                GUILayout.Button(
                    ""Сбросить камеру""
                )
            )
            {
                previewPan =
                    Vector2.zero;


                previewZoom =
                    1.15f;


                mirrorPreview =
                    false;
            }


            EditorGUILayout.EndVertical();
        }


        GUILayout.Space(
            10f
        );


        if (
            GUILayout.Button(
                ""СОХРАНИТЬ"",
                GUILayout.Height(
                    36f
                )
            )
        )
        {
            SaveAll();
        }


        GUILayout.Space(
            4f
        );
    }";


    private const string SamplePreviewAnimationMethod = @"
    private void SamplePreviewAnimation()
    {
        if (previewPlayer == null)
            return;


        RestorePreviewArmDefaults();


        AnimationClip clip =
            GetSelectedClip();


        if (clip != null)
        {
            GameObject sampleRoot =
                previewAnimator != null
                    ? previewAnimator.gameObject
                    : previewPlayer;


            float time =
                clip.length > 0f
                    ? Mathf.Clamp(
                        animationTime,
                        0f,
                        clip.length
                    )
                    : 0f;


            clip.SampleAnimation(
                sampleRoot,
                time
            );
        }


        UpdatePreviewHandPoint();

        UpdatePreviewHeldItemPose();


        Vector3 playerScale =
            previewPlayer.transform.localScale;


        playerScale.x =
            mirrorPreview
                ? -Mathf.Abs(
                    playerScale.x
                )
                : Mathf.Abs(
                    playerScale.x
                );


        previewPlayer.transform.localScale =
            playerScale;


        ApplyPreviewMiningOverlay();
    }";


    private const string ApplyPreviewMiningMethod = @"
    private void ApplyPreviewMiningOverlay()
    {
        if (
            !previewMining
            ||
            previewFrontArmPivot ==
            null
        )
        {
            return;
        }


        float phase =
            autoMiningSwing
                ? Mathf.Repeat(
                    (float)
                    EditorApplication.timeSinceStartup *
                    Mathf.Max(
                        0.01f,
                        miningSwingSpeed
                    )
                    /
                    (
                        Mathf.PI *
                        2f
                    ),
                    1f
                )
                : manualMiningPhase;


        float motion =
            0.5f
            -
            0.5f *
            Mathf.Cos(
                phase *
                Mathf.PI *
                2f
            );


        float frontAngle =
            Mathf.Lerp(
                frontArmBackAngle,
                frontArmForwardAngle,
                motion
            );


        float backAngle =
            Mathf.Lerp(
                backArmBackAngle,
                backArmForwardAngle,
                motion
            );


        ArmMiningOverlayController
            .RotateCurrentPoseAroundLocalPoint(
                previewFrontArmPivot,
                frontRotationPivotLocal,
                frontAngle
            );


        if (previewBackArmPivot != null)
        {
            ArmMiningOverlayController
                .RotateCurrentPoseAroundLocalPoint(
                    previewBackArmPivot,
                    backRotationPivotLocal,
                    backAngle
                );
        }
    }";


    // =====================================================
    // EXTRA METHODS
    // =====================================================

    private const string ExtraEditorMethods = @"
    private bool DrawSectionHeader(
        string title,
        bool state
    )
    {
        Rect rect =
            EditorGUILayout.GetControlRect(
                false,
                24f
            );


        EditorGUI.DrawRect(
            rect,
            new Color(
                0.16f,
                0.17f,
                0.19f,
                1f
            )
        );


        Rect foldRect =
            new Rect(
                rect.x +
                6f,
                rect.y +
                2f,
                rect.width -
                12f,
                rect.height -
                4f
            );


        return
            EditorGUI.Foldout(
                foldRect,
                state,
                title,
                true
            );
    }


    private void DrawRotationPointControls()
    {
        EditorGUILayout.LabelField(
            ""Точка вращения = локоть"",
            EditorStyles.boldLabel
        );


        EditorGUILayout.HelpBox(
            ""Жёлтый квадрат в preview — локоть передней руки. "" +
            ""Перетащи его мышью прямо на нужную точку руки. "" +
            ""Во время маха эта точка остаётся неподвижной."",
            MessageType.Info
        );


        EditorGUI.BeginChangeCheck();


        Vector2 nextFront =
            EditorGUILayout.Vector2Field(
                ""Передняя рука"",
                frontRotationPivotLocal
            );


        Vector2 nextBack =
            EditorGUILayout.Vector2Field(
                ""Задняя рука"",
                backRotationPivotLocal
            );


        if (
            EditorGUI.EndChangeCheck()
        )
        {
            frontRotationPivotLocal =
                nextFront;


            backRotationPivotLocal =
                nextBack;


            Repaint();
        }


        EditorGUILayout.BeginHorizontal();


        if (
            GUILayout.Button(
                ""Применить""
            )
        )
        {
            ApplyRotationPointSettings();
        }


        if (
            GUILayout.Button(
                ""Считать""
            )
        )
        {
            ReadMiningOverlaySettings();
        }


        if (
            GUILayout.Button(
                ""В центр""
            )
        )
        {
            frontRotationPivotLocal =
                Vector2.zero;


            backRotationPivotLocal =
                Vector2.zero;
        }


        EditorGUILayout.EndHorizontal();


        GUILayout.Space(
            8f
        );


        previewMining =
            EditorGUILayout.Toggle(
                ""Показать мах"",
                previewMining
            );


        if (previewMining)
        {
            autoMiningSwing =
                EditorGUILayout.Toggle(
                    ""Автоматически"",
                    autoMiningSwing
                );


            if (!autoMiningSwing)
            {
                manualMiningPhase =
                    EditorGUILayout.Slider(
                        ""Фаза"",
                        manualMiningPhase,
                        0f,
                        1f
                    );
            }


            miningSwingSpeed =
                EditorGUILayout.Slider(
                    ""Скорость"",
                    miningSwingSpeed,
                    0.5f,
                    12f
                );


            frontArmBackAngle =
                EditorGUILayout.Slider(
                    ""Назад"",
                    frontArmBackAngle,
                    -180f,
                    180f
                );


            frontArmForwardAngle =
                EditorGUILayout.Slider(
                    ""Вперёд"",
                    frontArmForwardAngle,
                    -180f,
                    180f
                );
        }
    }


    private static void ReadFloat(
        SerializedObject serialized,
        string propertyName,
        ref float value
    )
    {
        SerializedProperty property =
            serialized.FindProperty(
                propertyName
            );


        if (property != null)
        {
            value =
                property.floatValue;
        }
    }


    private void ApplyRotationPointSettings()
    {
        if (player == null)
            return;


        ArmMiningOverlayController overlay =
            player.GetComponent<
                ArmMiningOverlayController
            >();


        if (overlay == null)
            return;


        Undo.RecordObject(
            overlay,
            ""Change Arm Rotation Pivot""
        );


        SerializedObject serialized =
            new SerializedObject(
                overlay
            );


        SerializedProperty front =
            serialized.FindProperty(
                ""frontRotationPivotLocal""
            );


        SerializedProperty back =
            serialized.FindProperty(
                ""backRotationPivotLocal""
            );


        if (front != null)
        {
            front.vector2Value =
                frontRotationPivotLocal;
        }


        if (back != null)
        {
            back.vector2Value =
                backRotationPivotLocal;
        }


        SerializedProperty speed =
            serialized.FindProperty(
                ""swingSpeed""
            );


        if (speed != null)
        {
            speed.floatValue =
                miningSwingSpeed;
        }


        SerializedProperty backAngle =
            serialized.FindProperty(
                ""frontArmBackAngle""
            );


        if (backAngle != null)
        {
            backAngle.floatValue =
                frontArmBackAngle;
        }


        SerializedProperty forwardAngle =
            serialized.FindProperty(
                ""frontArmForwardAngle""
            );


        if (forwardAngle != null)
        {
            forwardAngle.floatValue =
                frontArmForwardAngle;
        }


        serialized.ApplyModifiedProperties();


        EditorUtility.SetDirty(
            overlay
        );
    }


    private void CapturePreviewArmDefaults()
    {
        if (previewFrontArmPivot != null)
        {
            previewFrontDefaultLocalPosition =
                previewFrontArmPivot.localPosition;


            previewFrontDefaultLocalRotation =
                previewFrontArmPivot.localRotation;
        }


        if (previewBackArmPivot != null)
        {
            previewBackDefaultLocalPosition =
                previewBackArmPivot.localPosition;


            previewBackDefaultLocalRotation =
                previewBackArmPivot.localRotation;
        }
    }


    private void RestorePreviewArmDefaults()
    {
        if (previewFrontArmPivot != null)
        {
            previewFrontArmPivot.localPosition =
                previewFrontDefaultLocalPosition;


            previewFrontArmPivot.localRotation =
                previewFrontDefaultLocalRotation;
        }


        if (previewBackArmPivot != null)
        {
            previewBackArmPivot.localPosition =
                previewBackDefaultLocalPosition;


            previewBackArmPivot.localRotation =
                previewBackDefaultLocalRotation;
        }
    }


    private void DrawRotationPivotHandles(
        Rect previewRect,
        Vector2 worldCenter,
        Vector2 screenCenter,
        float pixelsPerWorldUnit
    )
    {
        DrawOneRotationPivotHandle(
            previewFrontArmPivot,
            ref frontRotationPivotLocal,
            ref draggingFrontRotationPoint,
            previewRect,
            worldCenter,
            screenCenter,
            pixelsPerWorldUnit,
            new Color(
                1f,
                0.76f,
                0.15f,
                1f
            )
        );


        DrawOneRotationPivotHandle(
            previewBackArmPivot,
            ref backRotationPivotLocal,
            ref draggingBackRotationPoint,
            previewRect,
            worldCenter,
            screenCenter,
            pixelsPerWorldUnit,
            new Color(
                0.35f,
                0.75f,
                1f,
                1f
            )
        );
    }


    private void DrawOneRotationPivotHandle(
        Transform arm,
        ref Vector2 localPoint,
        ref bool dragging,
        Rect previewRect,
        Vector2 worldCenter,
        Vector2 screenCenter,
        float pixelsPerWorldUnit,
        Color color
    )
    {
        if (
            arm == null
            ||
            pixelsPerWorldUnit <=
            0.0001f
        )
        {
            return;
        }


        Vector3 world =
            arm.TransformPoint(
                new Vector3(
                    localPoint.x,
                    localPoint.y,
                    0f
                )
            );


        Vector2 screen =
            new Vector2(
                screenCenter.x
                +
                (
                    world.x -
                    worldCenter.x
                )
                *
                pixelsPerWorldUnit,

                screenCenter.y
                -
                (
                    world.y -
                    worldCenter.y
                )
                *
                pixelsPerWorldUnit
            );


        Rect handle =
            new Rect(
                screen.x -
                6f,
                screen.y -
                6f,
                12f,
                12f
            );


        Event evt =
            Event.current;


        if (
            evt.type ==
            EventType.MouseDown
            &&
            evt.button ==
            0
            &&
            handle.Contains(
                evt.mousePosition
            )
        )
        {
            dragging =
                true;


            evt.Use();
        }


        if (
            dragging
            &&
            evt.type ==
            EventType.MouseDrag
            &&
            evt.button ==
            0
        )
        {
            Vector2 mouse =
                evt.mousePosition;


            Vector3 targetWorld =
                new Vector3(
                    worldCenter.x
                    +
                    (
                        mouse.x -
                        screenCenter.x
                    )
                    /
                    pixelsPerWorldUnit,

                    worldCenter.y
                    -
                    (
                        mouse.y -
                        screenCenter.y
                    )
                    /
                    pixelsPerWorldUnit,

                    world.z
                );


            Vector3 local =
                arm.InverseTransformPoint(
                    targetWorld
                );


            localPoint =
                new Vector2(
                    local.x,
                    local.y
                );


            Repaint();


            evt.Use();
        }


        if (
            dragging
            &&
            evt.type ==
            EventType.MouseUp
            &&
            evt.button ==
            0
        )
        {
            dragging =
                false;


            evt.Use();
        }


        EditorGUI.DrawRect(
            handle,
            color
        );


        EditorGUI.DrawRect(
            new Rect(
                handle.x +
                3f,
                handle.y +
                3f,
                6f,
                6f
            ),
            new Color(
                0.08f,
                0.08f,
                0.08f,
                1f
            )
        );
    }";


    // =====================================================
    // METHOD REPLACER
    // =====================================================

    private static string ReplaceMethod(
        string source,
        string signature,
        string replacement
    )
    {
        int start =
            source.IndexOf(
                signature,
                StringComparison.Ordinal
            );


        if (start < 0)
        {
            throw new InvalidOperationException(
                "Method not found: " +
                signature
            );
        }


        int brace =
            source.IndexOf(
                '{',
                start
            );


        int end =
            FindMatchingBrace(
                source,
                brace
            );


        return
            source.Remove(
                start,
                end -
                start +
                1
            )
            .Insert(
                start,
                replacement
            );
    }


    private static int FindMatchingBrace(
        string source,
        int openingBrace
    )
    {
        int depth =
            0;


        bool inString =
            false;


        bool inChar =
            false;


        bool escape =
            false;


        bool lineComment =
            false;


        bool blockComment =
            false;


        for (
            int i = openingBrace;
            i < source.Length;
            i++
        )
        {
            char c =
                source[i];


            char next =
                i + 1 <
                source.Length
                    ? source[i + 1]
                    : '\0';


            if (lineComment)
            {
                if (c == '\n')
                    lineComment = false;


                continue;
            }


            if (blockComment)
            {
                if (
                    c == '*'
                    &&
                    next == '/'
                )
                {
                    blockComment = false;

                    i++;
                }


                continue;
            }


            if (inString)
            {
                if (escape)
                {
                    escape = false;

                    continue;
                }


                if (c == '\\')
                {
                    escape = true;

                    continue;
                }


                if (c == '"')
                    inString = false;


                continue;
            }


            if (inChar)
            {
                if (escape)
                {
                    escape = false;

                    continue;
                }


                if (c == '\\')
                {
                    escape = true;

                    continue;
                }


                if (c == '\'')
                    inChar = false;


                continue;
            }


            if (
                c == '/'
                &&
                next == '/'
            )
            {
                lineComment = true;

                i++;

                continue;
            }


            if (
                c == '/'
                &&
                next == '*'
            )
            {
                blockComment = true;

                i++;

                continue;
            }


            if (c == '"')
            {
                inString = true;

                continue;
            }


            if (c == '\'')
            {
                inChar = true;

                continue;
            }


            if (c == '{')
            {
                depth++;
            }
            else if (c == '}')
            {
                depth--;


                if (depth == 0)
                    return i;
            }
        }


        throw new InvalidOperationException(
            "Matching brace not found."
        );
    }


    // =====================================================
    // FILES
    // =====================================================

    private static string FindScriptPath(
        string className
    )
    {
        string[] guids =
            AssetDatabase.FindAssets(
                className +
                " t:MonoScript"
            );


        for (
            int i = 0;
            i < guids.Length;
            i++
        )
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guids[i]
                );


            if (
                string.Equals(
                    Path.GetFileNameWithoutExtension(
                        path
                    ),
                    className,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return path;
            }
        }


        return null;
    }


    private static string ToAbsolutePath(
        string assetPath
    )
    {
        string project =
            Directory.GetParent(
                Application.dataPath
            ).FullName;


        return
            Path.Combine(
                project,
                assetPath
            );
    }


    private static void BackupOnce(
        string assetPath,
        string suffix
    )
    {
        string source =
            ToAbsolutePath(
                assetPath
            );


        string backup =
            source +
            suffix;


        if (
            File.Exists(
                source
            )
            &&
            !File.Exists(
                backup
            )
        )
        {
            File.Copy(
                source,
                backup,
                false
            );
        }
    }
}

#endif
