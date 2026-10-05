using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Inventory;
using Game.GameplaySystems.Multiblock;
using Game.GameplaySystems.Respawn;
using Game.GameplaySystems.Furnace;

namespace Game.GameplaySystems
{
    public sealed class GameplaySystemsBootstrap : MonoBehaviour
    {
        private static GameplaySystemsBootstrap instance;
        private Coroutine attachRoutine;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Ensure()
        {
            if (instance != null)
                return;

            GameObject go = new GameObject("GameplaySystemsBootstrap");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<GameplaySystemsBootstrap>();
            go.AddComponent<FurnaceRuntime>();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            BeginAttach();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (attachRoutine != null)
                StopCoroutine(attachRoutine);
            attachRoutine = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            BeginAttach();
        }

        private void BeginAttach()
        {
            if (attachRoutine != null)
                StopCoroutine(attachRoutine);
            attachRoutine = StartCoroutine(AttachWhenPlayerExists());
        }

        private IEnumerator AttachWhenPlayerExists()
        {
            // Player may be spawned after initial chunks are ready, so wait without
            // permanently scanning the scene for the rest of the session.
            while (true)
            {
                PlayerInventory inventory = FindFirstObjectByType<PlayerInventory>();
                if (inventory != null)
                {
                    GameObject player = inventory.gameObject;
                    if (player.GetComponent<MultiBlockPlayerController>() == null)
                        player.AddComponent<MultiBlockPlayerController>();
                    if (player.GetComponent<PlayerDeathRespawnController>() == null)
                        player.AddComponent<PlayerDeathRespawnController>();
                    attachRoutine = null;
                    yield break;
                }

                yield return new WaitForSecondsRealtime(0.25f);
            }
        }
    }
}
