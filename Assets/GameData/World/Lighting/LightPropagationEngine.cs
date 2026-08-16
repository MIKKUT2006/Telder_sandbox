using System.Collections.Generic;
using Game.Blocks;

namespace Game.World.Lighting
{
    public class LightPropagationEngine
    {
        private readonly ILightWorld world;

        private readonly Queue<LightNodePosition>
            propagationQueue =
                new Queue<LightNodePosition>();

        private readonly Queue<LightNodePosition>
            removalQueue =
                new Queue<LightNodePosition>();

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
            this.world = world;
        }

        // =====================================================
        // ADD SOURCE
        // =====================================================

        public void AddLightSource(
            LightSource source
        )
        {
            if (source == null)
                return;

            if (!source.Enabled)
                return;

            if (
                !world.IsLoaded(
                    source.X,
                    source.Y
                )
            )
            {
                return;
            }

            LightNode light =
                source.ToLight();

            propagationQueue.Enqueue(
                new LightNodePosition(
                    source.X,
                    source.Y,
                    light
                )
            );

            ProcessPropagation();
        }

        // =====================================================
        // REMOVE SOURCE
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

            LightNode old =
                world.GetLight(
                    worldX,
                    worldY
                );

            if (old.IsEmpty)
                return;

            world.SetLight(
                worldX,
                worldY,
                0,
                0,
                0,
                0
            );

            removalQueue.Enqueue(
                new LightNodePosition(
                    worldX,
                    worldY,
                    old
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
                LightNodePosition node =
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
                        continue;

                    byte opacity =
                        block.LightOpacity;

                    if (opacity >= 15)
                        continue;

                    LightNode current =
                        world.GetLight(
                            nx,
                            ny
                        );

                    LightNode next =
                        Attenuate(
                            node.Light,
                            opacity
                        );

                    if (next.IsEmpty)
                        continue;

                    LightNode result =
                        MaxLight(
                            current,
                            next
                        );

                    if (
                        SameLight(
                            current,
                            result
                        )
                    )
                    {
                        continue;
                    }

                    world.SetLight(
                        nx,
                        ny,
                        result.Sun,
                        result.R,
                        result.G,
                        result.B
                    );

                    propagationQueue.Enqueue(
                        new LightNodePosition(
                            nx,
                            ny,
                            result
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
            Queue<LightNodePosition>
                relightQueue =
                    new Queue<LightNodePosition>();

            while (
                removalQueue.Count > 0
            )
            {
                LightNodePosition removed =
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

                    LightNode neighbour =
                        world.GetLight(
                            nx,
                            ny
                        );

                    if (
                        neighbour.IsEmpty
                    )
                    {
                        continue;
                    }

                    if (
                        IsAffectedBy(
                            neighbour,
                            removed.Light
                        )
                    )
                    {
                        world.SetLight(
                            nx,
                            ny,
                            0,
                            0,
                            0,
                            0
                        );

                        removalQueue.Enqueue(
                            new LightNodePosition(
                                nx,
                                ny,
                                neighbour
                            )
                        );
                    }
                    else
                    {
                        relightQueue.Enqueue(
                            new LightNodePosition(
                                nx,
                                ny,
                                neighbour
                            )
                        );
                    }
                }
            }

            while (
                relightQueue.Count > 0
            )
            {
                propagationQueue.Enqueue(
                    relightQueue.Dequeue()
                );
            }

            ProcessPropagation();
        }

        // =====================================================
        // ATTENUATION
        // =====================================================

        private LightNode Attenuate(
            LightNode light,
            byte opacity
        )
        {
            byte sun =
                AttenuateChannel(
                    light.Sun,
                    opacity
                );

            byte r =
                AttenuateChannel(
                    light.R,
                    opacity
                );

            byte g =
                AttenuateChannel(
                    light.G,
                    opacity
                );

            byte b =
                AttenuateChannel(
                    light.B,
                    opacity
                );

            return new LightNode(
                sun,
                r,
                g,
                b
            );
        }

        private byte AttenuateChannel(
            byte value,
            byte opacity
        )
        {
            if (value == 0)
                return 0;

            int result =
                value -
                1 -
                opacity;

            if (result <= 0)
                return 0;

            if (result > 15)
                result = 15;

            return (byte)result;
        }

        // =====================================================
        // MAX
        // =====================================================

        private LightNode MaxLight(
            LightNode a,
            LightNode b
        )
        {
            return new LightNode(
                MaxByte(a.Sun, b.Sun),
                MaxByte(a.R, b.R),
                MaxByte(a.G, b.G),
                MaxByte(a.B, b.B)
            );
        }

        private byte MaxByte(
            byte a,
            byte b
        )
        {
            return
                a > b
                    ? a
                    : b;
        }

        // =====================================================
        // EQUALITY
        // =====================================================

        private bool SameLight(
            LightNode a,
            LightNode b
        )
        {
            return
                a.Sun == b.Sun &&
                a.R == b.R &&
                a.G == b.G &&
                a.B == b.B;
        }

        // =====================================================
        // REMOVAL TEST
        // =====================================================

        private bool IsAffectedBy(
            LightNode neighbour,
            LightNode source
        )
        {
            return
                neighbour.R <= source.R &&
                neighbour.G <= source.G &&
                neighbour.B <= source.B &&
                neighbour.Sun <= source.Sun;
        }
    }
}