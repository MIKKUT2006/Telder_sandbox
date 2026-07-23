using Game.Blocks;
using Game.Content;
using System.IO;
using UnityEngine;


namespace Game.Resources
{

    public static class BlockTextureLoader
    {

        public static void Load()
        {

            foreach (
                BlockDefinition block
                in BlockRegistry.GetAll()
            )
            {

                string path =
                    Path.Combine(
                        DataPaths.ResourcePacksFolder,
                        "Default",
                        "textures",
                        "blocks",
                        block.Texture + ".png"
                    );



                TextureManager.LoadBlockTexture(
                    block.Texture,
                    path
                );

            }

        }

    }

}