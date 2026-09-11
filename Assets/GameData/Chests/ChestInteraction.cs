
using UnityEngine;

using Game.Inventory.UI;
using Game.UI.Cursor;
using Game.Chests.UI;


namespace Game.Chests
{

    [DefaultExecutionOrder(-5100)]
    public class ChestInteraction :
        MonoBehaviour
    {

        [Header("Interaction")]

        [SerializeField]
        private float interactionDistance =
            6f;


        [SerializeField]
        private Camera worldCamera;


        private ChestManager chestManager;


        private void Awake()
        {

            chestManager =
                GetComponent<
                    ChestManager
                >();


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
                chestManager ==
                null
                ||
                World.WorldManager.Instance ==
                null
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


            if (
                InventoryUI.Instance !=
                null
                &&
                InventoryUI.Instance.IsOpen
            )
            {

                return;

            }


            GetMouseCell(
                out int worldX,
                out int worldY
            );


            if (
                !IsInRange(
                    worldX,
                    worldY
                )
                ||
                !chestManager.IsChestAt(
                    worldX,
                    worldY
                )
            )
            {

                return;

            }


            PixelCursorController
                .RequestPointer();


            if (
                !Input.GetMouseButtonDown(
                    1
                )
            )
            {

                return;

            }


            if (
                chestManager.IsClosedChestAt(
                    worldX,
                    worldY
                )
            )
            {

                Debug.Log(
                    "CHEST: This chest is closed."
                );


                return;

            }


            if (
                ChestUIController.Instance !=
                null
            )
            {

                ChestUIController.Instance
                    .OpenChest(
                        worldX,
                        worldY
                    );

            }

        }


        private void GetMouseCell(
            out int worldX,
            out int worldY
        )
        {

            Vector3 mouse =
                worldCamera.ScreenToWorldPoint(
                    Input.mousePosition
                );


            worldX =
                Mathf.FloorToInt(
                    mouse.x
                );


            worldY =
                Mathf.FloorToInt(
                    mouse.y
                );

        }


        private bool IsInRange(
            int worldX,
            int worldY
        )
        {

            Vector2 center =
                new Vector2(
                    worldX +
                    0.5f,
                    worldY +
                    0.5f
                );


            return
                Vector2.Distance(
                    transform.position,
                    center
                )
                <=
                interactionDistance;

        }

    }

}
