using System.Collections.Generic;

using Game.Save;


namespace Game.World.Dimensions
{

    public static class SaveScopedDimensionList
    {

        public static List<DimensionDefinition>
            GetVisited()
        {

            List<DimensionDefinition> result =
                new List<DimensionDefinition>();


            if (
                !SaveGameRuntime.HasActiveSave
            )
            {

                return result;

            }


            List<DimensionSummarySaveData> summaries =
                SaveGameRuntime
                    .GetVisitedDimensions();


            for (
                int i = 0;
                i < summaries.Count;
                i++
            )
            {

                DimensionSummarySaveData summary =
                    summaries[i];


                if (
                    summary ==
                    null
                    ||
                    string.IsNullOrWhiteSpace(
                        summary.Name
                    )
                )
                {

                    continue;

                }


                result.Add(
                    new DimensionDefinition(
                        summary.Name,
                        summary.Seed
                    )
                );

            }


            return result;

        }

    }

}
