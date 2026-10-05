using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Inventory;
using Game.Items.Durability;

namespace Game.ToolBench
{
    public class ToolBenchRuntimeBootstrap : MonoBehaviour
    {
        private Coroutine attachRoutine;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            ToolBenchUI.EnsureCreated();
            if (FindFirstObjectByType<ToolBenchRuntimeBootstrap>() != null)
                return;

            GameObject go = new GameObject("ToolBenchRuntimeBootstrap");
            DontDestroyOnLoad(go);
            go.AddComponent<ToolBenchRuntimeBootstrap>();
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
            ToolBenchUI.EnsureCreated();
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
            while (true)
            {
                PlayerInventory inventory = FindFirstObjectByType<PlayerInventory>();
                if (inventory != null)
                {
                    if (inventory.GetComponent<ToolBenchInteraction>() == null)
                        inventory.gameObject.AddComponent<ToolBenchInteraction>();

                    if (inventory.GetComponent<DurabilityMiningHook>() == null)
                        inventory.gameObject.AddComponent<DurabilityMiningHook>();

                    attachRoutine = null;
                    yield break;
                }

                yield return new WaitForSecondsRealtime(0.25f);
            }
        }
    }
}
