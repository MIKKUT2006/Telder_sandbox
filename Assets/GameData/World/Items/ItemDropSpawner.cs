using UnityEngine;
using Game.Items;

namespace Game.World.Items
{
    public class ItemDropSpawner : MonoBehaviour
    {
        public static ItemDropSpawner Instance { get; private set; }

        [SerializeField] private DroppedItem droppedItemPrefab;

        public static ItemDropSpawner EnsureInstance()
        {
            if (Instance != null)
                return Instance;

            ItemDropSpawner existing =
                UnityEngine.Object.FindFirstObjectByType<ItemDropSpawner>();

            if (existing != null)
            {
                Instance = existing;
                DontDestroyOnLoad(existing.gameObject);
                return existing;
            }

            // Some generated/test gameplay scenes do not contain the scene prefab
            // spawner. A lightweight fallback still gives fully functional drops.
            GameObject go = new GameObject("RuntimeItemDropSpawner");
            return go.AddComponent<ItemDropSpawner>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // Prefer a scene-configured spawner over the runtime fallback so
                // custom drop prefabs/visuals are restored when available.
                if (Instance.droppedItemPrefab == null && droppedItemPrefab != null)
                {
                    ItemDropSpawner old = Instance;
                    Instance = this;
                    DontDestroyOnLoad(gameObject);
                    Destroy(old.gameObject);
                    return;
                }

                Debug.LogWarning("ITEM DROP SPAWNER: Duplicate instance ignored.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public bool SpawnFromBlock(string itemId, int count, Vector2 position)
        {
            var voxel=Game.World.Voxel3D.VoxelWorld3DRuntime.Instance;
            if(voxel!=null&&voxel.Player!=null)return Game.World.Voxel3D.VoxelItemDrop3D.Spawn(itemId,count,new Vector3(position.x,position.y,voxel.Player.position.z));
            return Spawn(
                itemId,
                count,
                position,
                new Vector2(Random.Range(-1.25f, 1.25f), Random.Range(1.6f, 2.4f)),
                0.15f) != null;
        }

        public bool SpawnFromPlayer(string itemId, int count, Vector2 position)
        {
            var voxel=Game.World.Voxel3D.VoxelWorld3DRuntime.Instance;
            if(voxel!=null&&voxel.Player!=null)return Game.World.Voxel3D.VoxelItemDrop3D.Spawn(itemId,count,new Vector3(position.x,position.y,voxel.Player.position.z));
            return Spawn(
                itemId,
                count,
                position,
                new Vector2(Random.Range(-1.5f, 1.5f), Random.Range(2f, 3f)),
                0.75f) != null;
        }

        public bool SpawnRestored(string itemId, int count, Vector2 position, Vector2 velocity)
        {
            var voxel=Game.World.Voxel3D.VoxelWorld3DRuntime.Instance;
            if(voxel!=null&&voxel.Player!=null)return Game.World.Voxel3D.VoxelItemDrop3D.Spawn(itemId,count,new Vector3(position.x,position.y,voxel.Player.position.z));
            return Spawn(itemId, count, position, velocity, 0.25f) != null;
        }

        public bool SpawnEmbedded(string itemId, Vector2 hitPoint, Quaternion flightRotation)
        {
            DroppedItem arrow = Spawn(itemId, 1, hitPoint, Vector2.zero, 0.25f);
            if (arrow == null)
                return false;
            arrow.SetEmbedded(hitPoint, flightRotation);
            return true;
        }

        private DroppedItem Spawn(
            string itemId,
            int count,
            Vector2 position,
            Vector2 velocity,
            float pickupDelay)
        {
            if (count <= 0 || string.IsNullOrWhiteSpace(itemId))
                return null;

            if (!ItemRegistry.Contains(itemId))
            {
                Debug.LogWarning("ITEM DROP: Item is not registered: " + itemId);
                return null;
            }

            DroppedItem dropped;

            if (droppedItemPrefab != null)
            {
                dropped = Instantiate(
                    droppedItemPrefab,
                    position,
                    Quaternion.identity);
            }
            else
            {
                // Runtime fallback. The DroppedItem component already uses custom
                // world collision, so a SpriteRenderer is the only required visual.
                GameObject go = new GameObject("DroppedItem_" + itemId);
                go.transform.position = position;
                go.AddComponent<SpriteRenderer>();
                dropped = go.AddComponent<DroppedItem>();
            }

            if (dropped == null)
                return null;

            dropped.Initialize(itemId, count, pickupDelay);
            dropped.SetVelocity(velocity);
            return dropped;
        }
    }
}
