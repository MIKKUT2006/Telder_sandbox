
using UnityEngine;

using Game.Blocks;
using Game.Content;
using Game.Inventory;
using Game.Inventory.UI;
using Game.Mining;
using Game.World;
using Game.World.Items;
using Game.World.Structures;


public class BlockInteraction :
    MonoBehaviour
{

    // =====================================================
    // SETTINGS
    // =====================================================

    [Header("Interaction")]

    [SerializeField]
    private float interactionDistance =
        6f;


    [Header("Break")]

    [SerializeField]
    private float breakInterval =
        0.08f;


    [Header("Place")]

    [SerializeField]
    private float placeInterval =
        0.08f;


    [Header("Camera")]

    [SerializeField]
    private Camera playerCamera;


    // =====================================================
    // REFERENCES
    // =====================================================

    private WorldManager worldManager;

    private World world;

    private PlayerCollision playerCollision;

    private PlayerInventory inventory;


    // =====================================================
    // STATE
    // =====================================================

    private bool initialized;

    private float nextBreakTime;

    private float nextPlaceTime;


    // =====================================================
    // START
    // =====================================================

    private void Start()
    {

        if (
            playerCamera ==
            null
        )
        {

            playerCamera =
                Camera.main;

        }


        if (
            playerCamera ==
            null
        )
        {

            Debug.LogError(
                "BLOCK INTERACTION: Camera not found."
            );


            return;

        }


        worldManager =
            WorldManager.Instance;


        if (
            worldManager ==
            null
        )
        {

            Debug.LogError(
                "BLOCK INTERACTION: WorldManager is null."
            );


            return;

        }


        world =
            worldManager.GetWorld();


        if (
            world ==
            null
        )
        {

            Debug.LogError(
                "BLOCK INTERACTION: World is null."
            );


            return;

        }


        playerCollision =
            GetComponent<
                PlayerCollision
            >();


        inventory =
            GetComponent<
                PlayerInventory
            >();


        nextBreakTime =
            0f;


        nextPlaceTime =
            0f;


        MiningMetadataRegistry.Reload();


        initialized =
            true;


        Debug.Log(
            "BLOCK INTERACTION: INITIALIZED."
        );

    }


    // =====================================================
    // UPDATE
    // =====================================================

    private void Update()
    {

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
            !initialized
        )
        {

            return;

        }


        // =================================================
        // CONTINUOUS MINING VISUAL
        // =================================================
        //
        // The held item animation is independent from locomotion.
        // It is driven by this signal, not by Idle/Run animator clips.
        //
        // =================================================

        if (
            Input.GetMouseButton(
                0
            )
            &&
            IsMouseOverAnyBlock()
        )
        {

            MiningVisualSignal.Pulse();

        }


        // =================================================
        // BREAK
        // =================================================

        if (
            Input.GetMouseButton(
                0
            )
        )
        {

            if (
                Time.time >=
                nextBreakTime
            )
            {

                BreakBlock();


                float miningSpeed =
                    MiningToolRules
                        .GetSelectedMiningSpeed(
                            inventory
                        );


                nextBreakTime =
                    Time.time +
                    breakInterval /
                    Mathf.Max(
                        0.05f,
                        miningSpeed
                    );

            }

        }
        else
        {

            nextBreakTime =
                0f;

        }


        // =================================================
        // PLACE
        // =================================================

        if (
            Input.GetMouseButton(
                1
            )
        )
        {

            if (
                Time.time >=
                nextPlaceTime
            )
            {

                PlaceBlock();


                nextPlaceTime =
                    Time.time +
                    placeInterval;

            }

        }
        else
        {

            nextPlaceTime =
                0f;

        }

    }


    // =====================================================
    // BREAK BLOCK
    // =====================================================

    private bool BreakBlock()
    {

        if (
            !TryGetMouseCell(
                out UnityEngine.Vector2Int blockPosition
            )
        )
        {

            return false;

        }


        int x =
            blockPosition.x;


        int y =
            blockPosition.y;


        // =================================================
        // FOREGROUND
        // =================================================

        ushort foregroundID =
            world.GetBlock(
                x,
                y
            );


        if (
            foregroundID !=
            0
        )
        {

            if (
                !MiningToolRules.CanBreak(
                    foregroundID,
                    inventory
                )
            )
            {

                return false;

            }


            // Special structure behaviour.
            //
            // For Tree structures, a click on a trunk cell cuts
            // the tree at this height. The clicked trunk and all
            // matching structure cells above it are removed and
            // each block produces its normal drop.
            if (
                StructureCascadeBreakRuntime
                    .TryBreakTreeFromTrunk(
                        worldManager,
                        world,
                        x,
                        y,
                        foregroundID
                    )
            )
            {
                ForcePlayerCollisionUpdate();

                return true;
            }


            bool foregroundChanged =
                worldManager.SetBlock(
                    x,
                    y,
                    0
                );


            if (
                foregroundChanged
            )
            {

                SpawnBlockDrop(
                    foregroundID,
                    x,
                    y
                );


                ForcePlayerCollisionUpdate();

            }


            return foregroundChanged;

        }


        // =================================================
        // BACKGROUND
        // =================================================

        ushort backgroundID =
            world.GetBackground(
                x,
                y
            );


        if (
            backgroundID ==
            0
        )
        {

            return false;

        }


        if (
            !MiningToolRules.CanBreak(
                backgroundID,
                inventory
            )
        )
        {

            return false;

        }


        bool backgroundChanged =
            worldManager.SetBackground(
                x,
                y,
                0
            );


        if (
            backgroundChanged
        )
        {

            SpawnBlockDrop(
                backgroundID,
                x,
                y
            );

        }


        return backgroundChanged;

    }


    // =====================================================
    // BLOCK DROP
    // =====================================================

    private void SpawnBlockDrop(
        ushort blockID,
        int worldX,
        int worldY
    )
    {

        if (
            blockID ==
            0
        )
        {

            return;

        }


        if (
            ItemDropSpawner.Instance ==
            null
        )
        {

            Debug.LogWarning(
                "MINING: ItemDropSpawner.Instance is null. " +
                "Block was removed but its item cannot be spawned."
            );


            return;

        }


        ContentID contentID;


        try
        {

            contentID =
                BlockIDRegistry.GetContentID(
                    blockID
                );

        }
        catch
        {

            return;

        }


        if (
            !BlockRegistry.Contains(
                contentID
            )
        )
        {

            return;

        }


        BlockDefinition block =
            BlockRegistry.Get(
                contentID
            );


        if (
            block ==
            null
        )
        {

            return;

        }


        int count =
            block.DropCount;


        if (
            count <=
            0
        )
        {

            return;

        }


        string dropId =
            null;


        try
        {

            dropId =
                block.Drop.ToString();

        }
        catch
        {
        }


        if (
            string.IsNullOrWhiteSpace(
                dropId
            )
        )
        {

            // Safe fallback for simple blocks whose item has the
            // same ContentID as the block.
            dropId =
                contentID.ToString();

        }


        ItemDropSpawner.Instance
            .SpawnFromBlock(
                dropId,
                count,

                new Vector2(
                    worldX +
                    0.5f,

                    worldY +
                    0.55f
                )
            );

    }


    // =====================================================
    // PLACE BLOCK
    // =====================================================

    private bool PlaceBlock()
    {

        if (
            !TryGetSelectedBlockID(
                out ushort selectedBlockID
            )
        )
        {

            return false;

        }


        if (
            !TryGetMouseCell(
                out UnityEngine.Vector2Int placePosition
            )
        )
        {

            return false;

        }


        int x =
            placePosition.x;


        int y =
            placePosition.y;


        ushort foregroundID =
            world.GetBlock(
                x,
                y
            );


        if (
            foregroundID !=
            0
        )
        {

            return false;

        }


        if (
            IsInsidePlayer(
                placePosition
            )
        )
        {

            return false;

        }


        ushort backgroundID =
            world.GetBackground(
                x,
                y
            );


        bool canPlace =
            backgroundID !=
            0
            ||
            HasAdjacentForegroundBlock(
                x,
                y
            );


        if (
            !canPlace
        )
        {

            return false;

        }


        bool placed =
            worldManager.SetBlock(
                x,
                y,
                selectedBlockID
            );


        if (
            !placed
        )
        {

            return false;

        }


        // Consume ONLY after the world accepted the placement.
        //
        // This prevents inventory loss when:
        // - target cell is invalid;
        // - player overlaps the cell;
        // - SetBlock rejects the change.
        if (
            inventory !=
            null
        )
        {

            bool consumed =
                inventory.TryConsumeSelected(
                    1
                );


            if (
                !consumed
            )
            {

                Debug.LogWarning(
                    "BLOCK INTERACTION: Block was placed, " +
                    "but selected inventory item could not be consumed."
                );

            }

        }


        return true;

    }


    // =====================================================
    // SELECTED PLACEABLE BLOCK
    // =====================================================

    private bool TryGetSelectedBlockID(
        out ushort blockID
    )
    {

        blockID =
            0;


        if (
            inventory ==
            null
        )
        {

            return false;

        }


        string selectedItemID =
            inventory.GetSelectedItemId();


        if (
            string.IsNullOrWhiteSpace(
                selectedItemID
            )
        )
        {

            return false;

        }


        ContentID contentID;


        try
        {

            contentID =
                ContentID.Parse(
                    selectedItemID
                );

        }
        catch
        {

            return false;

        }


        // Only items that are actually registered as blocks
        // are allowed through normal foreground placement.
        if (
            !BlockIDRegistry.Contains(
                contentID
            )
        )
        {

            return false;

        }


        blockID =
            BlockIDRegistry.GetID(
                contentID
            );


        return
            blockID !=
            0;

    }


    // =====================================================
    // MINING VISUAL TARGET
    // =====================================================

    private bool IsMouseOverAnyBlock()
    {

        if (
            !TryGetMouseCell(
                out UnityEngine.Vector2Int cell
            )
        )
        {

            return false;

        }


        return
            world.GetBlock(
                cell.x,
                cell.y
            )
            !=
            0
            ||
            world.GetBackground(
                cell.x,
                cell.y
            )
            !=
            0;

    }


    // =====================================================
    // GET MOUSE CELL
    // =====================================================

    private bool TryGetMouseCell(
        out UnityEngine.Vector2Int cell
    )
    {

        cell =
            UnityEngine.Vector2Int.zero;


        if (
            playerCamera ==
            null
            ||
            world ==
            null
        )
        {

            return false;

        }


        Vector2 mouseWorld =
            GetMouseWorldPosition();


        int x =
            Mathf.FloorToInt(
                mouseWorld.x
            );


        int y =
            Mathf.FloorToInt(
                mouseWorld.y
            );


        cell =
            new UnityEngine.Vector2Int(
                x,
                y
            );


        Vector2 cellCenter =
            new Vector2(
                x +
                0.5f,

                y +
                0.5f
            );


        Vector2 playerPosition =
            new Vector2(
                transform.position.x,
                transform.position.y
            );


        return
            Vector2.Distance(
                playerPosition,
                cellCenter
            )
            <=
            interactionDistance;

    }


    private Vector2 GetMouseWorldPosition()
    {

        Vector3 mouseScreenPosition =
            Input.mousePosition;


        mouseScreenPosition.z =
            Mathf.Abs(
                playerCamera
                    .transform
                    .position
                    .z
            );


        Vector3 mouseWorldPosition =
            playerCamera.ScreenToWorldPoint(
                mouseScreenPosition
            );


        return
            new Vector2(
                mouseWorldPosition.x,
                mouseWorldPosition.y
            );

    }


    // =====================================================
    // ADJACENCY
    // =====================================================

    private bool HasAdjacentForegroundBlock(
        int x,
        int y
    )
    {

        return
            world.GetBlock(
                x -
                1,
                y
            )
            !=
            0
            ||
            world.GetBlock(
                x +
                1,
                y
            )
            !=
            0
            ||
            world.GetBlock(
                x,
                y -
                1
            )
            !=
            0
            ||
            world.GetBlock(
                x,
                y +
                1
            )
            !=
            0;

    }


    // =====================================================
    // PLAYER COLLISION
    // =====================================================

    private void ForcePlayerCollisionUpdate()
    {

        if (
            playerCollision ==
            null
        )
        {

            return;

        }


        playerCollision.ResolveOverlaps();


        playerCollision.ForceGroundCheck();

    }


    private bool IsInsidePlayer(
        UnityEngine.Vector2Int blockPosition
    )
    {

        if (
            playerCollision ==
            null
        )
        {

            return false;

        }


        Vector2 playerSize =
            playerCollision.GetColliderSize();


        Vector2 playerPosition =
            transform.position;


        float playerLeft =
            playerPosition.x -
            playerSize.x *
            0.5f;


        float playerRight =
            playerPosition.x +
            playerSize.x *
            0.5f;


        float playerBottom =
            playerPosition.y -
            playerSize.y *
            0.5f;


        float playerTop =
            playerPosition.y +
            playerSize.y *
            0.5f;


        float blockLeft =
            blockPosition.x;


        float blockRight =
            blockPosition.x +
            1f;


        float blockBottom =
            blockPosition.y;


        float blockTop =
            blockPosition.y +
            1f;


        return
            playerRight >
            blockLeft
            &&
            playerLeft <
            blockRight
            &&
            playerTop >
            blockBottom
            &&
            playerBottom <
            blockTop;

    }

}
