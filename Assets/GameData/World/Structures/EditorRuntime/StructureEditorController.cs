
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Game.World.Structures.EditorRuntime
{
    public class StructureEditorController : MonoBehaviour
    {
        private StructureDefinition current =
            new StructureDefinition();

        private List<
            StructureEditorContentScanner.BlockInfo
        > blocks;

        private List<string> biomes;

        private string selectedBlockId;

        private int selectedCellX = -1;
        private int selectedCellY = -1;

        private Vector2 blockScroll;
        private Vector2 settingsScroll;
        private Vector2 gridScroll;
        private Vector2 lootScroll;

        private string status = string.Empty;

        private const float CellSize = 34f;

        private void Start()
        {
            RefreshContent();
            NewStructure();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F5))
                Save();
        }

        private void OnGUI()
        {
            DrawTopBar();
            DrawBlockPalette();
            DrawGrid();
            DrawSettings();
        }

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
                GUILayout.Width(220f)
            );

            if (GUILayout.Button(
                "Новая",
                GUILayout.Width(90f)))
            {
                NewStructure();
            }

            if (GUILayout.Button(
                "Сохранить [F5]",
                GUILayout.Width(130f)))
            {
                Save();
            }

            if (GUILayout.Button(
                "Обновить контент",
                GUILayout.Width(140f)))
            {
                RefreshContent();
            }

            GUILayout.Space(10f);
            GUILayout.Label(status);
            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawBlockPalette()
        {
            const float width = 250f;

            GUILayout.BeginArea(
                new Rect(
                    0f,
                    54f,
                    width,
                    Screen.height - 54f
                ),
                GUI.skin.box
            );

            GUILayout.Label("БЛОКИ");
            GUILayout.Label(
                "Выбери блок.\n" +
                "ЛКМ по сетке — поставить.\n" +
                "ПКМ — удалить."
            );

            blockScroll =
                GUILayout.BeginScrollView(blockScroll);

            if (blocks != null)
            {
                for (int i = 0;
                     i < blocks.Count;
                     i++)
                {
                    var block = blocks[i];

                    string label =
                        block.Name +
                        "\n" +
                        block.ID;

                    if (block.IsChest)
                    {
                        label += block.Closed
                            ? "\n[CHEST CLOSED]"
                            : "\n[CHEST]";
                    }

                    Color old =
                        GUI.backgroundColor;

                    if (selectedBlockId == block.ID)
                    {
                        GUI.backgroundColor =
                            new Color(
                                0.35f,
                                0.55f,
                                0.90f
                            );
                    }

                    if (GUILayout.Button(
                        label,
                        GUILayout.Height(54f)))
                    {
                        selectedBlockId = block.ID;
                    }

                    GUI.backgroundColor = old;
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawGrid()
        {
            const float left = 250f;
            const float right = 390f;

            Rect area =
                new Rect(
                    left,
                    54f,
                    Screen.width - left - right,
                    Screen.height - 54f
                );

            GUILayout.BeginArea(
                area,
                GUI.skin.box
            );

            GUILayout.Label("СТРУКТУРА");

            gridScroll =
                GUILayout.BeginScrollView(gridScroll);

            Rect grid =
                GUILayoutUtility.GetRect(
                    current.Width * CellSize,
                    current.Height * CellSize,
                    GUILayout.ExpandWidth(false),
                    GUILayout.ExpandHeight(false)
                );

            Event evt = Event.current;

            for (int y = current.Height - 1;
                 y >= 0;
                 y--)
            {
                for (int x = 0;
                     x < current.Width;
                     x++)
                {
                    int displayY =
                        current.Height - 1 - y;

                    Rect cellRect =
                        new Rect(
                            grid.x + x * CellSize,
                            grid.y + displayY * CellSize,
                            CellSize - 1f,
                            CellSize - 1f
                        );

                    StructureCellDefinition cell =
                        FindCell(x, y);

                    Color old =
                        GUI.backgroundColor;

                    if (x == current.OriginX &&
                        y == current.OriginY)
                    {
                        GUI.backgroundColor =
                            new Color(
                                0.30f,
                                0.45f,
                                0.25f
                            );
                    }
                    else if (
                        x == selectedCellX &&
                        y == selectedCellY)
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

                    string text =
                        cell != null &&
                        !string.IsNullOrWhiteSpace(
                            cell.ForegroundId)
                            ? ShortId(cell.ForegroundId)
                            : string.Empty;

                    GUI.Box(
                        cellRect,
                        text
                    );

                    GUI.backgroundColor = old;

                    if (evt.type == EventType.MouseDown &&
                        cellRect.Contains(
                            evt.mousePosition))
                    {
                        selectedCellX = x;
                        selectedCellY = y;

                        if (evt.button == 0)
                            Place(x, y);
                        else if (evt.button == 1)
                            Remove(x, y);

                        evt.Use();
                    }
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawSettings()
        {
            const float width = 390f;

            GUILayout.BeginArea(
                new Rect(
                    Screen.width - width,
                    54f,
                    width,
                    Screen.height - 54f
                ),
                GUI.skin.box
            );

            settingsScroll =
                GUILayout.BeginScrollView(settingsScroll);

            GUILayout.Label("ПАРАМЕТРЫ");

            DrawExistingStructures();

            GUILayout.Space(8f);

            current.ID =
                LabeledText(
                    "ID",
                    current.ID
                );

            current.DisplayName =
                LabeledText(
                    "Имя",
                    current.DisplayName
                );

            GUILayout.Label("Размер");

            GUILayout.BeginHorizontal();

            current.Width =
                IntField(
                    current.Width,
                    1,
                    64
                );

            current.Height =
                IntField(
                    current.Height,
                    1,
                    64
                );

            GUILayout.EndHorizontal();

            current.OriginX =
                Mathf.Clamp(
                    LabeledInt(
                        "Origin X",
                        current.OriginX
                    ),
                    0,
                    current.Width - 1
                );

            current.OriginY =
                Mathf.Clamp(
                    LabeledInt(
                        "Origin Y",
                        current.OriginY
                    ),
                    0,
                    current.Height - 1
                );

            GUILayout.Space(8f);
            GUILayout.Label("Тип появления");

            current.SpawnType =
                (StructureSpawnType)
                GUILayout.SelectionGrid(
                    (int)current.SpawnType,
                    new[]
                    {
                        "ПЕЩЕРА",
                        "ПОВЕРХНОСТЬ",
                        "ЛЮБОЙ"
                    },
                    1
                );

            current.SpawnChance =
                Mathf.Clamp01(
                    LabeledFloat(
                        "Шанс 0..1",
                        current.SpawnChance
                    )
                );

            current.RegionSize =
                Mathf.Max(
                    8,
                    LabeledInt(
                        "Region size",
                        current.RegionSize
                    )
                );

            current.RequireFreeSpace =
                GUILayout.Toggle(
                    current.RequireFreeSpace,
                    "Искать свободное пространство"
                );

            current.FreeSpacePadding =
                Mathf.Max(
                    0,
                    LabeledInt(
                        "Отступ воздуха",
                        current.FreeSpacePadding
                    )
                );

            current.UseHeightRange =
                GUILayout.Toggle(
                    current.UseHeightRange,
                    "Ограничить высоту"
                );

            if (current.UseHeightRange)
            {
                current.MinY =
                    LabeledInt(
                        "Min Y",
                        current.MinY
                    );

                current.MaxY =
                    LabeledInt(
                        "Max Y",
                        current.MaxY
                    );
            }

            GUILayout.Space(8f);
            DrawBiomes();

            GUILayout.Space(10f);
            DrawSelectedCell();

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawExistingStructures()
        {
            GUILayout.Label("СУЩЕСТВУЮЩИЕ");

            var structures =
                StructureRegistry.GetAll();

            for (int i = 0;
                 i < structures.Count;
                 i++)
            {
                StructureDefinition structure =
                    structures[i];

                if (structure == null)
                    continue;

                if (GUILayout.Button(
                    "Загрузить: " +
                    structure.DisplayName))
                {
                    string fileName =
                        structure.ID
                            .Replace(":", "_")
                            .Replace("/", "_")
                            .Replace("\\", "_")
                        + ".json";

                    string file =
                        Path.Combine(
                            StructurePaths.Folder,
                            fileName
                        );

                    if (File.Exists(file))
                    {
                        StructureDefinition loaded =
                            JsonUtility.FromJson<
                                StructureDefinition
                            >(
                                File.ReadAllText(file)
                            );

                        if (loaded != null)
                        {
                            current = loaded;

                            if (current.Cells == null)
                                current.Cells =
                                    new List<
                                        StructureCellDefinition
                                    >();

                            if (current.Biomes == null)
                                current.Biomes =
                                    new List<string>();

                            selectedCellX = -1;
                            selectedCellY = -1;

                            status =
                                "Загружено: " +
                                current.ID;
                        }
                    }
                }
            }
        }


        private void DrawBiomes()
        {
            GUILayout.Label(
                "БИОМЫ (ничего = все)"
            );

            if (biomes == null)
                return;

            for (int i = 0;
                 i < biomes.Count;
                 i++)
            {
                string biome = biomes[i];

                bool enabled =
                    current.Biomes.Contains(biome);

                bool next =
                    GUILayout.Toggle(
                        enabled,
                        biome
                    );

                if (next && !enabled)
                    current.Biomes.Add(biome);
                else if (!next && enabled)
                    current.Biomes.Remove(biome);
            }
        }

        private void DrawSelectedCell()
        {
            GUILayout.Label("ВЫБРАННАЯ КЛЕТКА");

            if (selectedCellX < 0 ||
                selectedCellY < 0)
            {
                GUILayout.Label("Выбери клетку.");
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

            if (cell == null)
            {
                GUILayout.Label("Пусто");
                return;
            }

            GUILayout.Label(cell.ForegroundId);

            if (!IsChestId(cell.ForegroundId))
            {
                GUILayout.Label(
                    "Loot доступен только для блока " +
                    "с тегом chest."
                );
                return;
            }

            GUILayout.Label("LOOT TABLE");

            if (cell.Loot == null)
            {
                cell.Loot =
                    new List<
                        StructureLootEntryDefinition
                    >();
            }

            lootScroll =
                GUILayout.BeginScrollView(
                    lootScroll,
                    GUILayout.Height(220f)
                );

            for (int i = 0;
                 i < cell.Loot.Count;
                 i++)
            {
                StructureLootEntryDefinition entry =
                    cell.Loot[i];

                GUILayout.BeginVertical(
                    GUI.skin.box
                );

                entry.ItemId =
                    LabeledText(
                        "Item",
                        entry.ItemId
                    );

                entry.Chance =
                    Mathf.Clamp01(
                        LabeledFloat(
                            "Chance",
                            entry.Chance
                        )
                    );

                entry.MinCount =
                    Mathf.Max(
                        1,
                        LabeledInt(
                            "Min",
                            entry.MinCount
                        )
                    );

                entry.MaxCount =
                    Mathf.Max(
                        entry.MinCount,
                        LabeledInt(
                            "Max",
                            entry.MaxCount
                        )
                    );

                if (GUILayout.Button(
                    "Удалить loot"))
                {
                    cell.Loot.RemoveAt(i);
                    i--;
                    GUILayout.EndVertical();
                    continue;
                }

                GUILayout.EndVertical();
            }

            GUILayout.EndScrollView();

            if (GUILayout.Button(
                "+ Добавить loot"))
            {
                cell.Loot.Add(
                    new StructureLootEntryDefinition
                    {
                        ItemId = "game:stone",
                        Chance = 1f,
                        MinCount = 1,
                        MaxCount = 1
                    }
                );
            }
        }

        private void NewStructure()
        {
            current =
                new StructureDefinition();

            selectedCellX = -1;
            selectedCellY = -1;
            status = "Новая структура";
        }

        private void RefreshContent()
        {
            blocks =
                StructureEditorContentScanner
                    .LoadBlocks();

            biomes =
                StructureEditorContentScanner
                    .LoadBiomeIds();

            if (blocks.Count > 0 &&
                string.IsNullOrWhiteSpace(
                    selectedBlockId))
            {
                selectedBlockId =
                    blocks[0].ID;
            }

            status =
                "Blocks: " +
                blocks.Count +
                " | Biomes: " +
                biomes.Count;
        }

        private void Place(int x, int y)
        {
            if (string.IsNullOrWhiteSpace(
                selectedBlockId))
                return;

            StructureCellDefinition cell =
                FindCell(x, y);

            if (cell == null)
            {
                cell =
                    new StructureCellDefinition
                    {
                        X = x,
                        Y = y
                    };

                current.Cells.Add(cell);
            }

            cell.ForegroundId =
                selectedBlockId;

            if (cell.Loot == null)
            {
                cell.Loot =
                    new List<
                        StructureLootEntryDefinition
                    >();
            }
        }

        private void Remove(int x, int y)
        {
            for (int i = current.Cells.Count - 1;
                 i >= 0;
                 i--)
            {
                StructureCellDefinition cell =
                    current.Cells[i];

                if (cell.X == x &&
                    cell.Y == y)
                {
                    current.Cells.RemoveAt(i);
                }
            }
        }

        private StructureCellDefinition FindCell(
            int x,
            int y
        )
        {
            for (int i = 0;
                 i < current.Cells.Count;
                 i++)
            {
                StructureCellDefinition cell =
                    current.Cells[i];

                if (cell != null &&
                    cell.X == x &&
                    cell.Y == y)
                    return cell;
            }

            return null;
        }

        private bool IsChestId(string id)
        {
            if (blocks == null)
                return false;

            for (int i = 0;
                 i < blocks.Count;
                 i++)
            {
                if (blocks[i].ID == id)
                    return blocks[i].IsChest;
            }

            return false;
        }

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(
                current.ID))
            {
                status = "ERROR: ID пустой";
                return;
            }

            string safe =
                current.ID
                    .Replace(":", "_")
                    .Replace("/", "_")
                    .Replace("\\", "_");

            string file =
                Path.Combine(
                    StructurePaths.Folder,
                    safe + ".json"
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
                "Сохранено: " +
                file;
        }

        private string LabeledText(
            string label,
            string value
        )
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(
                label,
                GUILayout.Width(120f)
            );

            value =
                GUILayout.TextField(
                    value ?? string.Empty
                );

            GUILayout.EndHorizontal();
            return value;
        }

        private int LabeledInt(
            string label,
            int value
        )
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(
                label,
                GUILayout.Width(120f)
            );

            string text =
                GUILayout.TextField(
                    value.ToString()
                );

            GUILayout.EndHorizontal();

            return int.TryParse(
                text,
                out int result)
                    ? result
                    : value;
        }

        private float LabeledFloat(
            string label,
            float value
        )
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(
                label,
                GUILayout.Width(120f)
            );

            string text =
                GUILayout.TextField(
                    value.ToString("0.###")
                );

            GUILayout.EndHorizontal();

            return float.TryParse(
                text,
                out float result)
                    ? result
                    : value;
        }

        private int IntField(
            int value,
            int min,
            int max
        )
        {
            string text =
                GUILayout.TextField(
                    value.ToString(),
                    GUILayout.Width(70f)
                );

            if (int.TryParse(
                text,
                out int result))
            {
                return Mathf.Clamp(
                    result,
                    min,
                    max
                );
            }

            return value;
        }

        private string ShortId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return string.Empty;

            int separator = id.IndexOf(':');

            string value =
                separator >= 0
                    ? id.Substring(separator + 1)
                    : id;

            return value.Length > 4
                ? value.Substring(0, 4)
                : value;
        }
    }
}
