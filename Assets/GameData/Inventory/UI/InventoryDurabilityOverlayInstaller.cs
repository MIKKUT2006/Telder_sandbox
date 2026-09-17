using UnityEngine;

namespace Game.Inventory.UI
{
    public class InventoryDurabilityOverlayInstaller : MonoBehaviour
    {
        private float nextScanTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindObjectOfType<InventoryDurabilityOverlayInstaller>() != null) return;
            GameObject go = new GameObject("InventoryDurabilityOverlayInstaller");
            DontDestroyOnLoad(go);
            go.AddComponent<InventoryDurabilityOverlayInstaller>();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextScanTime) return;
            nextScanTime = Time.unscaledTime + 0.75f;

            InventorySlotUI[] slots = UnityEngine.Resources.FindObjectsOfTypeAll<InventorySlotUI>();
            for (int i = 0; i < slots.Length; i++)
            {
                InventorySlotUI slot = slots[i];
                if (slot == null || !slot.gameObject.scene.IsValid()) continue;
                if (slot.GetComponent<InventoryDurabilityBar>() == null)
                    slot.gameObject.AddComponent<InventoryDurabilityBar>();
            }
        }
    }
}
