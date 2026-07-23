using Game.Content;
using System;
using System.Collections.Generic;
using UnityEngine;


namespace Game.World.Generation.Ores
{

    public class OreGenerator
    {

        private readonly WorldSettings settings;

        private readonly List<OreData> ores =
            new List<OreData>();


        // =====================================================
        // DEBUG STATISTICS
        // =====================================================

        private readonly Dictionary<string, OreStatistics>
            statistics =
            new Dictionary<string, OreStatistics>();


        public OreGenerator(
            WorldSettings settings
        )
        {

            this.settings =
                settings;

        }


        // =====================================================
        // RELOAD ORES
        // =====================================================

        public void ReloadOres()
        {

            LoadOres();

        }


        // =====================================================
        // LOAD ORES
        // =====================================================

        private void LoadOres()
        {

            ores.Clear();

            statistics.Clear();


            Debug.Log(
                "========================================"
            );


            Debug.Log(
                "ORE GENERATOR: LOADING ORES"
            );


            Debug.Log(
                "========================================"
            );


            foreach (
                OreDefinition definition
                in OreRegistry.GetAll()
            )
            {

                if (
                    definition ==
                    null
                )
                {
                    continue;
                }


                if (
                    string.IsNullOrWhiteSpace(
                        definition.ID
                    )
                )
                {

                    Debug.LogWarning(
                        "ORE WITHOUT ID"
                    );

                    continue;

                }


                Debug.Log(
                    "ORE FOUND IN REGISTRY: " +
                    definition.ID
                );


                ContentID contentID =
                    ContentID.Parse(
                        definition.ID
                    );


                if (
                    !BlockIDRegistry.Contains(
                        contentID
                    )
                )
                {

                    Debug.LogError(
                        "ORE BLOCK NOT FOUND: " +
                        definition.ID
                    );

                    continue;

                }


                ushort blockID =
                    BlockIDRegistry.GetID(
                        contentID
                    );


                OreData data =
                    new OreData();


                data.ID =
                    definition.ID;


                data.BlockID =
                    blockID;


                data.MinDepth =
                    Mathf.Max(
                        definition.MinDepth,
                        0
                    );


                data.MaxDepth =
                    Mathf.Max(
                        definition.MaxDepth,
                        data.MinDepth
                    );


                data.Rarity =
                    Mathf.Clamp01(
                        definition.Rarity
                    );


                data.MinVeinSize =
                    Mathf.Max(
                        definition.MinVeinSize,
                        1
                    );


                data.MaxVeinSize =
                    Mathf.Max(
                        definition.VeinSize,
                        data.MinVeinSize
                    );


                ores.Add(
                    data
                );


                statistics.Add(
                    data.ID,
                    new OreStatistics()
                );


                Debug.Log(
                    "ORE REGISTERED:"
                );


                Debug.Log(
                    "ID: " +
                    data.ID
                );


                Debug.Log(
                    "BLOCK ID: " +
                    data.BlockID
                );


                Debug.Log(
                    "DEPTH: " +
                    data.MinDepth +
                    " - " +
                    data.MaxDepth
                );


                Debug.Log(
                    "RARITY: " +
                    data.Rarity
                );


                Debug.Log(
                    "VEIN SIZE: " +
                    data.MinVeinSize +
                    " - " +
                    data.MaxVeinSize
                );

            }


            Debug.Log(
                "TOTAL ORES REGISTERED: " +
                ores.Count
            );

        }


        // =====================================================
        // GET ORE
        // =====================================================

        public ushort GetOre(
    int worldX,
    int worldY,
    int surfaceHeight
)
        {
            if (
                ores.Count == 0
            )
            {
                return 0;
            }


            // =====================================================
            // √À”¡»Õ¿ Œ“ÕŒ—»“≈À‹ÕŒ œŒ¬≈–’ÕŒ—“»
            // =====================================================

            int depth =
                surfaceHeight -
                worldY;


            // =====================================================
            // Õ≈ √≈Õ≈–»–”≈Ã –”ƒ” Õ¿ƒ œŒ¬≈–’ÕŒ—“‹ﬁ
            // =====================================================

            if (
                depth <= 0
            )
            {
                return 0;
            }


            // =====================================================
            // œ–Œ¬≈– ¿ ¬—≈’ –”ƒ
            // =====================================================

            for (
                int i = 0;
                i < ores.Count;
                i++
            )
            {

                OreData ore =
                    ores[i];


                OreStatistics stat =
                    statistics[
                        ore.ID
                    ];


                // =================================================
                // œ–Œ¬≈– ¿ ƒ»¿œ¿«ŒÕ¿ √À”¡»Õ€
                // =================================================

                if (
                    depth <
                    ore.MinDepth
                )
                {
                    continue;
                }


                if (
                    depth >
                    ore.MaxDepth
                )
                {
                    continue;
                }


                // =================================================
                // √À”¡»Õ¿ œŒƒ’Œƒ»“
                // =================================================

                stat.ValidDepthChecks++;


                // =================================================
                // œ–Œ¬≈– ¿ ∆»À€
                // =================================================

                bool isOrePosition =
                    IsOrePosition(
                        worldX,
                        worldY,
                        ore
                    );


                if (
                    !isOrePosition
                )
                {
                    continue;
                }


                // =================================================
                // –”ƒ¿ Õ¿…ƒ≈Õ¿
                // =================================================

                stat.GeneratedBlocks++;


                return ore.BlockID;

            }


            // =====================================================
            // –”ƒ€ Õ≈“
            // =====================================================

            return 0;
        }


