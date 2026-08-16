using Game.Blocks;
using Game.Content;

namespace Game.World.Lighting
{
    public class SunlightPropagator
    {
        private readonly World world;


        public SunlightPropagator(
            World world
        )
        {
            this.world =
                world;
        }


        // =====================================================
        // GENERATE CHUNK SUNLIGHT
        // =====================================================

        public void GenerateChunk(
            Chunk chunk
        )
        {
            if (
                chunk == null
            )
            {
                return;
            }


            ChunkLightData light =
                chunk.GetLightData();


            light.Clear();


            int originX =
                chunk.X *
                Chunk.SizeX;


            int originY =
                chunk.Y *
                Chunk.SizeY;


            // =================================================
            // EACH COLUMN
            // =================================================

            for (
                int x = 0;
                x < Chunk.SizeX;
                x++
            )
            {
                GenerateColumn(
                    chunk,
                    light,
                    originX + x,
                    originY,
                    x
                );
            }
        }


        // =====================================================
        // COLUMN
        // =====================================================

        private void GenerateColumn(
            Chunk chunk,
            ChunkLightData light,
            int worldX,
            int originY,
            int localX
        )
        {
            byte sunlight =
                15;


            // =================================================
            // TOP -> BOTTOM
            // =================================================

            for (
                int localY = Chunk.SizeY - 1;
                localY >= 0;
                localY--
            )
            {
                int worldY =
                    originY +
                    localY;


                ushort blockID =
                    world.GetBlock(
                        worldX,
                        worldY
                    );


                // =============================================
                // AIR
                // =============================================

                if (
                    blockID == 0
                )
                {
                    if (
                        sunlight > 0
                    )
                    {
                        light.SetSunlight(
                            localX,
                            localY,
                            sunlight
                        );
                    }


                    continue;
                }


                // =============================================
                // BLOCK
                // =============================================

                BlockDefinition block =
                    GetBlock(
                        blockID
                    );


                if (
                    block == null
                )
                {
                    sunlight =
                        0;

                    continue;
                }


                // =============================================
                // LIGHT BLOCKING
                // =============================================

                if (
                    block.BlocksLight
                )
                {
                    sunlight =
                        ReduceSunlight(
                            sunlight,
                            block.LightOpacity
                        );
                }


                // =============================================
                // STORE
                // =============================================

                if (
                    sunlight > 0
                )
                {
                    light.SetSunlight(
                        localX,
                        localY,
                        sunlight
                    );
                }


                // =============================================
                // COMPLETELY DARK
                // =============================================

                if (
                    sunlight == 0
                )
                {
                    break;
                }
            }
        }


        // =====================================================
        // SUNLIGHT REDUCTION
        // =====================================================

        private byte ReduceSunlight(
            byte current,
            byte opacity
        )
        {
            if (
                current == 0
            )
            {
                return 0;
            }


            if (
                opacity >= current
            )
            {
                return 0;
            }


            return
                (byte)(
                    current -
                    opacity
                );
        }


        // =====================================================
        // GET BLOCK
        // =====================================================

        private BlockDefinition GetBlock(
            ushort blockID
        )
        {
            if (
                blockID == 0
            )
            {
                return null;
            }


            ContentID contentID =
                BlockIDRegistry.GetContentID(
                    blockID
                );


            if (
                !BlockRegistry.Contains(
                    contentID
                )
            )
            {
                return null;
            }


            return
                BlockRegistry.Get(
                    contentID
                );
        }
    }
}