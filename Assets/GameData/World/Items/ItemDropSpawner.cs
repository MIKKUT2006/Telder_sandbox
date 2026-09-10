using UnityEngine;
using Game.Items;

namespace Game.World.Items
{
    public class ItemDropSpawner : MonoBehaviour
    {
        public static ItemDropSpawner Instance { get; private set; }

        [SerializeField] private DroppedItem droppedItemPrefab;

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Debug.LogWarning("ITEM DROP SPAWNER: Duplicate instance.");

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public bool SpawnFromBlock(string itemId, int count, Vector2 position)
        {
            return Spawn(
                itemId,
                count,
                position,
                new Vector2(Random.Range(-1.25f, 1.25f), Random.Range(1.6f, 2.4f)),
                0.15f);
        }

        public bool SpawnFromPlayer(string itemId, int count, Vector2 position)
        {
            return Spawn(
                itemId,
                count,
                position,
                new Vector2(Random.Range(-1.5f, 1.5f), Random.Range(2f, 3f)),
                0.75f);
        }

        private bool Spawn(
            string itemId,
            int count,
            Vector2 position,
            Vector2 velocity,
            float pickupDelay)
        {
            if (count <= 0 || string.IsNullOrWhiteSpace(itemId))
                return false;

            if (!ItemRegistry.Contains(itemId))
            {
                Debug.LogWarning("ITEM DROP: Item is not registered: " + itemId);
                return false;
            }

            if (droppedItemPrefab == null)
            {
                Debug.LogError("ITEM DROP: DroppedItem prefab is not assigned.");
                return false;
            }

            DroppedItem dropped = Instantiate(droppedItemPrefab, position, Quaternion.identity);
            dropped.Initialize(itemId, count, pickupDelay);
            dropped.SetVelocity(velocity);
            return true;
        }
    }
}
