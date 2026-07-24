using UnityEngine;


namespace Game.World
{

    public class WorldController :
        MonoBehaviour
    {

        // =====================================================
        // PLAYER
        // =====================================================

        [Header("Player")]

        [SerializeField]
        private Transform player;


        // =====================================================
        // STATE
        // =====================================================

        private WorldManager worldManager;


        private Vector2Int lastPlayerChunk;


        private bool initialized;


        // =====================================================
        // START
        // =====================================================

        private void Start()
        {

            worldManager =
                WorldManager.Instance;


            if (
                worldManager == null
            )
            {

                Debug.LogError(
                    "WORLD CONTROLLER: WorldManager is null."
                );

                enabled =
                    false;

                return;

            }


            if (
                player == null
            )
            {

                Debug.LogError(
                    "WORLD CONTROLLER: Player reference is null."
                );

                enabled =
                    false;

                return;

            }


            initialized =
                false;


            UpdatePlayerChunk(
                true
            );

        }


        // =====================================================
        // UPDATE
        // =====================================================

        private void Update()
        {

            if (
                worldManager == null
            )
            {
                return;
            }


            if (
                player == null
            )
            {
                return;
            }


            if (
                !worldManager.IsReady
            )
            {
                return;
            }


            UpdatePlayerChunk(
                false
            );

        }


        // =====================================================
        // UPDATE PLAYER CHUNK
        // =====================================================

        private void UpdatePlayerChunk(
            bool forceUpdate
        )
        {

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


            Vector2Int currentChunk =
                new Vector2Int(
                    chunkX,
                    chunkY
                );


            // =================================================
            // CHECK SAME CHUNK
            // =================================================

            if (
                !forceUpdate &&
                initialized &&
                currentChunk ==
                lastPlayerChunk
            )
            {
                return;
            }


            // =================================================
            // SAVE
            // =================================================

            lastPlayerChunk =
                currentChunk;


            initialized =
                true;


            // =================================================
            // SEND TO WORLD MANAGER
            // =================================================

            worldManager.UpdatePlayerChunk(
                currentChunk.x,
                currentChunk.y
            );

        }

    }

}