using Game.Achievements;
using Game.Blocks;
using Game.Fluids;
using Game.Items;
using Game.World;
using System.Linq;
using UnityEngine;
using static Unity.Collections.AllocatorManager;


namespace Game.Content
{

    public static class ContentLoader
    {

        public static void Initialize()
        {
            Debug.Log("CONTENT LOADER START");

            LoadBlocks();

            Debug.Log(
                "BLOCKS AFTER LOAD: " +
                BlockRegistry.GetAll().Count()
            );

            LoadItems();

            LoadFluids();

            LoadAchievements();
        }


        private static void RegisterBlockIDs()
        {

            foreach (
                BlockDefinition block
                in BlockRegistry.GetAll()
            )
            {

                ushort id =
                    BlockIDRegistry.GetID(
                        ContentID.Parse(
                            block.ID
                        )
                    );

            }

        }
        private static void LoadBlocks()
        {
            Debug.Log("LOAD BLOCKS START");


            Debug.Log("BEFORE BLOCK FOLDER LOADER");


            BlockFolderLoader.LoadFolder(
                DataPaths.BlocksFolder
            );


            Debug.Log("AFTER BLOCK FOLDER LOADER");


            Debug.Log(
                "BLOCKS COUNT: " +
                BlockRegistry.GetAll().Count()
            );


            Debug.Log("LOAD BLOCKS END");
        }





        private static void LoadItems()
        {
            ItemFolderLoader.LoadDefaultFolder();
        }

        private static void LoadFluids()
        {

            FluidRegistry.Register(

                new FluidDefinition()
                {

                    ID = new ContentID(
                        "game",
                        "water"
                    ),


                    Name = "Water",


                    Texture = "water",


                    FlowSpeed = 5,


                    Viscosity = 1,


                    IsLiquid = true

                }

            );



            FluidRegistry.Register(

                new FluidDefinition()
                {

                    ID = new ContentID(
                        "game",
                        "lava"
                    ),


                    Name = "Lava",


                    Texture = "lava",


                    FlowSpeed = 2,


                    Viscosity = 3,


                    Damage = 10,


                    IsHot = true,


                    IsLiquid = true

                }

            );

        }





        private static void LoadAchievements()
        {

            AchievementRegistry.Register(

                new AchievementDefinition()
                {

                    ID = new ContentID(
                        "game",
                        "first_block"
                    ),


                    Title = "First Block",


                    Description =
                    "Break your first block",


                    Icon = "stone",


                    ConditionType =
                    AchievementConditionType.BlockBreak,


                    Target =
                    new ContentID(
                        "game",
                        "stone"
                    )

                }

            );

        }


    }

}