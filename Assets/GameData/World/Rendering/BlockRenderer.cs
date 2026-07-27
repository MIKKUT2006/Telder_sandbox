using UnityEngine;

using Game.Resources;
using Game.World;


namespace Game.World.Rendering
{

    public static class BlockRenderer
    {

        // =====================================================
        // SETTINGS
        // =====================================================

        public const int BlockPixelSize = 16;


        // =====================================================
        // DRAW BLOCK
        // =====================================================

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


            // =================================================
            // BLOCK PIXEL POSITION
            // =================================================

            int pixelX =
                x *
                BlockPixelSize;


            int pixelY =
                y *
                BlockPixelSize;


            // =================================================
            // AIR
            // =================================================

            // ВАЖНО:
            // Раньше здесь был просто return.
            //
            // Из-за этого при удалении блока старые пиксели
            // оставались в Texture2D.
            //
            // Теперь область блока полностью очищается.

            if (
                blockID == 0
            )
            {

                ClearBlockPixels(
                    target,
                    pixelX,
                    pixelY
                );


                return;

            }


            // =================================================
            // BLOCK DOES NOT EXIST
            // =================================================

            if (
                !BlockDatabase.Contains(
                    blockID
                )
            )
            {

                ClearBlockPixels(
                    target,
                    pixelX,
                    pixelY
                );


                return;

            }


            // =================================================
            // GET BLOCK
            // =================================================

            var block =
                BlockDatabase.Get(
                    blockID
                );


            if (
                block == null
            )
            {

                ClearBlockPixels(
                    target,
                    pixelX,
                    pixelY
                );


                return;

            }


            // =================================================
            // GET TEXTURE
            // =================================================

            Texture2D texture =
                TextureManager.Get(
                    block.Texture
                );


            if (
                texture == null
            )
            {

                ClearBlockPixels(
                    target,
                    pixelX,
                    pixelY
                );


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


        // =====================================================
        // CLEAR BLOCK
        // =====================================================

        private static void ClearBlockPixels(
            Texture2D target,
            int pixelX,
            int pixelY
        )
        {

            Color32[] pixels =
                new Color32[
                    BlockPixelSize *
                    BlockPixelSize
                ];


            // Все значения Color32 по умолчанию:
            //
            // R = 0
            // G = 0
            // B = 0
            // A = 0
            //
            // То есть полностью прозрачный пиксель.

            target.SetPixels32(
                pixelX,
                pixelY,
                BlockPixelSize,
                BlockPixelSize,
                pixels
            );

        }

    }

}