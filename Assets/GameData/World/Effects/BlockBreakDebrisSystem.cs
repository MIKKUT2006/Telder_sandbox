using UnityEngine;

namespace Game.World.Effects
{
    // Compatibility entry points: all mining and destruction effects now share
    // the single-mesh PixelImpactParticles renderer (no duplicate sprite pool).
    public sealed class BlockBreakDebrisSystem : MonoBehaviour
    {
        public static void Emit(Game.World.World world, int worldX, int worldY,
            ushort oldBlockID, bool background)
        {
            PixelImpactParticles.EmitBlock(world, worldX, worldY, oldBlockID, background, false);
        }

        public static void EmitMiningHit(Game.World.World world, int worldX, int worldY,
            ushort blockID, bool background)
        {
            PixelImpactParticles.EmitBlock(world, worldX, worldY, blockID, background, true);
        }

        public static void ClearAll()
        {
            PixelImpactParticles.ClearAll();
        }
    }
}
