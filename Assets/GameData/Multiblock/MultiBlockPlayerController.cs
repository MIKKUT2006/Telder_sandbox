using Game.InputSupport;
using System;
using UnityEngine;
using Game.Inventory;
using Game.Inventory.UI;
using Game.World;
using Game.World.Furniture;
using Game.GameplaySystems.Respawn;
using Game.GameplaySystems.Furnace;
using Game.GameplaySystems.Interactions;

namespace Game.GameplaySystems.Multiblock
{
    [DefaultExecutionOrder(-20000)]
    public sealed class MultiBlockPlayerController : MonoBehaviour
    {
        [SerializeField] private float interactionDistance = 6f;

        private PlayerInventory inventory;
        private PlayerCollision playerCollision;
        private Camera cam;
        private MonoBehaviour legacyFurnitureController;
        private MonoBehaviour legacyBlockInteraction;
        private bool reenableLegacy;
        private bool reenableBlockInteraction;

        private void Awake()
        {
            inventory = GetComponent<PlayerInventory>();
            playerCollision = GetComponent<PlayerCollision>();
            cam = Camera.main;
            ResolveLegacy();
        }

        private void OnEnable()
        {
            FurnitureLayerManager.FurnitureRemoved += OnFurnitureRemoved;
        }

        private void OnDisable()
        {
            FurnitureLayerManager.FurnitureRemoved -= OnFurnitureRemoved;
            if (reenableLegacy && legacyFurnitureController != null)
                legacyFurnitureController.enabled = true;
            if (reenableBlockInteraction && legacyBlockInteraction != null)
                legacyBlockInteraction.enabled = true;
            reenableLegacy = false;
            reenableBlockInteraction = false;
        }

        private void ResolveLegacy()
        {
            MonoBehaviour[] all = GetComponents<MonoBehaviour>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null || all[i] == this)
                    continue;

                string typeName = all[i].GetType().Name;

