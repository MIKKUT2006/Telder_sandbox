using System;
using UnityEngine;
using Game.Inventory;
using Game.Inventory.UI;
using Game.World;
using Game.World.Furniture;
using Game.GameplaySystems.Respawn;
using Game.GameplaySystems.Furnace;

namespace Game.GameplaySystems.Multiblock
{
    [DefaultExecutionOrder(-20000)]
    public sealed class MultiBlockPlayerController : MonoBehaviour
    {
        [SerializeField] private float interactionDistance = 6f;

        private PlayerInventory inventory;
        private Camera cam;
        private MonoBehaviour legacyFurnitureController;
        private MonoBehaviour legacyBlockInteraction;
        private bool reenableLegacy;
        private bool reenableBlockInteraction;

        private void Awake()
        {
            inventory = GetComponent<PlayerInventory>();
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

            Vector3 wp = cam.ScreenToWorldPoint(Input.mousePosition);
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

            MultiBlockRecord existing = MultiBlockStore.FindCell(x, y);
            if (existing != null)
            {
                if (Input.GetMouseButtonDown(1))
                {
                    SuppressLegacyOneFrame();
                    if (string.Equals(existing.Kind, "bed", StringComparison.OrdinalIgnoreCase) ||
                        MultiBlockMetadataRegistry.HasTag(existing.BlockId, "bed"))
                    {
                        RespawnPointService.SetSpawn(existing);
                    }
                }

                if (Input.GetMouseButtonDown(0))
                {
                    SuppressLegacyOneFrame();
                    Break(existing);
                }
                return;
            }

            if (!Input.GetMouseButtonDown(1))
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
