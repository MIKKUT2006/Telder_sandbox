
using System.Collections.Generic;

namespace Game.World.Dimensions
{
    public static class DimensionTypeRegistry
    {
        private static readonly List<DimensionTypeDefinition>
            definitions =
            new List<DimensionTypeDefinition>()
            {
                // TIER I
                new DimensionTypeDefinition(
                    "normal",
                    "NORMAL",
                    "Слишком обычный мир",
                    tier: 1
                ),
                // TIER I
                new DimensionTypeDefinition(
                    "alien",
                    "ALIEN",
                    "Чуждая биология и инопланетная среда.",
                    tier: 1
                ),

                // TIER III
                new DimensionTypeDefinition(
                    "cosmic",
                    "COSMIC",
                    "Космическое измерение с нестабильным пространством.",
                    tier: 3
                ),

                // TIER II
                new DimensionTypeDefinition(
                    "volcanic",
                    "VOLCANIC",
                    "Горячий мир лавы, пепла и магмы.",
                    tier: 2
                ),

                // TIER I
                new DimensionTypeDefinition(
                    "frozen",
                    "FROZEN",
                    "Холодное ледяное измерение.",
                    tier: 1
                ),

                // TIER II
                new DimensionTypeDefinition(
                    "toxic",
                    "TOXIC",
                    "Ядовитая агрессивная среда.",
                    tier: 2
                ),

                new DimensionTypeDefinition(
                    "crystalline",
                    "CRYSTALLINE",
                    "Кристаллическая геология и аномалии.",
                    tier: 2
                ),

                new DimensionTypeDefinition(
                    "oceanic",
                    "OCEANIC",
                    "Мир воды и влажных биомов.",
                    tier: 2
                ),

                // TIER I
                new DimensionTypeDefinition(
                    "barren",
                    "BARREN",
                    "Пустынное бедное жизнью пространство.",
                    tier: 1
                ),

                new DimensionTypeDefinition(
                    "drawn",
                    "DRAWN",
                    "Нарисованное измерение",
                    tier: 1
                ),

                // TIER III
                new DimensionTypeDefinition(
                    "abyssal",
                    "ABYSSAL",
                    "Глубокое тёмное измерение.",
                    tier: 3
                ),

                new DimensionTypeDefinition(
                    "distorted",
                    "DISTORTED",
                    "Искажённый мир с деформированной геометрией и чужеродной материей.",
                    tier: 3
                ),

                new DimensionTypeDefinition(
                    "voxel3d",
                    "VOXEL 3D",
                    "Трёхмерное блочное измерение с объёмными биомами, пещерами и горами.",
                    DimensionGeometryMode.Voxel3D,
                    tier: 1
                )
            };

        public static IReadOnlyList<DimensionTypeDefinition> All
        {
            get { return definitions; }
        }

        // Исходный детерминированный алгоритм не изменён.
        public static DimensionTypeDefinition GetForSeed(int seed)
        {
            DimensionDeterministicRandom random =
                new DimensionDeterministicRandom(
                    seed,
                    0x54595045u
                );

            int index = random.NextInt(definitions.Count);
            return definitions[index];
        }

        public static DimensionTypeDefinition GetById(string id)
        {
            for (int i = 0; i < definitions.Count; i++)
            {
                if (definitions[i].Id == id)
                    return definitions[i];
            }

            return null;
        }
    }
}
