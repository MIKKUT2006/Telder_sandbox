using UnityEngine;
using Game.World;
using Game.World.Collision;

namespace Game.Harpoon
{
    /// <summary>
    /// Grapple against the custom foreground collision grid; no Physics2D needed.
    /// The returned anchor is the last free point immediately before the impact.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HarpoonSurfaceProbe2D : MonoBehaviour
    {
        [SerializeField, Range(0.025f, 0.2f)] private float sampleSpacing = 0.06f;

        private WorldManager manager;
        private WorldCollision collision;

        private bool Resolve()
        {
            WorldManager current = WorldManager.Instance;
            if (current == null)
            {
                manager = null;
                collision = null;
                return false;
            }

            // Dimensions may swap WorldCollision without replacing the manager.
            // Refresh the reference so a hook never hits the previous dimension.
            manager = current;
            collision = manager.GetWorldCollision();
            return collision != null;
        }

        public bool IsReady { get { return Resolve(); } }

        public bool IsSolid(UnityEngine.Vector2Int cell)
        {
            return Resolve() && collision.IsSolid(cell.x, cell.y);
        }

        public bool TryTrace(Vector2 from, Vector2 to, out Vector2 hitPoint, out UnityEngine.Vector2Int hitCell)
        {
            hitPoint = to;
            hitCell = default(UnityEngine.Vector2Int);
            if (!Resolve()) return false;

            Vector2 segment = to - from;
            int steps = Mathf.Max(1, Mathf.CeilToInt(segment.magnitude / Mathf.Max(0.025f, sampleSpacing)));
            Vector2 lastFree = from;
            for (int i = 1; i <= steps; i++)
            {
                Vector2 point = from + segment * (i / (float)steps);
                UnityEngine.Vector2Int cell = new UnityEngine.Vector2Int(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.y));
                if (collision.IsSolid(cell.x, cell.y))
                {
                    hitPoint = lastFree;
                    hitCell = cell;
                    return true;
                }
                lastFree = point;
            }
            return false;
        }
    }
}