                if (typeName == "FurniturePlacementModeController")
                    legacyFurnitureController = all[i];
                else if (typeName == "BlockInteraction")
                    legacyBlockInteraction = all[i];
            }
        }

        private void Update()
        {
            if (cam == null)
                cam = Camera.main;
            if (inventory == null || cam == null)
                return;
            if (FurnaceRuntime.Instance != null && FurnaceRuntime.Instance.IsUIOpen)
                return;

            // Do not use EventSystem.IsPointerOverGameObject() here. The HUD uses
            // full-screen raycastable UI elements in some layouts, so that check
            // intermittently classified normal world clicks as UI clicks and let
            // the legacy BlockInteraction place a multi-block as a normal 1x1 block.
            if (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen)
                return;

            Vector3 wp = cam.ScreenToWorldPoint(GameInput.PointerPosition);
            int x = Mathf.FloorToInt(wp.x);
            int y = Mathf.FloorToInt(wp.y);

            Vector2 targetCenter = new Vector2(x + 0.5f, y + 0.5f);
            if (((Vector2)transform.position - targetCenter).sqrMagnitude > interactionDistance * interactionDistance)
                return;

            // Ensure the real furniture save is loaded before consulting the
            // occupancy cache. FindCell can then prune stale/ghost bed records.
            FurnitureLayerManager furniture =
                FurnitureLayerManager.EnsureInstance();

            if (furniture == null)
                return;

            bool pressedUse = GameInput.Pressed(GameAction.Use) ||
                              GameInput.Pressed(GameAction.Interact);
            MultiBlockRecord existing = MultiBlockStore.FindCell(x, y);
            // A more expensive missing-index recovery runs only on the
            // actual interaction click, never every idle frame.
            if (existing == null && pressedUse)
                existing = FindPlacedMultiBlock(furniture, x, y);
            if (existing != null)
            {
                if (pressedUse)
                {
                    SuppressLegacyOneFrame();

                    Vector2 colliderSize =
                        playerCollision != null
                            ? playerCollision.GetColliderSize()
                            : new Vector2(0.8f, 1.8f);

                    Rect playerBounds =
                        InteractiveBlockRuntime.GetPlayerBounds(
                            transform,
                            colliderSize
                        );

                    // Beds must be handled before generic furniture interaction.
                    // Some older saves have the furniture anchor but no cached
                    // multi-block record; FindPlacedMultiBlock repairs that index.
                    if (string.Equals(existing.Kind, "bed", StringComparison.OrdinalIgnoreCase) ||
                        MultiBlockMetadataRegistry.HasTag(existing.BlockId, "bed"))
                    {
                        RespawnPointService.SetSpawn(existing);
                        Game.Audio.TelderAudioSystem.PlayAt("bed.respawn", transform.position, GetInstanceID());
                        return;
                    }

                    if (InteractiveBlockRuntime.TryToggleFurniture(x, y, playerBounds))
                    {
                        Game.Audio.TelderAudioSystem.PlayAt("door.toggle", new Vector3(x+.5f,y+.5f,0f), GetInstanceID());
                        if (playerCollision != null)
                            playerCollision.RefreshAfterWorldChange();
                        return;
                    }
                }

                // Mining of multiblocks is performed by BlockInteraction using
                // the same hardness/progress/debris as normal blocks.
                return;
            }

            if (!pressedUse)
                return;

            string itemId = inventory.GetSelectedItemId();
            if (!MultiBlockMetadataRegistry.TryGet(itemId, out MultiBlockMetadata meta))
                return;

            World.World world = WorldManager.Instance != null ? WorldManager.Instance.GetWorld() : null;
            int anchorX = x;
            int anchorY = y;
            int minX = anchorX - meta.AnchorX;
            int minY = anchorY - meta.AnchorY;

            for (int ix = 0; ix < meta.Width; ix++)
            for (int iy = 0; iy < meta.Height; iy++)
            {
                int cx = minX + ix;
                int cy = minY + iy;

                if (furniture.HasFurniture(cx, cy) || MultiBlockStore.FindCell(cx, cy) != null)
                    return;

                // A furniture multi-block occupies these cells visually. Do not allow
                // placement through solid foreground terrain.
                if (world != null && world.GetBlock(cx, cy) != 0)
                    return;
            }

            if (meta.RequireFloor && world != null)
            {
                int floorY = minY - 1;
                for (int ix = 0; ix < meta.Width; ix++)
                {
                    if (world.GetBlock(minX + ix, floorY) == 0)
                        return;
                }
            }

            SuppressLegacyOneFrame();
            if (!furniture.SetFurniture(anchorX, anchorY, itemId))
                return;

            if (!inventory.TryConsumeSelected(1))
            {
                furniture.RemoveFurniture(anchorX, anchorY);
                return;
            }

            MultiBlockRecord record = new MultiBlockRecord
            {
                X = anchorX,
                Y = anchorY,
                Width = meta.Width,
                Height = meta.Height,
                AnchorX = meta.AnchorX,
                AnchorY = meta.AnchorY,
                BlockId = itemId,
                Kind = meta.Kind
            };

            if (!MultiBlockStore.Add(record))
            {
                // Roll back atomically if another placement claimed a cell this frame.
                furniture.RemoveFurniture(anchorX, anchorY);
                inventory.AddItem(itemId, 1);
            }
        }

        // Finds a placed multi-block even if an old save kept only its
        // furniture anchor, without the 2D occupancy index. Never guesses
        // from an item held in the player's inventory.
        private static MultiBlockRecord FindPlacedMultiBlock(
            FurnitureLayerManager furniture, int cellX, int cellY)
        {
            MultiBlockRecord indexed = MultiBlockStore.FindCell(cellX, cellY);
            if (indexed != null)
                return indexed;

            // Multi-block anchors may sit left/below the clicked cell.
            // 8x8 is ample for beds/doors, and this path runs only on a
            // nearby player interaction, not during chunk generation.
            for (int ay = cellY - 7; ay <= cellY + 7; ay++)
            for (int ax = cellX - 7; ax <= cellX + 7; ax++)
            {
                string id = furniture.GetFurniture(ax, ay);
                if (string.IsNullOrWhiteSpace(id) ||
                    !MultiBlockMetadataRegistry.TryGet(id, out MultiBlockMetadata meta))
                    continue;

                int left = ax - meta.AnchorX;
                int bottom = ay - meta.AnchorY;
                if (cellX < left || cellX >= left + meta.Width ||
                    cellY < bottom || cellY >= bottom + meta.Height)
                    continue;

                MultiBlockRecord repaired = new MultiBlockRecord
                {
                    X = ax,
                    Y = ay,
                    Width = meta.Width,
                    Height = meta.Height,
                    AnchorX = meta.AnchorX,
                    AnchorY = meta.AnchorY,
                    BlockId = id,
                    Kind = meta.Kind
                };

                if (MultiBlockStore.Add(repaired))
                    return repaired;

                return MultiBlockStore.FindAnchor(ax, ay);
            }

            return null;
        }

        private void Break(MultiBlockRecord record)
        {
            FurnitureLayerManager furniture = FurnitureLayerManager.EnsureInstance();
            if (furniture != null && furniture.BreakFurniture(record.X, record.Y))
                return; // FurnitureRemoved event removes the occupancy record.

            // Keep persistence sane even if the visual furniture entry was already lost.
            MultiBlockStore.Remove(record);
        }

        private void OnFurnitureRemoved(int x, int y, string blockId)
        {
            MultiBlockRecord record = MultiBlockStore.FindAnchor(x, y);
            if (record == null)
                return;
            if (!string.Equals(record.BlockId, blockId, StringComparison.OrdinalIgnoreCase))
                return;
            MultiBlockStore.Remove(record);
        }

        private void SuppressLegacyOneFrame()
        {
            if (legacyFurnitureController == null)
                ResolveLegacy();

            if (legacyFurnitureController != null && legacyFurnitureController.enabled)
            {
                legacyFurnitureController.enabled = false;
                reenableLegacy = true;
            }

            if (legacyBlockInteraction != null && legacyBlockInteraction.enabled)
            {
                legacyBlockInteraction.enabled = false;
                reenableBlockInteraction = true;
            }
        }

        private void LateUpdate()
        {
            if (reenableLegacy && legacyFurnitureController != null)
            {
                legacyFurnitureController.enabled = true;
                reenableLegacy = false;
            }

            if (reenableBlockInteraction && legacyBlockInteraction != null)
            {
                legacyBlockInteraction.enabled = true;
                reenableBlockInteraction = false;
            }
        }
    }
}
