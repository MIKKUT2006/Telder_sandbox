using UnityEngine;

namespace Game.World.Generation
{
    public class CaveGenerator
    {

        // =====================================================
        // SETTINGS
        // =====================================================

        private readonly WorldSettings worldSettings;
        private readonly CaveSettings settings;


        // =====================================================
        // SEEDS
        // =====================================================

        private readonly float tunnelSeed;
        private readonly float roomSeed;
        private readonly float entranceSeed;


        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public CaveGenerator(
            WorldSettings worldSettings,
            CaveSettings settings
        )
        {

            this.worldSettings =
                worldSettings;

            this.settings =
                settings;


            tunnelSeed =
                worldSettings.Seed *
                0.17321f;

            roomSeed =
                worldSettings.Seed *
                0.73129f;

            entranceSeed =
                worldSettings.Seed *
                1.91371f;

        }


        // =====================================================
        // PUBLIC
        // =====================================================

        public bool IsCave(
            int worldX,
            int worldY,
            int surfaceHeight
        )
        {

            // -------------------------------------------------
            // ABOVE SURFACE
            // -------------------------------------------------

            if (
                worldY >
                surfaceHeight
            )
            {
                return false;
            }


            // -------------------------------------------------
            // DEPTH
            // -------------------------------------------------

            int depth =
                surfaceHeight -
                worldY;


            // -------------------------------------------------
            // SURFACE ENTRANCE
            // -------------------------------------------------

            if (
                settings.EnableSurfaceEntrances
            )
            {

                if (
                    IsSurfaceEntrance(
                        worldX,
                        worldY,
                        surfaceHeight
                    )
                )
                {
                    return true;
                }

            }


            // -------------------------------------------------
            // SURFACE PROTECTION
            // -------------------------------------------------

            if (
                depth <
                settings.SurfaceProtectionDepth
            )
            {
                return false;
            }


            // -------------------------------------------------
            // NORMAL CAVES
            // -------------------------------------------------

            if (
                depth <
                settings.StartDepth
            )
            {
                return false;
            }


            // -------------------------------------------------
            // TUNNELS
            // -------------------------------------------------

            if (
                IsTunnel(
                    worldX,
                    worldY,
                    surfaceHeight
                )
            )
            {
                return true;
            }


            // -------------------------------------------------
            // ROOMS
            // -------------------------------------------------

            if (
                IsRoom(
                    worldX,
                    worldY,
                    surfaceHeight
                )
            )
            {
                return true;
            }


            return false;
        }


        // =====================================================
        // TUNNEL
        // =====================================================

        private bool IsTunnel(
            int worldX,
            int worldY,
            int surfaceHeight
        )
        {

            float regionSize =
                settings.TunnelRegionSize;


            int regionX =
                Mathf.FloorToInt(
                    worldX /
                    regionSize
                );


            int regionY =
                Mathf.FloorToInt(
                    worldY /
                    regionSize
                );


            // Проверяем соседние регионы.
            //
            // Это важно, чтобы тоннель не обрывался
            // ровно на границе области.

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

                    int cellX =
                        regionX +
                        offsetX;


                    int cellY =
                        regionY +
                        offsetY;


                    if (
                        IsInsideTunnelSystem(
                            worldX,
                            worldY,
                            surfaceHeight,
                            cellX,
                            cellY
                        )
                    )
                    {
                        return true;
                    }

                }

            }


