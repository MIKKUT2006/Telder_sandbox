using System.Collections.Generic;
using Game.Content;

namespace Game.Achievements
{
    public static class AchievementRegistry
    {
        private static readonly ContentRegistry<AchievementDefinition> registry =
            new ContentRegistry<AchievementDefinition>();

        public static void Register(AchievementDefinition achievement)
        {
            if (achievement == null) return;
            registry.Register(achievement.ID, achievement);
        }

        public static AchievementDefinition Get(ContentID id) => registry.Get(id);
        public static bool Contains(ContentID id) => registry.Contains(id);
        public static IEnumerable<AchievementDefinition> GetAll() => registry.GetAll();
        public static int Count => registry.Count;
        public static void Clear() => registry.Clear();
    }
}
