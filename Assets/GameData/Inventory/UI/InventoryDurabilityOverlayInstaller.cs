using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Inventory.UI
{
    public class InventoryDurabilityOverlayInstaller : MonoBehaviour
    {
        private Coroutine installRoutine;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<InventoryDurabilityOverlayInstaller>() != null)
                return;

            GameObject go = new GameObject("InventoryDurabilityOverlayInstaller");
            DontDestroyOnLoad(go);
            go.AddComponent<InventoryDurabilityOverlayInstaller>();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            BeginInstall();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (installRoutine != null)
                StopCoroutine(installRoutine);
            installRoutine = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            BeginInstall();
        }

        private void BeginInstall()
        {
            if (installRoutine != null)
                StopCoroutine(installRoutine);
            installRoutine = StartCoroutine(InstallWhenReady());
        }

        private IEnumerator InstallWhenReady()
        {
            // UI is built shortly after scene load. Scan only during that bootstrap
            // window instead of Resources.FindObjectsOfTypeAll every 0.75 seconds forever.
            for (int attempt = 0; attempt < 40; attempt++)
            {
                InventorySlotUI[] slots = UnityEngine.Resources.FindObjectsOfTypeAll<InventorySlotUI>();
                int sceneSlots = 0;

                for (int i = 0; i < slots.Length; i++)
                {
                    InventorySlotUI slot = slots[i];
                    if (slot == null || !slot.gameObject.scene.IsValid())
                        continue;

                    sceneSlots++;
                    if (slot.GetComponent<InventoryDurabilityBar>() == null)
                        slot.gameObject.AddComponent<InventoryDurabilityBar>();
                }

                if (sceneSlots > 0)
                {
                    installRoutine = null;
                    yield break;
                }

                yield return new WaitForSecondsRealtime(0.25f);
            }

            installRoutine = null;
        }
    }
}
