#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using UnityEditor;
using UnityEngine;


public static class TelderStructureCaveFeatureInstaller
{
    private const string MenuPath =
        "Tools/Game/Apply Structure Selection + Cave Features";


    [MenuItem(MenuPath)]
    public static void Apply()
    {
        try
        {
            string editorPath =
                FindScriptPath(
                    "StructureEditorController"
                );


            string generationPath =
                FindScriptPath(
                    "StructureGenerationRuntime"
                );


            string worldGeneratorPath =
                FindScriptPath(
                    "WorldGenerator"
                );


            if (
                string.IsNullOrWhiteSpace(
                    editorPath
                )
                ||
                string.IsNullOrWhiteSpace(
                    generationPath
                )
                ||
                string.IsNullOrWhiteSpace(
                    worldGeneratorPath
                )
            )
            {
                Debug.LogError(
                    "TELDER: required source files not found.\n" +
                    "StructureEditorController: " +
                    editorPath +
                    "\nStructureGenerationRuntime: " +
                    generationPath +
                    "\nWorldGenerator: " +
                    worldGeneratorPath
                );

                return;
            }


            BackupOnce(
                editorPath,
                ".area_selection_backup"
            );


            BackupOnce(
                generationPath,
                ".cave_floor_backup"
            );


            BackupOnce(
                worldGeneratorPath,
                ".cave_vegetation_backup"
            );


            PatchStructureEditor(
                editorPath
            );


            PatchStructureGeneration(
                generationPath
            );


            PatchWorldGenerator(
                worldGeneratorPath
            );


            AssetDatabase.Refresh();


            Debug.Log(
                "TELDER: Structure selection + cave floor structures + cave vegetation applied."
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "TELDER FEATURE PATCH FAILED:\n" +
                exception
            );
        }
    }


    // =====================================================
    // STRUCTURE EDITOR: SHIFT AREA SELECTION / MOVE / DELETE
    // =====================================================

