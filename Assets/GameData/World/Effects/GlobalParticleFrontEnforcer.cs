namespace Game.World.Effects
{
    /// <summary>
    /// Compatibility shell.
    ///
    /// V34 used Object.FindObjectsOfType<ParticleSystem>() every 0.25 seconds.
    /// That global scan has been removed. Particle systems must set their
    /// sortingOrder once when they are created.
    /// </summary>
    public sealed class GlobalParticleFrontEnforcer :
        UnityEngine.MonoBehaviour
    {
        public static void ApplyNow()
        {
            // Intentionally empty.
            // Current fire/debris/explosion particle creators already assign
            // their sorting order directly.
        }
    }
}
