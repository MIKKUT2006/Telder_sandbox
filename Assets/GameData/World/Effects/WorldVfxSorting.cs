namespace Game.World.Effects
{
    /// <summary>
    /// Shared draw-order contract for world-space visual effects.
    /// World block layers currently use small sorting orders, so 1000 keeps
    /// fire, mining debris and explosion particles in front of blocks.
    /// </summary>
    public static class WorldVfxSorting
    {
        public const int ParticleSortingOrder = 1000;
    }
}
