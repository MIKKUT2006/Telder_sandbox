using UnityEngine;

using Game.Resources;
using Game.World;


namespace Game.World.Rendering
{

    public static class BlockRenderer
    {

        public const int BlockPixelSize = 16;


        public static void DrawBlock(
            Texture2D target,
            int x,
            int y,
            ushort blockID
        )
        {

            if (
                target == null
            )
            {
                return;
            }


            int pixelX =
                x *
                BlockPixelSize;


            int pixelY =
                y *
                BlockPixelSize;


            // =================================================
            // AIR
            // =================================================

            if (
                blockID == 0
            )
            {

                Color[] emptyPixels =
                    new Color[
                        BlockPixelSize *
                        BlockPixelSize
                    ];


                for (
                    int i = 0;
                    i < emptyPixels.Length;
                    i++
                )
                {

                    emptyPixels[i] =
                        Color.clear;

                }


                target.SetPixels(
                    pixelX,
                    pixelY,
                    BlockPixelSize,
                    BlockPixelSize,
                    emptyPixels
                );


                return;

            }


            // =================================================
            // UNKNOWN BLOCK
            // =================================================

            if (
                !BlockDatabase.Contains(
                    blockID
                )
            )
            {
                return;
            }


            var block =
                BlockDatabase.Get(
                    blockID
                );


            Texture2D texture =
                TextureManager.Get(
                    block.Texture
                );


            if (
                texture == null
            )
            {
                return;
            }


            // =================================================
            // DRAW
            // =================================================

            target.SetPixels(
                pixelX,
                pixelY,
                BlockPixelSize,
                BlockPixelSize,
                texture.GetPixels()
            );

        }

    }

}