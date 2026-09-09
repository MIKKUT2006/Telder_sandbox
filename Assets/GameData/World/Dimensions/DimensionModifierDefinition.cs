using System;

namespace Game.World.Dimensions
{
    [Serializable]
    public class DimensionModifierDefinition
    {
        public string Id;
        public string DisplayName;
        public string Description;

        /*
         * Модификаторы из одной группы не могут выпасть вместе.
         *
         * Например:
         * low_gravity и high_gravity имеют группу "gravity".
         */
        public string ConflictGroup;

        /*
         * Уже сейчас позволяет реально применять модификаторы
         * низкой/высокой гравитации.
         *
         * Для остальных эффектов позднее можно добавить свои поля
         * или отдельные gameplay systems.
         */
        public float GravityMultiplier = 1f;


        public DimensionModifierDefinition(
            string id,
            string displayName,
            string description,
            string conflictGroup = "",
            float gravityMultiplier = 1f
        )
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            ConflictGroup = conflictGroup;
            GravityMultiplier = gravityMultiplier;
        }
    }
}