using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Inventory;

namespace Game.UI.Character
{
    /// <summary>Reapplies selected appearance after Animator changes SpriteRenderer sprites.
    /// Survives travel; does not become part of world-save inventory snapshots.</summary>
    [DefaultExecutionOrder(32001)]
    public sealed class PlayerAppearanceRuntime : MonoBehaviour
    {
        private static PlayerAppearanceRuntime instance;
        private PlayerInventory inventory;
        private AppearanceRig rig;
        private AppearancePreset active;
        private float nextLookup;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void StartService()
        {
            if (instance != null) return;
            var go = new GameObject("PlayerAppearance_Persistent");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<PlayerAppearanceRuntime>();
        }
        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
            active = PlayerAppearanceStorage.Active();
            SceneManager.sceneLoaded += ChangedScene;
        }
        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= ChangedScene;
            if (instance == this) instance = null;
        }
        private void ChangedScene(Scene scene, LoadSceneMode mode)
        {
            inventory = null;
            rig = null;
            nextLookup = 0f;
        }
        public static void Refresh()
        {
            if (instance == null) return;
            instance.active = PlayerAppearanceStorage.Active();
            instance.inventory = null;
            instance.rig = null;
            instance.nextLookup = 0f;
        }
        private void LateUpdate()
        {
            if (inventory == null || rig == null || !rig.HasParts)
            {
                if (Time.unscaledTime < nextLookup) return;
                nextLookup = Time.unscaledTime + 0.5f;
                // Do not ever select the disabled preview prefab in the main menu.
                foreach (PlayerInventory candidate in FindObjectsOfType<PlayerInventory>())
                {
                    if (candidate == null || candidate.GetComponentInParent<AppearancePreviewMarker>() != null)
                        continue;
                    if (!candidate.gameObject.activeInHierarchy || !candidate.enabled) continue;
                    AppearanceRig created = new AppearanceRig(candidate.transform);
                    if (!created.HasParts) continue;
                    inventory = candidate;
                    rig = created;
                    rig.Set(active);
                    break;
                }
                return;
            }
            if (active != null) rig.Tick();
        }
    }

    // Distinguishes a purely visual preview from an actual gameplay player.
    public sealed class AppearancePreviewMarker : MonoBehaviour { }
}
