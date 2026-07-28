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


            // =====================================================
            // СНАЧАЛА ОЧИЩАЕМ ОБЛАСТЬ БЛОКА
            //
            // Это обязательно.
            //
            // Если старый блок был STONE,
            // а новый блок AIR,
            // старые пиксели нельзя оставлять.
            // =====================================================

            Color[] clearPixels =
                new Color[
                    BlockPixelSize *
                    BlockPixelSize
                ];


            target.SetPixels(
                pixelX,
                pixelY,
                BlockPixelSize,
                BlockPixelSize,
                clearPixels
            );


            // =====================================================
            // AIR
            // =====================================================

            if (
                blockID == 0
            )
            {
                return;
            }


            // =====================================================
            // BLOCK DOES NOT EXIST
            // =====================================================

            if (
                !BlockDatabase.Contains(
                    blockID
                )
            )
            {
                return;
            }


            // =====================================================
            // GET BLOCK
            // =====================================================

            var block =
                BlockDatabase.Get(
                    blockID
                );


            if (
                block == null
            )
            {
                return;
            }


            // =====================================================
            // GET TEXTURE
            // =====================================================

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


            // =====================================================
            // DRAW
            // =====================================================

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