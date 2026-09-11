
namespace Game.Crafting
{

    public static class CraftingSession
    {

        public static bool HasWorkbench
        {
            get;
            private set;
        }


        public static void SetWorkbench(
            bool value
        )
        {

            HasWorkbench =
                value;

        }


        public static void Clear()
        {

            HasWorkbench =
                false;

        }

    }

}
