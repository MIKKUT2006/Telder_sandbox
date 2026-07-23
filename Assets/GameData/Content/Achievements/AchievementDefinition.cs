using System;
using Game.Content;


namespace Game.Achievements
{

    [Serializable]
    public class AchievementDefinition
    {

        public ContentID ID;


        public string Title;


        public string Description;


        public string Icon;


        public AchievementConditionType ConditionType;


        public ContentID Target;



        public AchievementDefinition()
        {

            ConditionType = AchievementConditionType.Custom;

        }

    }

}