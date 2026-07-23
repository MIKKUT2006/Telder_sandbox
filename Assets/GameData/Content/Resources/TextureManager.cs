using System.Collections.Generic;
using System.IO;
using UnityEngine;


namespace Game.Resources
{

    public static class TextureManager
    {

        private static Dictionary<string, Texture2D> textures =
            new Dictionary<string, Texture2D>();



        public static void LoadBlockTexture(
            string name,
            string path
        )
        {

            if (!File.Exists(path))
            {
                Debug.LogWarning(
                    "Texture not found: " + path
                );

                return;
            }



            byte[] data =
                File.ReadAllBytes(path);



            Texture2D texture =
                new Texture2D(
                    2,
                    2
                );



            texture.filterMode =
                FilterMode.Point;



            texture.LoadImage(
                data
            );



            textures[name] = texture;

        }





        public static Texture2D Get(
            string name
        )
        {

            if (textures.TryGetValue(
                name,
                out Texture2D texture
            ))
            {
                return texture;
            }



            Debug.LogWarning(
                "Texture missing: " + name
            );


            return null;

        }





        public static void Clear()
        {

            textures.Clear();

        }

    }

}