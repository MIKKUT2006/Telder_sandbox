
using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;

using UnityEngine;


namespace Game.World.Structures.EditorRuntime
{
    public class StructureEditorController :
        MonoBehaviour
    {
        private StructureDefinition current =
            new StructureDefinition();


        private List<
            StructureEditorContentScanner.BlockInfo
        > blocks =
            new List<
                StructureEditorContentScanner.BlockInfo
            >();


        private readonly List<
            StructureEditorContentScanner.BlockInfo
        > filteredBlocks =
            new List<
                StructureEditorContentScanner.BlockInfo
            >();


        private List<
            StructureEditorContentScanner.LootContentInfo
        > lootContents =
            new List<
                StructureEditorContentScanner.LootContentInfo
            >();


        private readonly List<
            StructureEditorContentScanner.LootContentInfo
        > filteredLootContents =
            new List<
                StructureEditorContentScanner.LootContentInfo
            >();


        private readonly Dictionary<
            string,
            StructureEditorContentScanner.LootContentInfo
        > lootById =
            new Dictionary<
                string,
                StructureEditorContentScanner.LootContentInfo
            >(
                StringComparer.OrdinalIgnoreCase
            );


        private string lootPickerSearch =
            string.Empty;


        private int lootPickerEntryIndex =
            -1;


        private Vector2 lootPickerScroll;


        private const float LootPickerRowHeight =
            58f;


        // Numeric fields use text buffers and commit only on Enter.
        // This allows intermediate text such as "", "0,", "-".
        private readonly Dictionary<
            string,
            string
        > numericFieldBuffers =
            new Dictionary<
                string,
                string
            >();


        private readonly Dictionary<
            string,
            StructureEditorContentScanner.BlockInfo
        > blocksById =
            new Dictionary<
                string,
                StructureEditorContentScanner.BlockInfo
            >(
                StringComparer.OrdinalIgnoreCase
            );


        private List<string> surfaceBiomes =
            new List<string>();


        private List<string> caveBiomes =
            new List<string>();


        // O(1) cell lookup.
        //
        // Old editor did a linear scan through current.Cells for every
        // visible grid cell on every OnGUI pass.
        private readonly Dictionary<
            int,
            StructureCellDefinition
        > cellIndex =
            new Dictionary<
                int,
                StructureCellDefinition
            >();


        private string selectedBlockId;


        // false = foreground, true = background
        private bool editBackground;


        private int selectedCellX =
            -1;


        private int selectedCellY =
            -1;


        private Vector2 blockScroll;

        private Vector2 settingsScroll;

        private Vector2 gridScroll;

        private Vector2 lootScroll;


        private string blockSearch =
            string.Empty;


        private string status =
            string.Empty;


        private GUIStyle cellIdStyle;

        private GUIStyle backgroundBadgeStyle;


        private const float CellSize =
            34f;


        private const float PaletteWidth =
            270f;


        private const float SettingsWidth =
            410f;


        private const float PaletteRowHeight =
            62f;


        private void Start()
        {
            RefreshContent();

            NewStructure();
        }


        private void OnDestroy()
        {
            StructureEditorIconCache.Clear();

            StructureEditorLootIconCache.Clear();
        }


        private void Update()
        {
            if (
                Input.GetKeyDown(
                    KeyCode.F5
                )
            )
            {
                Save();
            }
        }


        private void OnGUI()
        {
            EnsureStyles();

            DrawTopBar();

            DrawBlockPalette();

            DrawGrid();

            DrawSettings();
        }


        // =====================================================
        // STYLES
        // =====================================================

        private void EnsureStyles()
        {
            if (
                cellIdStyle ==
                null
            )
            {
                cellIdStyle =
                    new GUIStyle(
                        GUI.skin.label
                    );


                cellIdStyle.fontSize =
                    8;


                cellIdStyle.alignment =
                    TextAnchor.LowerLeft;


                cellIdStyle.normal.textColor =
                    Color.white;
            }


            if (
                backgroundBadgeStyle ==
                null
            )
            {
                backgroundBadgeStyle =
                    new GUIStyle(
                        GUI.skin.label
                    );


                backgroundBadgeStyle.fontSize =
                    8;


                backgroundBadgeStyle.alignment =
                    TextAnchor.LowerRight;


                backgroundBadgeStyle.normal.textColor =
                    new Color(
                        0.55f,
                        0.75f,
                        1f,
                        1f
                    );
            }
        }


        // =====================================================
        // TOP BAR
        // =====================================================

        private void DrawTopBar()
        {
            GUILayout.BeginArea(
                new Rect(
                    0f,
                    0f,
                    Screen.width,
                    54f
                ),
                GUI.skin.box
            );


            GUILayout.BeginHorizontal();


            GUILayout.Label(
                "TELDER STRUCTURE EDITOR",
                GUILayout.Width(
                    220f
                )
            );


            if (
                GUILayout.Button(
                    "РќРѕРІР°СЏ",
                    GUILayout.Width(
                        90f
                    )
                )
            )
            {
                NewStructure();
            }


            if (
                GUILayout.Button(
                    "РЎРѕС…СЂР°РЅРёС‚СЊ [F5]",
                    GUILayout.Width(
                        130f
                    )
                )
            )
            {
                Save();
            }


            if (
                GUILayout.Button(
                    "РћР±РЅРѕРІРёС‚СЊ РєРѕРЅС‚РµРЅС‚",
                    GUILayout.Width(
                        140f
                    )
                )
            )
            {
                StructureEditorIconCache.Clear();

                StructureEditorLootIconCache.Clear();

                RefreshContent();
            }


            GUILayout.Space(
                10f
            );


            Color oldLayerColor =
                GUI.backgroundColor;


            GUI.backgroundColor =
                !editBackground
                    ? new Color(
                        0.35f,
                        0.65f,
                        0.95f
                    )
                    : oldLayerColor;


            if (
                GUILayout.Button(
                    "РџР•Р Р•Р”РќРР™",
                    GUILayout.Width(
                        110f
                    )
                )
            )
            {
                editBackground =
                    false;
            }


            GUI.backgroundColor =
                editBackground
                    ? new Color(
                        0.30f,
                        0.42f,
                        0.65f
                    )
                    : oldLayerColor;


            if (
                GUILayout.Button(
                    "Р—РђР”РќРР™",
                    GUILayout.Width(
                        110f
                    )
                )
            )
            {
                editBackground =
                    true;
            }


            GUI.backgroundColor =
                oldLayerColor;


            GUILayout.Space(
                10f
            );


            GUILayout.Label(
                status
            );


            GUILayout.FlexibleSpace();


            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }


        // =====================================================
        // BLOCK PALETTE
        // =====================================================

        private void DrawBlockPalette()
        {
            Rect area =
                new Rect(
                    0f,
                    54f,
                    PaletteWidth,
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
                    area.x +
                    8f,
                    area.y +
                    7f,
                    area.width -
                    16f,
                    22f
                ),
                "Р‘Р›РћРљР"
            );


            GUI.Label(
                new Rect(
                    area.x +
                    8f,
                    area.y +
                    28f,
                    area.width -
                    16f,
                    38f
                ),
                "Р›РљРњ вЂ” РїРѕСЃС‚Р°РІРёС‚СЊ | РџРљРњ вЂ” СѓРґР°Р»РёС‚СЊ"
            );


            string nextSearch =
                GUI.TextField(
                    new Rect(
                        area.x +
                        8f,
                        area.y +
                        67f,
                        area.width -
                        16f,
                        24f
                    ),
                    blockSearch ??
                    string.Empty
                );


            if (
                !string.Equals(
                    nextSearch,
                    blockSearch,
                    StringComparison.Ordinal
                )
            )
            {
                blockSearch =
                    nextSearch;


                RebuildFilteredBlocks();


                blockScroll =
                    Vector2.zero;
            }


            Rect viewport =
                new Rect(
                    area.x +
                    8f,
                    area.y +
                    97f,
                    area.width -
                    16f,
                    Mathf.Max(
                        10f,
                        area.height -
                        105f
                    )
                );


            float contentHeight =
                Mathf.Max(
                    viewport.height,
                    filteredBlocks.Count *
                    PaletteRowHeight
                );


            Rect content =
                new Rect(
                    0f,
                    0f,
                    Mathf.Max(
                        1f,
                        viewport.width -
                        18f
                    ),
                    contentHeight
                );


            blockScroll =
                GUI.BeginScrollView(
                    viewport,
                    blockScroll,
                    content
                );


            if (
                filteredBlocks.Count >
                0
            )
            {
                int first =
                    Mathf.Clamp(
                        Mathf.FloorToInt(
                            blockScroll.y /
                            PaletteRowHeight
                        )
                        -
                        1,
                        0,
                        filteredBlocks.Count -
                        1
                    );


                int last =
                    Mathf.Clamp(
                        Mathf.CeilToInt(
                            (
                                blockScroll.y +
                                viewport.height
                            )
                            /
                            PaletteRowHeight
                        )
                        +
                        1,
                        0,
                        filteredBlocks.Count -
                        1
                    );


                for (
                    int i = first;
                    i <= last;
                    i++
                )
                {
                    StructureEditorContentScanner.BlockInfo block =
                        filteredBlocks[i];


                    Rect buttonRect =
                        new Rect(
                            0f,
                            i *
                            PaletteRowHeight,
                            content.width,
                            PaletteRowHeight -
                            4f
                        );


                    Color old =
                        GUI.backgroundColor;


                    if (
                        string.Equals(
                            selectedBlockId,
                            block.ID,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        GUI.backgroundColor =
                            new Color(
                                0.35f,
                                0.55f,
                                0.90f
                            );
                    }


                    if (
                        GUI.Button(
                            buttonRect,
                            string.Empty
                        )
                    )
                    {
                        selectedBlockId =
                            block.ID;
                    }


                    GUI.backgroundColor =
                        old;


                    if (
                        Event.current.type ==
                        EventType.Repaint
                    )
                    {
                        Texture2D icon =
                            StructureEditorIconCache.Get(
                                block
                            );


                        if (
                            icon !=
                            null
                        )
                        {
                            GUI.DrawTexture(
                                new Rect(
                                    buttonRect.x +
                                    5f,
                                    buttonRect.y +
                                    5f,
                                    48f,
                                    48f
                                ),
                                icon,
                                ScaleMode.ScaleToFit,
                                true
                            );
                        }


                        GUI.Label(
                            new Rect(
                                buttonRect.x +
                                60f,
                                buttonRect.y +
                                3f,
                                buttonRect.width -
                                64f,
                                buttonRect.height -
                                6f
                            ),
                            block.DisplayLabel
                        );
                    }
                }
            }


            GUI.EndScrollView();
        }


        // =====================================================
        // GRID
        // =====================================================

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
                    area.x +
                    8f,
                    area.y +
                    7f,
                    area.width -
                    16f,
                    22f
                ),
                "РЎРўР РЈРљРўРЈР Рђ"
            );


            Rect viewport =
                new Rect(
                    area.x +
                    8f,
                    area.y +
                    31f,
                    Mathf.Max(
                        10f,
                        area.width -
                        16f
                    ),
                    Mathf.Max(
                        10f,
                        area.height -
                        39f
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
            }


            GUI.EndScrollView();


            if (
                (
                    evt.type ==
                    EventType.MouseDown
                    ||
                    evt.type ==
                    EventType.MouseDrag
                )
                &&
                (
                    evt.button ==
                    0
                    ||
                    evt.button ==
                    1
                )
                &&
                viewport.Contains(
                    screenMouse
                )
            )
            {
                float contentX =
                    screenMouse.x -
                    viewport.x +
                    gridScroll.x;


                float contentY =
                    screenMouse.y -
                    viewport.y +
                    gridScroll.y;


                int x =
                    Mathf.FloorToInt(
                        contentX /
                        CellSize
                    );


                int displayY =
                    Mathf.FloorToInt(
                        contentY /
                        CellSize
                    );


                int y =
                    current.Height -
                    1 -
                    displayY;


                if (
                    x >=
                    0
                    &&
                    x <
                    current.Width
                    &&
                    y >=
                    0
                    &&
                    y <
                    current.Height
                )
                {
                    selectedCellX =
                        x;


                    selectedCellY =
                        y;


                    if (
                        evt.button ==
                        0
                    )
                    {
                        Place(
                            x,
                            y
                        );
                    }
                    else
                    {
                        Remove(
                            x,
                            y
                        );
                    }


                    evt.Use();
                }
            }
        }


        private void DrawVisibleGridCells(
            Rect viewport
        )
        {
            int firstX =
                Mathf.Clamp(
                    Mathf.FloorToInt(
                        gridScroll.x /
                        CellSize
                    )
                    -
                    1,
                    0,
                    Mathf.Max(
                        0,
                        current.Width -
                        1
                    )
                );


            int lastX =
                Mathf.Clamp(
                    Mathf.CeilToInt(
                        (
                            gridScroll.x +
                            viewport.width
                        )
                        /
                        CellSize
                    )
                    +
                    1,
                    0,
                    Mathf.Max(
                        0,
                        current.Width -
                        1
                    )
                );


            int firstDisplayY =
                Mathf.Clamp(
                    Mathf.FloorToInt(
                        gridScroll.y /
                        CellSize
                    )
                    -
                    1,
                    0,
                    Mathf.Max(
                        0,
                        current.Height -
                        1
                    )
                );


            int lastDisplayY =
                Mathf.Clamp(
                    Mathf.CeilToInt(
                        (
                            gridScroll.y +
                            viewport.height
                        )
                        /
                        CellSize
                    )
                    +
                    1,
                    0,
                    Mathf.Max(
                        0,
                        current.Height -
                        1
                    )
                );


            for (
                int displayY = firstDisplayY;
                displayY <= lastDisplayY;
                displayY++
            )
            {
                int y =
                    current.Height -
                    1 -
                    displayY;


                for (
                    int x = firstX;
                    x <= lastX;
                    x++
                )
                {
                    Rect cellRect =
                        new Rect(
                            x *
                            CellSize,
                            displayY *
                            CellSize,
                            CellSize -
                            1f,
                            CellSize -
                            1f
                        );


                    StructureCellDefinition cell =
                        FindCell(
                            x,
                            y
                        );


                    Color old =
                        GUI.backgroundColor;


                    if (
                        x ==
                        current.OriginX
                        &&
                        y ==
                        current.OriginY
                    )
                    {
                        GUI.backgroundColor =
                            new Color(
                                0.30f,
                                0.45f,
                                0.25f
                            );
                    }
                    else if (
                        x ==
                        selectedCellX
                        &&
                        y ==
                        selectedCellY
                    )
                    {
                        GUI.backgroundColor =
                            new Color(
                                0.45f,
                                0.45f,
                                0.75f
                            );
                    }
                    else
                    {
                        GUI.backgroundColor =
                            new Color(
                                0.16f,
                                0.16f,
                                0.18f
                            );
                    }


                    GUI.Box(
                        cellRect,
                        string.Empty
                    );


                    GUI.backgroundColor =
                        old;


                    DrawCellVisual(
                        cellRect,
                        cell
                    );


                    if (
                        cell !=
                        null
                    )
                    {
                        string activeId =
                            editBackground
                                ? cell.BackgroundId
                                : cell.ForegroundId;


                        if (
                            !string.IsNullOrWhiteSpace(
                                activeId
                            )
                        )
                        {
                            GUI.Label(
                                cellRect,
                                ShortId(
                                    activeId
                                ),
                                cellIdStyle
                            );
                        }
                    }
                }
            }
        }


        private void DrawCellVisual(
            Rect cellRect,
            StructureCellDefinition cell
        )
        {
            if (
                cell ==
                null
            )
            {
                return;
            }


            if (
                !string.IsNullOrWhiteSpace(
                    cell.BackgroundId
                )
            )
            {
                Texture2D background =
                    StructureEditorIconCache.Get(
                        cell.BackgroundId
                    );


                if (
                    background !=
                    null
                )
                {
                    Color oldColor =
                        GUI.color;


                    GUI.color =
                        new Color(
                            0.82f,
                            0.82f,
                            0.82f,
                            1f
                        );


                    GUI.DrawTexture(
                        new Rect(
                            cellRect.x +
                            2f,
                            cellRect.y +
                            2f,
                            cellRect.width -
                            4f,
                            cellRect.height -
                            4f
                        ),
                        background,
                        ScaleMode.ScaleToFit,
                        true
                    );


                    GUI.color =
                        oldColor;
                }
            }


            if (
                !string.IsNullOrWhiteSpace(
                    cell.ForegroundId
                )
            )
            {
                Texture2D foreground =
                    StructureEditorIconCache.Get(
                        cell.ForegroundId
                    );


                if (
                    foreground !=
                    null
                )
                {
                    GUI.DrawTexture(
                        new Rect(
                            cellRect.x +
                            2f,
                            cellRect.y +
                            2f,
                            cellRect.width -
                            4f,
                            cellRect.height -
                            4f
                        ),
                        foreground,
                        ScaleMode.ScaleToFit,
                        true
                    );
                }
            }


            if (
                !string.IsNullOrWhiteSpace(
                    cell.BackgroundId
                )
            )
            {
                GUI.Label(
                    cellRect,
                    "BG",
                    backgroundBadgeStyle
                );
            }
        }


        // =====================================================
        // SETTINGS
        // =====================================================

        private void DrawSettings()
        {
            GUILayout.BeginArea(
                new Rect(
                    Screen.width -
                    SettingsWidth,
                    54f,
                    SettingsWidth,
                    Mathf.Max(
                        1f,
                        Screen.height -
                        54f
                    )
                ),
                GUI.skin.box
            );


            settingsScroll =
                GUILayout.BeginScrollView(
                    settingsScroll
                );


            GUILayout.Label(
                "РџРђР РђРњР•РўР Р«"
            );


            DrawExistingStructures();


            GUILayout.Space(
                8f
            );


            current.ID =
                LabeledText(
                    "ID",
                    current.ID
                );


            current.DisplayName =
                LabeledText(
                    "РРјСЏ",
                    current.DisplayName
                );


            GUILayout.Label(
                "Р Р°Р·РјРµСЂ"
            );


            current.Width =
                BufferedIntField(
                    "structure.width",
                    "Width",
                    current.Width,
                    1,
                    128
                );


            current.Height =
                BufferedIntField(
                    "structure.height",
                    "Height",
                    current.Height,
                    1,
                    128
                );


            current.OriginX =
                BufferedIntField(
                    "structure.originX",
                    "Origin X",
                    current.OriginX,
                    0,
                    Mathf.Max(
                        0,
                        current.Width -
                        1
                    )
                );


            current.OriginY =
                BufferedIntField(
                    "structure.originY",
                    "Origin Y",
                    current.OriginY,
                    0,
                    Mathf.Max(
                        0,
                        current.Height -
                        1
                    )
                );


            GUILayout.Space(
                8f
            );


            GUILayout.Label(
                "РўРёРї СЃС‚СЂСѓРєС‚СѓСЂС‹"
            );


            current.Type =
                (StructureType)
                GUILayout.SelectionGrid(
                    (int)
                    current.Type,

                    new[]
                    {
                        "РћР‘Р«Р§РќРђРЇ",
                        "Р”Р•Р Р•Р’Рћ"
                    },

                    1
                );


            if (
                current.Type ==
                StructureType.Tree
            )
            {
                GUILayout.Label(
                    "Р”Р»СЏ Р”Р•Р Р•Р’Рђ Origin X = РєРѕР»РѕРЅРЅР° СЃС‚РІРѕР»Р°."
                );
            }


            GUILayout.Space(
                8f
            );


            GUILayout.Label(
                "РўРёРї РїРѕСЏРІР»РµРЅРёСЏ"
            );


            current.SpawnType =
                (StructureSpawnType)
                GUILayout.SelectionGrid(
                    (int)
                    current.SpawnType,

                    new[]
                    {
                        "РџР•Р©Р•Р Рђ",
                        "РџРћР’Р•Р РҐРќРћРЎРўР¬",
                        "Р›Р®Р‘РћР™"
                    },

                    1
                );


            GUILayout.Space(
                8f
            );


            GUILayout.Label(
                "Р¤РёР»СЊС‚СЂ Р±РёРѕРјР°"
            );


            StructureBiomeSource oldBiomeSource =
                current.BiomeSource;


            current.BiomeSource =
                (StructureBiomeSource)
                GUILayout.SelectionGrid(
                    (int)
                    current.BiomeSource,

                    new[]
                    {
                        "РџРћР’Р•Р РҐРќРћРЎРўРќР«Р™",
                        "РџР•Р©Р•Р РќР«Р™",
                        "Р›Р®Р‘РћР™"
                    },

                    1
                );


            if (
                current.BiomeSource !=
                oldBiomeSource
            )
            {
                if (
                    current.Biomes ==
                    null
                )
                {
                    current.Biomes =
                        new List<string>();
                }


                current.Biomes.Clear();


                if (
                    current.BiomeSource ==
                    StructureBiomeSource.Cave
                    &&
                    current.SpawnType ==
                    StructureSpawnType.Surface
                )
                {
                    current.SpawnType =
                        StructureSpawnType.Underground;
                }
            }


            if (
                current.BiomeSource ==
                StructureBiomeSource.Cave
                &&
                current.SpawnType ==
                StructureSpawnType.Surface
            )
            {
                GUILayout.Label(
                    "РџРµС‰РµСЂРЅС‹Р№ Р±РёРѕРј РЅРµ РёСЃРїРѕР»СЊР·СѓРµС‚СЃСЏ РЅР° РїРѕРІРµСЂС…РЅРѕСЃС‚Рё. " +
                    "Р’С‹Р±РµСЂРё РџР•Р©Р•Р Рђ РёР»Рё Р›Р®Р‘РћР™."
                );
            }


            current.SpawnChance =
                BufferedFloatField(
                    "structure.spawnChance",
                    "РЁР°РЅСЃ 0..1",
                    current.SpawnChance,
                    0f,
                    1f
                );


            current.RegionSize =
                BufferedIntField(
                    "structure.regionSize",
                    "Region size",
                    current.RegionSize,
                    8,
                    4096
                );


            current.RequireFreeSpace =
                GUILayout.Toggle(
                    current.RequireFreeSpace,
                    "РСЃРєР°С‚СЊ СЃРІРѕР±РѕРґРЅРѕРµ РїСЂРѕСЃС‚СЂР°РЅСЃС‚РІРѕ"
                );


            current.FreeSpacePadding =
                BufferedIntField(
                    "structure.freeSpacePadding",
                    "РћС‚СЃС‚СѓРї РІРѕР·РґСѓС…Р°",
                    current.FreeSpacePadding,
                    0,
                    64
                );


            current.UseHeightRange =
                GUILayout.Toggle(
                    current.UseHeightRange,
                    "РћРіСЂР°РЅРёС‡РёС‚СЊ РІС‹СЃРѕС‚Сѓ"
                );


            if (
                current.UseHeightRange
            )
            {
                current.MinY =
                    BufferedIntField(
                        "structure.minY",
                        "Min Y",
                        current.MinY,
                        -1000000,
                        1000000
                    );


                current.MaxY =
                    BufferedIntField(
                        "structure.maxY",
                        "Max Y",
                        current.MaxY,
                        -1000000,
                        1000000
                    );
            }


            GUILayout.Space(
                8f
            );


            DrawBiomes();


            GUILayout.Space(
                10f
            );


            DrawSelectedCell();


            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }


        private void DrawExistingStructures()
        {
            GUILayout.Label(
                "РЎРЈР©Р•РЎРўР’РЈР®Р©РР•"
            );


            IReadOnlyList<
                StructureDefinition
            > structures =
                StructureRegistry.GetAll();


            for (
                int i = 0;
                i < structures.Count;
                i++
            )
            {
                StructureDefinition structure =
                    structures[i];


                if (
                    structure ==
                    null
                )
                {
                    continue;
                }


                if (
                    GUILayout.Button(
                        "Р—Р°РіСЂСѓР·РёС‚СЊ: " +
                        structure.DisplayName
                    )
                )
                {
                    LoadStructure(
                        structure
                    );
                }
            }
        }


        private void LoadStructure(
            StructureDefinition structure
        )
        {
            string fileName =
                structure.ID
                    .Replace(
                        ":",
                        "_"
                    )
                    .Replace(
                        "/",
                        "_"
                    )
                    .Replace(
                        "\\",
                        "_"
                    )
                +
                ".json";


            string file =
                Path.Combine(
                    StructurePaths.Folder,
                    fileName
                );


            if (
                !File.Exists(
                    file
                )
            )
            {
                return;
            }


            StructureDefinition loaded =
                JsonUtility.FromJson<
                    StructureDefinition
                >(
                    File.ReadAllText(
                        file
                    )
                );


            if (
                loaded ==
                null
            )
            {
                return;
            }


            current =
                loaded;


            numericFieldBuffers.Clear();


            lootPickerEntryIndex =
                -1;


            NormalizeCurrent();

            RebuildCellIndex();


            selectedCellX =
                -1;


            selectedCellY =
                -1;


            gridScroll =
                Vector2.zero;


            status =
                "Р—Р°РіСЂСѓР¶РµРЅРѕ: " +
                current.ID;
        }


        private void DrawBiomes()
        {
            GUILayout.Label(
                "Р‘РРћРњР« (РЅРёС‡РµРіРѕ = РІСЃРµ)"
            );


            if (
                current.Biomes ==
                null
            )
            {
                current.Biomes =
                    new List<string>();
            }


            if (
                current.BiomeSource ==
                StructureBiomeSource.Surface
            )
            {
                DrawBiomeList(
                    "РџРѕРІРµСЂС…РЅРѕСЃС‚РЅС‹Рµ",
                    surfaceBiomes
                );


                return;
            }


            if (
                current.BiomeSource ==
                StructureBiomeSource.Cave
            )
            {
                DrawBiomeList(
                    "РџРµС‰РµСЂРЅС‹Рµ",
                    caveBiomes
                );


                return;
            }


            DrawBiomeList(
                "РџРѕРІРµСЂС…РЅРѕСЃС‚РЅС‹Рµ",
                surfaceBiomes
            );


            GUILayout.Space(
                6f
            );


            DrawBiomeList(
                "РџРµС‰РµСЂРЅС‹Рµ",
                caveBiomes
            );
        }


        private void DrawBiomeList(
            string title,
            List<string> list
        )
        {
            GUILayout.Label(
                title +
                " (" +
                (
                    list !=
                    null
                        ? list.Count
                        : 0
                ) +
                ")"
            );


            if (
                list ==
                null
                ||
                list.Count ==
                0
            )
            {
                GUILayout.Label(
                    "РќРµС‚ РґРѕСЃС‚СѓРїРЅС‹С… Р±РёРѕРјРѕРІ."
                );


                return;
            }


            for (
                int i = 0;
                i < list.Count;
                i++
            )
            {
                string biome =
                    list[i];


                bool enabled =
                    current.Biomes.Contains(
                        biome
                    );


                bool next =
                    GUILayout.Toggle(
                        enabled,
                        biome
                    );


                if (
                    next
                    &&
                    !enabled
                )
                {
                    current.Biomes.Add(
                        biome
                    );
                }
                else if (
                    !next
                    &&
                    enabled
                )
                {
                    current.Biomes.Remove(
                        biome
                    );
                }
            }
        }


        // =====================================================
        // SELECTED CELL / LOOT
        // =====================================================

        private void DrawSelectedCell()
        {
            GUILayout.Label(
                "Р’Р«Р‘Р РђРќРќРђРЇ РљР›Р•РўРљРђ"
            );


            if (
                selectedCellX <
                0
                ||
                selectedCellY <
                0
            )
            {
                GUILayout.Label(
                    "Р’С‹Р±РµСЂРё РєР»РµС‚РєСѓ."
                );


                return;
            }


            GUILayout.Label(
                selectedCellX +
                ", " +
                selectedCellY
            );


            StructureCellDefinition cell =
                FindCell(
                    selectedCellX,
                    selectedCellY
                );


            if (
                cell ==
                null
            )
            {
                GUILayout.Label(
                    "РџСѓСЃС‚Рѕ"
                );


                return;
            }


            GUILayout.Label(
                "Foreground: " +
                (
                    string.IsNullOrWhiteSpace(
                        cell.ForegroundId
                    )
                        ? "-"
                        : cell.ForegroundId
                )
            );


            GUILayout.Label(
                "Background: " +
                (
                    string.IsNullOrWhiteSpace(
                        cell.BackgroundId
                    )
                        ? "-"
                        : cell.BackgroundId
                )
            );


            GUILayout.Label(
                "РђРєС‚РёРІРЅС‹Р№ СЃР»РѕР№: " +
                (
                    editBackground
                        ? "Р—РђР”РќРР™"
                        : "РџР•Р Р•Р”РќРР™"
                )
            );


            if (
                !IsChestId(
                    cell.ForegroundId
                )
            )
            {
                GUILayout.Label(
                    "Loot РґРѕСЃС‚СѓРїРµРЅ С‚РѕР»СЊРєРѕ РґР»СЏ chest."
                );


                return;
            }


            GUILayout.Label(
                "LOOT TABLE"
            );


            if (
                cell.Loot ==
                null
            )
            {
                cell.Loot =
                    new List<
                        StructureLootEntryDefinition
                    >();
            }


            lootScroll =
                GUILayout.BeginScrollView(
                    lootScroll,
                    GUILayout.Height(
                        220f
                    )
                );


            for (
                int i = 0;
                i < cell.Loot.Count;
                i++
            )
            {
                StructureLootEntryDefinition entry =
                    cell.Loot[i];


                GUILayout.BeginVertical(
                    GUI.skin.box
                );


                DrawLootSelectedContent(
                    entry,
                    i
                );


                entry.Chance =
                    BufferedFloatField(
                        "loot." +
                        i +
                        ".chance",
                        "Chance",
                        entry.Chance,
                        0f,
                        1f
                    );


                entry.MinCount =
                    BufferedIntField(
                        "loot." +
                        i +
                        ".min",
                        "Min",
                        entry.MinCount,
                        1,
                        9999
                    );


                entry.MaxCount =
                    BufferedIntField(
                        "loot." +
                        i +
                        ".max",
                        "Max",
                        entry.MaxCount,
                        entry.MinCount,
                        9999
                    );


                if (
                    lootPickerEntryIndex ==
                    i
                )
                {
                    DrawLootPicker(
                        entry
                    );
                }


                if (
                    GUILayout.Button(
                        "РЈРґР°Р»РёС‚СЊ loot"
                    )
                )
                {
                    cell.Loot.RemoveAt(
                        i
                    );


                    lootPickerEntryIndex =
                        -1;


                    numericFieldBuffers.Clear();


                    i--;


                    GUILayout.EndVertical();


                    continue;
                }


                GUILayout.EndVertical();
            }


            GUILayout.EndScrollView();


            if (
                GUILayout.Button(
                    "+ Р”РѕР±Р°РІРёС‚СЊ loot"
                )
            )
            {
                cell.Loot.Add(
                    new StructureLootEntryDefinition
                    {
                        ItemId =
                            lootContents.Count >
                            0
                                ? lootContents[
                                    0
                                ].ID
                                : "game:stone",

                        Chance =
                            1f,

                        MinCount =
                            1,

                        MaxCount =
                            1
                    }
                );
            }
        }


        private void DrawLootSelectedContent(
            StructureLootEntryDefinition entry,
            int entryIndex
        )
        {
            GUILayout.BeginHorizontal();


            Texture2D icon =
                null;


            string label =
                string.IsNullOrWhiteSpace(
                    entry.ItemId
                )
                    ? "РќРµ РІС‹Р±СЂР°РЅ"
                    : entry.ItemId;


            if (
                !string.IsNullOrWhiteSpace(
                    entry.ItemId
                )
                &&
                lootById.TryGetValue(
                    entry.ItemId,
                    out StructureEditorContentScanner.LootContentInfo info
                )
            )
            {
                icon =
                    StructureEditorLootIconCache.Get(
                        info
                    );


                label =
                    info.DisplayLabel;
            }


            Rect iconRect =
                GUILayoutUtility.GetRect(
                    48f,
                    48f,
                    GUILayout.Width(
                        48f
                    ),
                    GUILayout.Height(
                        48f
                    )
                );


            GUI.Box(
                iconRect,
                string.Empty
            );


            if (
                icon !=
                null
            )
            {
                GUI.DrawTexture(
                    new Rect(
                        iconRect.x +
                        3f,
                        iconRect.y +
                        3f,
                        iconRect.width -
                        6f,
                        iconRect.height -
                        6f
                    ),
                    icon,
                    ScaleMode.ScaleToFit,
                    true
                );
            }


            GUILayout.Label(
                label,
                GUILayout.MinHeight(
                    48f
                )
            );


            if (
                GUILayout.Button(
                    lootPickerEntryIndex ==
                    entryIndex
                        ? "Р—Р°РєСЂС‹С‚СЊ"
                        : "Р’С‹Р±СЂР°С‚СЊ",
                    GUILayout.Width(
                        78f
                    ),
                    GUILayout.Height(
                        38f
                    )
                )
            )
            {
                lootPickerEntryIndex =
                    lootPickerEntryIndex ==
                    entryIndex
                        ? -1
                        : entryIndex;


                lootPickerScroll =
                    Vector2.zero;
            }


            GUILayout.EndHorizontal();
        }


        private void DrawLootPicker(
            StructureLootEntryDefinition entry
        )
        {
            GUILayout.BeginVertical(
                GUI.skin.box
            );


            GUILayout.Label(
                "Р’С‹Р±РѕСЂ item / block"
            );


            string nextSearch =
                GUILayout.TextField(
                    lootPickerSearch ??
                    string.Empty
                );


            if (
                !string.Equals(
                    nextSearch,
                    lootPickerSearch,
                    StringComparison.Ordinal
                )
            )
            {
                lootPickerSearch =
                    nextSearch;


                RebuildFilteredLootContents();


                lootPickerScroll =
                    Vector2.zero;
            }


            float viewportHeight =
                220f;


            Rect viewport =
                GUILayoutUtility.GetRect(
                    10f,
                    viewportHeight,
                    GUILayout.ExpandWidth(
                        true
                    )
                );


            float contentHeight =
                Mathf.Max(
                    viewport.height,
                    filteredLootContents.Count *
                    LootPickerRowHeight
                );


            Rect content =
                new Rect(
                    0f,
                    0f,
                    Mathf.Max(
                        1f,
                        viewport.width -
                        18f
                    ),
                    contentHeight
                );


            lootPickerScroll =
                GUI.BeginScrollView(
                    viewport,
                    lootPickerScroll,
                    content
                );


            if (
                filteredLootContents.Count >
                0
            )
            {
                int first =
                    Mathf.Clamp(
                        Mathf.FloorToInt(
                            lootPickerScroll.y /
                            LootPickerRowHeight
                        )
                        -
                        1,
                        0,
                        filteredLootContents.Count -
                        1
                    );


                int last =
                    Mathf.Clamp(
                        Mathf.CeilToInt(
                            (
                                lootPickerScroll.y +
                                viewport.height
                            )
                            /
                            LootPickerRowHeight
                        )
                        +
                        1,
                        0,
                        filteredLootContents.Count -
                        1
                    );


                for (
                    int i = first;
                    i <= last;
                    i++
                )
                {
                    StructureEditorContentScanner.LootContentInfo info =
                        filteredLootContents[i];


                    Rect row =
                        new Rect(
                            0f,
                            i *
                            LootPickerRowHeight,
                            content.width,
                            LootPickerRowHeight -
                            3f
                        );


                    if (
                        GUI.Button(
                            row,
                            string.Empty
                        )
                    )
                    {
                        entry.ItemId =
                            info.ID;


                        lootPickerEntryIndex =
                            -1;


                        GUI.EndScrollView();

                        GUILayout.EndVertical();

                        return;
                    }


                    Texture2D icon =
                        StructureEditorLootIconCache.Get(
                            info
                        );


                    if (
                        icon !=
                        null
                    )
                    {
                        GUI.DrawTexture(
                            new Rect(
                                row.x +
                                4f,
                                row.y +
                                4f,
                                46f,
                                46f
                            ),
                            icon,
                            ScaleMode.ScaleToFit,
                            true
                        );
                    }


                    GUI.Label(
                        new Rect(
                            row.x +
                            58f,
                            row.y +
                            3f,
                            row.width -
                            62f,
                            row.height -
                            6f
                        ),
                        info.DisplayLabel
                    );
                }
            }


            GUI.EndScrollView();


            GUILayout.EndVertical();
        }


        private void RebuildFilteredLootContents()
        {
            filteredLootContents.Clear();


            string search =
                (
                    lootPickerSearch ??
                    string.Empty
                ).Trim();


            if (
                string.IsNullOrWhiteSpace(
                    search
                )
            )
            {
                filteredLootContents.AddRange(
                    lootContents
                );


                return;
            }


            for (
                int i = 0;
                i < lootContents.Count;
                i++
            )
            {
                StructureEditorContentScanner.LootContentInfo info =
                    lootContents[i];


                if (
                    info ==
                    null
                )
                {
                    continue;
                }


                if (
                    ContainsIgnoreCase(
                        info.ID,
                        search
                    )
                    ||
                    ContainsIgnoreCase(
                        info.Name,
                        search
                    )
                )
                {
                    filteredLootContents.Add(
                        info
                    );
                }
            }
        }


        // =====================================================
        // DATA / INDEX
        // =====================================================

        private void NewStructure()
        {
            current =
                new StructureDefinition();


            numericFieldBuffers.Clear();


            lootPickerEntryIndex =
                -1;


            NormalizeCurrent();

            RebuildCellIndex();


            selectedCellX =
                -1;


            selectedCellY =
                -1;


            gridScroll =
                Vector2.zero;


            status =
                "РќРѕРІР°СЏ СЃС‚СЂСѓРєС‚СѓСЂР°";
        }


        private void NormalizeCurrent()
        {
            if (
                current ==
                null
            )
            {
                current =
                    new StructureDefinition();
            }


            if (
                current.Cells ==
                null
            )
            {
                current.Cells =
                    new List<
                        StructureCellDefinition
                    >();
            }


            if (
                current.Biomes ==
                null
            )
            {
                current.Biomes =
                    new List<string>();
            }


            current.Width =
                Mathf.Max(
                    1,
                    current.Width
                );


            current.Height =
                Mathf.Max(
                    1,
                    current.Height
                );
        }


        private void RefreshContent()
        {
            blocks =
                StructureEditorContentScanner
                    .LoadBlocks();


            lootContents =
                StructureEditorContentScanner
                    .LoadLootContents(
                        blocks
                    );


            lootById.Clear();


            for (
                int i = 0;
                i < lootContents.Count;
                i++
            )
            {
                StructureEditorContentScanner.LootContentInfo loot =
                    lootContents[i];


                if (
                    loot ==
                    null
                    ||
                    string.IsNullOrWhiteSpace(
                        loot.ID
                    )
                )
                {
                    continue;
                }


                lootById[
                    loot.ID
                ] =
                    loot;
            }


            RebuildFilteredLootContents();


            surfaceBiomes =
                StructureEditorContentScanner
                    .LoadSurfaceBiomeIds();


            caveBiomes =
                StructureEditorContentScanner
                    .LoadCaveBiomeIds();


            blocksById.Clear();


            for (
                int i = 0;
                i < blocks.Count;
                i++
            )
            {
                StructureEditorContentScanner.BlockInfo block =
                    blocks[i];


                if (
                    block ==
                    null
                    ||
                    string.IsNullOrWhiteSpace(
                        block.ID
                    )
                )
                {
                    continue;
                }


                blocksById[
                    block.ID
                ] =
                    block;
            }


            StructureEditorIconCache.Configure(
                blocks
            );


            RebuildFilteredBlocks();


            if (
                blocks.Count >
                0
                &&
                (
                    string.IsNullOrWhiteSpace(
                        selectedBlockId
                    )
                    ||
                    !blocksById.ContainsKey(
                        selectedBlockId
                    )
                )
            )
            {
                selectedBlockId =
                    blocks[
                        0
                    ].ID;
            }


            status =
                "Blocks: " +
                blocks.Count +
                " | Loot entries: " +
                lootContents.Count +
                " | Surface biomes: " +
                surfaceBiomes.Count +
                " | Cave biomes: " +
                caveBiomes.Count;
        }


        private void RebuildFilteredBlocks()
        {
            filteredBlocks.Clear();


            string search =
                (
                    blockSearch ??
                    string.Empty
                ).Trim();


            if (
                string.IsNullOrWhiteSpace(
                    search
                )
            )
            {
                filteredBlocks.AddRange(
                    blocks
                );


                return;
            }


            for (
                int i = 0;
                i < blocks.Count;
                i++
            )
            {
                StructureEditorContentScanner.BlockInfo block =
                    blocks[i];


                if (
                    block ==
                    null
                )
                {
                    continue;
                }


                if (
                    ContainsIgnoreCase(
                        block.ID,
                        search
                    )
                    ||
                    ContainsIgnoreCase(
                        block.Name,
                        search
                    )
                )
                {
                    filteredBlocks.Add(
                        block
                    );
                }
            }
        }


        private static bool ContainsIgnoreCase(
            string text,
            string search
        )
        {
            return
                !string.IsNullOrEmpty(
                    text
                )
                &&
                text.IndexOf(
                    search,
                    StringComparison.OrdinalIgnoreCase
                ) >=
                0;
        }


        private void RebuildCellIndex()
        {
            cellIndex.Clear();


            if (
                current ==
                null
                ||
                current.Cells ==
                null
            )
            {
                return;
            }


            for (
                int i = 0;
                i < current.Cells.Count;
                i++
            )
            {
                StructureCellDefinition cell =
                    current.Cells[i];


                if (
                    cell ==
                    null
                )
                {
                    continue;
                }


                cellIndex[
                    CellKey(
                        cell.X,
                        cell.Y
                    )
                ] =
                    cell;
            }
        }


        private void Place(
            int x,
            int y
        )
        {
            if (
                string.IsNullOrWhiteSpace(
                    selectedBlockId
                )
            )
            {
                return;
            }


            int key =
                CellKey(
                    x,
                    y
                );


            if (
                !cellIndex.TryGetValue(
                    key,
                    out StructureCellDefinition cell
                )
            )
            {
                cell =
                    new StructureCellDefinition
                    {
                        X =
                            x,

                        Y =
                            y,

                        Loot =
                            new List<
                                StructureLootEntryDefinition
                            >()
                    };


                current.Cells.Add(
                    cell
                );


                cellIndex.Add(
                    key,
                    cell
                );
            }


            if (
                editBackground
            )
            {
                cell.BackgroundId =
                    selectedBlockId;
            }
            else
            {
                cell.ForegroundId =
                    selectedBlockId;
            }


            if (
                cell.Loot ==
                null
            )
            {
                cell.Loot =
                    new List<
                        StructureLootEntryDefinition
                    >();
            }
        }


        private void Remove(
            int x,
            int y
        )
        {
            int key =
                CellKey(
                    x,
                    y
                );


            if (
                !cellIndex.TryGetValue(
                    key,
                    out StructureCellDefinition cell
                )
            )
            {
                return;
            }


            if (
                editBackground
            )
            {
                cell.BackgroundId =
                    string.Empty;
            }
            else
            {
                cell.ForegroundId =
                    string.Empty;


                if (
                    cell.Loot !=
                    null
                )
                {
                    cell.Loot.Clear();
                }
            }


            if (
                string.IsNullOrWhiteSpace(
                    cell.ForegroundId
                )
                &&
                string.IsNullOrWhiteSpace(
                    cell.BackgroundId
                )
            )
            {
                current.Cells.Remove(
                    cell
                );


                cellIndex.Remove(
                    key
                );
            }
        }


        private StructureCellDefinition FindCell(
            int x,
            int y
        )
        {
            cellIndex.TryGetValue(
                CellKey(
                    x,
                    y
                ),
                out StructureCellDefinition cell
            );


            return cell;
        }


        private static int CellKey(
            int x,
            int y
        )
        {
            return
                (
                    x &
                    0xFFFF
                )
                |
                (
                    y <<
                    16
                );
        }


        private bool IsChestId(
            string id
        )
        {
            return
                !string.IsNullOrWhiteSpace(
                    id
                )
                &&
                blocksById.TryGetValue(
                    id,
                    out StructureEditorContentScanner.BlockInfo block
                )
                &&
                block !=
                null
                &&
                block.IsChest;
        }


        // =====================================================
        // SAVE
        // =====================================================

        private void Save()
        {
            if (
                string.IsNullOrWhiteSpace(
                    current.ID
                )
            )
            {
                status =
                    "ERROR: ID РїСѓСЃС‚РѕР№";


                return;
            }


            string safe =
                current.ID
                    .Replace(
                        ":",
                        "_"
                    )
                    .Replace(
                        "/",
                        "_"
                    )
                    .Replace(
                        "\\",
                        "_"
                    );


            string file =
                Path.Combine(
                    StructurePaths.Folder,
                    safe +
                    ".json"
                );


            File.WriteAllText(
                file,
                JsonUtility.ToJson(
                    current,
                    true
                )
            );


            StructureRegistry.Reload();


            status =
                "РЎРѕС…СЂР°РЅРµРЅРѕ: " +
                file;
        }


        // =====================================================
        // FIELD HELPERS
        // =====================================================

        private string LabeledText(
            string label,
            string value
        )
        {
            GUILayout.BeginHorizontal();


            GUILayout.Label(
                label,
                GUILayout.Width(
                    120f
                )
            );


            value =
                GUILayout.TextField(
                    value ??
                    string.Empty
                );


            GUILayout.EndHorizontal();


            return value;
        }


        private int BufferedIntField(
            string key,
            string label,
            int value,
            int min,
            int max
        )
        {
            GUILayout.BeginHorizontal();


            GUILayout.Label(
                label,
                GUILayout.Width(
                    120f
                )
            );


            string bufferKey =
                "int:" +
                key;


            if (
                !numericFieldBuffers.TryGetValue(
                    bufferKey,
                    out string text
                )
            )
            {
                text =
                    value.ToString(
                        CultureInfo.InvariantCulture
                    );


                numericFieldBuffers[
                    bufferKey
                ] =
                    text;
            }


            string controlName =
                "StructureNumeric_" +
                bufferKey;


            GUI.SetNextControlName(
                controlName
            );


            string next =
                GUILayout.TextField(
                    text
                );


            numericFieldBuffers[
                bufferKey
            ] =
                next;


            bool commit =
                IsEnterPressedForControl(
                    controlName
                );


            if (
                commit
            )
            {
                if (
                    int.TryParse(
                        next,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out int parsed
                    )
                    ||
                    int.TryParse(
                        next,
                        NumberStyles.Integer,
                        CultureInfo.CurrentCulture,
                        out parsed
                    )
                )
                {
                    value =
                        Mathf.Clamp(
                            parsed,
                            min,
                            max
                        );
                }


                numericFieldBuffers[
                    bufferKey
                ] =
                    value.ToString(
                        CultureInfo.InvariantCulture
                    );


                GUI.FocusControl(
                    null
                );
            }


            GUILayout.EndHorizontal();


            return value;
        }


        private float BufferedFloatField(
            string key,
            string label,
            float value,
            float min,
            float max
        )
        {
            GUILayout.BeginHorizontal();


            GUILayout.Label(
                label,
                GUILayout.Width(
                    120f
                )
            );


            string bufferKey =
                "float:" +
                key;


            if (
                !numericFieldBuffers.TryGetValue(
                    bufferKey,
                    out string text
                )
            )
            {
                text =
                    value.ToString(
                        "0.###",
                        CultureInfo.InvariantCulture
                    );


                numericFieldBuffers[
                    bufferKey
                ] =
                    text;
            }


            string controlName =
                "StructureNumeric_" +
                bufferKey;


            GUI.SetNextControlName(
                controlName
            );


            string next =
                GUILayout.TextField(
                    text
                );


            numericFieldBuffers[
                bufferKey
            ] =
                next;


            bool commit =
                IsEnterPressedForControl(
                    controlName
                );


            if (
                commit
            )
            {
                if (
                    TryParseFlexibleFloat(
                        next,
                        out float parsed
                    )
                )
                {
                    value =
                        Mathf.Clamp(
                            parsed,
                            min,
                            max
                        );
                }


                numericFieldBuffers[
                    bufferKey
                ] =
                    value.ToString(
                        "0.###",
                        CultureInfo.InvariantCulture
                    );


                GUI.FocusControl(
                    null
                );
            }


            GUILayout.EndHorizontal();


            return value;
        }


        private static bool TryParseFlexibleFloat(
            string text,
            out float value
        )
        {
            value =
                0f;


            if (
                string.IsNullOrWhiteSpace(
                    text
                )
            )
            {
                return false;
            }


            if (
                float.TryParse(
                    text,
                    NumberStyles.Float,
                    CultureInfo.CurrentCulture,
                    out value
                )
            )
            {
                return true;
            }


            string invariantText =
                text
                    .Trim()
                    .Replace(
                        ',',
                        '.'
                    );


            return
                float.TryParse(
                    invariantText,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out value
                );
        }


        private static bool IsEnterPressedForControl(
            string controlName
        )
        {
            Event currentEvent =
                Event.current;


            if (
                currentEvent ==
                null
                ||
                currentEvent.type !=
                EventType.KeyDown
                ||
                (
                    currentEvent.keyCode !=
                    KeyCode.Return
                    &&
                    currentEvent.keyCode !=
                    KeyCode.KeypadEnter
                )
                ||
                !string.Equals(
                    GUI.GetNameOfFocusedControl(),
                    controlName,
                    StringComparison.Ordinal
                )
            )
            {
                return false;
            }


            currentEvent.Use();


            return true;
        }


        private string ShortId(
            string id
        )
        {
            if (
                string.IsNullOrWhiteSpace(
                    id
                )
            )
            {
                return string.Empty;
            }


            int separator =
                id.IndexOf(
                    ':'
                );


            string value =
                separator >=
                0
                    ? id.Substring(
                        separator +
                        1
                    )
                    : id;


            return
                value.Length >
                4
                    ? value.Substring(
                        0,
                        4
                    )
                    : value;
        }
    

    // [BT-AUTO-STRUCTURE-DATA]
    public byte Transform;
}
}
