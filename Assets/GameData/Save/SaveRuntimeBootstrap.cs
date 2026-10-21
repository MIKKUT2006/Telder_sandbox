using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

using Game.Inventory;
using Game.World;


namespace Game.Save
{
    public class SaveRuntimeBootstrap :
        MonoBehaviour
    {
        private static SaveRuntimeBootstrap instance;

        private float nextAutosaveTime;


        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.BeforeSceneLoad
        )]
        private static void Create()
        {
            if (
                instance != null
            )
            {
                return;
            }


            GameObject gameObject =
                new GameObject(
                    "TelderSaveRuntime"
                );


            instance =
                gameObject.AddComponent<
                    SaveRuntimeBootstrap
                >();


            DontDestroyOnLoad(
                gameObject
            );
        }


        private void Awake()
        {
            if (
                instance != null &&
                instance != this
            )
            {
                Destroy(
                    gameObject
                );

                return;
            }


            instance =
                this;


            DontDestroyOnLoad(
                gameObject
            );


            SceneManager.sceneLoaded +=
                OnSceneLoaded;


            nextAutosaveTime =
                Time.unscaledTime +
                30f;
        }


        private void OnDestroy()
        {
            SceneManager.sceneLoaded -=
                OnSceneLoaded;
        }


        private void Update()
        {
            if (
                !SaveGameRuntime.HasActiveSave ||
                SaveGameRuntime.IsInventoryTransferPending
            )
            {
                return;
            }


            if (
                Time.unscaledTime <
                nextAutosaveTime
            )
            {
                return;
            }


            nextAutosaveTime =
                Time.unscaledTime +
                30f;


            if (
                WorldManager.Instance != null
            )
            {
                SaveGameRuntime.SaveCurrentScene();
            }
        }


        private void OnApplicationQuit()
        {
            if (
                SaveGameRuntime.HasActiveSave &&
                !SaveGameRuntime.IsInventoryTransferPending &&
                WorldManager.Instance != null
            )
            {
                SaveGameRuntime.SaveCurrentScene();
            }
        }


        private void OnSceneLoaded(
            Scene scene,
            LoadSceneMode mode
        )
        {
            if (
                !SaveGameRuntime.HasActiveSave
            )
            {
                return;
            }


            SaveGameRuntime.MarkDroppedItemsRestorePending();
            StartCoroutine(
                RestoreWhenReady()
            );
        }


        private IEnumerator RestoreWhenReady()
        {
            WorldManager manager =
                null;


            float timeout =
                Time.unscaledTime +
                20f;


            while (
                Time.unscaledTime <
                timeout
            )
            {
                manager =
                    WorldManager.Instance;


                if (
                    manager != null &&
                    manager.GetLoader() != null
                )
                {
                    break;
                }


                yield return null;
            }


            if (
                manager == null ||
                manager.GetLoader() == null
            )
            {
                yield break;
            }


            Game.World.Items.ItemDropSpawner.EnsureInstance();


            // Inventory must be restored after the real player spawn, not
            // after the entire view-distance generation queue drains.
            while (!manager.InitialSpawnReady)
            {
                yield return null;
            }


            yield return null;

            yield return null;


            PlayerInventory inventory =
                Object.FindFirstObjectByType<
                    PlayerInventory
                >();


            if (
                inventory == null
            )
            {
                yield break;
            }


            SaveGameRuntime.RestoreInventory(
                inventory
            );


            Game.PlayerStats.PlayerStats stats =
                inventory.GetComponent<
                    Game.PlayerStats.PlayerStats
                >();

            if (stats != null)
            {
                SaveGameRuntime.RestorePlayerStats(stats);
            }


            HeldItemVisualController heldVisual =
                inventory.GetComponent<
                    HeldItemVisualController
                >();

            if (heldVisual == null)
            {
                heldVisual =
                    inventory.gameObject.AddComponent<
                        HeldItemVisualController
                    >();
            }

            if (heldVisual != null)
            {
                heldVisual.ForceRefresh();
            }


            if (
                !SaveGameRuntime
                    .TryGetCurrentPlayerPosition(
                        out Vector3 savedPosition
                    )
            )
            {
                SaveGameRuntime.RestoreDroppedItems();
                SaveGameRuntime.CompleteInventoryTransfer();
                yield break;
            }


            int chunkX =
                Mathf.FloorToInt(
                    savedPosition.x /
                    Chunk.SizeX
                );


            int chunkY =
                Mathf.FloorToInt(
                    savedPosition.y /
                    Chunk.SizeY
                );


            manager.GetLoader()
                .SetPlayerChunk(
                    chunkX,
                    chunkY
                );


            while (
                !manager.GetLoader()
                    .IsChunkLoaded(
                        chunkX,
                        chunkY
                    )
            )
            {
                manager.GetLoader()
                    .Process();


                yield return null;
            }


            inventory.transform.position =
                savedPosition;

            SaveGameRuntime.RestoreDroppedItems();


            PlayerCollision collision =
                inventory.GetComponent<
                    PlayerCollision
                >();


            if (
                collision != null
            )
            {
                collision.ResolveOverlaps();

                collision.ForceGroundCheck();
            }


            // A dimension reload can rebuild the arm/hand hierarchy after the
            // inventory restore. Refresh once more after positioning so the
            // selected item is guaranteed to be visible in the new rig.
            yield return null;

            heldVisual =
                inventory.GetComponent<
                    HeldItemVisualController
                >();

            if (heldVisual != null)
            {
                heldVisual.ForceRefresh();
            }
            SaveGameRuntime.CompleteInventoryTransfer();
        }
    }
}
