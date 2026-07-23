using Game.Achievements;
using Game.Blocks;
using Game.Content;
using Game.Fluids;
using Game.Items;
using System.IO;
using UnityEngine;

namespace Game.Mods
{

    public static class ModLoader
    {


        public static void LoadMods()
        {

            string modsPath =
                Game.Content.DataPaths.ModsFolder;



            if (!Directory.Exists(modsPath))
            {
                return;
            }



            string[] mods =
                Directory.GetDirectories(modsPath);



            foreach (string modPath in mods)
            {

                LoadMod(modPath);

            }

        }




        private static void LoadMod(string path)
        {

            string modFile =
                Path.Combine(
                    path,
                    "mod.json"
                );



            ModDefinition mod =
                Game.Content.JsonLoader.Load<ModDefinition>(
                    modFile
                );



            if (mod == null)
                return;



            Debug.Log(
                "Loaded mod: " + mod.Name
            );



            LoadBlocks(path);

            LoadItems(path);

            LoadFluids(path);

            LoadAchievements(path);
        }





        private static void LoadBlocks(string modPath)
        {

            string blocksPath =
                Path.Combine(
                    modPath,
                    "blocks"
                );



            if (!Directory.Exists(blocksPath))
                return;



            string[] files =
                Directory.GetFiles(
                    blocksPath,
                    "*.json"
                );



            foreach (string file in files)
            {

                BlockDefinition block =
                    Game.Content.JsonLoader.Load<BlockDefinition>(
                        file
                    );



                if (block != null)
                {

                    ContentID id =
    ContentID.Parse(
        block.ID
    );

                    Debug.Log(
                        "Loaded block: " + block.Name
                    );

                }

            }

        }

        private static void LoadItems(string modPath)
        {

            string itemsPath =
                Path.Combine(
                    modPath,
                    "items"
                );



            if (!Directory.Exists(itemsPath))
                return;



            string[] files =
                Directory.GetFiles(
                    itemsPath,
                    "*.json"
                );



            foreach (string file in files)
            {

                ItemDefinition item =
                    Game.Content.JsonLoader.Load<ItemDefinition>(
                        file
                    );



                if (item != null)
                {

                    ItemRegistry.Register(
                        item
                    );


                    Debug.Log(
                        "Loaded item: " + item.Name
                    );

                }

            }

        }

        private static void LoadFluids(string modPath)
        {

            string fluidsPath =
                Path.Combine(
                    modPath,
                    "fluids"
                );



            if (!Directory.Exists(fluidsPath))
                return;



            string[] files =
                Directory.GetFiles(
                    fluidsPath,
                    "*.json"
                );



            foreach (string file in files)
            {

                FluidDefinition fluid =
                    Game.Content.JsonLoader.Load<FluidDefinition>(
                        file
                    );



                if (fluid != null)
                {

                    FluidRegistry.Register(
                        fluid
                    );


                    Debug.Log(
                        "Loaded fluid: " + fluid.Name
                    );

                }

            }

        }
    

    private static void LoadAchievements(string modPath)
        {

            string achievementsPath =
                Path.Combine(
                    modPath,
                    "achievements"
                );



            if (!Directory.Exists(achievementsPath))
                return;



            string[] files =
                Directory.GetFiles(
                    achievementsPath,
                    "*.json"
                );



            foreach (string file in files)
            {

                AchievementDefinition achievement =
                    Game.Content.JsonLoader.Load<AchievementDefinition>(
                        file
                    );



                if (achievement != null)
                {

                    AchievementRegistry.Register(
                        achievement
                    );


                    Debug.Log(
                        "Loaded achievement: " + achievement.Title
                    );

                }

            }

        }
    }
}