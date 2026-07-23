using Game.Content;


namespace Game.Fluids
{

    public static class FluidRegistry
    {

        private static ContentRegistry<FluidDefinition> registry
            = new ContentRegistry<FluidDefinition>();



        public static void Register(
            FluidDefinition fluid
        )
        {
            registry.Register(
                fluid.ID,
                fluid
            );
        }



        public static FluidDefinition Get(
            ContentID id
        )
        {
            return registry.Get(id);
        }



        public static bool Contains(
            ContentID id
        )
        {
            return registry.Contains(id);
        }



        public static int Count
        {
            get
            {
                return registry.Count;
            }
        }


    }

}