            return false;
        }


        // =====================================================
        // TUNNEL SYSTEM
        // =====================================================

        private bool IsInsideTunnelSystem(
            int worldX,
            int worldY,
            int surfaceHeight,
            int cellX,
            int cellY
        )
        {

            // -------------------------------------------------
            // RANDOM
            // -------------------------------------------------

            float spawn =
                Hash01(
                    cellX,
                    cellY,
                    100
                );


            if (
                spawn >
                settings.TunnelSpawnChance *
                settings.CaveDensity
            )
            {
                return false;
            }


            // -------------------------------------------------
            // CENTER
            // -------------------------------------------------

            float centerX =
                (
                    cellX +
                    0.5f +
                    (
                        Hash01(
                            cellX,
                            cellY,
                            101
                        ) -
                        0.5f
                    )
                    *
                    0.7f
                )
                *
                settings.TunnelRegionSize;


            float centerY =
                (
                    cellY +
                    0.5f +
                    (
                        Hash01(
                            cellX,
                            cellY,
                            102
                        ) -
                        0.5f
                    )
                    *
                    0.7f
                )
                *
                settings.TunnelRegionSize;


            // -------------------------------------------------
            // DEPTH
            // -------------------------------------------------

            float minDepth =
                settings.StartDepth +
                10f;


            float caveDepth =
                surfaceHeight -
                centerY;


            if (
                caveDepth <
                minDepth
            )
            {
                return false;
            }


            // -------------------------------------------------
            // DIRECTION
            // -------------------------------------------------

            float angle =
                Hash01(
                    cellX,
                    cellY,
                    103
                )
                *
                Mathf.PI *
                2f;


            Vector2 direction =
                new Vector2(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle)
                );


            // -------------------------------------------------
            // LENGTH
            // -------------------------------------------------

            float length =
                settings.TunnelLength +
                (
                    Hash01(
                        cellX,
                        cellY,
                        104
                    ) -
                    0.5f
                )
                *
                settings.TunnelLengthVariation;


            // -------------------------------------------------
            // LOCAL POSITION
            // -------------------------------------------------

            Vector2 point =
                new Vector2(
                    worldX,
                    worldY
                );


            Vector2 center =
                new Vector2(
                    centerX,
                    centerY
                );


            Vector2 relative =
                point -
                center;


            float along =
                Vector2.Dot(
                    relative,
                    direction
                );


            // -------------------------------------------------
            // LENGTH CHECK
            // -------------------------------------------------

            if (
                Mathf.Abs(
                    along
                )
                >
                length
            )
            {
                return false;
            }


            // -------------------------------------------------
            // PERPENDICULAR DISTANCE
            // -------------------------------------------------

            float perpendicular =
                Mathf.Abs(
                    Vector2.Dot(
                        relative,
                        new Vector2(
                            -direction.y,
                            direction.x
                        )
                    )
                );


            // -------------------------------------------------
            // CURVE
            // -------------------------------------------------

            float curve =
                Mathf.PerlinNoise(
                    (
                        worldX +
                        tunnelSeed
                    )
                    *
                    settings.TunnelWanderScale,

                    (
                        worldY +
                        tunnelSeed
                    )
                    *
                    settings.TunnelWanderScale
                );


            float curveOffset =
                (
                    curve -
                    0.5f
                )
                *
                settings.TunnelWander;


            perpendicular =
                Mathf.Abs(
                    perpendicular -
                    curveOffset
                );


            // -------------------------------------------------
            // WIDTH
            // -------------------------------------------------

            float widthNoise =
                Mathf.PerlinNoise(
                    (
                        worldX +
                        tunnelSeed *
                        2f
                    )
                    *
                    0.04f,

                    (
                        worldY +
                        tunnelSeed *
                        2f
                    )
                    *
                    0.04f
                );


            float radius =
                Mathf.Lerp(
                    settings.TunnelRadiusMin,
                    settings.TunnelRadiusMax,
                    widthNoise
                );


            // -------------------------------------------------
            // SOFT EDGE
            // -------------------------------------------------

            return
                perpendicular <=
                radius;
        }


        // =====================================================
        // ROOM
        // =====================================================

        private bool IsRoom(
            int worldX,
            int worldY,
            int surfaceHeight
        )
        {

            float regionSize =
                settings.RoomRegionSize;


            int regionX =
                Mathf.FloorToInt(
                    worldX /
                    regionSize
                );


            int regionY =
                Mathf.FloorToInt(
                    worldY /
                    regionSize
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

                    int cellX =
                        regionX +
                        offsetX;


                    int cellY =
                        regionY +
                        offsetY;


                    if (
                        IsInsideRoom(
                            worldX,
                            worldY,
                            surfaceHeight,
                            cellX,
                            cellY
                        )
                    )
                    {
                        return true;
                    }

                }

            }


            return false;
        }


        // =====================================================
        // ROOM INSTANCE
        // =====================================================

        private bool IsInsideRoom(
            int worldX,
            int worldY,
            int surfaceHeight,
            int cellX,
            int cellY
        )
        {

            float spawn =
                Hash01(
                    cellX,
                    cellY,
                    200
                );


            if (
                spawn >
                settings.RoomSpawnChance
            )
            {
                return false;
            }


            // -------------------------------------------------
            // CENTER
            // -------------------------------------------------

            float centerX =
                (
                    cellX +
                    0.5f +
                    (
                        Hash01(
                            cellX,
                            cellY,
                            201
                        ) -
                        0.5f
                    )
                    *
                    0.7f
                )
                *
                settings.RoomRegionSize;


            float centerY =
                (
                    cellY +
                    0.5f +
                    (
                        Hash01(
                            cellX,
                            cellY,
                            202
                        ) -
                        0.5f
                    )
                    *
                    0.7f
                )
                *
                settings.RoomRegionSize;


            // -------------------------------------------------
            // DEPTH
            // -------------------------------------------------

            int depth =
                surfaceHeight -
                Mathf.RoundToInt(
                    centerY
                );


            if (
                depth <
                settings.StartDepth
            )
            {
                return false;
            }


            // -------------------------------------------------
            // RADIUS
            // -------------------------------------------------

            float radius =
                Mathf.Lerp(
                    settings.RoomRadiusMin,
                    settings.RoomRadiusMax,
                    Hash01(
                        cellX,
                        cellY,
                        203
                    )
                );


            // -------------------------------------------------
            // STRETCH
            // -------------------------------------------------

            float stretchX =
                Mathf.Lerp(
                    1f,
                    settings.RoomStretchX,
                    Hash01(
                        cellX,
                        cellY,
                        204
                    )
                );


            float stretchY =
                Mathf.Lerp(
                    1f,
                    settings.RoomStretchY,
                    Hash01(
                        cellX,
                        cellY,
                        205
                    )
                );


            // -------------------------------------------------
            // POSITION
            // -------------------------------------------------

            float dx =
                (
                    worldX -
                    centerX
                )
                /
                (
                    radius *
                    stretchX
                );


            float dy =
                (
                    worldY -
                    centerY
                )
                /
                (
                    radius *
                    stretchY
                );


            float distance =
                dx *
                dx +
                dy *
                dy;


            // -------------------------------------------------
            // WARP
            // -------------------------------------------------

            float warp =
                Mathf.PerlinNoise(
                    (
                        worldX +
                        roomSeed
                    )
                    *
                    settings.RoomWarpScale,

                    (
                        worldY +
                        roomSeed
                    )
                    *
                    settings.RoomWarpScale
                );


            float edge =
                (
                    warp -
                    0.5f
                )
                *
                settings.RoomWarpStrength;


            // -------------------------------------------------
            // DETAIL
            // -------------------------------------------------

            float detail =
                Mathf.PerlinNoise(
                    (
                        worldX +
                        roomSeed *
                        2f
                    )
                    *
                    settings.DetailScale,

                    (
                        worldY +
                        roomSeed *
                        2f
                    )
                    *
                    settings.DetailScale
                );


            // -------------------------------------------------
            // FINAL SHAPE
            // -------------------------------------------------

            return
                distance <
                1f +
                edge *
                0.7f +
                (
                    detail -
                    0.5f
                )
                *
                settings.DetailStrength;
        }


        // =====================================================
        // SURFACE ENTRANCE
        // =====================================================

        private bool IsSurfaceEntrance(
            int worldX,
            int worldY,
            int surfaceHeight
        )
        {

            int depth =
                surfaceHeight -
                worldY;


            // -------------------------------------------------
            // ONLY NEAR SURFACE
            // -------------------------------------------------

            if (
                depth <
                0 ||
                depth >
                settings.EntranceTunnelLength +
                10
            )
            {
                return false;
            }


            // -------------------------------------------------
            // FIND SURFACE REGION
            // -------------------------------------------------

            int cellX =
                Mathf.FloorToInt(
                    worldX /
                    80f
                );


            // -------------------------------------------------
            // RANDOM ENTRANCE
            // -------------------------------------------------

            float spawn =
                Hash01(
                    cellX,
                    0,
                    500
                );


            if (
                spawn >
                settings.SurfaceEntranceChance
            )
            {
                return false;
            }


            // -------------------------------------------------
            // ENTRANCE X
            // -------------------------------------------------

            float entranceX =
                (
                    cellX +
                    0.5f +
                    (
                        Hash01(
                            cellX,
                            0,
                            501
                        ) -
                        0.5f
                    )
                    *
                    0.7f
                )
                *
                80f;


            float dx =
                worldX -
                entranceX;


            // -------------------------------------------------
            // WANDER
            // -------------------------------------------------

            float wander =
                Mathf.Sin(
                    worldY *
                    0.08f +
                    entranceSeed
                )
                *
                settings.EntranceWander;


            float distance =
                Mathf.Abs(
                    dx -
                    wander
                );


            float radius =
                settings.EntranceTunnelRadius;


            // -------------------------------------------------
            // ENTRY
            // -------------------------------------------------

            if (
                distance >
                radius
            )
            {
                return false;
            }


            // -------------------------------------------------
            // TOP OPENING
            // -------------------------------------------------

            if (
                depth <=
                settings.EntranceRadius
            )
            {
                return true;
            }


            // -------------------------------------------------
            // TUNNEL DOWN
            // -------------------------------------------------

            if (
                depth <=
                settings.EntranceTunnelLength
            )
            {
                return true;
            }


            return false;
        }


        // =====================================================
        // HASH
        // =====================================================

        private float Hash01(
            int x,
            int y,
            int salt
        )
        {

            unchecked
            {

                int hash =
                    x *
                    374761393 +

                    y *
                    668265263 +

                    salt *
                    1442695041 +

                    worldSettings.Seed;


                hash =
                    (
                        hash ^
                        (
                            hash >>
                            13
                        )
                    )
                    *
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

    }
}