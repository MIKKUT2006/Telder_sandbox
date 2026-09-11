using System.Collections.Generic;

namespace Game.World.Dimensions
{
    public static class DimensionTypeRegistry
    {
        private static readonly List<DimensionTypeDefinition>
            definitions =
            new List<DimensionTypeDefinition>()
            {
                new DimensionTypeDefinition(
                    "alien",
                    "ALIEN",
                    "Чуждая биология и инопланетная среда."
                ),

                new DimensionTypeDefinition(
                    "cosmic",
                    "COSMIC",
                    "Космическое измерение с нестабильным пространством."
                ),

                new DimensionTypeDefinition(
                    "volcanic",
                    "VOLCANIC",
                    "Горячий мир лавы, пепла и магмы."
                ),

                new DimensionTypeDefinition(
                    "frozen",
                    "FROZEN",
                    "Холодное ледяное измерение."
                ),

                new DimensionTypeDefinition(
                    "toxic",
                    "TOXIC",
                    "Ядовитая агрессивная среда."
                ),

                new DimensionTypeDefinition(
                    "crystalline",
                    "CRYSTALLINE",
                    "Кристаллическая геология и аномалии."
                ),

                new DimensionTypeDefinition(
                    "oceanic",
                    "OCEANIC",
                    "Мир воды и влажных биомов."
                ),

                new DimensionTypeDefinition(
                    "barren",
                    "BARREN",
                    "Пустынное бедное жизнью пространство."
                ),

                new DimensionTypeDefinition(
                    "drawn",
                    "DRAWN",
                    "Нарисованное измерение"
                ),
                new DimensionTypeDefinition(
                    "abyssal",
                    "ABYSSAL",
                    "Глубокое тёмное измерение."
                ),

                new DimensionTypeDefinition(
                    "distorted",
                    "DISTORTED",
                    "Искажённый мир с деформированной геометрией и чужеродной материей."
                )
            };


        public static IReadOnlyList<DimensionTypeDefinition> All
        {
            get
            {
                return definitions;
            }
        }


        public static DimensionTypeDefinition GetForSeed(
            int seed
        )
        {
            DimensionDeterministicRandom random =
                new DimensionDeterministicRandom(
                    seed,
                    0x54595045u
                );

            int index =
                random.NextInt(
                    definitions.Count
                );

            return
                definitions[
                    index
                ];
        }


        public static DimensionTypeDefinition GetById(
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
                    return
                        definitions[i];
                }
            }

            return null;
        }
    }
}