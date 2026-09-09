using System.Collections.Generic;

namespace Game.World.Dimensions
{
    /*
     * Расширяемый список модификаторов.
     *
     * Чтобы добавить новый модификатор:
     * 1. Добавь его в definitions.
     * 2. Если он конфликтует с другим — укажи одинаковый ConflictGroup.
     * 3. Если нужен реальный gameplay effect — система игры может
     *    проверять Current.HasModifier("id").
     */
    public static class DimensionModifierRegistry
    {
        private static readonly List<DimensionModifierDefinition>
            definitions =
            new List<DimensionModifierDefinition>()
            {
                new DimensionModifierDefinition(
                    "low_gravity",
                    "LOW GRAVITY",
                    "Гравитация значительно слабее обычной.",
                    "gravity",
                    0.58f
                ),

                new DimensionModifierDefinition(
                    "high_gravity",
                    "HIGH GRAVITY",
                    "Гравитация значительно сильнее обычной.",
                    "gravity",
                    1.48f
                ),

                new DimensionModifierDefinition(
                    "acid_rain",
                    "ACID RAIN",
                    "Периодически идут кислотные дожди."
                ),

                new DimensionModifierDefinition(
                    "meteor_showers",
                    "METEOR SHOWERS",
                    "В измерении возможны метеоритные дожди."
                ),

                new DimensionModifierDefinition(
                    "eternal_night",
                    "ETERNAL NIGHT",
                    "Естественное дневное освещение сильно ограничено."
                ),

                new DimensionModifierDefinition(
                    "toxic_atmosphere",
                    "TOXIC ATMOSPHERE",
                    "Атмосфера мира токсична.",
                    "atmosphere"
                ),

                new DimensionModifierDefinition(
                    "thin_atmosphere",
                    "THIN ATMOSPHERE",
                    "Разреженная атмосфера.",
                    "atmosphere"
                ),

                new DimensionModifierDefinition(
                    "rich_ores",
                    "RICH ORES",
                    "Повышенный потенциал рудных залежей.",
                    "resources"
                ),

                new DimensionModifierDefinition(
                    "poor_ores",
                    "POOR ORES",
                    "Редкие и бедные залежи ресурсов.",
                    "resources"
                ),

                new DimensionModifierDefinition(
                    "electrical_storms",
                    "ELECTRICAL STORMS",
                    "Сильные электрические бури."
                ),

                new DimensionModifierDefinition(
                    "unstable_reality",
                    "UNSTABLE REALITY",
                    "Пространство мира нестабильно."
                ),

                new DimensionModifierDefinition(
                    "dense_fog",
                    "DENSE FOG",
                    "Большие области покрыты плотным туманом."
                ),

                new DimensionModifierDefinition(
                    "aggressive_fauna",
                    "AGGRESSIVE FAUNA",
                    "Местная фауна значительно агрессивнее."
                ),

                new DimensionModifierDefinition(
                    "crystal_growth",
                    "CRYSTAL GROWTH",
                    "В мире активно растут аномальные кристаллы."
                ),

                new DimensionModifierDefinition(
                    "extreme_winds",
                    "EXTREME WINDS",
                    "Периодически возникают экстремально сильные ветры."
                ),

                new DimensionModifierDefinition(
                    "high_radiation",
                    "HIGH RADIATION",
                    "Некоторые области имеют высокий радиационный фон."
                )
            };


        public static IReadOnlyList<DimensionModifierDefinition> All =>
            definitions;


        /*
         * "Около четырёх":
         * seed выбирает 3, 4 или 5 модификаторов.
         * Среднее значение = 4.
         */
        public static List<DimensionModifierDefinition> GetForSeed(
            int seed
        )
        {
            DimensionDeterministicRandom random =
                new DimensionDeterministicRandom(
                    seed,
                    0x4D4F4453u // "MODS"
                );

            int targetCount =
                3 +
                random.NextInt(3);

            List<int> indices =
                new List<int>();

            for (
                int i = 0;
                i < definitions.Count;
                i++
            )
            {
                indices.Add(i);
            }


            /*
             * Детерминированный Fisher-Yates shuffle.
             */
            for (
                int i = indices.Count - 1;
                i > 0;
                i--
            )
            {
                int j =
                    random.NextInt(
                        i + 1
                    );

                int temp =
                    indices[i];

                indices[i] =
                    indices[j];

                indices[j] =
                    temp;
            }


            List<DimensionModifierDefinition> result =
                new List<DimensionModifierDefinition>();

            HashSet<string> usedConflictGroups =
                new HashSet<string>();


            for (
                int i = 0;
                i < indices.Count &&
                result.Count < targetCount;
                i++
            )
            {
                DimensionModifierDefinition modifier =
                    definitions[
                        indices[i]
                    ];


                if (
                    !string.IsNullOrEmpty(
                        modifier.ConflictGroup
                    ) &&
                    usedConflictGroups.Contains(
                        modifier.ConflictGroup
                    )
                )
                {
                    continue;
                }


                result.Add(
                    modifier
                );


                if (
                    !string.IsNullOrEmpty(
                        modifier.ConflictGroup
                    )
                )
                {
                    usedConflictGroups.Add(
                        modifier.ConflictGroup
                    );
                }
            }


            return result;
        }


        public static DimensionModifierDefinition GetById(
            string id
        )
        {
            for (
                int i = 0;
                i < definitions.Count;
                i++
            )
            {
                if (
                    definitions[i].Id ==
                    id
                )
                {
                    return definitions[i];
                }
            }

            return null;
        }
    }
}