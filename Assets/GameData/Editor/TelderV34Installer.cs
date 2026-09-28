#if UNITY_EDITOR

using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class TelderV34Installer
{
    private const string WorldManagerMarker =
        "// [TELDER-V34-FALLING-NOTIFY]";

    private const string PoseMarker =
        "// [TELDER-V34-ARM-PIVOT-EDITOR]";

    [MenuItem("Tools/Game/Apply V34 Physics + Explosions + Cursor + Pivot")]
    public static void Apply()
    {
        int changed = 0;

        try
        {
            changed += PatchWorldManager();
            changed += PatchBlockBreakDebris();
            changed += PatchFurnitureFireSorting();
            changed += PatchHeldItemPoseEditor();

            AssetDatabase.Refresh();

            Debug.Log(
                "TELDER V34: applied successfully. Changed files: " +
                changed
            );

            EditorUtility.DisplayDialog(
                "TELDER V34",
                "Готово.\n\n" +
                "Исправлены/добавлены:\n" +
                "• частицы поверх блоков\n" +
                "• глобальный квадратный курсор\n" +
                "• pivot рук в Held Item Pose Editor\n" +
                "• falling blocks по тегу \"falling\"\n" +
                "• ExplosionSystem + пиксельный взрыв\n\n" +
                "Дождись окончания компиляции Unity.",
                "OK"
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "TELDER V34 INSTALLER FAILED:\n" +
                exception
            );

            EditorUtility.DisplayDialog(
                "TELDER V34 — ошибка",
                exception.ToString(),
                "OK"
            );
        }
    }

    private static int PatchWorldManager()
    {
        string path =
            FindScriptPath("WorldManager");

        if (string.IsNullOrWhiteSpace(path))
        {
            Debug.LogWarning(
                "TELDER V34: WorldManager.cs not found. " +
                "Falling blocks still have a nearby fallback scan, " +
                "but support removal will not wake them instantly."
            );
            return 0;
        }

        string source = Read(path);

        if (source.Contains(WorldManagerMarker))
            return 0;

        int methodStart =
            source.IndexOf(
                "public bool SetBlock",
                StringComparison.Ordinal
            );

        if (methodStart < 0)
        {
            Debug.LogWarning(
                "TELDER V34: WorldManager.SetBlock() not found."
            );
            return 0;
        }

        int openBrace =
            source.IndexOf('{', methodStart);

        if (openBrace < 0)
            return 0;

        int closeBrace =
            FindMatchingBrace(source, openBrace);

        string method =
            source.Substring(
                methodStart,
                closeBrace - methodStart + 1
            );

        int returnIndex =
            method.LastIndexOf(
                "return true;",
                StringComparison.Ordinal
            );

        if (returnIndex < 0)
        {
            Debug.LogWarning(
                "TELDER V34: final return true; in SetBlock() not found."
            );
            return 0;
        }

        string hook =
@"// [TELDER-V34-FALLING-NOTIFY]
            Game.World.Physics.FallingBlockSystem.NotifyCellChanged(
                worldX,
                worldY
            );


            ";

        method =
            method.Insert(
                returnIndex,
                hook
            );

        source =
            source.Remove(
                methodStart,
                closeBrace - methodStart + 1
            )
            .Insert(
                methodStart,
                method
            );

        BackupAndWrite(path, source);
        return 1;
    }

    private static int PatchBlockBreakDebris()
    {
        string path =
            FindScriptPath("BlockBreakDebrisSystem");

        if (string.IsNullOrWhiteSpace(path))
        {
            Debug.LogWarning(
                "TELDER V34: BlockBreakDebrisSystem.cs not found."
            );
            return 0;
        }

        string source = Read(path);
        string original = source;

        source =
            ReplaceConstInt(
                source,
                "ForegroundSortingOrder",
                1000
            );

        source =
            ReplaceConstInt(
                source,
                "BackgroundSortingOrder",
                1000
            );

        if (source == original)
            return 0;

        BackupAndWrite(path, source);
        return 1;
    }

    private static int PatchFurnitureFireSorting()
    {
        string path =
            FindScriptPath("FurnitureLayerManager");

        if (string.IsNullOrWhiteSpace(path))
            return 0;

        string source = Read(path);
        string original = source;

        // The current fire-particle creator uses "renderer.sortingOrder = 3".
        // Restrict replacement to the CreateFireParticles method.
        int methodStart =
            source.IndexOf(
                "private void CreateFireParticles",
                StringComparison.Ordinal
            );

        if (methodStart < 0)
            return 0;

        int openBrace =
            source.IndexOf('{', methodStart);

        if (openBrace < 0)
            return 0;

        int closeBrace =
            FindMatchingBrace(source, openBrace);

        string method =
            source.Substring(
                methodStart,
                closeBrace - methodStart + 1
            );

        method =
            ReplaceSortingOrderLiteral(
                method,
                1000
            );

        source =
            source.Remove(
                methodStart,
                closeBrace - methodStart + 1
            )
            .Insert(
                methodStart,
                method
            );

        if (source == original)
            return 0;

        BackupAndWrite(path, source);
        return 1;
    }

    private static int PatchHeldItemPoseEditor()
    {
        string path =
            FindScriptPath("HeldItemPoseEditorWindow");

        if (string.IsNullOrWhiteSpace(path))
        {
            Debug.LogWarning(
                "TELDER V34: HeldItemPoseEditorWindow.cs not found."
            );
            return 0;
        }

        string source = Read(path);

        if (source.Contains(PoseMarker))
            return 0;

        string original = source;

        // -----------------------------------------------------
        // 1. Fields
        // -----------------------------------------------------
        int handField =
            source.IndexOf(
                "private Vector2 handLocalPosition",
                StringComparison.Ordinal
            );

        if (handField < 0)
        {
            Debug.LogWarning(
                "TELDER V34: handLocalPosition field not found."
            );
            return 0;
        }

        int handFieldEnd =
            source.IndexOf(';', handField);

        if (handFieldEnd < 0)
            return 0;

        string fields =
@"

// [TELDER-V34-ARM-PIVOT-EDITOR]
    private Vector2 frontArmPivotLocalPosition;

    private Vector2 backArmPivotLocalPosition;
";

        source =
            source.Insert(
                handFieldEnd + 1,
                fields
            );

        // -----------------------------------------------------
        // 2. Read pivots when Player is selected
        // -----------------------------------------------------
        int readOverlay =
            source.IndexOf(
                "ReadMiningOverlaySettings();",
                StringComparison.Ordinal
            );

        if (readOverlay < 0)
        {
            throw new InvalidOperationException(
                "ReadMiningOverlaySettings() call not found."
            );
        }

        source =
            source.Insert(
                readOverlay,
@"ReadArmPivotPositions();

        "
            );

        // -----------------------------------------------------
        // 3. Inspector section before Hand Point section
        // -----------------------------------------------------
        int handSection =
            source.IndexOf(
                "\"Точка ладони\"",
                StringComparison.Ordinal
            );

        if (handSection < 0)
        {
            throw new InvalidOperationException(
                "Held Item Pose: 'Точка ладони' section not found."
            );
        }

        int labelStart =
            source.LastIndexOf(
                "EditorGUILayout.LabelField(",
                handSection,
                StringComparison.Ordinal
            );

        if (labelStart < 0)
            labelStart = handSection;

        string inspectorBlock =
@"EditorGUILayout.LabelField(
            ""Точки вращения рук"",
            EditorStyles.boldLabel
        );


        EditorGUI.BeginChangeCheck();


        Vector2 newFrontPivot =
            EditorGUILayout.Vector2Field(
                ""Front Arm Pivot"",
                frontArmPivotLocalPosition
            );


        Vector2 newBackPivot =
            EditorGUILayout.Vector2Field(
                ""Back Arm Pivot"",
                backArmPivotLocalPosition
            );


        if (
            EditorGUI.EndChangeCheck()
        )
        {
            frontArmPivotLocalPosition =
                newFrontPivot;

            backArmPivotLocalPosition =
                newBackPivot;

            UpdatePreviewArmPivots();

            Repaint();
        }


        EditorGUILayout.BeginHorizontal();


        if (
            GUILayout.Button(
                ""Применить Pivot в сцену""
            )
        )
        {
            ApplyArmPivots();
        }


        if (
            GUILayout.Button(
                ""Считать из сцены""
            )
        )
        {
            ReadArmPivotPositions();
            UpdatePreviewArmPivots();
        }


        EditorGUILayout.EndHorizontal();


        EditorGUILayout.HelpBox(
            ""Pivot — это точка, вокруг которой вращается рука. "" +
            ""При переносе Pivot дочерние спрайты сохраняют мировое положение, "" +
            ""поэтому сама рука не прыгает на экране."",
            MessageType.Info
        );


        EditorGUILayout.Space(
            12f
        );


        ";

        source =
            source.Insert(
                labelStart,
                inspectorBlock
            );

        // -----------------------------------------------------
        // 4. Save pivots together with hand/item pose
        // -----------------------------------------------------
        int saveAllStart =
            source.IndexOf(
                "private void SaveAll()",
                StringComparison.Ordinal
            );

        if (saveAllStart < 0)
        {
            throw new InvalidOperationException(
                "Held Item Pose: SaveAll() not found."
            );
        }

        int saveOpen =
            source.IndexOf('{', saveAllStart);

        int saveClose =
            FindMatchingBrace(source, saveOpen);

        string saveMethod =
            source.Substring(
                saveAllStart,
                saveClose - saveAllStart + 1
            );

        int applyHand =
            saveMethod.IndexOf(
                "ApplyHandPoint();",
                StringComparison.Ordinal
            );

        if (applyHand >= 0)
        {
            saveMethod =
                saveMethod.Insert(
                    applyHand,
@"ApplyArmPivots();

        "
                );

            source =
                source.Remove(
                    saveAllStart,
                    saveClose - saveAllStart + 1
                )
                .Insert(
                    saveAllStart,
                    saveMethod
                );
        }

        // -----------------------------------------------------
        // 5. Keep preview pivots current after preview rebuild
        // -----------------------------------------------------
        int rebuildStart =
            source.IndexOf(
                "private void RebuildPreview()",
                StringComparison.Ordinal
            );

        if (rebuildStart >= 0)
        {
            int rebuildOpen =
                source.IndexOf('{', rebuildStart);

            int rebuildClose =
                FindMatchingBrace(source, rebuildOpen);

            string rebuild =
                source.Substring(
                    rebuildStart,
                    rebuildClose - rebuildStart + 1
                );

            int createHeld =
                rebuild.LastIndexOf(
                    "CreatePreviewHeldItem();",
                    StringComparison.Ordinal
                );

            if (createHeld >= 0)
            {
                rebuild =
                    rebuild.Insert(
                        createHeld,
@"UpdatePreviewArmPivots();


        "
                    );

                source =
                    source.Remove(
                        rebuildStart,
                        rebuildClose - rebuildStart + 1
                    )
                    .Insert(
                        rebuildStart,
                        rebuild
                    );
            }
        }

        // -----------------------------------------------------
        // 6. Helper methods before ApplyHandPoint()
        // -----------------------------------------------------
        int applyHandMethod =
            source.IndexOf(
                "private void ApplyHandPoint()",
                StringComparison.Ordinal
            );

        if (applyHandMethod < 0)
        {
            throw new InvalidOperationException(
                "Held Item Pose: ApplyHandPoint() not found."
            );
        }

        string helpers =
@"private void ReadArmPivotPositions()
    {
        if (sourceFrontArmPivot != null)
        {
            frontArmPivotLocalPosition =
                new Vector2(
                    sourceFrontArmPivot.localPosition.x,
                    sourceFrontArmPivot.localPosition.y
                );
        }

        if (sourceBackArmPivot != null)
        {
            backArmPivotLocalPosition =
                new Vector2(
                    sourceBackArmPivot.localPosition.x,
                    sourceBackArmPivot.localPosition.y
                );
        }
    }


    private void UpdatePreviewArmPivots()
    {
        MovePivotPreserveChildren(
            previewFrontArmPivot,
            frontArmPivotLocalPosition
        );

        MovePivotPreserveChildren(
            previewBackArmPivot,
            backArmPivotLocalPosition
        );
    }


    private void ApplyArmPivots()
    {
        ApplySingleArmPivot(
            sourceFrontArmPivot,
            frontArmPivotLocalPosition,
            ""Move Front Arm Pivot""
        );

        ApplySingleArmPivot(
            sourceBackArmPivot,
            backArmPivotLocalPosition,
            ""Move Back Arm Pivot""
        );

        // HandPoint is a child of the front-arm hierarchy. Since the visual
        // children are preserved in world space, read its compensated local
        // position back into the editor field.
        if (sourceHandPoint != null)
        {
            handLocalPosition =
                new Vector2(
                    sourceHandPoint.localPosition.x,
                    sourceHandPoint.localPosition.y
                );
        }

        RebuildPreview();
    }


    private static void ApplySingleArmPivot(
        Transform pivot,
        Vector2 targetLocalPosition,
        string undoName
    )
    {
        if (pivot == null)
            return;

        Undo.RecordObject(
            pivot,
            undoName
        );

        Transform[] children =
            new Transform[pivot.childCount];

        Vector3[] positions =
            new Vector3[pivot.childCount];

        Quaternion[] rotations =
            new Quaternion[pivot.childCount];

        for (int i = 0; i < pivot.childCount; i++)
        {
            Transform child =
                pivot.GetChild(i);

            children[i] = child;

            Undo.RecordObject(
                child,
                undoName
            );

            positions[i] =
                child.position;

            rotations[i] =
                child.rotation;
        }

        Vector3 local =
            pivot.localPosition;

        local.x =
            targetLocalPosition.x;

        local.y =
            targetLocalPosition.y;

        pivot.localPosition =
            local;

        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] == null)
                continue;

            children[i].SetPositionAndRotation(
                positions[i],
                rotations[i]
            );

            EditorUtility.SetDirty(
                children[i]
            );

            PrefabUtility
                .RecordPrefabInstancePropertyModifications(
                    children[i]
                );
        }

        EditorUtility.SetDirty(
            pivot
        );

        PrefabUtility
            .RecordPrefabInstancePropertyModifications(
                pivot
            );

        if (pivot.gameObject.scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(
                pivot.gameObject.scene
            );
        }
    }


    private static void MovePivotPreserveChildren(
        Transform pivot,
        Vector2 targetLocalPosition
    )
    {
        if (pivot == null)
            return;

        int count =
            pivot.childCount;

        Transform[] children =
            new Transform[count];

        Vector3[] positions =
            new Vector3[count];

        Quaternion[] rotations =
            new Quaternion[count];

        for (int i = 0; i < count; i++)
        {
            Transform child =
                pivot.GetChild(i);

            children[i] =
                child;

            positions[i] =
                child.position;

            rotations[i] =
                child.rotation;
        }

        Vector3 local =
            pivot.localPosition;

        local.x =
            targetLocalPosition.x;

        local.y =
            targetLocalPosition.y;

        pivot.localPosition =
            local;

        for (int i = 0; i < count; i++)
        {
            if (children[i] == null)
                continue;

            children[i].SetPositionAndRotation(
                positions[i],
                rotations[i]
            );
        }
    }


    ";

        source =
            source.Insert(
                applyHandMethod,
                helpers
            );

        if (source == original)
            return 0;

        BackupAndWrite(path, source);
        return 1;
    }

    private static string ReplaceConstInt(
        string source,
        string fieldName,
        int newValue)
    {
        string token =
            "private const int " + fieldName;

        int start =
            source.IndexOf(
                token,
                StringComparison.Ordinal
            );

        if (start < 0)
            return source;

        int equals =
            source.IndexOf('=', start);

        int semicolon =
            source.IndexOf(';', equals);

        if (equals < 0 || semicolon < 0)
            return source;

        return
            source.Substring(0, equals + 1) +
            "\n            " +
            newValue +
            source.Substring(semicolon);
    }

    private static string ReplaceSortingOrderLiteral(
        string method,
        int newValue)
    {
        int searchFrom = 0;

        while (true)
        {
            int property =
                method.IndexOf(
                    ".sortingOrder",
                    searchFrom,
                    StringComparison.Ordinal
                );

            if (property < 0)
                break;

            int equals =
                method.IndexOf('=', property);

            int semicolon =
                method.IndexOf(';', equals);

            if (equals < 0 || semicolon < 0)
                break;

            method =
                method.Substring(0, equals + 1) +
                "\n                " +
                newValue +
                method.Substring(semicolon);

            searchFrom =
                equals + 1;
        }

        return method;
    }

    private static string FindScriptPath(string className)
    {
        string[] guids =
            AssetDatabase.FindAssets(
                className + " t:MonoScript"
            );

        for (int i = 0; i < guids.Length; i++)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guids[i]
                );

            if (string.Equals(
                Path.GetFileNameWithoutExtension(path),
                className,
                StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }
        }

        return null;
    }

    private static int FindMatchingBrace(
        string source,
        int openingBrace)
    {
        int depth = 0;
        bool inString = false;
        bool inChar = false;
        bool escape = false;
        bool inLineComment = false;
        bool inBlockComment = false;

        for (int i = openingBrace; i < source.Length; i++)
        {
            char c = source[i];
            char next =
                i + 1 < source.Length
                    ? source[i + 1]
                    : '\0';

            if (inLineComment)
            {
                if (c == '\n')
                    inLineComment = false;

                continue;
            }

            if (inBlockComment)
            {
                if (c == '*' && next == '/')
                {
                    inBlockComment = false;
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

            if (c == '/' && next == '/')
            {
                inLineComment = true;
                i++;
                continue;
            }

            if (c == '/' && next == '*')
            {
                inBlockComment = true;
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
                continue;
            }

            if (c == '}')
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

    private static string Read(string assetPath)
    {
        return File.ReadAllText(
            ToAbsolutePath(assetPath),
            Encoding.UTF8
        );
    }

    private static void BackupAndWrite(
        string assetPath,
        string content)
    {
        string absolute =
            ToAbsolutePath(assetPath);

        string backup =
            absolute + ".v34_backup";

        if (!File.Exists(backup))
            File.Copy(absolute, backup, false);

        File.WriteAllText(
            absolute,
            content,
            new UTF8Encoding(false)
        );
    }

    private static string ToAbsolutePath(string assetPath)
    {
        string projectRoot =
            Directory
                .GetParent(Application.dataPath)
                .FullName;

        return Path.Combine(
            projectRoot,
            assetPath
        );
    }
}

#endif
