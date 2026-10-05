using System.Collections.Generic;
using UnityEngine;
using Game.Inventory;
using Game.Inventory.UI;
using Game.World;
using Game.World.Items;
using Game.GameplaySystems.Furnace;
using Game.UI.Pause;
using PlayerStatsComponent = Game.PlayerStats.PlayerStats;

namespace Game.GameplaySystems.Respawn
{
    public sealed class PlayerDeathRespawnController : MonoBehaviour
    {
        private PlayerInventory inventory;
        private PlayerStatsComponent stats;
        private bool dead;
        private readonly List<MonoBehaviour> disabled = new List<MonoBehaviour>();
        private float previousTimeScale = 1f;
        private CursorLockMode previousCursorLock;
        private bool previousCursorVisible;

        private void Awake()
        {
            inventory = GetComponent<PlayerInventory>();
            stats = GetComponent<PlayerStatsComponent>();
        }

        private void OnEnable()
        {
            if (stats == null)
                stats = GetComponent<PlayerStatsComponent>();
            if (stats != null)
                stats.Died += Die;
        }

        private void OnDisable()
        {
            if (stats != null)
                stats.Died -= Die;
        }

        private void Die()
        {
            if (dead)
                return;

            dead = true;

            // Death must replace any open inventory/container UI cleanly.
            // Closing first also safely drops a cursor-held stack before the
            // remaining inventory is dropped at the death position.
            if (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen)
                InventoryUI.Instance.CloseInventory();

            if (FurnaceRuntime.Instance != null && FurnaceRuntime.Instance.IsUIOpen)
                FurnaceRuntime.Instance.CloseUI();

            DropAllItems();
            DisablePlayerControl();

            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            previousTimeScale = Time.timeScale;
            //Time.timeScale = 0f;
            previousCursorLock = UnityEngine.Cursor.lockState;
            previousCursorVisible = UnityEngine.Cursor.visible;
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;

            DeathScreenUI.Show(this, Respawn, GoMenu);
        }

        private void DropAllItems()
        {
            if (inventory == null)
                return;

            ItemDropSpawner spawner = ItemDropSpawner.Instance;
            if (spawner == null)
            {
                Debug.LogWarning("RESPAWN: ItemDropSpawner is unavailable; inventory was kept to prevent item loss.");
                return;
            }

            Vector2 position = transform.position;
            bool changed = false;

            for (int i = 0; i < PlayerInventory.SlotCount; i++)
            {
                ItemStack stack = inventory.GetSlot(i);
                if (stack == null || stack.IsEmpty)
                    continue;

                if (spawner.SpawnFromPlayer(stack.ItemId, stack.Count, position))
                {
                    stack.Clear();
                    changed = true;
                }
            }

            if (changed)
                inventory.NotifyExternalChange();
        }

        private void DisablePlayerControl()
        {
            disabled.Clear();
            MonoBehaviour[] all = GetComponents<MonoBehaviour>();
            for (int i = 0; i < all.Length; i++)
            {
                MonoBehaviour component = all[i];
                if (component == null || component == this || !component.enabled)
                    continue;

                string name = component.GetType().Name;
                if (name == "PlayerController" ||
                    name == "BlockInteraction" ||
                    name == "MultiBlockPlayerController" ||
                    name == "PlayerWeaponController" ||
                    name == "FoodUseController" ||
                    name == "ToolBenchInteraction" ||
                    name == "DurabilityMiningHook")
                {
                    component.enabled = false;
                    disabled.Add(component);
                }
            }
        }

        private void Respawn()
        {
            Vector2 feetPoint;
            if (!RespawnPointService.TryGet(out feetPoint))
            {
                if (!TryGetDefaultSpawn(out feetPoint))
                    feetPoint = (Vector2)transform.position;
            }

            float halfHeight = 0.9f;
            PlayerCollision collision = GetComponent<PlayerCollision>();
            if (collision != null)
                halfHeight = collision.GetColliderSize().y * 0.5f;

            transform.position = new Vector3(
                feetPoint.x,
                feetPoint.y + halfHeight + 0.05f,
                transform.position.z
            );

            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            if (stats != null)
                stats.RestoreState(stats.MaxHealth, stats.MaxHunger);

            for (int i = 0; i < disabled.Count; i++)
            {
                if (disabled[i] != null)
                    disabled[i].enabled = true;
            }
            disabled.Clear();

            dead = false;
            DeathScreenUI.Hide();
            RestorePauseAndCursor();

            if (collision != null)
                collision.RefreshAfterWorldChange();
        }

        private bool TryGetDefaultSpawn(out Vector2 feetPoint)
        {
            feetPoint = Vector2.zero;
            WorldManager manager = WorldManager.Instance;
            return manager != null && manager.TryGetDefaultSpawnFeetPosition(out feetPoint);
        }

        private void GoMenu()
        {
            DeathScreenUI.Hide();
            RestorePauseAndCursor();

            PauseMenuController pause =
                FindFirstObjectByType<PauseMenuController>();

            if (pause != null)
            {
                // Reuse the normal pause-menu exit pipeline so the world is
                // saved, the preview is captured and persistent visuals are
                // cleaned before MainMenu loads.
                pause.ExitToMainMenu();
                return;
            }

            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }

        private void RestorePauseAndCursor()
        {
            Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
            UnityEngine.Cursor.lockState = previousCursorLock;
            UnityEngine.Cursor.visible = previousCursorVisible;
        }
    }
}
