using System.Collections.Generic;
using Game.Blocks;

namespace Game.World.Lighting
{
    public class LightPropagationEngine
    {
        private readonly ILightWorld world;


        private readonly Queue<LightNode>
            propagationQueue =
                new Queue<LightNode>();


        private readonly Queue<LightNode>
            removalQueue =
                new Queue<LightNode>();


        private static readonly int[] OffsetX =
        {
            1,
            -1,
            0,
            0
        };


        private static readonly int[] OffsetY =
        {
            0,
            0,
            1,
            -1
        };


        public LightPropagationEngine(
            ILightWorld world
        )
        {
            this.world =
                world;
        }


        // =====================================================
        // ADD LIGHT SOURCE
        // =====================================================

        public void AddLightSource(
            LightSource source
        )
        {
            if (
                !world.IsLoaded(
                    source.X,
                    source.Y
                )
            )
            {
                return;
            }


            LightNode node =
                new LightNode(
                    source.X,
                    source.Y,
                    source.R,
                    source.G,
                    source.B,
                    source.Sunlight
                );


            propagationQueue.Enqueue(
                node
            );


            ProcessPropagation();
        }


        // =====================================================
        // REMOVE LIGHT SOURCE
        // =====================================================

        public void RemoveLightSource(
            int worldX,
            int worldY
        )
        {
            if (
                !world.IsLoaded(
                    worldX,
                    worldY
                )
            )
            {
                return;
            }


            ChunkLightData light =
                world.GetLightData(
                    worldX,
                    worldY
                );


            if (light == null)
            {
                return;
            }


            byte oldR =
                GetLocalRed(
                    light,
                    worldX,
                    worldY
                );


            byte oldG =
                GetLocalGreen(
                    light,
                    worldX,
                    worldY
                );


            byte oldB =
                GetLocalBlue(
                    light,
                    worldX,
                    worldY
                );


            byte oldSun =
                GetLocalSunlight(
                    light,
                    worldX,
                    worldY
                );


            if (
                oldR == 0 &&
                oldG == 0 &&
                oldB == 0 &&
                oldSun == 0
            )
            {
                return;
            }


            world.SetLight(
                worldX,
                worldY,
                0,
                0,
                0,
                0
            );


            removalQueue.Enqueue(
                new LightNode(
                    worldX,
                    worldY,
                    oldR,
                    oldG,
                    oldB,
                    oldSun
                )
            );


            ProcessRemoval();
        }


        // =====================================================
        // PROPAGATION
        // =====================================================

        private void ProcessPropagation()
        {
            while (
                propagationQueue.Count > 0
            )
            {
                LightNode node =
                    propagationQueue.Dequeue();


                for (
                    int i = 0;
                    i < 4;
                    i++
                )
                {
                    int nx =
                        node.X +
                        OffsetX[i];


                    int ny =
                        node.Y +
                        OffsetY[i];


                    if (
                        !world.IsLoaded(
                            nx,
                            ny
                        )
                    )
                    {
                        continue;
                    }


                    ushort blockID =
                        world.GetBlock(
                            nx,
                            ny
                        );


                    BlockDefinition block =
                        world.GetBlockDefinition(
                            blockID
                        );


                    if (block == null)
                    {
                        continue;
                    }


                    byte opacity =
                        block.LightOpacity;


                    if (
                        opacity >= 15
                    )
                    {
                        continue;
                    }


                    ChunkLightData light =
                        world.GetLightData(
                            nx,
                            ny
                        );


                    if (light == null)
                    {
                        continue;
                    }


                    byte oldR =
                        GetLocalRed(
                            light,
                            nx,
                            ny
                        );


                    byte oldG =
                        GetLocalGreen(
                            light,
                            nx,
                            ny
                        );


                    byte oldB =
                        GetLocalBlue(
                            light,
                            nx,
                            ny
                        );


                    byte oldSun =
                        GetLocalSunlight(
                            light,
                            nx,
                            ny
                        );


                    byte newR =
                        Attenuate(
                            node.R,
                            opacity
                        );


                    byte newG =
                        Attenuate(
                            node.G,
                            opacity
                        );


                    byte newB =
                        Attenuate(
                            node.B,
                            opacity
                        );


                    byte newSun =
                        AttenuateSunlight(
                            node.Sunlight,
                            opacity
                        );


                    bool changed =
                        false;


                    if (
                        newR > oldR
                    )
                    {
                        oldR =
                            newR;

                        changed =
                            true;
                    }


                    if (
                        newG > oldG
                    )
                    {
                        oldG =
                            newG;

                        changed =
                            true;
                    }


                    if (
                        newB > oldB
                    )
                    {
                        oldB =
                            newB;

                        changed =
                            true;
                    }


                    if (
                        newSun > oldSun
                    )
                    {
                        oldSun =
                            newSun;

                        changed =
                            true;
                    }


                    if (!changed)
                    {
                        continue;
                    }


                    world.SetLight(
                        nx,
                        ny,
                        oldSun,
                        oldR,
                        oldG,
                        oldB
                    );


                    propagationQueue.Enqueue(
                        new LightNode(
                            nx,
                            ny,
                            oldR,
                            oldG,
                            oldB,
                            oldSun
                        )
                    );
                }
            }
        }