        // =====================================================
        // CHECK ORE POSITION
        // =====================================================

        private bool IsOrePosition(
            int worldX,
            int worldY,
            OreData ore
        )
        {

            int cellSize =
                Mathf.Max(
                    ore.MaxVeinSize * 3,
                    12
                );


            int cellX =
                Mathf.FloorToInt(
                    (float)worldX /
                    cellSize
                );


            int cellY =
                Mathf.FloorToInt(
                    (float)worldY /
                    cellSize
                );


            for (
                int offsetX = -1;
                offsetX <= 1;
                offsetX++
            )
            {

                for (
                    int offsetY = -1;
                    offsetY <= 1;
                    offsetY++
                )
                {

                    int currentCellX =
                        cellX +
                        offsetX;


                    int currentCellY =
                        cellY +
                        offsetY;


                    int seed =
                        GetSeed(
                            currentCellX,
                            currentCellY,
                            ore.ID
                        );


                    System.Random random =
                        new System.Random(
                            seed
                        );


                    // =================================================
                    // CHECK VEIN SPAWN
                    // =================================================

                    double chance =
                        random.NextDouble();


                    if (
                        chance >
                        ore.Rarity
                    )
                    {
                        continue;
                    }


                    // =================================================
                    // VEIN CENTER
                    // =================================================

                    int centerX =
                        currentCellX *
                        cellSize +
                        random.Next(
                            0,
                            cellSize
                        );


                    int centerY =
                        currentCellY *
                        cellSize +
                        random.Next(
                            0,
                            cellSize
                        );


                    // =================================================
                    // VEIN SIZE
                    // =================================================

                    int veinSize =
                        random.Next(
                            ore.MinVeinSize,
                            ore.MaxVeinSize + 1
                        );


                    float dx =
                        worldX -
                        centerX;


                    float dy =
                        worldY -
                        centerY;


                    float distance =
                        Mathf.Sqrt(
                            dx * dx +
                            dy * dy
                        );


                    if (
                        distance >
                        veinSize
                    )
                    {
                        continue;
                    }


                    // =================================================
                    // IRREGULARITY
                    // =================================================

                    int coordinateSeed =
                        GetSeed(
                            worldX,
                            worldY,
                            ore.ID
                        );


                    System.Random coordinateRandom =
                        new System.Random(
                            coordinateSeed
                        );


                    float shape =
                        0.75f +
                        (
                            (float)
                            coordinateRandom.NextDouble()
                            *
                            0.5f
                        );


                    if (
                        distance <=
                        veinSize *
                        shape
                    )
                    {

                        return true;

                    }

                }

            }


            return false;

        }


        // =====================================================
        // SEED
        // =====================================================

        private int GetSeed(
            int x,
            int y,
            string oreID
        )
        {

            unchecked
            {

                int hash =
                    settings.Seed;


                hash =
                    hash * 31 +
                    x;


                hash =
                    hash * 31 +
                    y;


                for (
                    int i = 0;
                    i < oreID.Length;
                    i++
                )
                {

                    hash =
                        hash * 31 +
                        oreID[i];

                }


                return hash;

            }

        }


        // =====================================================
        // DEBUG
        // =====================================================

        public void PrintStatistics()
        {

            Debug.Log(
                "========================================"
            );


            Debug.Log(
                "ORE GENERATION STATISTICS"
            );


            Debug.Log(
                "========================================"
            );


            foreach (
                OreData ore
                in ores
            )
            {

                OreStatistics stat =
                    statistics[
                        ore.ID
                    ];


                Debug.Log(
                    "ORE: " +
                    ore.ID
                );


                Debug.Log(
                    "VALID DEPTH CHECKS: " +
                    stat.ValidDepthChecks
                );


                Debug.Log(
                    "GENERATED BLOCKS: " +
                    stat.GeneratedBlocks
                );


                Debug.Log(
                    "========================================"
                );

            }

        }
        private int CalculateDepth(
    int worldY
)
        {

            int depth =
                settings.SurfaceHeight -
                worldY;


            return depth;

        }
    }


    // =========================================================
    // ORE DATA
    // =========================================================

    public class OreData
    {

        public string ID;

        public ushort BlockID;

        public int MinDepth;

        public int MaxDepth;

        public float Rarity;

        public int MinVeinSize;

        public int MaxVeinSize;

    }


    // =========================================================
    // STATISTICS
    // =========================================================

    public class OreStatistics
    {

        public int ValidDepthChecks;

        public int GeneratedBlocks;

    }

}