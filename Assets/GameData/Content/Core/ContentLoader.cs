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
            RegisterFluid("game:water", "Water", "#3C8DFFB8", 5f, 1f, 0f, false);
            RegisterFluid("game:lava", "Lava", "#FF6A1AD9", 2f, 3f, 10f, true);
            RegisterFluid("game:acid", "Acid", "#77FF39C8", 4f, 1.4f, 7f, false);
            RegisterFluid("game:ice_water", "Ice Water", "#9DE9FFE0", 4f, 1.25f, 3f, false);
            FluidIDRegistry.RegisterLoadedFluids();
        }

        private static void RegisterFluid(string id, string name, string color, float speed, float viscosity, float damage, bool hot)
        {
            FluidRegistry.Register(new FluidDefinition
            {
                ID = ContentID.Parse(id), Name = name, Color = color, FlowSpeed = speed,
                Viscosity = viscosity, Damage = damage, DamageInterval = 1f, IsHot = hot, IsLiquid = true
            });
        }

        private static void LoadAchievements()
        {
            AchievementFolderLoader.LoadDefaultFolder();
        }


    }

}