        // =====================================================
        // REMOVAL
        // =====================================================

        private void ProcessRemoval()
        {
            Queue<LightNode> relightQueue =
                new Queue<LightNode>();


            while (
                removalQueue.Count > 0
            )
            {
                LightNode removed =
                    removalQueue.Dequeue();


                for (
                    int i = 0;
                    i < 4;
                    i++
                )
                {
                    int nx =
                        removed.X +
                        OffsetX[i];


                    int ny =
                        removed.Y +
                        OffsetY[i];


                    if (
                        !world.IsLoaded(
                            nx,
                            ny
                        )
                    )
                    {
                        continue;
                    }


                    ChunkLightData light =
                        world.GetLightData(
                            nx,
                            ny
                        );


                    if (light == null)
                    {
                        continue;
                    }


                    byte r =
                        GetLocalRed(
                            light,
                            nx,
                            ny
                        );


                    byte g =
                        GetLocalGreen(
                            light,
                            nx,
                            ny
                        );


                    byte b =
                        GetLocalBlue(
                            light,
                            nx,
                            ny
                        );


                    byte sun =
                        GetLocalSunlight(
                            light,
                            nx,
                            ny
                        );


                    /*
                     * Если соседний блок всё ещё содержит свет,
                     * значит он потенциально может быть источником
                     * света для дальнейшей области.
                     */

                    if (
                        r > 0 ||
                        g > 0 ||
                        b > 0 ||
                        sun > 0
                    )
                    {
                        relightQueue.Enqueue(
                            new LightNode(
                                nx,
                                ny,
                                r,
                                g,
                                b,
                                sun
                            )
                        );


                        continue;
                    }
                }
            }


            /*
             * Повторно распространяем оставшийся свет.
             */

            while (
                relightQueue.Count > 0
            )
            {
                LightNode node =
                    relightQueue.Dequeue();


                propagationQueue.Enqueue(
                    node
                );
            }


            ProcessPropagation();
        }


        // =====================================================
        // REBUILD
        // =====================================================

        private void RebuildNearbyLight()
        {
            /*
             * После удаления источника нам необходимо
             * восстановить свет от других источников.
             *
             * Для этого смотрим соседние клетки.
             *
             * Если там остался свет,
             * снова запускаем propagation.
             */

            propagationQueue.Clear();


            for (
                int i = 0;
                i < removalQueue.Count;
                i++
            )
            {
                // Очередь уже очищена выше,
                // поэтому здесь намеренно ничего
                // не делаем.
            }
        }


        // =====================================================
        // ATTENUATION
        // =====================================================

        private byte Attenuate(
            byte value,
            byte opacity
        )
        {
            if (
                value == 0
            )
            {
                return 0;
            }


            int result =
                value -
                1 -
                opacity;


            if (
                result <= 0
            )
            {
                return 0;
            }


            if (
                result > 15
            )
            {
                result = 15;
            }


            return
                (byte)result;
        }


        private byte AttenuateSunlight(
            byte value,
            byte opacity
        )
        {
            if (
                value == 0
            )
            {
                return 0;
            }


            if (
                opacity >= 15
            )
            {
                return 0;
            }


            int result =
                value -
                1 -
                opacity;


            if (
                result <= 0
            )
            {
                return 0;
            }


            if (
                result > 15
            )
            {
                result = 15;
            }


            return
                (byte)result;
        }


        // =====================================================
        // AFFECTED TEST
        // =====================================================

        private bool IsAffected(
            LightNode source,
            byte currentR,
            byte currentG,
            byte currentB,
            byte currentSun
        )
        {
            return
                currentR <= source.R ||
                currentG <= source.G ||
                currentB <= source.B ||
                currentSun <= source.Sunlight;
        }


        // =====================================================
        // LOCAL COORDINATES
        // =====================================================

        private byte GetLocalRed(
            ChunkLightData data,
            int worldX,
            int worldY
        )
        {
            int x =
                Mod(
                    worldX,
                    data.Width
                );


            int y =
                Mod(
                    worldY,
                    data.Height
                );


            return
                data.GetRed(
                    x,
                    y
                );
        }


        private byte GetLocalGreen(
            ChunkLightData data,
            int worldX,
            int worldY
        )
        {
            int x =
                Mod(
                    worldX,
                    data.Width
                );


            int y =
                Mod(
                    worldY,
                    data.Height
                );


            return
                data.GetGreen(
                    x,
                    y
                );
        }


        private byte GetLocalBlue(
            ChunkLightData data,
            int worldX,
            int worldY
        )
        {
            int x =
                Mod(
                    worldX,
                    data.Width
                );


            int y =
                Mod(
                    worldY,
                    data.Height
                );


            return
                data.GetBlue(
                    x,
                    y
                );
        }


        private byte GetLocalSunlight(
            ChunkLightData data,
            int worldX,
            int worldY
        )
        {
            int x =
                Mod(
                    worldX,
                    data.Width
                );


            int y =
                Mod(
                    worldY,
                    data.Height
                );


            return
                data.GetSunlight(
                    x,
                    y
                );
        }


        private int Mod(
            int value,
            int size
        )
        {
            int result =
                value % size;


            if (
                result < 0
            )
            {
                result += size;
            }


            return result;
        }
    }
}