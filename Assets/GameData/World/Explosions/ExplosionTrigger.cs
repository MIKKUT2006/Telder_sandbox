using UnityEngine;

namespace Game.World.Explosions
{
    /// <summary>
    /// Optional bridge component for bombs, projectiles, traps, furniture, etc.
    /// Call ExplodeNow() from gameplay code or a UnityEvent.
    /// </summary>
    public sealed class ExplosionTrigger : MonoBehaviour
    {
        [Min(0.25f)]
        [SerializeField]
        private float radius = 4f;

        [SerializeField]
        private bool dropBlocks = true;

        [SerializeField]
        private bool destroyBackground = false;

        [SerializeField]
        private bool destroyFurniture = true;

        [SerializeField]
        private bool destroyObjectAfterExplosion = false;

        public void ExplodeNow()
        {
            ExplosionSystem.Explode(
                transform.position,
                radius,
                dropBlocks,
                destroyBackground,
                destroyFurniture
            );

            if (destroyObjectAfterExplosion)
                Destroy(gameObject);
        }

        [ContextMenu("TEST: Explode Now")]
        private void TestExplodeNow()
        {
            ExplodeNow();
        }
    }
}
