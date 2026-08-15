using Game.Content;
using System.Collections.Generic;
using UnityEngine;

namespace Game.World.Generation.Ores
{
    public class OreGenerator
    {
        private readonly WorldSettings settings;

        private readonly List<OreData> ores =
            new List<OreData>();


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
                            )
                    };


                ores.Add(
                    data
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
                ores.Count ==
                0
            )
            {
                return 0;
            }


            int depth =
                surfaceHeight -
                worldY;


            if (
                depth <=
                0
            )
            {
                return 0;
            }


            for (
                int i = 0;
                i < ores.Count;
                i++
            )
            {
                OreData ore =
                    ores[i];


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


                    int veinSize =
                        (int)Mathf.Lerp(
                            ore.MinVeinSize,

                            ore.MaxVeinSize,

                            Hash01(
                                currentCellX,
                                currentCellY,
                                ore.ID +
                                "_SIZE"
                            )
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


    // =====================================================
    // ORE DATA
    // =====================================================

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
}