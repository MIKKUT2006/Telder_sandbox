
using UnityEngine;

using Game.Blocks;
using Game.Content;
using Game.Inventory;
using Game.Inventory.UI;


namespace Game.World.Furniture
{

    [DefaultExecutionOrder(10000)]
    public class FurniturePlacementModeController :
        MonoBehaviour
    {

        private enum ExtendedMode
        {

            Foreground,

            Background,

            Furniture

        }


        [SerializeField]
        private float interactionDistance =
            6f;


        [SerializeField]
        private float placeInterval =
            0.08f;


        private ExtendedMode mode;


        private MonoBehaviour existingBlockInteraction;


        private PlayerInventory inventory;


        private Camera worldCamera;


        private float nextAction;


        private void Start()
        {

            inventory =
                GetComponent<
                    PlayerInventory
                >();


            worldCamera =
                Camera.main;


            MonoBehaviour[] behaviours =
                GetComponents<
                    MonoBehaviour
                >();


            for (
                int i = 0;
                i < behaviours.Length;
                i++
            )
            {

                MonoBehaviour behaviour =
                    behaviours[i];


                if (
                    behaviour !=
                    null
                    &&
                    behaviour !=
                    this
                    &&
                    behaviour
                        .GetType()
                        .Name ==
                    "BlockInteraction"
                )
                {

                    existingBlockInteraction =
                        behaviour;


                    break;

                }

            }

        }


        private void Update()
        {

            if (
                Input.GetKeyDown(
                    KeyCode.X
                )
            )
            {

                mode =
                    (
                        ExtendedMode
                    )
                    (
                        (
                            (int)mode +
                            1
                        )
                        %
                        3
                    );


                if (
                    existingBlockInteraction !=
                    null
                )
                {

                    existingBlockInteraction.enabled =
                        mode !=
                        ExtendedMode.Furniture;

                }


                PlacementModeIndicator.Show(
                    GetModeMessage(
                        mode
                    )
                );


                Debug.Log(
                    "PLACEMENT MODE: " +
                    mode
                );

            }


            if (
                mode !=
                ExtendedMode.Furniture
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
                ||
                inventory ==
                null
                ||
                FurnitureLayerManager.Instance ==
                null
            )
            {

                return;

            }


            if (
                Input.GetMouseButton(
                    0
                )
                &&
                Time.time >=
                nextAction
            )
            {

                BreakFurniture();


                nextAction =
                    Time.time +
                    placeInterval;

            }


            if (
                Input.GetMouseButton(
                    1
                )
                &&
                Time.time >=
                nextAction
            )
            {

                PlaceFurniture();


                nextAction =
                    Time.time +
                    placeInterval;

            }

        }


        private string GetModeMessage(
            ExtendedMode currentMode
        )
        {

            switch (
                currentMode
            )
            {

                case ExtendedMode.Background:

                    return
                        "Режим установки — фон";


                case ExtendedMode.Furniture:

                    return
                        "Режим установки — мебель";


                default:

                    return
                        "Режим установки — блоки";

            }

        }


        private bool TryCell(
            out int x,
            out int y
        )
        {

            Vector3 worldPoint =
                worldCamera.ScreenToWorldPoint(
                    Input.mousePosition
                );


            x =
                Mathf.FloorToInt(
                    worldPoint.x
                );


            y =
                Mathf.FloorToInt(
                    worldPoint.y
                );


            return
                Vector2.Distance(
                    transform.position,

                    new Vector2(
                        x +
                        0.5f,

                        y +
                        0.5f
                    )
                )
                <=
                interactionDistance;

        }


        private void BreakFurniture()
        {

            if (
                !TryCell(
                    out int x,
                    out int y
                )
            )
            {

                return;

            }


            FurnitureLayerManager.Instance
                .BreakFurniture(
                    x,
                    y
                );

        }


        private void PlaceFurniture()
        {

            if (
                !TryCell(
                    out int x,
                    out int y
                )
                ||
                FurnitureLayerManager.Instance
                    .HasFurniture(
                        x,
                        y
                    )
            )
            {

                return;

            }


            string itemId =
                inventory.GetSelectedItemId();


            if (
                string.IsNullOrWhiteSpace(
                    itemId
                )
            )
            {

                return;

            }


            BlockDefinition definition =
                null;


            try
            {

                ContentID contentID =
                    ContentID.Parse(
                        itemId
                    );


                if (
                    BlockRegistry.Contains(
                        contentID
                    )
                )
                {

                    definition =
                        BlockRegistry.Get(
                            contentID
                        );

                }

            }
            catch
            {
            }


            if (
                definition ==
                null
            )
            {

                return;

            }


            if (
                FurnitureLayerManager.Instance
                    .SetFurniture(
                        x,
                        y,
                        itemId
                    )
            )
            {

                inventory.TryConsumeSelected(
                    1
                );

            }

        }

    }

}
