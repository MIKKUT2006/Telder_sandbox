namespace Game.World.Biomes
{
    /*
     * Class instead of struct intentionally.
     *
     * This removes C# language-version definite-assignment errors
     * that appeared in the previous duplicate struct setup.
     */
    public sealed class BiomeSample
    {
        public BiomeDefinition Primary;
        public BiomeDefinition Secondary;
        public float Blend;


        public BiomeSample(
            BiomeDefinition primary,
            BiomeDefinition secondary,
            float blend
        )
        {
            Primary = primary;

            Secondary =
                secondary != null
                ? secondary
                : primary;

            Blend = blend;
        }


        public BiomeDefinition Dominant
        {
            get
            {
                if (Primary == null)
                    return Secondary;

                if (Secondary == null)
                    return Primary;

                return
                    Blend < 0.5f
                    ? Primary
                    : Secondary;
            }
        }
    }
}