    private static void PatchStructureEditor(
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
                "TELDER_AREA_SELECTION_V1"
            )
        )
        {
            const string fieldAnchor =
@"        private int selectedCellY =
            -1;";


            const string fieldPatch =
@"        private int selectedCellY =
            -1;


        // TELDER_AREA_SELECTION_V1
        private bool areaSelectionActive;

        private bool areaSelecting;

        private bool areaMoving;

        private int areaStartX;

        private int areaStartY;

        private int areaEndX;

        private int areaEndY;

        private int moveStartX;

        private int moveStartY;

        private int moveOffsetX;

        private int moveOffsetY;";


            RequireReplace(
                ref source,
                fieldAnchor,
                fieldPatch,
                "Structure editor area-selection fields"
            );
        }


        source =
            ReplaceMethod(
                source,
                "private void DrawGrid()",
                DrawGridReplacement
            );


        if (
            !source.Contains(
                "private void DrawAreaSelectionOverlay()"
            )
        )
        {
            int insertion =
                source.IndexOf(
                    "private void DrawVisibleGridCells(",
                    StringComparison.Ordinal
                );


            if (insertion < 0)
            {
                throw new InvalidOperationException(
                    "DrawVisibleGridCells insertion point not found."
                );
            }


            source =
                source.Insert(
                    insertion,
                    AreaSelectionHelpers +
                    "\n\n        "
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
    // STRUCTURES: ACTUAL CAVE FLOOR
    // =====================================================

    private static void PatchStructureGeneration(
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


        source =
            ReplaceMethod(
                source,
                "private static bool TryGetCandidate(",
                TryGetCandidateReplacement
            );


        source =
            ReplaceMethod(
                source,
                "private static bool HasRequiredFreeSpace(",
                FreeSpaceReplacement
            );


        if (
            !source.Contains(
                "private static bool TryFindCaveFloorAnchor("
            )
        )
        {
            int insertion =
                source.IndexOf(
                    "private static int PickY(",
                    StringComparison.Ordinal
                );


            if (insertion < 0)
            {
                throw new InvalidOperationException(
                    "PickY insertion point not found."
                );
            }


            source =
                source.Insert(
                    insertion,
                    CaveFloorHelper +
                    "\n\n        "
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
    // WORLD GENERATOR: CAVE VEGETATION HOOK
    // =====================================================

    private static void PatchWorldGenerator(
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
            source.Contains(
                "CaveVegetationGenerationRuntime.ApplyToChunk"
            )
        )
        {
            return;
        }


        int methodIndex =
            source.IndexOf(
                "public ChunkData GenerateChunkData(",
                StringComparison.Ordinal
            );


        if (methodIndex < 0)
        {
            throw new InvalidOperationException(
                "WorldGenerator.GenerateChunkData not found."
            );
        }


        int braceStart =
            source.IndexOf(
                '{',
                methodIndex
            );


        int braceEnd =
            FindMatchingBrace(
                source,
                braceStart
            );


        string method =
            source.Substring(
                methodIndex,
                braceEnd -
                methodIndex +
                1
            );


        int returnIndex =
            method.LastIndexOf(
                "return data;",
                StringComparison.Ordinal
            );


        if (returnIndex < 0)
        {
            throw new InvalidOperationException(
                "return data; not found inside GenerateChunkData."
            );
        }


        const string hook =
@"Game.World.Vegetation.Caves
                .CaveVegetationGenerationRuntime
                .ApplyToChunk(
                    this,
                    settings,
                    data,
                    chunkX,
                    chunkY
                );


            return data;";


        method =
            method.Remove(
                returnIndex,
                "return data;".Length
            )
            .Insert(
                returnIndex,
                hook
            );


        source =
            source.Remove(
                methodIndex,
                braceEnd -
                methodIndex +
                1
            )
            .Insert(
                methodIndex,
                method
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
    // GENERIC SOURCE HELPERS
    // =====================================================

    private static string ReplaceMethod(
        string source,
        string signatureStart,
        string replacement
    )
    {
        int signatureIndex =
            source.IndexOf(
                signatureStart,
                StringComparison.Ordinal
            );


        if (signatureIndex < 0)
        {
            throw new InvalidOperationException(
                "Method not found: " +
                signatureStart
            );
        }


        int braceStart =
            source.IndexOf(
                '{',
                signatureIndex
            );


        if (braceStart < 0)
        {
            throw new InvalidOperationException(
                "Opening brace not found: " +
                signatureStart
            );
        }


        int braceEnd =
            FindMatchingBrace(
                source,
                braceStart
            );


        return
            source.Remove(
                signatureIndex,
                braceEnd -
                signatureIndex +
                1
            )
            .Insert(
                signatureIndex,
                replacement
            );
    }


    private static int FindMatchingBrace(
        string source,
        int braceStart
    )
    {
        int depth =
            0;

        bool inString =
            false;

        bool escape =
            false;


        for (
            int i = braceStart;
            i < source.Length;
            i++
        )
        {
            char c =
                source[i];


            if (inString)
            {
                if (escape)
                {
                    escape =
                        false;

                    continue;
                }


                if (c == '\\')
                {
                    escape =
                        true;

                    continue;
                }


                if (c == '"')
                {
                    inString =
                        false;
                }


                continue;
            }


            if (c == '"')
            {
                inString =
                    true;

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
                {
                    return i;
                }
            }
        }


        throw new InvalidOperationException(
            "Matching brace not found."
        );
    }


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


    // =====================================================
    // REPLACEMENT SOURCE: STRUCTURE EDITOR
    // =====================================================

    private const string DrawGridReplacement = @"
        private void DrawGrid()
        {
            float width =
                Mathf.Max(
                    100f,
                    Screen.width -
                    PaletteWidth -
                    SettingsWidth
                );


            Rect area =
                new Rect(
                    PaletteWidth,
                    54f,
                    width,
                    Mathf.Max(
                        1f,
                        Screen.height -
                        54f
                    )
                );


            GUI.Box(
                area,
                string.Empty
            );


            GUI.Label(
                new Rect(
                    area.x + 8f,
                    area.y + 7f,
                    area.width - 16f,
                    22f
                ),
                ""СТРУКТУРА | Shift+ЛКМ — выделить | ЛКМ по выделению — перенести | Delete — удалить""
            );


            Rect viewport =
                new Rect(
                    area.x + 8f,
                    area.y + 31f,
                    Mathf.Max(
                        10f,
                        area.width - 16f
                    ),
                    Mathf.Max(
                        10f,
                        area.height - 39f
                    )
                );


            Rect content =
                new Rect(
                    0f,
                    0f,
                    Mathf.Max(
                        1f,
                        current.Width *
                        CellSize
                    ),
                    Mathf.Max(
                        1f,
                        current.Height *
                        CellSize
                    )
                );


            Event evt =
                Event.current;


            Vector2 screenMouse =
                evt.mousePosition;


            if (
                evt.type ==
                EventType.KeyDown
                &&
                evt.keyCode ==
                KeyCode.Delete
                &&
                areaSelectionActive
                &&
                GUIUtility.keyboardControl ==
                0
            )
            {
                DeleteAreaSelection();

                evt.Use();
            }


            if (
                evt.type ==
                EventType.KeyDown
                &&
                evt.keyCode ==
                KeyCode.Escape
                &&
                areaSelectionActive
            )
            {
                ClearAreaSelection();

                evt.Use();
            }


            gridScroll =
                GUI.BeginScrollView(
                    viewport,
                    gridScroll,
                    content
                );


            if (
                evt.type ==
                EventType.Repaint
            )
            {
                DrawVisibleGridCells(
                    viewport
                );


                DrawAreaSelectionOverlay();
            }


            GUI.EndScrollView();


            bool mouseInside =
                viewport.Contains(
                    screenMouse
                );


            int mouseX =
                -1;


            int mouseY =
                -1;


            if (mouseInside)
            {
                float contentX =
                    screenMouse.x -
                    viewport.x +
                    gridScroll.x;


                float contentY =
                    screenMouse.y -
                    viewport.y +
                    gridScroll.y;


                mouseX =
                    Mathf.FloorToInt(
                        contentX /
                        CellSize
                    );


                int displayY =
                    Mathf.FloorToInt(
                        contentY /
                        CellSize
                    );


                mouseY =
                    current.Height -
                    1 -
                    displayY;


                if (
                    mouseX <
                    0
                    ||
                    mouseX >=
                    current.Width
                    ||
                    mouseY <
                    0
                    ||
                    mouseY >=
                    current.Height
                )
                {
                    mouseX =
                        -1;

                    mouseY =
                        -1;
                }
            }


            if (
                evt.type ==
                EventType.MouseDown
                &&
                evt.button ==
                0
                &&
                mouseX >=
                0
            )
            {
                if (evt.shift)
                {
                    BeginAreaSelection(
                        mouseX,
                        mouseY
                    );


                    evt.Use();

                    return;
                }


                if (
                    areaSelectionActive
                    &&
                    IsInsideAreaSelection(
                        mouseX,
                        mouseY
                    )
                )
                {
                    areaMoving =
                        true;


                    moveStartX =
                        mouseX;


                    moveStartY =
                        mouseY;


                    moveOffsetX =
                        0;


                    moveOffsetY =
                        0;


                    evt.Use();

                    return;
                }


                ClearAreaSelection();


                selectedCellX =
                    mouseX;


                selectedCellY =
                    mouseY;


                Place(
                    mouseX,
                    mouseY
                );


                evt.Use();

                return;
            }


            if (
                evt.type ==
                EventType.MouseDrag
                &&
                evt.button ==
                0
                &&
                mouseX >=
                0
            )
            {
                if (areaSelecting)
                {
                    UpdateAreaSelection(
                        mouseX,
                        mouseY
                    );


                    evt.Use();

                    return;
                }


                if (areaMoving)
                {
                    UpdateMoveOffset(
                        mouseX -
                        moveStartX,
                        mouseY -
                        moveStartY
                    );


                    evt.Use();

                    return;
                }


                selectedCellX =
                    mouseX;


                selectedCellY =
                    mouseY;


                Place(
                    mouseX,
                    mouseY
                );


                evt.Use();

                return;
            }


            if (
                evt.type ==
                EventType.MouseUp
                &&
                evt.button ==
                0
            )
            {
                if (areaSelecting)
                {
                    areaSelecting =
                        false;


                    areaSelectionActive =
                        true;


                    evt.Use();

                    return;
                }


                if (areaMoving)
                {
                    CommitAreaMove();


                    areaMoving =
                        false;


                    evt.Use();

                    return;
                }
            }


            if (
                (
                    evt.type ==
                    EventType.MouseDown
                    ||
                    evt.type ==
                    EventType.MouseDrag
                )
                &&
                evt.button ==
                1
                &&
                mouseX >=
                0
            )
            {
                selectedCellX =
                    mouseX;


                selectedCellY =
                    mouseY;


                Remove(
                    mouseX,
                    mouseY
                );


                evt.Use();
            }
        }";


    private const string AreaSelectionHelpers = @"
        private void BeginAreaSelection(
            int x,
            int y
        )
        {
            areaSelecting =
                true;


            areaMoving =
                false;


            areaSelectionActive =
                true;


            areaStartX =
                x;


            areaEndX =
                x;


            areaStartY =
                y;


            areaEndY =
                y;


            moveOffsetX =
                0;


            moveOffsetY =
                0;
        }


        private void UpdateAreaSelection(
            int x,
            int y
        )
        {
            areaEndX =
                Mathf.Clamp(
                    x,
                    0,
                    current.Width - 1
                );


            areaEndY =
                Mathf.Clamp(
                    y,
                    0,
                    current.Height - 1
                );
        }


        private void ClearAreaSelection()
        {
            areaSelectionActive =
                false;


            areaSelecting =
                false;


            areaMoving =
                false;


            moveOffsetX =
                0;


            moveOffsetY =
                0;
        }


        private bool IsInsideAreaSelection(
            int x,
            int y
        )
        {
            if (!areaSelectionActive)
                return false;


            int minX =
                Mathf.Min(
                    areaStartX,
                    areaEndX
                );


            int maxX =
                Mathf.Max(
                    areaStartX,
                    areaEndX
                );


            int minY =
                Mathf.Min(
                    areaStartY,
                    areaEndY
                );


            int maxY =
                Mathf.Max(
                    areaStartY,
                    areaEndY
                );


            return
                x >= minX
                &&
                x <= maxX
                &&
                y >= minY
                &&
                y <= maxY;
        }


        private void UpdateMoveOffset(
            int requestedX,
            int requestedY
        )
        {
            int minX =
                Mathf.Min(
                    areaStartX,
                    areaEndX
                );


            int maxX =
                Mathf.Max(
                    areaStartX,
                    areaEndX
                );


            int minY =
                Mathf.Min(
                    areaStartY,
                    areaEndY
                );


            int maxY =
                Mathf.Max(
                    areaStartY,
                    areaEndY
                );


            moveOffsetX =
                Mathf.Clamp(
                    requestedX,
                    -minX,
                    current.Width -
                    1 -
                    maxX
                );


            moveOffsetY =
                Mathf.Clamp(
                    requestedY,
                    -minY,
                    current.Height -
                    1 -
                    maxY
                );
        }


        private void DeleteAreaSelection()
        {
            if (
                !areaSelectionActive
                ||
                current.Cells ==
                null
            )
            {
                return;
            }


            for (
                int i =
                    current.Cells.Count -
                    1;
                i >=
                0;
                i--
            )
            {
                StructureCellDefinition cell =
                    current.Cells[i];


                if (
                    cell !=
                    null
                    &&
                    IsInsideAreaSelection(
                        cell.X,
                        cell.Y
                    )
                )
                {
                    current.Cells.RemoveAt(
                        i
                    );
                }
            }


            RebuildCellIndex();


            selectedCellX =
                -1;


            selectedCellY =
                -1;


            status =
                ""Выделенная область удалена"";


            ClearAreaSelection();
        }


        private void CommitAreaMove()
        {
            if (
                !areaSelectionActive
                ||
                current.Cells ==
                null
            )
            {
                return;
            }


            if (
                moveOffsetX ==
                0
                &&
                moveOffsetY ==
                0
            )
            {
                return;
            }


            List<StructureCellDefinition> moving =
                new List<StructureCellDefinition>();


            HashSet<int> movingKeys =
                new HashSet<int>();


            for (
                int i = 0;
                i < current.Cells.Count;
                i++
            )
            {
                StructureCellDefinition cell =
                    current.Cells[i];


                if (
                    cell !=
                    null
                    &&
                    IsInsideAreaSelection(
                        cell.X,
                        cell.Y
                    )
                )
                {
                    moving.Add(
                        cell
                    );


                    movingKeys.Add(
                        cell.X +
                        cell.Y *
                        current.Width
                    );
                }
            }


            HashSet<int> destinationKeys =
                new HashSet<int>();


            for (
                int i = 0;
                i < moving.Count;
                i++
            )
            {
                StructureCellDefinition cell =
                    moving[i];


                int targetX =
                    cell.X +
                    moveOffsetX;


                int targetY =
                    cell.Y +
                    moveOffsetY;


                destinationKeys.Add(
                    targetX +
                    targetY *
                    current.Width
                );
            }


            for (
                int i =
                    current.Cells.Count -
                    1;
                i >=
                0;
                i--
            )
            {
                StructureCellDefinition cell =
                    current.Cells[i];


                if (cell == null)
                    continue;


                int key =
                    cell.X +
                    cell.Y *
                    current.Width;


                if (
                    !movingKeys.Contains(
                        key
                    )
                    &&
                    destinationKeys.Contains(
                        key
                    )
                )
                {
                    current.Cells.RemoveAt(
                        i
                    );
                }
            }


            for (
                int i = 0;
                i < moving.Count;
                i++
            )
            {
                moving[i].X +=
                    moveOffsetX;


                moving[i].Y +=
                    moveOffsetY;
            }


            areaStartX +=
                moveOffsetX;


            areaEndX +=
                moveOffsetX;


            areaStartY +=
                moveOffsetY;


            areaEndY +=
                moveOffsetY;


            moveOffsetX =
                0;


            moveOffsetY =
                0;


            RebuildCellIndex();


            status =
                ""Выделенная область перемещена"";
        }


        private void DrawAreaSelectionOverlay()
        {
            if (!areaSelectionActive)
                return;


            int minX =
                Mathf.Min(
                    areaStartX,
                    areaEndX
                );


            int maxX =
                Mathf.Max(
                    areaStartX,
                    areaEndX
                );


            int minY =
                Mathf.Min(
                    areaStartY,
                    areaEndY
                );


            int maxY =
                Mathf.Max(
                    areaStartY,
                    areaEndY
                );


            if (areaMoving)
            {
                minX +=
                    moveOffsetX;


                maxX +=
                    moveOffsetX;


                minY +=
                    moveOffsetY;


                maxY +=
                    moveOffsetY;
            }


            int topDisplayY =
                current.Height -
                1 -
                maxY;


            Rect selectionRect =
                new Rect(
                    minX *
                    CellSize,
                    topDisplayY *
                    CellSize,
                    (
                        maxX -
                        minX +
                        1
                    )
                    *
                    CellSize,
                    (
                        maxY -
                        minY +
                        1
                    )
                    *
                    CellSize
                );


            Color old =
                GUI.color;


            GUI.color =
                areaMoving
                    ? new Color(
                        0.35f,
                        1f,
                        0.55f,
                        0.18f
                    )
                    : new Color(
                        0.30f,
                        0.65f,
                        1f,
                        0.18f
                    );


            GUI.DrawTexture(
                selectionRect,
                Texture2D.whiteTexture
            );


            GUI.color =
                areaMoving
                    ? new Color(
                        0.45f,
                        1f,
                        0.60f,
                        0.95f
                    )
                    : new Color(
                        0.45f,
                        0.75f,
                        1f,
                        0.95f
                    );


            const float border =
                2f;


            GUI.DrawTexture(
                new Rect(
                    selectionRect.x,
                    selectionRect.y,
                    selectionRect.width,
                    border
                ),
                Texture2D.whiteTexture
            );


            GUI.DrawTexture(
                new Rect(
                    selectionRect.x,
                    selectionRect.yMax -
                    border,
                    selectionRect.width,
                    border
                ),
                Texture2D.whiteTexture
            );


            GUI.DrawTexture(
                new Rect(
                    selectionRect.x,
                    selectionRect.y,
                    border,
                    selectionRect.height
                ),
                Texture2D.whiteTexture
            );


            GUI.DrawTexture(
                new Rect(
                    selectionRect.xMax -
                    border,
                    selectionRect.y,
                    border,
                    selectionRect.height
                ),
                Texture2D.whiteTexture
            );


            GUI.color =
                old;
        }";


    // =====================================================
    // REPLACEMENT SOURCE: CAVE STRUCTURES
    // =====================================================

    private const string TryGetCandidateReplacement = @"
        private static bool TryGetCandidate(
            WorldGenerator generator,
            WorldSettings settings,
            StructureDefinition structure,
            int regionX,
            out int anchorX,
            out int anchorY
        )
        {
            anchorX =
                0;


            anchorY =
                0;


            int seed =
                StableHash(
                    settings.Seed,
                    structure.ID,
                    regionX
                );


            System.Random random =
                new System.Random(
                    seed
                );


            if (
                random.NextDouble()
                >
                Mathf.Clamp01(
                    structure.SpawnChance
                )
            )
            {
                return false;
            }


            int regionSize =
                Mathf.Max(
                    8,
                    structure.RegionSize
                );


            anchorX =
                regionX *
                regionSize +
                random.Next(
                    0,
                    regionSize
                );


            int surface =
                generator.GetSurfaceHeight(
                    anchorX
                );


            switch (
                structure.SpawnType
            )
            {
                case StructureSpawnType.Surface:
                    anchorY =
                        surface +
                        1;

                    break;


                case StructureSpawnType.Any:
                    if (
                        random.NextDouble()
                        <
                        0.5
                    )
                    {
                        anchorY =
                            surface +
                            1;
                    }
                    else if (
                        !TryFindCaveFloorAnchor(
                            generator,
                            settings,
                            structure,
                            random,
                            anchorX,
                            surface,
                            out anchorY
                        )
                    )
                    {
                        anchorY =
                            surface +
                            1;
                    }

                    break;


                default:
                    if (
                        !TryFindCaveFloorAnchor(
                            generator,
                            settings,
                            structure,
                            random,
                            anchorX,
                            surface,
                            out anchorY
                        )
                    )
                    {
                        return false;
                    }

                    break;
            }


            if (structure.UseHeightRange)
            {
                int min =
                    Mathf.Min(
                        structure.MinY,
                        structure.MaxY
                    );


                int max =
                    Mathf.Max(
                        structure.MinY,
                        structure.MaxY
                    );


                if (
                    anchorY <
                    min
                    ||
                    anchorY >
                    max
                )
                {
                    return false;
                }
            }


            if (
                !BiomeAllowed(
                    generator,
                    settings,
                    structure,
                    anchorX,
                    anchorY
                )
            )
            {
                return false;
            }


            if (
                structure.RequireFreeSpace
                &&
                !HasRequiredFreeSpace(
                    generator,
                    structure,
                    anchorX,
                    anchorY
                )
            )
            {
                return false;
            }


            return true;
        }";


    private const string CaveFloorHelper = @"
        private static bool TryFindCaveFloorAnchor(
            WorldGenerator generator,
            WorldSettings settings,
            StructureDefinition structure,
            System.Random random,
            int worldX,
            int surface,
            out int anchorY
        )
        {
            anchorY =
                0;


            int minAnchor;


            int maxAnchor;


            if (structure.UseHeightRange)
            {
                minAnchor =
                    Mathf.Min(
                        structure.MinY,
                        structure.MaxY
                    );


                maxAnchor =
                    Mathf.Max(
                        structure.MinY,
                        structure.MaxY
                    );
            }
            else
            {
                minAnchor =
                    settings.BottomWorldY +
                    3 +
                    structure.OriginY;


                // Structure bottom must remain safely underground.
                maxAnchor =
                    surface -
                    3 +
                    structure.OriginY;
            }


            maxAnchor =
                Mathf.Min(
                    maxAnchor,
                    surface -
                    2 +
                    structure.OriginY
                );


            if (
                maxAnchor <
                minAnchor
            )
            {
                return false;
            }


            int count =
                maxAnchor -
                minAnchor +
                1;


            int start =
                random.Next(
                    0,
                    count
                );


            for (
                int step = 0;
                step < count;
                step++
            )
            {
                int candidateAnchor =
                    minAnchor +
                    (
                        start +
                        step
                    )
                    %
                    count;


                int structureBottom =
                    candidateAnchor -
                    structure.OriginY;


                ushort floorBlock =
                    GetBaseForegroundBlock(
                        generator,
                        worldX,
                        structureBottom -
                        1
                    );


                ushort airBlock =
                    GetBaseForegroundBlock(
                        generator,
                        worldX,
                        structureBottom
                    );


                if (
                    floorBlock !=
                    0
                    &&
                    airBlock ==
                    0
                )
                {
                    anchorY =
                        candidateAnchor;


                    return true;
                }
            }


            return false;
        }";


    private const string FreeSpaceReplacement = @"
        private static bool HasRequiredFreeSpace(
            WorldGenerator generator,
            StructureDefinition structure,
            int anchorX,
            int anchorY
        )
        {
            int padding =
                Mathf.Max(
                    0,
                    structure.FreeSpacePadding
                );


            int minX =
                anchorX -
                structure.OriginX -
                padding;


            int maxX =
                minX +
                structure.Width -
                1 +
                padding *
                2;


            int minY =
                anchorY -
                structure.OriginY -
                padding;


            int maxY =
                minY +
                structure.Height -
                1 +
                padding *
                2;


            int structureBottom =
                anchorY -
                structure.OriginY;


            bool sittingOnGround =
                GetBaseForegroundBlock(
                    generator,
                    anchorX,
                    structureBottom -
                    1
                )
                !=
                0
                &&
                GetBaseForegroundBlock(
                    generator,
                    anchorX,
                    structureBottom
                )
                ==
                0;


            for (
                int x = minX;
                x <= maxX;
                x++
            )
            {
                for (
                    int y = minY;
                    y <= maxY;
                    y++
                )
                {
                    // Ground below a surface OR cave-floor structure
                    // is support, not an obstruction.
                    if (
                        sittingOnGround
                        &&
                        y <
                        structureBottom
                    )
                    {
                        continue;
                    }


                    ushort baseBlock =
                        GetBaseForegroundBlock(
                            generator,
                            x,
                            y
                        );


                    if (baseBlock != 0)
                    {
                        return false;
                    }
                }
            }


            return true;
        }";
}

#endif
