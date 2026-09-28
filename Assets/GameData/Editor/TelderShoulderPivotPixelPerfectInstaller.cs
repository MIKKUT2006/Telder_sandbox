#if UNITY_EDITOR

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class TelderShoulderPivotPixelPerfectInstaller
{
    private const string MenuPath =
        "Tools/Game/Apply Shoulder Pivot + Pixel Perfect Editors";


    [MenuItem(MenuPath)]
    public static void Apply()
    {
        try
        {
            string heldEditorPath =
                FindScriptPath(
                    "HeldItemPoseEditorWindow"
                );

            if (string.IsNullOrWhiteSpace(heldEditorPath))
            {
                Debug.LogError(
                    "TELDER: HeldItemPoseEditorWindow.cs not found."
                );

                return;
            }


            string structureControllerPath =
                FindScriptPath(
                    "StructureEditorController"
                );


            BackupOnce(
                heldEditorPath,
                ".shoulder_pixel_backup"
            );


            PatchHeldItemPoseEditor(
                heldEditorPath
            );


            if (
                !string.IsNullOrWhiteSpace(
                    structureControllerPath
                )
            )
            {
                BackupOnce(
                    structureControllerPath,
                    ".pixel_cursor_backup"
                );


                PatchStructureEditorController(
                    structureControllerPath
                );
            }


            PatchCacheIfPresent(
                "StructureEditorIconCache"
            );


            PatchCacheIfPresent(
                "StructureEditorLootIconCache"
            );


            ForcePixelTextureImports();


            AssetDatabase.Refresh();


            Debug.Log(
                "TELDER: Shoulder Pivot + Pixel Perfect Editors applied.\n" +
                "Held Item Pose Editor: " +
                heldEditorPath +
                (
                    string.IsNullOrWhiteSpace(
                        structureControllerPath
                    )
                        ? ""
                        : "\nStructure Editor: " +
                          structureControllerPath
                )
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "TELDER PATCH FAILED:\n" +
                exception
            );
        }
    }


    // =====================================================
    // HELD ITEM POSE EDITOR
    // =====================================================

    private static void PatchHeldItemPoseEditor(
        string assetPath
    )
    {
        string path =
            ToAbsolutePath(
                assetPath
            );


        string source =
            File.ReadAllText(
                path,
                Encoding.UTF8
            );


        if (
            !source.Contains(
                "TELDER_SHOULDER_PIVOT_V1"
            )
        )
        {
            const string fieldAnchor =
                "    private Transform sourceHandPoint;";


            const string fieldPatch =
@"    private Transform sourceHandPoint;


    // TELDER_SHOULDER_PIVOT_V1
    // Local position of the actual rotation pivots.
    private Vector2 frontShoulderLocalPosition;

    private Vector2 backShoulderLocalPosition;";


            RequireReplace(
                ref source,
                fieldAnchor,
                fieldPatch,
                "Held editor shoulder fields"
            );


            const string readAnchor =
                "        ReadMiningOverlaySettings();";


            const string readPatch =
@"        ReadShoulderPivotsFromScene();

        ReadMiningOverlaySettings();";


            RequireReplace(
                ref source,
                readAnchor,
                readPatch,
                "Held editor read shoulder values"
            );


            const string handLabelAnchor =
@"        EditorGUILayout.LabelField(
            ""Точка ладони"",
            EditorStyles.boldLabel
        );";


            string shoulderGui =
@"        EditorGUILayout.LabelField(
            ""Точки плеч / Pivot вращения"",
            EditorStyles.boldLabel
        );


        EditorGUILayout.HelpBox(
            ""Это реальные точки, вокруг которых вращаются FrontArmPivot и BackArmPivot. "" +
            ""При переносе pivot редактор компенсирует дочерние объекты, поэтому рука визуально не прыгает, "" +
            ""а центр её вращения меняется."",
            MessageType.Info
        );


        EditorGUI.BeginChangeCheck();


        frontShoulderLocalPosition =
            EditorGUILayout.Vector2Field(
                ""Front Shoulder"",
                frontShoulderLocalPosition
            );


        backShoulderLocalPosition =
            EditorGUILayout.Vector2Field(
                ""Back Shoulder"",
                backShoulderLocalPosition
            );


        if (
            EditorGUI.EndChangeCheck()
        )
        {
            UpdatePreviewShoulderPivots();

            Repaint();
        }


        EditorGUILayout.BeginHorizontal();


        if (
            GUILayout.Button(
                ""Применить плечи в сцену""
            )
        )
        {
            ApplyShoulderPivots();

            RebuildPreview();
        }


        if (
            GUILayout.Button(
                ""Считать из сцены""
            )
        )
        {
            ReadShoulderPivotsFromScene();

            RebuildPreview();
        }


        EditorGUILayout.EndHorizontal();


        EditorGUILayout.Space(
            12f
        );


" + handLabelAnchor;


            RequireReplace(
                ref source,
                handLabelAnchor,
                shoulderGui,
                "Held editor shoulder GUI"
            );


            const string previewAnchor =
                "        CreatePreviewHeldItem();";


            const string previewPatch =
@"        UpdatePreviewShoulderPivots();

        ForcePreviewTexturesPoint(
            previewPlayer
        );

        CreatePreviewHeldItem();";


            ReplaceFirstRequired(
                ref source,
                previewAnchor,
                previewPatch,
                "Held editor preview shoulder application"
            );


            const string applyAnchor =
                "    private void ApplyHandPoint()";


            string helperMethods =
@"    // =====================================================
    // SHOULDER PIVOTS
    // =====================================================

    private void ReadShoulderPivotsFromScene()
    {
        if (
            sourceFrontArmPivot !=
            null
        )
        {
            Vector3 local =
                sourceFrontArmPivot.localPosition;


            frontShoulderLocalPosition =
                new Vector2(
                    local.x,
                    local.y
                );
        }


        if (
            sourceBackArmPivot !=
            null
        )
        {
            Vector3 local =
                sourceBackArmPivot.localPosition;


            backShoulderLocalPosition =
                new Vector2(
                    local.x,
                    local.y
                );
        }
    }


    private void UpdatePreviewShoulderPivots()
    {
        bool frontMoved =
            MovePivotPreserveChildren(
                previewFrontArmPivot,
                frontShoulderLocalPosition
            );


        MovePivotPreserveChildren(
            previewBackArmPivot,
            backShoulderLocalPosition
        );


        if (
            frontMoved
            &&
            previewHandPoint !=
            null
        )
        {
            Vector3 handLocal =
                previewHandPoint.localPosition;


            // Moving a pivot while preserving the visible arm changes
            // the compensated local hand coordinate. Keep the editor
            // value synchronized with that real coordinate.
            handLocalPosition =
                new Vector2(
                    handLocal.x,
                    handLocal.y
                );
        }


        ForcePreviewTexturesPoint(
            previewPlayer
        );
    }


    private void ApplyShoulderPivots()
    {
        RecordPivotAndChildren(
            sourceFrontArmPivot,
            ""Move Front Shoulder Pivot""
        );


        RecordPivotAndChildren(
            sourceBackArmPivot,
            ""Move Back Shoulder Pivot""
        );


        bool frontMoved =
            MovePivotPreserveChildren(
                sourceFrontArmPivot,
                frontShoulderLocalPosition
            );


        MovePivotPreserveChildren(
            sourceBackArmPivot,
            backShoulderLocalPosition
        );


        if (
            frontMoved
            &&
            sourceHandPoint !=
            null
        )
        {
            Vector3 handLocal =
                sourceHandPoint.localPosition;


            handLocalPosition =
                new Vector2(
                    handLocal.x,
                    handLocal.y
                );
        }


        MarkTransformDirty(
            sourceFrontArmPivot
        );


        MarkTransformDirty(
            sourceBackArmPivot
        );


        MarkTransformDirty(
            sourceHandPoint
        );
    }


    private static bool MovePivotPreserveChildren(
        Transform pivot,
        Vector2 targetLocalPosition
    )
    {
        if (pivot == null)
            return false;


        Vector3 oldLocal =
            pivot.localPosition;


        Vector3 newLocal =
            new Vector3(
                targetLocalPosition.x,
                targetLocalPosition.y,
                oldLocal.z
            );


        if (
            Vector3.SqrMagnitude(
                oldLocal -
                newLocal
            )
            <
            0.0000001f
        )
        {
            return false;
        }


        int childCount =
            pivot.childCount;


        Transform[] children =
            new Transform[
                childCount
            ];


        Vector3[] worldPositions =
            new Vector3[
                childCount
            ];


        for (
            int i = 0;
            i < childCount;
            i++
        )
        {
            Transform child =
                pivot.GetChild(
                    i
                );


            children[i] =
                child;


            worldPositions[i] =
                child.position;
        }


        pivot.localPosition =
            newLocal;


        for (
            int i = 0;
            i < childCount;
            i++
        )
        {
            if (
                children[i] !=
                null
            )
            {
                children[i].position =
                    worldPositions[i];
            }
        }


        return true;
    }


    private static void RecordPivotAndChildren(
        Transform pivot,
        string undoName
    )
    {
        if (pivot == null)
            return;


        Undo.RecordObject(
            pivot,
            undoName
        );


        for (
            int i = 0;
            i < pivot.childCount;
            i++
        )
        {
            Transform child =
                pivot.GetChild(
                    i
                );


            if (
                child !=
                null
            )
            {
                Undo.RecordObject(
                    child,
                    undoName
                );
            }
        }
    }


    private static void MarkTransformDirty(
        Transform value
    )
    {
        if (value == null)
            return;


        EditorUtility.SetDirty(
            value
        );


        if (
            value.gameObject.scene.IsValid()
        )
        {
            EditorSceneManager.MarkSceneDirty(
                value.gameObject.scene
            );
        }
    }


    private static void ForcePreviewTexturesPoint(
        GameObject root
    )
    {
        if (root == null)
            return;


        SpriteRenderer[] renderers =
            root.GetComponentsInChildren<
                SpriteRenderer
            >(
                true
            );


        for (
            int i = 0;
            i < renderers.Length;
            i++
        )
        {
            SpriteRenderer renderer =
                renderers[i];


            if (
                renderer == null
                ||
                renderer.sprite == null
                ||
                renderer.sprite.texture == null
            )
            {
                continue;
            }


            renderer.sprite.texture.filterMode =
                FilterMode.Point;
        }
    }


" + applyAnchor;


            RequireReplace(
                ref source,
                applyAnchor,
                helperMethods,
                "Held editor shoulder methods"
            );


            string saveAllPattern =
                @"private\s+void\s+SaveAll\s*\(\s*\)\s*\{\s*ApplyHandPoint\s*\(\s*\)\s*;";


            if (
                !Regex.IsMatch(
                    source,
                    saveAllPattern
                )
            )
            {
                throw new InvalidOperationException(
                    "Held editor SaveAll() anchor not found."
                );
            }


            Match saveAllMatch =
                Regex.Match(
                    source,
                    saveAllPattern
                );


            if (
                !saveAllMatch.Success
            )
            {
                throw new InvalidOperationException(
                    "Held editor SaveAll() match not found."
                );
            }


            string patchedSaveAll =
                saveAllMatch.Value.Replace(
                    "ApplyHandPoint",
                    "ApplyShoulderPivots();\n\n        ApplyHandPoint"
                );


            source =
                source.Remove(
                    saveAllMatch.Index,
                    saveAllMatch.Length
                )
                .Insert(
                    saveAllMatch.Index,
                    patchedSaveAll
                );


            // Imported item sprite itself should also always be point-filtered.
            source =
                source.Replace(
                    "            return importedSprite;",
@"            if (
                importedSprite.texture !=
                null
            )
            {
                importedSprite.texture.filterMode =
                    FilterMode.Point;
            }


            return importedSprite;"
                );


            source =
                source.Replace(
                    "        generatedItemSprite =\n            Sprite.Create(",
@"        textureAsset.filterMode =
            FilterMode.Point;


        generatedItemSprite =
            Sprite.Create("
                );
        }


        File.WriteAllText(
            path,
            source,
            new UTF8Encoding(
                false
            )
        );
    }


    // =====================================================
    // STRUCTURE EDITOR
    // =====================================================

    private static void PatchStructureEditorController(
        string assetPath
    )
    {
        string path =
            ToAbsolutePath(
                assetPath
            );


        string source =
            File.ReadAllText(
                path,
                Encoding.UTF8
            );


        // The custom GUI cursor is drawn manually. The OS cursor must stay
        // hidden so Windows DPI scaling cannot blur/double it.
        source =
            Regex.Replace(
                source,
                @"global::UnityEngine\.Cursor\.visible\s*=\s*true\s*;",
                "global::UnityEngine.Cursor.visible = false;"
            );


        if (
            !source.Contains(
                "TELDER_STRUCTURE_POINT_FILTER_V1"
            )
        )
        {
            int classBrace =
                source.IndexOf(
                    '{',
                    source.IndexOf(
                        "class StructureEditorController",
                        StringComparison.Ordinal
                    )
                );


            if (
                classBrace >=
                0
            )
            {
                source =
                    source.Insert(
                        classBrace + 1,
                        "\n        // TELDER_STRUCTURE_POINT_FILTER_V1\n"
                    );
            }


            // Any Texture2D named icon/background/foreground that is checked
            // before GUI.DrawTexture is forced to Point. This covers palette,
            // grid cells and most loot/item previews.
            source =
                Regex.Replace(
                    source,
                    @"if\s*\(\s*(icon|background|foreground)\s*!=\s*null\s*\)\s*\{",
                    match =>
                        match.Value +
                        "\n                    " +
                        match.Groups[1].Value +
                        ".filterMode = FilterMode.Point;"
                );
        }


        File.WriteAllText(
            path,
            source,
            new UTF8Encoding(
                false
            )
        );
    }


    private static void PatchCacheIfPresent(
        string className
    )
    {
        string assetPath =
            FindScriptPath(
                className
            );


        if (
            string.IsNullOrWhiteSpace(
                assetPath
            )
        )
        {
            return;
        }


        BackupOnce(
            assetPath,
            ".point_filter_backup"
        );


        string path =
            ToAbsolutePath(
                assetPath
            );


        string source =
            File.ReadAllText(
                path,
                Encoding.UTF8
            );


        // If a cache explicitly asks for bilinear, that is definitely wrong
        // for Structure Editor pixel icons.
        source =
            source.Replace(
                "FilterMode.Bilinear",
                "FilterMode.Point"
            );


        File.WriteAllText(
            path,
            source,
            new UTF8Encoding(
                false
            )
        );
    }


    // =====================================================
    // PIXEL IMPORT SETTINGS
    // =====================================================

    private static void ForcePixelTextureImports()
    {
        const string root =
            "Assets/GameData/ResourcePacks/Default/textures";


        if (
            !AssetDatabase.IsValidFolder(
                root
            )
        )
        {
            Debug.LogWarning(
                "TELDER: texture root not found: " +
                root
            );

            return;
        }


        string[] guids =
            AssetDatabase.FindAssets(
                "t:Texture2D",
                new[]
                {
                    root
                }
            );


        int changed =
            0;


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


            TextureImporter importer =
                AssetImporter.GetAtPath(
                    path
                )
                as
                TextureImporter;


            if (importer == null)
                continue;


            bool dirty =
                false;


            if (
                importer.filterMode !=
                FilterMode.Point
            )
            {
                importer.filterMode =
                    FilterMode.Point;

                dirty =
                    true;
            }


            if (
                importer.mipmapEnabled
            )
            {
                importer.mipmapEnabled =
                    false;

                dirty =
                    true;
            }


            if (
                importer.textureCompression !=
                TextureImporterCompression.Uncompressed
            )
            {
                importer.textureCompression =
                    TextureImporterCompression.Uncompressed;

                dirty =
                    true;
            }


            if (dirty)
            {
                importer.SaveAndReimport();

                changed++;
            }
        }


        Debug.Log(
            "TELDER: pixel textures checked = " +
            guids.Length +
            ", reimported = " +
            changed
        );
    }


    // =====================================================
    // HELPERS
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


            string fileName =
                Path.GetFileNameWithoutExtension(
                    path
                );


            if (
                string.Equals(
                    fileName,
                    className,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return path;
            }
        }


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


            string absolute =
                ToAbsolutePath(
                    path
                );


            if (
                File.Exists(
                    absolute
                )
                &&
                File.ReadAllText(
                    absolute
                )
                .Contains(
                    "class " +
                    className
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
        string projectRoot =
            Directory.GetParent(
                Application.dataPath
            ).FullName;


        return
            Path.Combine(
                projectRoot,
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


    private static void RequireReplace(
        ref string source,
        string oldValue,
        string newValue,
        string description
    )
    {
        if (
            !source.Contains(
                oldValue
            )
        )
        {
            throw new InvalidOperationException(
                "Patch anchor not found: " +
                description
            );
        }


        source =
            source.Replace(
                oldValue,
                newValue
            );
    }


    private static void ReplaceFirstRequired(
        ref string source,
        string oldValue,
        string newValue,
        string description
    )
    {
        int index =
            source.IndexOf(
                oldValue,
                StringComparison.Ordinal
            );


        if (index < 0)
        {
            throw new InvalidOperationException(
                "Patch anchor not found: " +
                description
            );
        }


        source =
            source.Remove(
                index,
                oldValue.Length
            )
            .Insert(
                index,
                newValue
            );
    }
}

#endif
