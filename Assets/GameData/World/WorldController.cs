using UnityEngine;
using Game.World.Loading;

namespace Game.World
{
    public class WorldController : MonoBehaviour
    {
        // =====================================================
        // PLAYER
        // =====================================================

        [Header("Player")]
        [SerializeField]
        private Transform player;


        // =====================================================
        // REFERENCES
        // =====================================================

        private WorldManager worldManager;

        private ChunkLoader loader;


        // =====================================================
        // STATE
        // =====================================================

        private int lastChunkX;

        private int lastChunkY;

        private bool hasLastChunk;


        // =====================================================
        // START
        // =====================================================

        private void Start()
        {
            worldManager =
                WorldManager.Instance;


            if (worldManager == null)
            {
                Debug.LogError(
                    "WORLD CONTROLLER: WorldManager is null."
                );

                return;
            }


            loader =
                worldManager.GetLoader();


            if (loader == null)
            {
                Debug.LogError(
                    "WORLD CONTROLLER: ChunkLoader is null."
                );

                return;
            }


            if (player == null)
            {
                Debug.LogError(
                    "WORLD CONTROLLER: Player reference is null."
                );

                return;
            }


            Debug.Log(
                "WORLD CONTROLLER: Initialized."
            );


            // Первичная загрузка
            UpdateChunks();
        }


        // =====================================================
        // UPDATE
        // =====================================================

        private void Update()
        {
            if (loader == null)
            {
                return;
            }


            if (player == null)
            {
                return;
            }


            // Сообщаем загрузчику,
            // где находится игрок.
            UpdateChunks();


            // Обрабатываем очередь загрузки.
            loader.Process();
        }


        // =====================================================
        // UPDATE CHUNKS
        // =====================================================

        private void UpdateChunks()
        {
            if (player == null)
            {
                return;
            }


            int chunkX =
                Mathf.FloorToInt(
                    player.position.x /
                    Chunk.SizeX
                );


            int chunkY =
                Mathf.FloorToInt(
                    player.position.y /
                    Chunk.SizeY
                );


            // Если игрок всё ещё
            // в том же чанке,
            // новую позицию сообщать не нужно.
            if (
                hasLastChunk &&
                chunkX == lastChunkX &&
                chunkY == lastChunkY
            )
            {
                return;
            }


            lastChunkX =
                chunkX;


            lastChunkY =
                chunkY;


            hasLastChunk =
                true;


            Debug.Log(
                "WORLD CONTROLLER: Player chunk = " +
                chunkX +
                ", " +
                chunkY
            );


            loader.SetPlayerChunk(
                chunkX,
                chunkY
            );
        }
    }
}