using System;
using System.Collections.Generic;
using Game.Content;

namespace Game.Achievements
{
    [Serializable]
    public class AchievementCondition
    {
        public AchievementConditionType Type = AchievementConditionType.Custom;
        public string Target = "";
        public int Count = 1;
    }

    [Serializable]
    public class AchievementDefinition
    {
        public ContentID ID;
        public string Title;
        public string Description;
        public string Icon;

        // Empty = root node.
        public string Parent = "";
        public bool HiddenUntilUnlocked = false;
        public int SortOrder = 0;

        // New multi-condition format.
        public List<AchievementCondition> Conditions = new List<AchievementCondition>();

        // Legacy single-condition fields remain supported.
        public AchievementConditionType ConditionType = AchievementConditionType.Custom;
        public ContentID Target;
        public int Count = 1;
    }
}
