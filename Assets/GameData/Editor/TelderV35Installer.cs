#if UNITY_EDITOR

using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TelderV35Installer
{
    private const string FallingHook =
        "Game.World.Physics.FallingBlockSystem.NotifyCellChanged";

    private const string BiomeMarker =
        "// [TELDER-V35-BIOME-MATERIAL-BLEND]";

    private const string PivotMarker =
        "// [TELDER-V35-REAL-ARM-ROTATION-POINT]";

    [MenuItem("Tools/Game/Apply V35 Cursor + Falling + Pivot + Biome Blend")]
    public static void Apply()
    {
        try
        {
            int changed = 0;

            changed += PatchStructureEditorCursor();
            changed += PatchWorldManagerFallingHook();
            changed += PatchHeldItemPoseEditor();
            changed += PatchWorldGeneratorBiomeBlend();

            AssetDatabase.Refresh();

            Debug.Log(
                "TELDER V35 applied. Changed existing files = " +
                changed
            );

            EditorUtility.DisplayDialog(
                "TELDER V35",
                "Готово.\n\n" +
                "• стандартный курсор Windows;\n" +
                "• оптимизированные falling blocks;\n" +
                "• реальная точка вращения руки;\n" +
                "• плавное смешивание материалов биомов.\n\n" +
                "Дождись повторной компиляции Unity.",
                "OK"
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "TELDER V35 INSTALLER FAILED:\n" +
                exception
            );

            EditorUtility.DisplayDialog(
                "TELDER V35 — ошибка",
                exception.ToString(),
                "OK"
            );
        }
    }

    // =====================================================
    // CURSOR
    // =====================================================

    private static int PatchStructureEditorCursor()
    {
        string path = FindScriptPath("StructureEditorController");

        if (string.IsNullOrWhiteSpace(path))
            return 0;

        string source = Read(path);
        string original = source;

        source = source.Replace(
            "global::UnityEngine.Cursor.visible = false;",
            "global::UnityEngine.Cursor.visible = true;"
        );

        source = source.Replace(
            "Cursor.visible = false;",
            "global::UnityEngine.Cursor.visible = true;"
        );

        source = source.Replace(
            "Cursor.lockState = CursorLockMode.Locked;",
            "global::UnityEngine.Cursor.lockState = global::UnityEngine.CursorLockMode.None;"
        );

        source = source.Replace(
            "Cursor.lockState = CursorLockMode.Confined;",
            "global::UnityEngine.Cursor.lockState = global::UnityEngine.CursorLockMode.None;"
        );

        if (source == original)
            return 0;

        BackupAndWrite(path, source);
        return 1;
    }

    // =====================================================
    // FALLING BLOCK WAKE HOOK
    // =====================================================

    private static int PatchWorldManagerFallingHook()
    {
        string path = FindScriptPath("WorldManager");

        if (string.IsNullOrWhiteSpace(path))
        {
            Debug.LogWarning("TELDER V35: WorldManager.cs not found.");
            return 0;
        }

        string source = Read(path);

        if (source.Contains(FallingHook))
            return 0;

        int methodStart = source.IndexOf(
            "public bool SetBlock",
            StringComparison.Ordinal
        );

        if (methodStart < 0)
        {
            Debug.LogWarning("TELDER V35: WorldManager.SetBlock() not found.");
            return 0;
        }

        int braceStart = source.IndexOf('{', methodStart);
        int braceEnd = FindMatchingBrace(source, braceStart);

        string method = source.Substring(
            methodStart,
            braceEnd - methodStart + 1
        );

        int returnTrue = method.LastIndexOf(
            "return true;",
            StringComparison.Ordinal
        );

        if (returnTrue < 0)
        {
            Debug.LogWarning("TELDER V35: final return true in SetBlock() not found.");
            return 0;
        }

        string hook =
@"Game.World.Physics.FallingBlockSystem.NotifyCellChanged(
                worldX,
                worldY
            );


            ";

        method = method.Insert(returnTrue, hook);

        source = source.Remove(
            methodStart,
            braceEnd - methodStart + 1
        ).Insert(
            methodStart,
            method
        );

        BackupAndWrite(path, source);
        return 1;
    }

    // =====================================================
    // HELD ITEM POSE — REAL TRANSFORM PIVOT
    // =====================================================

    private static int PatchHeldItemPoseEditor()
    {
        string path = FindScriptPath("HeldItemPoseEditorWindow");

        if (string.IsNullOrWhiteSpace(path))
        {
            Debug.LogWarning("TELDER V35: HeldItemPoseEditorWindow.cs not found.");
            return 0;
        }

        string source = Read(path);

        // If an earlier patch already edits actual FrontArmPivot / BackArmPivot,
        // keep that implementation and make the labels unambiguous.
        if (
            source.Contains("TELDER-V34-ARM-PIVOT-EDITOR") ||
            source.Contains("TELDER_SHOULDER_PIVOT_V1"))
        {
            string changed = source
                .Replace(
                    "\"Точки вращения рук\"",
                    "\"ТОЧКА, ОТНОСИТЕЛЬНО КОТОРОЙ КРУТИТСЯ РУКА\""
                )
                .Replace(
                    "\"Front Arm Pivot\"",
                    "\"Front Rotation Point (Pivot)\""
                )
                .Replace(
                    "\"Back Arm Pivot\"",
                    "\"Back Rotation Point (Pivot)\""
                )
                .Replace(
                    "\"Front Shoulder\"",
                    "\"Front Rotation Point (Pivot)\""
                )
                .Replace(
                    "\"Back Shoulder\"",
                    "\"Back Rotation Point (Pivot)\""
                );

            if (changed == source)
                return 0;

            BackupAndWrite(path, changed);
            return 1;
        }

        if (source.Contains(PivotMarker))
            return 0;

        // ---------------- Fields ----------------
        int animationHeader = source.IndexOf(
            "// ANIMATION",
            StringComparison.Ordinal
        );

        if (animationHeader < 0)
            throw new InvalidOperationException("Held Item Pose: ANIMATION section not found.");

        int fieldInsert = source.LastIndexOf('\n', animationHeader);

        string fields =
@"
    // [TELDER-V35-REAL-ARM-ROTATION-POINT]
    // Real Transform.localPosition of the arm rotation pivots.
    private Vector2 frontArmRotationPoint;

    private Vector2 backArmRotationPoint;

";

        source = source.Insert(fieldInsert + 1, fields);

        // ---------------- Read from Player ----------------
        int readMining = source.IndexOf(
            "ReadMiningOverlaySettings();",
            StringComparison.Ordinal
        );

        if (readMining < 0)
            throw new InvalidOperationException("Held Item Pose: ReadMiningOverlaySettings() call not found.");

        source = source.Insert(
            readMining,
@"ReadRealArmRotationPoints();

        "
        );

        // ---------------- Inspector UI ----------------
        int handText = source.IndexOf(
            "\"Точка ладони\"",
            StringComparison.Ordinal
        );

        if (handText < 0)
            throw new InvalidOperationException("Held Item Pose: 'Точка ладони' section not found.");

        int labelStart = source.LastIndexOf(
            "EditorGUILayout.LabelField(",
            handText,
            StringComparison.Ordinal
        );

        if (labelStart < 0)
            labelStart = handText;

        string gui =
@"EditorGUILayout.LabelField(
            ""ТОЧКА, ОТНОСИТЕЛЬНО КОТОРОЙ КРУТИТСЯ РУКА"",
            EditorStyles.boldLabel
        );

        EditorGUILayout.HelpBox(
            ""Это НЕ offset предмета. Меняется реальная позиция FrontArmPivot / BackArmPivot. "" +
            ""Все повороты mining/attack затем физически происходят вокруг этой точки."",
            MessageType.Info
        );

        EditorGUI.BeginChangeCheck();

        Vector2 nextFrontRotationPoint =
            EditorGUILayout.Vector2Field(
                ""Front Rotation Point (Pivot)"",
                frontArmRotationPoint
            );

        Vector2 nextBackRotationPoint =
            EditorGUILayout.Vector2Field(
                ""Back Rotation Point (Pivot)"",
                backArmRotationPoint
            );

        if (EditorGUI.EndChangeCheck())
        {
            frontArmRotationPoint = nextFrontRotationPoint;
            backArmRotationPoint = nextBackRotationPoint;

            UpdatePreviewRealArmRotationPoints();
            Repaint();
        }

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button(""Применить точки вращения""))
        {
            ApplyRealArmRotationPoints();
            RebuildPreview();
        }

        if (GUILayout.Button(""Считать из Player""))
        {
            ReadRealArmRotationPoints();
            RebuildPreview();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(12f);

        ";

        source = source.Insert(labelStart, gui);

        // ---------------- Save together ----------------
        int saveStart = source.IndexOf(
            "private void SaveAll()",
            StringComparison.Ordinal
        );

        if (saveStart >= 0)
        {
            int saveBrace = source.IndexOf('{', saveStart);
            int saveEnd = FindMatchingBrace(source, saveBrace);

            string saveMethod = source.Substring(
                saveStart,
                saveEnd - saveStart + 1
            );

            int applyHand = saveMethod.IndexOf(
                "ApplyHandPoint();",
                StringComparison.Ordinal
            );

            if (applyHand >= 0)
            {
                saveMethod = saveMethod.Insert(
                    applyHand,
@"ApplyRealArmRotationPoints();

        "
                );

                source = source.Remove(
                    saveStart,
                    saveEnd - saveStart + 1
                ).Insert(
                    saveStart,
                    saveMethod
                );
            }
        }

        // ---------------- Preview after clone exists ----------------
        int previewItem = source.IndexOf(
            "CreatePreviewHeldItem();",
            StringComparison.Ordinal
        );

        if (previewItem >= 0)
        {
            source = source.Insert(
                previewItem,
@"UpdatePreviewRealArmRotationPoints();

        "
            );
        }

        // ---------------- Helpers ----------------
        int helperInsert = source.IndexOf(
            "private void ApplyHandPoint()",
            StringComparison.Ordinal
        );

        if (helperInsert < 0)
            throw new InvalidOperationException("Held Item Pose: ApplyHandPoint() not found.");

        string helpers =
@"private void ReadRealArmRotationPoints()
    {
        if (sourceFrontArmPivot != null)
        {
            frontArmRotationPoint = new Vector2(
                sourceFrontArmPivot.localPosition.x,
                sourceFrontArmPivot.localPosition.y
            );
        }

        if (sourceBackArmPivot != null)
        {
            backArmRotationPoint = new Vector2(
                sourceBackArmPivot.localPosition.x,
                sourceBackArmPivot.localPosition.y
            );
        }
    }

    private void UpdatePreviewRealArmRotationPoints()
    {
        MoveRealPivotKeepingChildrenInPlace(
            previewFrontArmPivot,
            frontArmRotationPoint
        );

        MoveRealPivotKeepingChildrenInPlace(
            previewBackArmPivot,
            backArmRotationPoint
        );
    }

    private void ApplyRealArmRotationPoints()
    {
        ApplyRealPivotToScene(
            sourceFrontArmPivot,
            frontArmRotationPoint,
            ""Front Arm Rotation Point""
        );

        ApplyRealPivotToScene(
            sourceBackArmPivot,
            backArmRotationPoint,
            ""Back Arm Rotation Point""
        );

        if (sourceHandPoint != null)
        {
            handLocalPosition = new Vector2(
                sourceHandPoint.localPosition.x,
                sourceHandPoint.localPosition.y
            );
        }
    }

    private static void ApplyRealPivotToScene(
        Transform pivot,
        Vector2 target,
        string undoName)
    {
        if (pivot == null)
            return;

        Undo.RecordObject(pivot, undoName);

        int childCount = pivot.childCount;
        Transform[] children = new Transform[childCount];
        Vector3[] positions = new Vector3[childCount];
        Quaternion[] rotations = new Quaternion[childCount];

        for (int i = 0; i < childCount; i++)
        {
            Transform child = pivot.GetChild(i);
            children[i] = child;
            positions[i] = child.position;
            rotations[i] = child.rotation;
            Undo.RecordObject(child, undoName);
        }

        Vector3 local = pivot.localPosition;
        local.x = target.x;
        local.y = target.y;
        pivot.localPosition = local;

        for (int i = 0; i < childCount; i++)
        {
            if (children[i] == null)
                continue;

            children[i].SetPositionAndRotation(
                positions[i],
                rotations[i]
            );

            EditorUtility.SetDirty(children[i]);
            PrefabUtility.RecordPrefabInstancePropertyModifications(children[i]);
        }

        EditorUtility.SetDirty(pivot);
        PrefabUtility.RecordPrefabInstancePropertyModifications(pivot);

        if (pivot.gameObject.scene.IsValid())
            EditorSceneManager.MarkSceneDirty(pivot.gameObject.scene);
    }

    private static void MoveRealPivotKeepingChildrenInPlace(
        Transform pivot,
        Vector2 target)
    {
        if (pivot == null)
            return;

        int childCount = pivot.childCount;
        Transform[] children = new Transform[childCount];
        Vector3[] positions = new Vector3[childCount];
        Quaternion[] rotations = new Quaternion[childCount];

        for (int i = 0; i < childCount; i++)
        {
            Transform child = pivot.GetChild(i);
            children[i] = child;
            positions[i] = child.position;
            rotations[i] = child.rotation;
        }

        Vector3 local = pivot.localPosition;
        local.x = target.x;
        local.y = target.y;
        pivot.localPosition = local;

        for (int i = 0; i < childCount; i++)
        {
            if (children[i] != null)
            {
                children[i].SetPositionAndRotation(
                    positions[i],
                    rotations[i]
                );
            }
        }
    }

    ";

        source = source.Insert(helperInsert, helpers);

        BackupAndWrite(path, source);
        return 1;
    }

    // =====================================================
    // BIOME MATERIAL BLEND
    // =====================================================

    private static int PatchWorldGeneratorBiomeBlend()
    {
        string path = FindScriptPath("WorldGenerator");

        if (string.IsNullOrWhiteSpace(path))
        {
            Debug.LogWarning("TELDER V35: WorldGenerator.cs not found.");
            return 0;
        }

        string source = Read(path);

        if (source.Contains(BiomeMarker))
            return 0;

        int methodStart = source.IndexOf(
            "public ChunkData GenerateChunkData(",
            StringComparison.Ordinal
        );

        if (methodStart < 0)
            throw new InvalidOperationException("WorldGenerator.GenerateChunkData() not found.");

        int methodBrace = source.IndexOf('{', methodStart);
        int methodEnd = FindMatchingBrace(source, methodBrace);

        string method = source.Substring(
            methodStart,
            methodEnd - methodStart + 1
        );

        string worldYAnchor =
@"int worldY =
                    chunkY *
                    Chunk.SizeY +
                    localY;";

        int worldYIndex = method.IndexOf(
            worldYAnchor,
            StringComparison.Ordinal
        );

        if (worldYIndex < 0)
        {
            // Alternate indentation from another current project snapshot.
            worldYAnchor =
@"int worldY =
                        chunkY *
                        Chunk.SizeY +
                        localY;";

            worldYIndex = method.IndexOf(
                worldYAnchor,
                StringComparison.Ordinal
            );
        }

        if (worldYIndex < 0)
            throw new InvalidOperationException("WorldGenerator worldY anchor not found.");

        int insertAfterWorldY = worldYIndex + worldYAnchor.Length;

        method = method.Insert(
            insertAfterWorldY,
@"


                    BiomeRuntimeData cellBiome =
                        SelectBlendedMaterialBiome(
                            worldX,
                            worldY,
                            sample,
                            biome
                        );"
        );

        int foregroundCall = method.IndexOf(
            "GenerateForegroundBlock(",
            insertAfterWorldY,
            StringComparison.Ordinal
        );

        if (foregroundCall < 0)
            throw new InvalidOperationException("GenerateForegroundBlock call not found.");

        int foregroundEnd = method.IndexOf(
            ");",
            foregroundCall,
            StringComparison.Ordinal
        );

        string foregroundText = method.Substring(
            foregroundCall,
            foregroundEnd - foregroundCall + 2
        );

        string changedForeground = ReplaceLastWord(
            foregroundText,
            "biome",
            "cellBiome"
        );

        method = method.Remove(
            foregroundCall,
            foregroundText.Length
        ).Insert(
            foregroundCall,
            changedForeground
        );

        int backgroundCall = method.IndexOf(
            "GenerateBackgroundBlock(",
            foregroundCall + changedForeground.Length,
            StringComparison.Ordinal
        );

        if (backgroundCall < 0)
            throw new InvalidOperationException("GenerateBackgroundBlock call not found.");

        int backgroundEnd = method.IndexOf(
            ");",
            backgroundCall,
            StringComparison.Ordinal
        );

        string backgroundText = method.Substring(
            backgroundCall,
            backgroundEnd - backgroundCall + 2
        );

        string changedBackground = ReplaceLastWord(
            backgroundText,
            "biome",
            "cellBiome"
        );

        method = method.Remove(
            backgroundCall,
            backgroundText.Length
        ).Insert(
            backgroundCall,
            changedBackground
        );

        source = source.Remove(
            methodStart,
            methodEnd - methodStart + 1
        ).Insert(
            methodStart,
            method
        );

        int helperInsert = source.IndexOf(
            "private ushort GenerateForegroundBlock(",
            StringComparison.Ordinal
        );

        if (helperInsert < 0)
            throw new InvalidOperationException("GenerateForegroundBlock method insertion point not found.");

        string helper =
@"// [TELDER-V35-BIOME-MATERIAL-BLEND]
        private BiomeRuntimeData SelectBlendedMaterialBiome(
            int worldX,
            int worldY,
            BiomeSample sample,
            BiomeRuntimeData fallback)
        {
            if (sample.Primary == null)
                return fallback;

            BiomeRuntimeData primary =
                biomeRuntime.Get(sample.Primary);

            if (
                sample.Secondary == null ||
                sample.Secondary == sample.Primary)
            {
                return primary ?? fallback;
            }

            BiomeRuntimeData secondary =
                biomeRuntime.Get(sample.Secondary);

            if (primary == null)
                return secondary ?? fallback;

            if (secondary == null)
                return primary;

            float blend =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Clamp01(sample.Blend)
                );

            if (blend <= 0.015f)
                return primary;

            if (blend >= 0.985f)
                return secondary;

            // Two coherent noise scales create irregular patches instead of
            // a vertical one-column boundary between block palettes.
            float largePatch =
                Mathf.PerlinNoise(
                    (worldX + settings.Seed * 0.371f) * 0.115f,
                    (worldY + settings.Seed * 0.619f) * 0.115f
                );

            float finePatch =
                Mathf.PerlinNoise(
                    (worldX + settings.Seed * 1.173f) * 0.31f,
                    (worldY + settings.Seed * 0.847f) * 0.31f
                );

            float threshold =
                Mathf.Clamp01(
                    largePatch * 0.72f +
                    finePatch * 0.28f
                );

            return blend >= threshold
                ? secondary
                : primary;
        }


        ";

        source = source.Insert(helperInsert, helper);

        BackupAndWrite(path, source);
        return 1;
    }

    private static string ReplaceLastWord(
        string text,
        string oldWord,
        string newWord)
    {
        int index = text.LastIndexOf(
            oldWord,
            StringComparison.Ordinal
        );

        if (index < 0)
            return text;

        return text.Remove(index, oldWord.Length)
            .Insert(index, newWord);
    }

    // =====================================================
    // HELPERS
    // =====================================================

    private static string FindScriptPath(string className)
    {
        string[] guids =
            AssetDatabase.FindAssets(
                className + " t:MonoScript"
            );

        for (int i = 0; i < guids.Length; i++)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guids[i]);

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

    private static string Read(string assetPath)
    {
        return File.ReadAllText(
            ToAbsolutePath(assetPath),
            Encoding.UTF8
        );
    }

    private static void BackupAndWrite(
        string assetPath,
        string source)
    {
        string absolute = ToAbsolutePath(assetPath);
        string backup = absolute + ".v35_backup";

        if (!File.Exists(backup))
            File.Copy(absolute, backup, false);

        File.WriteAllText(
            absolute,
            source,
            new UTF8Encoding(false)
        );
    }

    private static string ToAbsolutePath(string assetPath)
    {
        string projectRoot =
            Directory.GetParent(Application.dataPath).FullName;

        return Path.Combine(projectRoot, assetPath);
    }

    private static int FindMatchingBrace(
        string source,
        int openingBrace)
    {
        int depth = 0;
        bool inString = false;
        bool inChar = false;
        bool escape = false;
        bool lineComment = false;
        bool blockComment = false;

        for (int i = openingBrace; i < source.Length; i++)
        {
            char c = source[i];
            char next =
                i + 1 < source.Length
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
                if (c == '*' && next == '/')
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

            if (c == '/' && next == '/')
            {
                lineComment = true;
                i++;
                continue;
            }

            if (c == '/' && next == '*')
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
                continue;
            }

            if (c == '}')
            {
                depth--;

                if (depth == 0)
                    return i;
            }
        }

        throw new InvalidOperationException("Matching brace not found.");
    }
}

#endif
