using Game.Blocks;


namespace Game.Content
{

    public static class ContentInitializer
    {


        public static void Initialize()
        {

            RegisterBlocks();


            RegisterItems();


            // позже:
            // RegisterFluids();
            // RegisterAchievements();
            // LoadMods();

        }



        private static void RegisterBlocks()
        {

            BlockDefinition stone =
                new BlockDefinition()
                {
                    ID = "game:stone",

                    Name = "Stone",

                    Texture = "stone",

                    Hardness = 3f,

                    Solid = true
                };


            BlockRegistry.Register(
                stone
            );





            BlockDefinition dirt =
                new BlockDefinition()
                {
                    ID = "game:dirt",

                    Name = "Dirt",

                    Texture = "dirt",

                    Hardness = 1f,

                    Solid = true
                };


            BlockRegistry.Register(
                dirt
            );

        }




        private static void RegisterItems()
        {

            // тут позже будут предметы

        }


    }

}