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

            if (blockID == 0)
                return;



            if (!BlockDatabase.Contains(blockID))
                return;



            var block =
                BlockDatabase.Get(
                    blockID
                );



            Texture2D texture =
                TextureManager.Get(
                    block.Texture
                );



            if (texture == null)
                return;



            target.SetPixels(
                x * BlockPixelSize,
                y * BlockPixelSize,
                BlockPixelSize,
                BlockPixelSize,
                texture.GetPixels()
            );

        }


    }

}