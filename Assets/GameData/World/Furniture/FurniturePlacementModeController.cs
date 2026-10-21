using Game.InputSupport;

using UnityEngine;

using Game.Blocks;
using Game.Content;
using Game.Inventory;
using Game.Inventory.UI;
using Game.GameplaySystems.Furnace;
using Game.GameplaySystems.Multiblock;
using Game.GameplaySystems.Interactions;
using Game.World.Structures;


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
        public bool FurniturePlacementActive => mode == ExtendedMode.Furniture;
        public bool BackgroundPlacementActive => mode == ExtendedMode.Background;


        private MonoBehaviour existingBlockInteraction;


        private PlayerInventory inventory;


        private PlayerCollision playerCollision;


        private Camera worldCamera;


        private float nextAction;


        private void Start()
        {

            inventory =
                GetComponent<
                    PlayerInventory
                >();


            playerCollision =
                GetComponent<
                    PlayerCollision
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
                (Game.Inventory.UI.InventoryUI.Instance == null || !Game.Inventory.UI.InventoryUI.Instance.IsOpen) &&
                !Game.UI.Settings.SettingsTabsController.IsOpen &&
                GameInput.Pressed(GameAction.SwitchLayer)
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

                    existingBlockInteraction.enabled = true;

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


            // FurnaceRuntime executes before this controller. If RMB opened a
            // furnace this frame, do not also place furniture behind the UI.
            if (
                FurnaceRuntime.Instance != null
                &&
                FurnaceRuntime.Instance.IsUIOpen
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


            // Mining is exclusively handled by BlockInteraction in every placement mode.
            // Keeping two left-click handlers caused double drops and layer-dependent mining.

            if (
                GameInput.Held(GameAction.Use)
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
                    GameInput.PointerPosition
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


            FurnitureLayerManager furniture =
                FurnitureLayerManager.Instance;


            if (furniture == null)
            {

                return;

            }


            string furnitureId =
                furniture.GetFurniture(
                    x,
                    y
                );


            if (
                !string.IsNullOrWhiteSpace(
                    furnitureId
                )
            )
            {

                WorldManager manager =
                    WorldManager.Instance;


                World currentWorld =
                    manager != null
                        ? manager.GetWorld()
                        : null;


                if (
                    manager != null
                    &&
                    currentWorld != null
                    &&
                    StructureCascadeBreakRuntime
                        .TryBreakTreeFromFurnitureTrunk(
                            manager,
                            currentWorld,
                            x,
                            y,
                            furnitureId
                        )
                )
                {

                    if (playerCollision != null)
                    {

                        playerCollision.RefreshAfterWorldChange();

                    }


                    return;

                }

            }


            furniture.BreakFurniture(
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
            )
            {

                return;

            }


            if (Input.GetMouseButtonDown(1))
            {
                Vector2 colliderSize =
                    playerCollision != null
                        ? playerCollision.GetColliderSize()
                        : new Vector2(0.8f, 1.8f);

                Rect playerBounds =
                    InteractiveBlockRuntime.GetPlayerBounds(
                        transform,
                        colliderSize
                    );

                WorldManager worldManager =
                    WorldManager.Instance;

                if (
                    worldManager != null &&
                    InteractiveBlockRuntime.TryToggleForeground(
                        worldManager.GetWorld(),
                        x,
                        y,
                        playerBounds
                    )
                )
                {
                    if (playerCollision != null)
                    {
                        playerCollision.RefreshAfterWorldChange();
                    }

                    return;
                }

                if (
                    InteractiveBlockRuntime.TryToggleFurniture(
                        x,
                        y,
                        playerBounds
                    )
                )
                {
                    if (playerCollision != null)
                    {
                        playerCollision.RefreshAfterWorldChange();
                    }

                    return;
                }
            }


            if (
                FurnitureLayerManager.Instance
                    .HasFurniture(
                        x,
                        y
                    )
                ||
                MultiBlockStore.FindCell(
                    x,
                    y
                ) != null
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


            // Multi-block furniture has its own atomic placement path. If this
            // legacy 1x1 furniture controller ever receives the same click, do
            // nothing instead of creating a second/partial object.
            if (
                MultiBlockMetadataRegistry.TryGet(
                    itemId,
                    out MultiBlockMetadata _
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
