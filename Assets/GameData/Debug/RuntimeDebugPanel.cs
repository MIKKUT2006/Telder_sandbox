
using System;
using System.Collections.Generic;

using UnityEngine;

using Game.Blocks;
using Game.Content;
using Game.Inventory;
using Game.Items;
using Game.World;
using Game.World.Biomes;
using Game.World.Biomes.Caves;
using Game.World.Dimensions;
using Game.World.Generation;
using Game.World.Structures;
using Game.World.Weather;

namespace Game.Debugging
{
    public class RuntimeDebugPanel :
        MonoBehaviour
    {
        private static RuntimeDebugPanel instance;


        private bool open;


        private Vector2 mainScroll;


        private Vector2 chestScroll;


        private Vector2 structureScroll;


        private Vector2 generatedStructureScroll;


        private string structureSearch =
            string.Empty;


        private string candidateSearchRadiusText =
            "24";


        private string structureSearchResult =
            "Нажми «Найти кандидат» у нужной структуры.";


        private Rect windowRect =
            new Rect(
                24f,
                24f,
                660f,
                820f
            );


        // =====================================================
        // CREATE
        // =====================================================

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad
        )]
        private static void EnsureCreated()
        {
            if (
                instance !=
                null
            )
            {
                return;
            }


            GameObject gameObject =
                new GameObject(
                    "RuntimeDebugPanel"
                );


            instance =
                gameObject.AddComponent<
                    RuntimeDebugPanel
                >();


            DontDestroyOnLoad(
                gameObject
            );
        }


        private void Awake()
        {
            if (
                instance !=
                null
                &&
                instance !=
                this
            )
            {
                Destroy(
                    gameObject
                );


                return;
            }


            instance =
                this;


            DontDestroyOnLoad(
                gameObject
            );
        }


        // =====================================================
        // UNITY
        // =====================================================

        private void Update()
        {
            if (
                !Input.GetKeyDown(
                    KeyCode.L
                )
            )
            {
                return;
            }


            if (
                WorldManager.Instance ==
                null
            )
            {
                open =
                    false;


                return;
            }


            open =
                !open;


            if (
                open
            )
            {
                StructureRegistry.Reload();

                CaveBiomeRegistry.Reload();
            }
        }


        private void OnGUI()
        {
            if (
                !open
                ||
                WorldManager.Instance ==
                null
            )
            {
                return;
            }


            windowRect.width =
                Mathf.Min(
                    windowRect.width,
                    Mathf.Max(
                        420f,
                        Screen.width -
                        32f
                    )
                );


            windowRect.height =
                Mathf.Min(
                    windowRect.height,
                    Mathf.Max(
                        480f,
                        Screen.height -
                        32f
                    )
                );


            windowRect =
                GUI.Window(
                    941723,
                    windowRect,
                    DrawWindow,
                    "TELDER DEBUG [L]"
                );
        }


        // =====================================================
        // WINDOW
        // =====================================================

        private void DrawWindow(
            int id
        )
        {
            mainScroll =
                GUILayout.BeginScrollView(
                    mainScroll
                );


            DrawBiomeStatus();


            GUILayout.Space(
                10f
            );

            DrawWeatherSection();

            GUILayout.Space(
                10f
            );

            DrawGeneratedStructures();


            GUILayout.Space(
                10f
            );


            DrawStructureDefinitions();


            GUILayout.Space(
                10f
            );


            DrawQuickGive();


            GUILayout.Space(
                10f
            );


            DrawChests();


            GUILayout.Space(
                8f
            );


            GUILayout.Label(
                "L — закрыть окно"
            );


            GUILayout.EndScrollView();


            GUI.DragWindow(
                new Rect(
                    0f,
                    0f,
                    windowRect.width,
                    24f
                )
            );
        }


        // =====================================================
        // BIOME STATUS
        // =====================================================

        private void DrawBiomeStatus()
        {
            GUILayout.Label(
                "ПОЗИЦИЯ / БИОМ",
                GUI.skin.box
            );


            PlayerInventory inventory =
                FindPlayerInventory();


            WorldManager manager =
                WorldManager.Instance;


            if (
                inventory ==
                null
                ||
                manager ==
                null
            )
            {
                GUILayout.Label(
                    "Player / WorldManager не найден."
                );


                return;
            }


            WorldGenerator generator =
                manager.GetGenerator();


            WorldSettings settings =
                manager.GetSettings();


            int worldX =
                Mathf.FloorToInt(
                    inventory.transform.position.x
                );


            int worldY =
                Mathf.FloorToInt(
                    inventory.transform.position.y
                );


            int surfaceY =
                generator !=
                null
                    ? generator.GetSurfaceHeight(
                        worldX
                    )
                    : 0;


            BiomeDefinition surfaceBiome =
                generator !=
                null
                    ? generator.GetDominantBiome(
                        worldX
                    )
                    : null;


            string surfaceName =
                surfaceBiome !=
                null
                    ? (
                        string.IsNullOrWhiteSpace(
                            surfaceBiome.DisplayName
                        )
                            ? surfaceBiome.ToString()
                            : surfaceBiome.DisplayName
                    )
                    : "NONE";


            string caveBiomeId =
                settings !=
                null
                    ? CaveBiomeRegistry.GetBiomeIdAt(
                        worldX,
                        worldY,
                        settings.Seed
                    )
                    : null;


            string effectiveBiome =
                !string.IsNullOrWhiteSpace(
                    caveBiomeId
                )
                    ? caveBiomeId
                    : surfaceName;


            GUILayout.Label(
                "X: " +
                worldX +
                " | Y: " +
                worldY +
                " | Surface Y: " +
                surfaceY
            );


            GUILayout.Label(
                "Surface biome (по X): " +
                surfaceName
            );


            GUILayout.Label(
                "Cave biome (по X/Y): " +
                (
                    string.IsNullOrWhiteSpace(
                        caveBiomeId
                    )
                        ? "NONE"
                        : caveBiomeId
                )
            );


            GUILayout.Label(
                "Текущий / effective biome: " +
                effectiveBiome
            );


            if (
                worldY <
                surfaceY
                &&
                string.IsNullOrWhiteSpace(
                    caveBiomeId
                )
            )
            {
                GUILayout.Label(
                    "Под землёй, но cave-biome в этой точке не активен."
                );
            }


            if (
                !string.IsNullOrWhiteSpace(
                    caveBiomeId
                )
            )
            {
                GUILayout.Label(
                    "Важно: surface biome может оставаться Plains. " +
                    "Пещерный биом — отдельный слой и имеет приоритет здесь."
                );
            }
        }

        // Погодные условия
        private void DrawWeatherSection()
        {
            WeatherManager weather =
                WeatherManager.Instance;

            GUILayout.Space(10f);

            GUILayout.Label("WEATHER");

            if (weather == null)
            {
                GUILayout.Label(
                    "WeatherManager: not found"
                );

                return;
            }

            GUILayout.Label(
                "Biome: " +
                weather.CurrentBiomeId
            );

            GUILayout.Label(
                "Mode: " +
                weather.ControlMode
            );

            GUILayout.Label(
                "Current: " +
                weather.CurrentWeather
            );

            if (
                weather.CurrentWeather ==
                WeatherType.Rain
            )
            {
                GUILayout.Label(
                    "Rain angle: " +
                    weather.RainTiltDegrees
                        .ToString("0.0") +
                    " deg"
                );
            }

            GUILayout.Space(4f);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("AUTO"))
            {
                weather.SetControlMode(
                    WeatherControlMode.Auto
                );
            }

            if (GUILayout.Button("CLEAR"))
            {
                weather.SetControlMode(
                    WeatherControlMode.Clear
                );
            }

            if (GUILayout.Button("RAIN"))
            {
                weather.SetControlMode(
                    WeatherControlMode.Rain
                );
            }

            if (GUILayout.Button("SNOW"))
            {
                weather.SetControlMode(
                    WeatherControlMode.Snow
                );
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(4f);

            GUILayout.Label(
                "Debug intensity: " +
                weather.DebugIntensity
                    .ToString("0.00")
            );

            float intensity =
                GUILayout.HorizontalSlider(
                    weather.DebugIntensity,
                    0.1f,
                    1.5f
                );

            if (
                !Mathf.Approximately(
                    intensity,
                    weather.DebugIntensity
                )
            )
            {
                weather.SetDebugIntensity(
                    intensity
                );
            }
        }

        // =====================================================
        // GENERATED STRUCTURES
        // =====================================================

        private void DrawGeneratedStructures()
        {
            GUILayout.Label(
                "СГЕНЕРИРОВАННЫЕ СТРУКТУРЫ (ЭТА СЕССИЯ)",
                GUI.skin.box
            );


            PlayerInventory inventory =
                FindPlayerInventory();


            if (
                inventory ==
                null
            )
            {
                GUILayout.Label(
                    "PlayerInventory не найден."
                );


                return;
            }


            int playerX =
                Mathf.FloorToInt(
                    inventory.transform.position.x
                );


            int playerY =
                Mathf.FloorToInt(
                    inventory.transform.position.y
                );


            string dimensionName =
                DimensionTravelRuntime.Current !=
                null
                    ? DimensionTravelRuntime.Current.Name
                    : string.Empty;


            int total =
                StructureDebugRuntimeRegistry
                    .CountForDimension(
                        dimensionName
                    );


            GUILayout.Label(
                "В текущем измерении зарегистрировано: " +
                total
            );


            List<
                StructureDebugRuntimeRegistry.Entry
            > nearby =
                StructureDebugRuntimeRegistry
                    .GetNearby(
                        dimensionName,
                        playerX,
                        playerY,
                        structureSearch,
                        24
                    );


            generatedStructureScroll =
                GUILayout.BeginScrollView(
                    generatedStructureScroll,
                    GUILayout.Height(
                        145f
                    )
                );


            if (
                nearby.Count ==
                0
            )
            {
                GUILayout.Label(
                    "Подходящих сгенерированных структур пока не зарегистрировано."
                );
            }


            for (
                int i = 0;
                i < nearby.Count;
                i++
            )
            {
                StructureDebugRuntimeRegistry.Entry entry =
                    nearby[i];


                int dx =
                    entry.AnchorX -
                    playerX;


                int dy =
                    entry.AnchorY -
                    playerY;


                GUILayout.Label(
                    entry.DisplayName +
                    "\n" +
                    entry.StructureId +
                    " | anchor=(" +
                    entry.AnchorX +
                    ", " +
                    entry.AnchorY +
                    ")" +
                    " | Δ=(" +
                    dx +
                    ", " +
                    dy +
                    ")"
                );
            }


            GUILayout.EndScrollView();


            GUILayout.Label(
                "Запись появляется только когда валидный кандидат реально дошёл " +
                "до StampCandidateIntoChunk в загруженном чанке."
            );
        }


        // =====================================================
        // STRUCTURE DEFINITIONS / SEARCH
        // =====================================================

        private void DrawStructureDefinitions()
        {
            GUILayout.Label(
                "СТРУКТУРЫ / ПОИСК ГЕНЕРАЦИИ",
                GUI.skin.box
            );


            GUILayout.BeginHorizontal();


            GUILayout.Label(
                "Поиск",
                GUILayout.Width(
                    55f
                )
            );


            structureSearch =
                GUILayout.TextField(
                    structureSearch ??
                    string.Empty
                );


            GUILayout.Label(
                "Радиус регионов",
                GUILayout.Width(
                    105f
                )
            );


            candidateSearchRadiusText =
                GUILayout.TextField(
                    candidateSearchRadiusText ??
                    "24",
                    GUILayout.Width(
                        52f
                    )
                );


            GUILayout.EndHorizontal();


            GUILayout.Label(
                structureSearchResult,
                GUI.skin.box
            );


            structureScroll =
                GUILayout.BeginScrollView(
                    structureScroll,
                    GUILayout.Height(
                        290f
                    )
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
                    ||
                    !MatchesStructureSearch(
                        structure,
                        structureSearch
                    )
                )
                {
                    continue;
                }


                GUILayout.BeginVertical(
                    GUI.skin.box
                );


                GUILayout.Label(
                    structure.DisplayName +
                    "\n" +
                    structure.ID
                );


                GUILayout.Label(
                    "SpawnChance=" +
                    structure.SpawnChance.ToString(
                        "0.###"
                    ) +
                    " | Region=" +
                    structure.RegionSize +
                    " | SpawnType=" +
                    structure.SpawnType +
                    " | BiomeSource=" +
                    structure.BiomeSource
                );


                if (
                    structure.Biomes !=
                    null
                    &&
                    structure.Biomes.Count >
                    0
                )
                {
                    GUILayout.Label(
                        "Biomes: " +
                        string.Join(
                            ", ",
                            structure.Biomes
                        )
                    );
                }
                else
                {
                    GUILayout.Label(
                        "Biomes: ANY"
                    );
                }


                GUILayout.BeginHorizontal();


                if (
                    GUILayout.Button(
                        "Спавн перед собой",
                        GUILayout.Height(
                            34f
                        )
                    )
                )
                {
                    SpawnStructure(
                        structure
                    );
                }


                if (
                    GUILayout.Button(
                        "Найти кандидат",
                        GUILayout.Height(
                            34f
                        )
                    )
                )
                {
                    FindCandidate(
                        structure
                    );
                }


                GUILayout.EndHorizontal();


                GUILayout.EndVertical();
            }


            GUILayout.EndScrollView();
        }


        private void FindCandidate(
            StructureDefinition structure
        )
        {
            WorldManager manager =
                WorldManager.Instance;


            PlayerInventory inventory =
                FindPlayerInventory();


            if (
                manager ==
                null
                ||
                inventory ==
                null
                ||
                structure ==
                null
            )
            {
                structureSearchResult =
                    "Не удалось получить WorldManager / Player / Structure.";


                return;
            }


            WorldGenerator generator =
                manager.GetGenerator();


            WorldSettings settings =
                manager.GetSettings();


            if (
                generator ==
                null
                ||
                settings ==
                null
            )
            {
                structureSearchResult =
                    "WorldGenerator / WorldSettings недоступен.";


                return;
            }


            int radius =
                24;


            if (
                int.TryParse(
                    candidateSearchRadiusText,
                    out int parsedRadius
                )
            )
            {
                radius =
                    Mathf.Clamp(
                        parsedRadius,
                        0,
                        256
                    );
            }


            int centerX =
                Mathf.FloorToInt(
                    inventory.transform.position.x
                );


            bool found =
                StructureGenerationRuntime
                    .TryFindNearestCandidate(
                        generator,
                        settings,
                        structure,
                        centerX,
                        radius,
                        out int anchorX,
                        out int anchorY,
                        out int regionX
                    );


            if (
                !found
            )
            {
                structureSearchResult =
                    structure.ID +
                    ": НЕТ валидного кандидата в ±" +
                    radius +
                    " регионов. Проверяй SpawnChance / Height / Biome / FreeSpace.";


                Debug.LogWarning(
                    "DEBUG STRUCTURE SEARCH: " +
                    structureSearchResult
                );


                return;
            }


            string caveBiome =
                CaveBiomeRegistry.GetBiomeIdAt(
                    anchorX,
                    anchorY,
                    settings.Seed
                );


            BiomeDefinition surfaceBiome =
                generator.GetDominantBiome(
                    anchorX
                );


            string surface =
                surfaceBiome !=
                null
                    ? surfaceBiome.DisplayName
                    : "NONE";


            int dx =
                anchorX -
                centerX;


            structureSearchResult =
                structure.ID +
                " -> anchor=(" +
                anchorX +
                ", " +
                anchorY +
                ")" +
                " | region=" +
                regionX +
                " | ΔX=" +
                dx +
                " | surface=" +
                surface +
                " | cave=" +
                (
                    string.IsNullOrWhiteSpace(
                        caveBiome
                    )
                        ? "NONE"
                        : caveBiome
                );


            Debug.Log(
                "DEBUG STRUCTURE SEARCH: " +
                structureSearchResult
            );
        }


        private static bool MatchesStructureSearch(
            StructureDefinition structure,
            string search
        )
        {
            if (
                structure ==
                null
            )
            {
                return false;
            }


            if (
                string.IsNullOrWhiteSpace(
                    search
                )
            )
            {
                return true;
            }


            string filter =
                search.Trim();


            return
                ContainsIgnoreCase(
                    structure.ID,
                    filter
                )
                ||
                ContainsIgnoreCase(
                    structure.DisplayName,
                    filter
                );
        }


        private static bool ContainsIgnoreCase(
            string value,
            string search
        )
        {
            return
                !string.IsNullOrWhiteSpace(
                    value
                )
                &&
                value.IndexOf(
                    search,
                    StringComparison.OrdinalIgnoreCase
                )
                >=
                0;
        }


        // =====================================================
        // QUICK GIVE
        // =====================================================

        private void DrawQuickGive()
        {
            GUILayout.Label(
                "БЫСТРАЯ ВЫДАЧА",
                GUI.skin.box
            );


            GUILayout.BeginHorizontal();


            if (
                GUILayout.Button(
                    "Факел",
                    GUILayout.Height(
                        30f
                    )
                )
            )
            {
                GiveItem(
                    "game:torch"
                );
            }


            if (
                GUILayout.Button(
                    "Верстак",
                    GUILayout.Height(
                        30f
                    )
                )
            )
            {
                GiveItem(
                    "game:workbench"
                );
            }


            GUILayout.EndHorizontal();


            GUILayout.BeginHorizontal();


            if (
                GUILayout.Button(
                    "Кирка I",
                    GUILayout.Height(
                        30f
                    )
                )
            )
            {
                GiveItem(
                    "game:wood_pickaxe"
                );
            }


            if (
                GUILayout.Button(
                    "Кирка II",
                    GUILayout.Height(
                        30f
                    )
                )
            )
            {
                GiveItem(
                    "game:iron_pickaxe"
                );
            }


            if (
                GUILayout.Button(
                    "Кирка III",
                    GUILayout.Height(
                        30f
                    )
                )
            )
            {
                GiveItem(
                    "game:teleportium_pickaxe"
                );
            }


            GUILayout.EndHorizontal();
        }


        // =====================================================
        // CHESTS
        // =====================================================

        private void DrawChests()
        {
            GUILayout.Label(
                "СУНДУКИ",
                GUI.skin.box
            );


            chestScroll =
                GUILayout.BeginScrollView(
                    chestScroll,
                    GUILayout.Height(
                        130f
                    )
                );


            foreach (
                BlockDefinition block
                in BlockRegistry.GetAll()
            )
            {
                if (
                    block ==
                    null
                    ||
                    !HasTag(
                        block,
                        "chest"
                    )
                )
                {
                    continue;
                }


                GUILayout.BeginHorizontal();


                GUILayout.Label(
                    block.Name +
                    "\n" +
                    block.ID,
                    GUILayout.Width(
                        270f
                    )
                );


                if (
                    GUILayout.Button(
                        block.Closed
                            ? "Выдать (Closed)"
                            : "Выдать",
                        GUILayout.Height(
                            38f
                        )
                    )
                )
                {
                    GiveItem(
                        block.ID
                    );
                }


                GUILayout.EndHorizontal();
            }


            GUILayout.EndScrollView();
        }


        // =====================================================
        // ACTIONS
        // =====================================================

        private void GiveItem(
            string itemId
        )
        {
            PlayerInventory inventory =
                FindPlayerInventory();


            if (
                inventory ==
                null
            )
            {
                Debug.LogError(
                    "DEBUG: PlayerInventory not found."
                );


                return;
            }


            if (
                !ItemRegistry.Contains(
                    itemId
                )
            )
            {
                Debug.LogWarning(
                    "DEBUG: Item not registered: " +
                    itemId
                );


                return;
            }


            inventory.AddItem(
                itemId,
                1
            );
        }


        private void SpawnStructure(
            StructureDefinition structure
        )
        {
            PlayerInventory inventory =
                FindPlayerInventory();


            if (
                inventory ==
                null
            )
            {
                return;
            }


            bool spawned =
                StructureRuntimeSpawner
                    .SpawnAheadOfPlayer(
                        structure,
                        inventory.transform
                    );


            Debug.Log(
                "DEBUG STRUCTURE SPAWN: " +
                structure.ID +
                " = " +
                spawned
            );
        }


        private static PlayerInventory FindPlayerInventory()
        {
            return
                UnityEngine.Object
                    .FindFirstObjectByType<
                        PlayerInventory
                    >();
        }


        private bool HasTag(
            BlockDefinition block,
            string tag
        )
        {
            if (
                block.Tags ==
                null
            )
            {
                return false;
            }


            for (
                int i = 0;
                i < block.Tags.Count;
                i++
            )
            {
                if (
                    string.Equals(
                        block.Tags[i],
                        tag,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return true;
                }
            }


            return false;
        }
    }
}
