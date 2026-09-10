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
        // CONSTRUCTOR
        // =====================================================

        public OreGenerator(
            WorldSettings settings
        )
        {
            this.settings =
                settings;
        }


        // =====================================================
        // RELOAD
        // =====================================================

        public void ReloadOres()
        {
            LoadOres();
        }


        // =====================================================
        // LOAD
        // =====================================================

        private void LoadOres()
        {
            ores.Clear();


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
                    continue;
                }


                // =================================================
                // BLOCK
                // =================================================

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


                // =================================================
                // BIOMES
                // =================================================

                string[] biomes =
                    NormalizeBiomes(
                        definition.Biomes
                    );


                // =================================================
                // DATA
                // =================================================

                OreData data =
                    new OreData
                    {
                        ID =
                            definition.ID,

                        BlockID =
                            blockID,

                        MinDepth =
                            Mathf.Max(
                                definition.MinDepth,
                                0
                            ),

                        MaxDepth =
                            Mathf.Max(
                                definition.MaxDepth,
                                definition.MinDepth
                            ),

                        Rarity =
                            Mathf.Clamp01(
                                definition.Rarity
                            ),

                        MinVeinSize =
                            Mathf.Max(
                                definition.MinVeinSize,
                                1
                            ),

                        MaxVeinSize =
                            Mathf.Max(
                                definition.VeinSize,
                                definition.MinVeinSize
                            ),

                        Biomes =
                            biomes
                    };


                ores.Add(
                    data
                );


                // =================================================
                // DEBUG
                // =================================================

                Debug.Log(
                    "ORE REGISTERED: " +
                    data.ID +
                    " | DEPTH: " +
                    data.MinDepth +
                    "-" +
                    data.MaxDepth +
                    " | BIOMES: " +
                    GetBiomeDebugString(
                        data.Biomes
                    )
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
            int surfaceHeight,
            string biomeID
        )
        {
            if (
                ores.Count ==
                0
            )
            {
                return 0;
            }


            int depth =
                surfaceHeight -
                worldY;


            // =================================================
            // ABOVE SURFACE
            // =================================================

            if (
                depth <=
                0
            )
            {
                return 0;
            }


            // =================================================
            // CHECK ORES
            // =================================================

            for (
                int i = 0;
                i < ores.Count;
                i++
            )
            {
                OreData ore =
                    ores[i];


                // =================================================
                // DEPTH
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
                // BIOME
                // =================================================

                if (
                    !CanGenerateInBiome(
                        ore,
                        biomeID
                    )
                )
                {
                    continue;
                }


                // =================================================
                // VEIN
                // =================================================

                if (
                    IsOrePosition(
                        worldX,
                        worldY,
                        ore
                    )
                )
                {
                    return ore.BlockID;
                }
            }


            return 0;
        }


        // =====================================================
        // OLD API
        // =====================================================

        public ushort GetOre(
            int worldX,
            int worldY,
            int surfaceHeight
        )
        {
            // -------------------------------------------------
            // Оставлено, чтобы старый WorldGenerator
            // продолжал компилироваться.
            //
            // Но руда, у которой задан список Biomes,
            // через этот метод НЕ появится.
            //
            // Для новой системы нужно передавать biomeID.
            // -------------------------------------------------

            return GetOre(
                worldX,
                worldY,
                surfaceHeight,
                null
            );
        }


        // =====================================================
        // BIOME CHECK
        // =====================================================

        private bool CanGenerateInBiome(
            OreData ore,
            string biomeID
        )
        {
            if (
                ore ==
                null
            )
            {
                return false;
            }


            // -------------------------------------------------
            // Нет ограничения по биомам.
            //
            // Старые JSON без Biomes продолжают работать.
            // -------------------------------------------------

            if (
                ore.Biomes ==
                null ||
                ore.Biomes.Length ==
                0
            )
            {
                return true;
            }


            // -------------------------------------------------
            // Если руда ограничена биомами,
            // но нам не сообщили текущий биом,
            // генерировать её нельзя.
            // -------------------------------------------------

            if (
                string.IsNullOrWhiteSpace(
                    biomeID
                )
            )
            {
                return false;
            }


            for (
                int i = 0;
                i < ore.Biomes.Length;
                i++
            )
            {
                string allowedBiome =
                    ore.Biomes[i];


                if (
                    string.IsNullOrWhiteSpace(
                        allowedBiome
                    )
                )
                {
                    continue;
                }


                // =============================================
                // ALL BIOMES
                // =============================================

                if (
                    allowedBiome ==
                    "*"
                )
                {
                    return true;
                }


                // =============================================
                // EXACT BIOME
                // =============================================

                if (
                    string.Equals(
                        allowedBiome,
                        biomeID,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return true;
                }
            }


            return false;
        }


        // =====================================================
        // NORMALIZE BIOMES
        // =====================================================

        private string[] NormalizeBiomes(
            string[] source
        )
        {
            if (
                source ==
                null ||
                source.Length ==
                0
            )
            {
                return Array.Empty<string>();
            }


            List<string> result =
                new List<string>();


            HashSet<string> added =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase
                );


            for (
                int i = 0;
                i < source.Length;
                i++
            )
            {
                string biome =
                    source[i];


                if (
                    string.IsNullOrWhiteSpace(
                        biome
                    )
                )
                {
                    continue;
                }


                biome =
                    biome.Trim();


                if (
                    added.Add(
                        biome
                    )
                )
                {
                    result.Add(
                        biome
                    );
                }
            }


            return result.ToArray();
        }


        // =====================================================
        // DEBUG BIOMES
        // =====================================================

        private string GetBiomeDebugString(
            string[] biomes
        )
        {
            if (
                biomes ==
                null ||
                biomes.Length ==
                0
            )
            {
                return "ALL";
            }


            return string.Join(
                ", ",
                biomes
            );
        }


        // =====================================================
        // ORE POSITION
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
                FloorDiv(
                    worldX,
                    cellSize
                );


            int cellY =
                FloorDiv(
                    worldY,
                    cellSize
                );


            // =================================================
            // NEIGHBOUR CELLS
            // =================================================

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


                    // =========================================
                    // SPAWN CHANCE
                    // =========================================

                    float spawnChance =
                        Hash01(
                            currentCellX,
                            currentCellY,
                            ore.ID
                        );


                    if (
                        spawnChance >
                        ore.Rarity
                    )
                    {
                        continue;
                    }


                    // =========================================
                    // CENTER X
                    // =========================================

                    int centerX =
                        currentCellX *
                        cellSize +

                        Mathf.FloorToInt(
                            Hash01(
                                currentCellX,
                                currentCellY,
                                ore.ID +
                                "_X"
                            )
                            *
                            cellSize
                        );


                    // =========================================
                    // CENTER Y
                    // =========================================

                    int centerY =
                        currentCellY *
                        cellSize +

                        Mathf.FloorToInt(
                            Hash01(
                                currentCellX,
                                currentCellY,
                                ore.ID +
                                "_Y"
                            )
                            *
                            cellSize
                        );


                    // =========================================
                    // VEIN SIZE
                    // =========================================

                    int veinSize =
                        Mathf.RoundToInt(
                            Mathf.Lerp(
                                ore.MinVeinSize,
                                ore.MaxVeinSize,

                                Hash01(
                                    currentCellX,
                                    currentCellY,
                                    ore.ID +
                                    "_SIZE"
                                )
                            )
                        );


                    veinSize =
                        Mathf.Clamp(
                            veinSize,
                            ore.MinVeinSize,
                            ore.MaxVeinSize
                        );


                    // =========================================
                    // DISTANCE
                    // =========================================

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


                    // =========================================
                    // IRREGULAR SHAPE
                    // =========================================

                    float shape =
                        0.75f +

                        Hash01(
                            worldX,
                            worldY,
                            ore.ID +
                            "_SHAPE"
                        )
                        *
                        0.5f;


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
        // HASH
        // =====================================================

        private float Hash01(
            int x,
            int y,
            string value
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
                    i < value.Length;
                    i++
                )
                {
                    hash =
                        hash * 31 +
                        value[i];
                }


                hash ^=
                    hash >>
                    13;


                hash *=
                    1274126177;


                hash ^=
                    hash >>
                    16;


                return
                    (
                        hash &
                        0x7fffffff
                    )
                    /
                    2147483647f;
            }
        }


        // =====================================================
        // FLOOR DIV
        // =====================================================

        private int FloorDiv(
            int value,
            int divisor
        )
        {
            int result =
                value /
                divisor;


            int remainder =
                value %
                divisor;


            if (
                remainder !=
                0 &&
                remainder <
                0
            )
            {
                result--;
            }


            return result;
        }
    }


    // =========================================================
    // ORE DATA
    // =========================================================

    public class OreData
    {
        public string ID;

        public ushort BlockID;


        // =====================================================
        // DEPTH
        // =====================================================

        public int MinDepth;

        public int MaxDepth;


        // =====================================================
        // GENERATION
        // =====================================================

        public float Rarity;

        public int MinVeinSize;

        public int MaxVeinSize;


        // =====================================================
        // BIOMES
        // =====================================================

        public string[] Biomes;
    }
}