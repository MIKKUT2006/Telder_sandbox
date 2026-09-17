using UnityEngine;
using Game.Inventory;
using Game.Items.Durability;

namespace Game.ToolBench
{
    // No Inspector wiring required: scans the active scene and attaches the two player-side components.
    public class ToolBenchRuntimeBootstrap : MonoBehaviour
    {
        private float nextScanTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            ToolBenchUI.EnsureCreated();
            if (FindObjectOfType<ToolBenchRuntimeBootstrap>() != null) return;
            GameObject go = new GameObject("ToolBenchRuntimeBootstrap");
            DontDestroyOnLoad(go);
            go.AddComponent<ToolBenchRuntimeBootstrap>();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextScanTime) return;
            nextScanTime = Time.unscaledTime + 1f;

            PlayerInventory[] inventories = UnityEngine.Resources.FindObjectsOfTypeAll<PlayerInventory>();
            for (int i = 0; i < inventories.Length; i++)
            {
                PlayerInventory inv = inventories[i];
                if (inv == null || !inv.gameObject.scene.IsValid()) continue;

                if (inv.GetComponent<ToolBenchInteraction>() == null)
                    inv.gameObject.AddComponent<ToolBenchInteraction>();

                if (inv.GetComponent<DurabilityMiningHook>() == null)
                    inv.gameObject.AddComponent<DurabilityMiningHook>();
            }
        }
    }
}
