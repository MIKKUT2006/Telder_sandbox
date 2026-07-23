using Game.Content;


namespace Game.Items
{

    public static class ItemRegistry
    {

        private static ContentRegistry<ItemDefinition> registry
            = new ContentRegistry<ItemDefinition>();



        public static void Register(
            ItemDefinition item
        )
        {
            registry.Register(
                item.ID,
                item
            );
        }



        public static ItemDefinition Get(
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