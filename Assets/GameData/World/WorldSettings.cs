using Game.Save;
using Game.World.Dimensions;


namespace Game.World
{
    public class WorldSettings
    {
        public int Seed;

        public int ChunkSize;

        public int ViewDistance;

        public int WorldHeight;

        public string WorldName;


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


        public int BaseSurfaceHeight;


        public float SoilDepthScale;

        public float SoilDepthBase;

        public float SoilDepthVariation;

        public float SoilDepthDetailScale;

        public float SoilDepthDetailStrength;


        public float LargeTerrainScale;

        public int LargeTerrainHeight;


        public float HillScale;

        public int HillHeight;


        public float MountainScale;

        public int MountainHeight;

        public float MountainDetailScale;

        public int MountainDetailHeight;


        public float MountainThreshold;

        public float MountainTransition;


        public float SurfaceDetailScale;

        public int SurfaceDetailHeight;


        public int CaveMinDepth;

        public int CaveMaxDepth;


        public WorldSettings()
        {
            ChunkSize =
                32;

            ViewDistance =
                5;

            WorldHeight =
                256;


            DimensionDefinition dimension =
                DimensionTravelRuntime.Current;


            Seed =
                dimension != null
                    ? dimension.Seed
                    : 0;


            WorldName =
                !string.IsNullOrWhiteSpace(
                    SaveGameRuntime.CurrentSaveName
                )
                    ? SaveGameRuntime.CurrentSaveName
                    : (
                        dimension != null
                            ? dimension.Name
                            : "New World"
                    );


            SurfaceHeight =
                100;

            BaseSurfaceHeight =
                100;


            SoilDepthScale =
                0.012f;

            SoilDepthBase =
                8f;

            SoilDepthVariation =
                5f;

            SoilDepthDetailScale =
                0.035f;

            SoilDepthDetailStrength =
                1.5f;


            LargeTerrainScale =
                0.0018f;

            LargeTerrainHeight =
                32;


            HillScale =
                0.0065f;

            HillHeight =
                10;


            MountainScale =
                0.0028f;

            MountainHeight =
                60;

            MountainDetailScale =
                0.014f;

            MountainDetailHeight =
                18;


            MountainThreshold =
                0.57f;

            MountainTransition =
                0.16f;


            SurfaceDetailScale =
                0.045f;

            SurfaceDetailHeight =
                4;


            CaveMinDepth =
                14;

            CaveMaxDepth =
                WorldHeight -
                20;
        }
    }
}
