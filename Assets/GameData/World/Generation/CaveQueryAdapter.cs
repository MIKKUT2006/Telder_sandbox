using System;

namespace Game.World.Generation
{
    public sealed class CaveQueryAdapter
    {
        private readonly CaveGenerator generator;


        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public CaveQueryAdapter(
            CaveGenerator generator,
            int seed
        )
        {
            if (generator == null)
            {
                throw new ArgumentNullException(
                    nameof(generator)
                );
            }

            this.generator =
                generator;

            // seed специально оставлен в параметрах,
            // чтобы существующий WorldGenerator
            // не пришлось менять.
            //
            // Реальный Seed уже находится внутри
            // CaveGenerator -> WorldSettings.
        }


        // =====================================================
        // SINGLE BLOCK QUERY
        // =====================================================

        public bool IsCave(
            int worldX,
            int worldY,
            int surfaceHeight
        )
        {
            return generator.IsCave(
                worldX,
                worldY,
                surfaceHeight
            );
        }


        // =====================================================
        // CHUNK MASK QUERY
        // =====================================================

        public bool[,] GenerateCaveMask(
            int originX,
            int originY,
            int width,
            int height,
            int[] surfaceHeights
        )
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(width)
                );
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(height)
                );
            }

            if (surfaceHeights == null)
            {
                throw new ArgumentNullException(
                    nameof(surfaceHeights)
                );
            }

            if (
                surfaceHeights.Length <
                width
            )
            {
                throw new ArgumentException(
                    "surfaceHeights.Length must be >= width.",
                    nameof(surfaceHeights)
                );
            }


            return generator.GenerateCaveMask(
                originX,
                originY,
                width,
                height,
                surfaceHeights
            );
        }
    }
}