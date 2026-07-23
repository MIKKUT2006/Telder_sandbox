using Game.Content;


namespace Game.Achievements
{

    public static class AchievementRegistry
    {

        private static ContentRegistry<AchievementDefinition> registry
            = new ContentRegistry<AchievementDefinition>();



        public static void Register(
            AchievementDefinition achievement
        )
        {
            registry.Register(
                achievement.ID,
                achievement
            );
        }



        public static AchievementDefinition Get(
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