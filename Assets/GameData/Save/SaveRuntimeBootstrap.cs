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
                !SaveGameRuntime.HasActiveSave
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


            // Даём WorldManager.Start закончить
            // начальную генерацию и SpawnPlayer().
            while (
                manager.GetLoader()
                    .GetLoadQueueSize() >
                0
            )
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


            if (
                !SaveGameRuntime
                    .TryGetCurrentPlayerPosition(
                        out Vector3 savedPosition
                    )
            )
            {
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
        }
    }
}
