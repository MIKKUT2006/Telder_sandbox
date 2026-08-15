using System;

namespace Game.World
{
    public class WorldSettings
    {
        // =====================================================
        // WORLD
        // =====================================================

        public int Seed;

        public int ChunkSize;

        public int ViewDistance;

        public int WorldHeight;

        public string WorldName;


        // =====================================================
        // VERTICAL
        // =====================================================

        public int SurfaceHeight;

        public int BottomWorldY
        {
            get
            {
                return
                    SurfaceHeight -
                    WorldHeight +
                    1;
            }
        }


        // =====================================================
        // TERRAIN
        // =====================================================

        public int HillHeight;

        public float HillScale;

        public int TerrainVariation;

        public float TerrainScale;

        public int TerrainDetail;

        public float TerrainDetailScale;


        // =====================================================
        // MOUNTAINS
        // =====================================================

        public float MountainScale;

        public int MountainHeight;

        public float MountainDetailScale;

        public int MountainDetailHeight;

        public float MountainPower;


        


        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public WorldSettings()
        {
            ChunkSize =
                32;

            ViewDistance =
                5;

            WorldHeight =
                256;

            WorldName =
                "New World";

            Seed =
                new Random().Next();


            // =================================================
            // SURFACE
            // =================================================

            SurfaceHeight =
                100;


            // =================================================
            // LARGE HILLS
            // =================================================

            HillHeight =
                26;

            HillScale =
                0.0032f;


            // =================================================
            // MEDIUM TERRAIN
            // =================================================

            TerrainVariation =
                18;

            TerrainScale =
                0.011f;


            // =================================================
            // SMALL DETAIL
            // =================================================

            TerrainDetail =
                5;

            TerrainDetailScale =
                0.045f;


            // =================================================
            // MOUNTAINS
            // =================================================

            MountainScale =
                0.00125f;

            MountainHeight =
                72;

            MountainDetailScale =
                0.010f;

            MountainDetailHeight =
                24;

            MountainPower =
                1.75f;


            
        }
    }
}