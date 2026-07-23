using System.Collections.Generic;
using Game.Content;


namespace Game.World.Generation.Ores
{

    public static class OreRegistry
    {

        private static readonly List<OreDefinition> ores =
            new List<OreDefinition>();



        public static void Register(
            OreDefinition ore
        )
        {

            if (
                ore == null
            )
            {

                return;

            }



            if (
                string.IsNullOrEmpty(
                    ore.ID
                )
            )
            {

                return;

            }



            ores.Add(
                ore
            );

        }



        public static IEnumerable<OreDefinition> GetAll()
        {

            return ores;

        }



        public static bool Contains(
            ContentID id
        )
        {

            foreach (
                OreDefinition ore
                in ores
            )
            {

                if (
                    ore.ID ==
                    id.ToString()
                )
                {

                    return true;

                }

            }



            return false;

        }



        public static void Clear()
        {

            ores.Clear();

        }

    }

}