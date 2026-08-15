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

        // Базовая высота поверхности.
        public int BaseSurfaceHeight;

        // =====================================================
        // SOIL
        // =====================================================

        public float SoilDepthScale;

        public float SoilDepthBase;

        public float SoilDepthVariation;

        public float SoilDepthDetailScale;

        public float SoilDepthDetailStrength;

        // =====================================================
        // LARGE LANDSCAPE
        // =====================================================

        // Очень крупные изменения высоты.
        public float LargeTerrainScale;

        public int LargeTerrainHeight;


        // =====================================================
        // HILLS
        // =====================================================

        // Обычные холмы.
        public float HillScale;

        public int HillHeight;


        // =====================================================
        // MOUNTAINS
        // =====================================================

        // Частота расположения гор.
        public float MountainScale;

        // Максимальная высота гор.
        public int MountainHeight;

        // Детализация гор.
        public float MountainDetailScale;

        public int MountainDetailHeight;


        // =====================================================
        // MOUNTAIN SHAPE
        // =====================================================

        // Чем выше значение, тем реже горы.
        public float MountainThreshold;

        // Насколько мягко начинается гора.
        public float MountainTransition;


        // =====================================================
        // SURFACE DETAIL
        // =====================================================

        public float SurfaceDetailScale;

        public int SurfaceDetailHeight;


        // =====================================================
        // CAVES
        // =====================================================

        public int CaveMinDepth;

        public int CaveMaxDepth;


        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public WorldSettings()
        {
            // -------------------------------------------------
            // WORLD
            // -------------------------------------------------

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


            // -------------------------------------------------
            // BASE SURFACE
            // -------------------------------------------------

            SurfaceHeight =
                100;

            BaseSurfaceHeight =
                100;


            SoilDepthScale = 0.012f;

            SoilDepthBase =
                8f;

            SoilDepthVariation =
                5f;

            SoilDepthDetailScale =
                0.035f;

            SoilDepthDetailStrength =
                1.5f;

            // -------------------------------------------------
            // LARGE LANDSCAPE
            // -------------------------------------------------

            LargeTerrainScale =
                0.0018f;

            LargeTerrainHeight =
                32;


            // -------------------------------------------------
            // HILLS
            // -------------------------------------------------

            HillScale =
                0.0065f;

            HillHeight =
                10;


            // -------------------------------------------------
            // MOUNTAINS
            // -------------------------------------------------

            MountainScale =
                0.0028f;

            MountainHeight =
                60;

            MountainDetailScale =
                0.014f;

            MountainDetailHeight =
                18;


            // -------------------------------------------------
            // MOUNTAIN SHAPE
            // -------------------------------------------------

            MountainThreshold =
                0.57f;

            MountainTransition =
                0.16f;


            // -------------------------------------------------
            // SURFACE DETAIL
            // -------------------------------------------------

            SurfaceDetailScale =
                0.045f;

            SurfaceDetailHeight =
                4;


            // -------------------------------------------------
            // CAVES
            // -------------------------------------------------

            CaveMinDepth =
                14;

            CaveMaxDepth =
                WorldHeight -
                20;
        }
    }
}