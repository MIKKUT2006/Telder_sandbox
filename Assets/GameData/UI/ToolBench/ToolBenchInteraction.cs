using System;

using UnityEngine;

using Game.Blocks;
using Game.Content;
using Game.Inventory;
using Game.Inventory.UI;
using Game.UI.Cursor;
using Game.World;
using Game.World.Furniture;


namespace Game.ToolBench
{

    [DefaultExecutionOrder(-5200)]
    public class ToolBenchInteraction :
        MonoBehaviour
    {

        // =====================================================
        // SETTINGS
        // =====================================================

        [SerializeField]
        private float interactionDistance =
            6f;


        [SerializeField]
        private Camera worldCamera;


        [SerializeField]
        private string toolBenchTag =
            "tool_bench";


        // =====================================================
        // REFERENCES
        // =====================================================

        private PlayerInventory inventory;


        // =====================================================
        // UNITY
        // =====================================================

        private void Awake()
        {

            inventory =
                GetComponent<
                    PlayerInventory
                >();


            if (
                inventory ==
                null
            )
            {

                inventory =
                    GetComponentInParent<
                        PlayerInventory
                    >();

            }


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

            // =================================================
            // WORLD CHECK
            // =================================================

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


            // =================================================
            // DO NOT INTERACT THROUGH UI
            // =================================================

            if (
                InventoryUI.Instance !=
                null
                &&
                InventoryUI.Instance.IsOpen
            )
            {

                return;

            }


            // =================================================
            // CAMERA
            // =================================================

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


            // =================================================
            // MOUSE WORLD POSITION
            // =================================================

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


            // =================================================
            // DISTANCE
            // =================================================

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


            // =================================================
            // FIND TOOL BENCH
            // =================================================

            bool isToolBench =
                false;


            // ---------------------------------------------
            // FURNITURE LAYER
            // ---------------------------------------------

            if (
                FurnitureLayerManager.Instance !=
                null
                &&
                FurnitureLayerManager.Instance
                    .TryGetDefinition(
                        x,
                        y,
                        out BlockDefinition furnitureDefinition
                    )
            )
            {

                isToolBench =
                    HasToolBenchTag(
                        furnitureDefinition
                    );

            }


            // ---------------------------------------------
            // NORMAL BLOCK FALLBACK
            // ---------------------------------------------

            if (
                !isToolBench
            )
            {

                ushort blockId =
                    WorldManager.Instance
                        .GetWorld()
                        .GetBlock(
                            x,
                            y
                        );


                isToolBench =
                    HasToolBenchTag(
                        blockId
                    );

            }


            if (
                !isToolBench
            )
            {

                return;

            }


            // =================================================
            // CURSOR
            // =================================================

            PixelCursorController
                .RequestPointer();


            // =================================================
            // RMB
            // =================================================

            if (
                !Input.GetMouseButtonDown(
                    1
                )
            )
            {

                return;

            }


            if (
                inventory ==
                null
            )
            {

                Debug.LogWarning(
                    "TOOL BENCH: PlayerInventory not found."
                );


                return;

            }


            ToolBenchUI toolBenchUI =
                ToolBenchUI
                    .EnsureCreated();


            if (
                toolBenchUI ==
                null
            )
            {

                Debug.LogWarning(
                    "TOOL BENCH: ToolBenchUI could not be created."
                );


                return;

            }


            toolBenchUI
                .Open(
                    inventory
                );

        }


        // =====================================================
        // TOOL BENCH TAG
        // =====================================================

        private bool HasToolBenchTag(
            BlockDefinition block
        )
        {

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
                        toolBenchTag,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {

                    return true;

                }

            }


            return false;

        }


        // =====================================================
        // NORMAL BLOCK FALLBACK
        // =====================================================

        private bool HasToolBenchTag(
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


                return
                    HasToolBenchTag(
                        block
                    );

            }
            catch
            {

                return false;

            }

        }

    }

}