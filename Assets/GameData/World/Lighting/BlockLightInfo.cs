namespace Game.World.Lighting
{
    public readonly struct BlockLightInfo
    {
        public readonly byte Opacity;

        public readonly byte EmissionR;
        public readonly byte EmissionG;
        public readonly byte EmissionB;

        public readonly bool BlocksLight;

        public BlockLightInfo(
            byte opacity,
            byte emissionR,
            byte emissionG,
            byte emissionB,
            bool blocksLight
        )
        {
            Opacity = opacity;

            EmissionR = emissionR;
            EmissionG = emissionG;
            EmissionB = emissionB;

            BlocksLight = blocksLight;
        }

        public bool IsLightSource
        {
            get
            {
                return
                    EmissionR > 0 ||
                    EmissionG > 0 ||
                    EmissionB > 0;
            }
        }
    }
}