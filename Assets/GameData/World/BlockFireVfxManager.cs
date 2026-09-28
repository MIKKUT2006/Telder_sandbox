using UnityEngine;

namespace Game.World
{
    /// <summary>
    /// Compatibility shell for the old V34 block-fire scanner.
    ///
    /// The previous implementation repeatedly scanned a rectangle around the
    /// player and called World.GetBlock through reflection. It is deliberately
    /// disabled because FurnitureLayerManager already creates fire particles
    /// for tagged torch/campfire furniture.
    /// </summary>
    public sealed class BlockFireVfxManager :
        MonoBehaviour
    {
        private void Awake()
        {
            enabled =
                false;
        }
    }
}
