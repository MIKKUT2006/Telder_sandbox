using System.IO;
using UnityEngine;


namespace Game.ResourcePacks
{

    public static class ResourcePackLoader
    {

        public static void LoadPacks()
        {

            string path =
                Game.Content.DataPaths.ResourcePacksFolder;



            if (!Directory.Exists(path))
                return;



            string[] packs =
                Directory.GetDirectories(path);



            foreach (string pack in packs)
            {

                LoadPack(pack);

            }

        }





        private static void LoadPack(
            string path
        )
        {

            string file =
                Path.Combine(
                    path,
                    "pack.json"
                );



            ResourcePackDefinition pack =
                Game.Content.JsonLoader.Load<ResourcePackDefinition>(
                    file
                );



            if (pack == null)
                return;



            Debug.Log(
                "Loaded resource pack: " + pack.Name
            );

        }


    }

}