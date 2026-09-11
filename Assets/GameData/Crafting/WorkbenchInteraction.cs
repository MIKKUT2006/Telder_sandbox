
using System;

using UnityEngine;

using Game.Blocks;
using Game.Content;
using Game.Crafting.UI;
using Game.Inventory.UI;
using Game.UI.Cursor;
using Game.World;
using Game.World.Furniture;


namespace Game.Crafting
{

    [DefaultExecutionOrder(-5200)]
    public class WorkbenchInteraction :
        MonoBehaviour
    {

        [SerializeField]
        private float interactionDistance =
            6f;


        [SerializeField]
        private Camera worldCamera;


        [SerializeField]
        private string workbenchTag =
            "workbench";


        private void Awake()
        {

            if (
                worldCamera ==
                null
            )
            {

                worldCamera =
                    Camera.main;

            }

        }


        private void Update()
        {

            if (
                WorldManager.Instance ==
                null
                ||
                WorldManager.Instance.GetWorld() ==
                null
            )
            {

                return;

            }


            if (
                InventoryUI.Instance !=
                null
                &&
                InventoryUI.Instance.IsOpen
            )
            {

                return;

            }


            if (
                worldCamera ==
                null
            )
            {

                worldCamera =
                    Camera.main;

            }


            if (
                worldCamera ==
                null
            )
            {

                return;

            }


            Vector3 mouse =
                worldCamera.ScreenToWorldPoint(
                    Input.mousePosition
                );


            int x =
                Mathf.FloorToInt(
                    mouse.x
                );


            int y =
                Mathf.FloorToInt(
                    mouse.y
                );


            Vector2 center =
                new Vector2(
                    x +
                    0.5f,
                    y +
                    0.5f
                );


            if (
                Vector2.Distance(
                    transform.position,
                    center
                )
                >
                interactionDistance
            )
            {

                return;

            }


            bool isWorkbench = false;

            if (FurnitureLayerManager.Instance != null &&
                FurnitureLayerManager.Instance.TryGetDefinition(x, y, out BlockDefinition furnitureDefinition))
            {
                isWorkbench = HasWorkbenchTag(furnitureDefinition);
            }

            if (!isWorkbench)
            {
                ushort blockId =
                    WorldManager.Instance
                        .GetWorld()
                        .GetBlock(x, y);
                isWorkbench = HasWorkbenchTag(blockId);
            }

            if (!isWorkbench)
                return;


            PixelCursorController
                .RequestPointer();


            if (
                Input.GetMouseButtonDown(
                    1
                )
            )
            {

                if (
                    CraftingUIController.Instance !=
                    null
                )
                {

                    CraftingUIController.Instance
                        .OpenWorkbench();

                }

            }

        }


        private bool HasWorkbenchTag(BlockDefinition block)
        {
            if (block == null || block.Tags == null) return false;
            for (int i=0;i<block.Tags.Count;i++)
                if (string.Equals(block.Tags[i], workbenchTag, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private bool HasWorkbenchTag(
            ushort blockId
        )
        {

            if (
                blockId ==
                0
            )
            {

                return false;

            }


            try
            {

                ContentID contentId =
                    BlockIDRegistry
                        .GetContentID(
                            blockId
                        );


                if (
                    !BlockRegistry.Contains(
                        contentId
                    )
                )
                {

                    return false;

                }


                BlockDefinition block =
                    BlockRegistry.Get(
                        contentId
                    );


                if (
                    block ==
                    null
                    ||
                    block.Tags ==
                    null
                )
                {

                    return false;

                }


                for (
                    int i = 0;
                    i < block.Tags.Count;
                    i++
                )
                {

                    if (
                        string.Equals(
                            block.Tags[i],
                            workbenchTag,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {

                        return true;

                    }

                }

            }
            catch
            {
            }


            return false;

        }

    }

}
