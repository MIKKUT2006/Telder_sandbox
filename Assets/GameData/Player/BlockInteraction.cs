using Game.Blocks;
using Game.Content;
using Game.Inventory;
using Game.Inventory.UI;
using Game.Items;
using Game.World;
using Game.World.Collision;
using Game.World.Items;

using UnityEngine;


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


    // =====================================================
    // CAMERA
    // =====================================================

    [Header("Camera")]

    [SerializeField]
    private Camera playerCamera;


    // =====================================================
    // INVENTORY
    // =====================================================

    [Header("Inventory")]

    [SerializeField]
    private PlayerInventory playerInventory;


    // =====================================================
    // REFERENCES
    // =====================================================

    private WorldManager worldManager;

    private World world;

    private PlayerCollision playerCollision;


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

        // =================================================
        // CAMERA
        // =================================================

        if (
            playerCamera == null
        )
        {
            playerCamera =
                Camera.main;
        }


        if (
            playerCamera == null
        )
        {
            Debug.LogError(
                "BLOCK INTERACTION: Camera not found."
            );

            return;
        }


        // =================================================
        // WORLD MANAGER
        // =================================================

        worldManager =
            WorldManager.Instance;


        if (
            worldManager == null
        )
        {
            Debug.LogError(
                "BLOCK INTERACTION: WorldManager is null."
            );

            return;
        }


        // =================================================
        // WORLD
        // =================================================

        world =
            worldManager.GetWorld();


        if (
            world == null
        )
        {
            Debug.LogError(
                "BLOCK INTERACTION: World is null."
            );

            return;
        }


        // =================================================
        // PLAYER COLLISION
        // =================================================

        playerCollision =
            GetComponent<PlayerCollision>();


        // =================================================
        // PLAYER INVENTORY
        // =================================================

        if (
            playerInventory == null
        )
        {
            playerInventory =
                GetComponent<PlayerInventory>();
        }


        if (
            playerInventory == null
        )
        {
            Debug.LogWarning(
                "BLOCK INTERACTION: PlayerInventory not found. " +
                "Breaking will work, but placing from hotbar will not."
            );
        }


        // =================================================
        // STATE
        // =================================================

        nextBreakTime =
            0f;

        nextPlaceTime =
            0f;


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
            !initialized
        )
        {
            return;
        }


        // =================================================
        // INVENTORY OPEN
        // =================================================
        //
        // Не ломаем и не ставим блоки через UI.
        //

        if (
            InventoryUI.Instance != null &&
            InventoryUI.Instance.IsOpen
        )
        {
            nextBreakTime =
                0f;

            nextPlaceTime =
                0f;

            return;
        }


        // =================================================
        // BREAK
        // =================================================
        //
        // ЛКМ можно держать.
        //

        if (
            Input.GetMouseButton(0)
        )
        {

            if (
                Time.time >=
                nextBreakTime
            )
            {

                BreakBlock();


                nextBreakTime =
                    Time.time +
                    breakInterval;

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
        //
        // ПКМ можно держать.
        //

        if (
            Input.GetMouseButton(1)
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

        // =================================================
        // GET CELL
        // =================================================

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
        //
        // Всегда сначала ломаем foreground.
        //
        // ВАЖНО:
        // definition получаем ДО SetBlock(..., 0).
        //

        ushort foregroundID =
            world.GetBlock(
                x,
                y
            );


        if (
            foregroundID != 0
        )
        {

            BlockDefinition foregroundDefinition =
                world.GetBlockDefinition(
                    foregroundID
                );


            bool changed =
                worldManager.SetBlock(
                    x,
                    y,
                    0
                );


            if (
                !changed
            )
            {
                return false;
            }


            ForcePlayerCollisionUpdate();


            SpawnBlockDrop(
                foregroundDefinition,
                x,
                y
            );


            return true;

        }


        // =================================================
        // BACKGROUND
        // =================================================
        //
        // Если foreground пустой,
        // ломаем background.
        //
        // Definition тоже сохраняем ДО удаления.
        //

        ushort backgroundID =
            world.GetBackground(
                x,
                y
            );


        if (
            backgroundID == 0
        )
        {
            return false;
        }


        BlockDefinition backgroundDefinition =
            world.GetBlockDefinition(
                backgroundID
            );


        bool backgroundChanged =
            worldManager.SetBackground(
                x,
                y,
                0
            );


        if (
            !backgroundChanged
        )
        {
            return false;
        }


        SpawnBlockDrop(
            backgroundDefinition,
            x,
            y
        );


        return true;

    }


    // =====================================================
    // SPAWN BLOCK DROP
    // =====================================================

    private void SpawnBlockDrop(
    BlockDefinition definition,
    int x,
    int y
)
    {
        if (definition == null)
        {
            Debug.LogError(
                "DROP DEBUG: BlockDefinition is NULL."
            );

            return;
        }


        Debug.Log(
            "DROP DEBUG: Block = " +
            definition.ID +
            " Drop = " +
            definition.Drop +
            " Count = " +
            definition.DropCount
        );


        if (definition.DropCount <= 0)
        {
            Debug.LogWarning(
                "DROP DEBUG: DropCount <= 0"
            );

            return;
        }


        string dropID =
            definition.Drop.ToString();


        Debug.Log(
            "DROP DEBUG: Drop string = " +
            dropID
        );


        if (!ItemRegistry.TryGet(
                dropID,
                out ItemDefinition itemDefinition
            ))
        {
            Debug.LogError(
                "DROP DEBUG: ITEM NOT FOUND IN ItemRegistry: " +
                dropID
            );

            return;
        }


        Debug.Log(
            "DROP DEBUG: Item found: " +
            itemDefinition.Name
        );


        if (ItemDropSpawner.Instance == null)
        {
            Debug.LogError(
                "DROP DEBUG: ItemDropSpawner.Instance IS NULL."
            );

            return;
        }


        bool spawned =
            ItemDropSpawner.Instance
                .SpawnFromBlock(
                    dropID,
                    definition.DropCount,
                    new Vector2(
                        x + 0.5f,
                        y + 0.5f
                    )
                );


        Debug.Log(
            "DROP DEBUG: Spawn result = " +
            spawned
        );
    }


    // =====================================================
    // PLACE BLOCK
    // =====================================================

    private bool PlaceBlock()
    {

        // =================================================
        // INVENTORY
        // =================================================

        if (
            playerInventory == null
        )
        {
            return false;
        }


        // =================================================
        // SELECTED HOTBAR ITEM -> BLOCK ID
        // =================================================

        if (
            !TryGetSelectedBlockID(
                out ushort selectedBlockID
            )
        )
        {
            return false;
        }


        // =================================================
        // GET CELL UNDER MOUSE
        // =================================================

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


        // =================================================
        // FOREGROUND MUST BE EMPTY
        // =================================================

        ushort foregroundID =
            world.GetBlock(
                x,
                y
            );


        if (
            foregroundID != 0
        )
        {
            return false;
        }


        // =================================================
        // DON'T PLACE INSIDE PLAYER
        // =================================================

        if (
            IsInsidePlayer(
                placePosition
            )
        )
        {
            return false;
        }


        // =================================================
        // BACKGROUND EXISTS
        // =================================================
        //
        // Если background есть,
        // foreground разрешено поставить поверх него.
        //

        ushort backgroundID =
            world.GetBackground(
                x,
                y
            );


        if (
            backgroundID != 0
        )
        {
            return TryPlaceSelectedBlock(
                x,
                y,
                selectedBlockID
            );
        }


        // =================================================
        // NO BACKGROUND
        // =================================================
        //
        // Если background отсутствует,
        // разрешаем строительство при наличии
        // хотя бы одного соседнего foreground-блока.
        //
        // Благодаря этому можно строить столбы в небо.
        //

        if (
            !HasAdjacentForegroundBlock(
                x,
                y
            )
        )
        {
            return false;
        }


        return TryPlaceSelectedBlock(
            x,
            y,
            selectedBlockID
        );

    }


    // =====================================================
    // GET SELECTED BLOCK ID
    // =====================================================

    private bool TryGetSelectedBlockID(
        out ushort blockID
    )
    {

        blockID =
            0;


        if (
            playerInventory == null
        )
        {
            return false;
        }


        string selectedItemID =
            playerInventory
                .GetSelectedItemId();


        if (
            string.IsNullOrWhiteSpace(
                selectedItemID
            )
        )
        {
            return false;
        }


        // =================================================
        // STRING -> CONTENT ID
        // =================================================

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
            Debug.LogWarning(
                "BLOCK INTERACTION: Invalid selected item ID: " +
                selectedItemID
            );

            return false;
        }


        // =================================================
        // ITEM MUST EXIST
        // =================================================

        if (
            !ItemRegistry.TryGet(
                selectedItemID,
                out ItemDefinition itemDefinition
            )
        )
        {
            Debug.LogWarning(
                "BLOCK INTERACTION: Selected item is not registered: " +
                selectedItemID
            );

            return false;
        }


        // =================================================
        // ITEM MUST BE A BLOCK
        // =================================================

        if (
            itemDefinition.Type !=
            ItemType.Block
        )
        {
            return false;
        }


        // =================================================
        // BLOCK MUST EXIST
        // =================================================

        if (
            !BlockRegistry.Contains(
                contentID
            )
        )
        {
            Debug.LogWarning(
                "BLOCK INTERACTION: No block definition for item: " +
                selectedItemID
            );

            return false;
        }


        if (
            !BlockIDRegistry.Contains(
                contentID
            )
        )
        {
            Debug.LogWarning(
                "BLOCK INTERACTION: No numeric block ID for item: " +
                selectedItemID
            );

            return false;
        }


        blockID =
            BlockIDRegistry.GetID(
                contentID
            );


        return
            blockID != 0;

    }


    // =====================================================
    // PLACE SELECTED BLOCK
    // =====================================================

    private bool TryPlaceSelectedBlock(
        int x,
        int y,
        ushort blockID
    )
    {

        if (
            blockID == 0
        )
        {
            return false;
        }


        bool changed =
            worldManager.SetBlock(
                x,
                y,
                blockID
            );


        if (
            !changed
        )
        {
            return false;
        }


        // =================================================
        // CONSUME ONLY AFTER SUCCESSFUL PLACEMENT
        // =================================================

        bool consumed =
            playerInventory
                .TryConsumeSelected(
                    1
                );


        if (
            !consumed
        )
        {
            // Теоретически сюда попадать не должны,
            // потому что до SetBlock уже был выбран
            // существующий предмет.
            //
            // Но чтобы нельзя было получить бесплатный
            // блок при неожиданном рассинхроне,
            // откатываем установку.

            worldManager.SetBlock(
                x,
                y,
                0
            );


            Debug.LogWarning(
                "BLOCK INTERACTION: Failed to consume selected item. " +
                "Placed block was rolled back."
            );


            return false;
        }


        ForcePlayerCollisionUpdate();


        return true;

    }


    // =====================================================
    // GET MOUSE CELL
    // =====================================================
    //
    // Получаем ИМЕННО пустую/занятую клетку,
    // на которую указывает мышь.
    //
    // =====================================================

    private bool TryGetMouseCell(
        out UnityEngine.Vector2Int cell
    )
    {

        cell =
            UnityEngine.Vector2Int.zero;


        if (
            playerCamera == null ||
            world == null
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


        // =================================================
        // DISTANCE TO CELL CENTER
        // =================================================

        Vector2 cellCenter =
            new Vector2(
                x + 0.5f,
                y + 0.5f
            );


        Vector2 playerPosition =
            transform.position;


        float distance =
            Vector2.Distance(
                playerPosition,
                cellCenter
            );


        if (
            distance >
            interactionDistance
        )
        {
            return false;
        }


        return true;

    }


    // =====================================================
    // GET MOUSE WORLD POSITION
    // =====================================================

    private Vector2 GetMouseWorldPosition()
    {

        Vector3 mouseScreenPosition =
            Input.mousePosition;


        mouseScreenPosition.z =
            Mathf.Abs(
                playerCamera.transform.position.z
            );


        Vector3 mouseWorldPosition =
            playerCamera.ScreenToWorldPoint(
                mouseScreenPosition
            );


        return new Vector2(
            mouseWorldPosition.x,
            mouseWorldPosition.y
        );

    }


    // =====================================================
    // HAS ADJACENT FOREGROUND BLOCK
    // =====================================================

    private bool HasAdjacentForegroundBlock(
        int x,
        int y
    )
    {

        if (
            world.GetBlock(
                x - 1,
                y
            ) != 0
        )
        {
            return true;
        }


        if (
            world.GetBlock(
                x + 1,
                y
            ) != 0
        )
        {
            return true;
        }


        if (
            world.GetBlock(
                x,
                y - 1
            ) != 0
        )
        {
            return true;
        }


        if (
            world.GetBlock(
                x,
                y + 1
            ) != 0
        )
        {
            return true;
        }


        return false;

    }


    // =====================================================
    // FORCE PLAYER COLLISION UPDATE
    // =====================================================

    private void ForcePlayerCollisionUpdate()
    {

        if (
            playerCollision == null
        )
        {
            return;
        }


        playerCollision.ResolveOverlaps();

        playerCollision.ForceGroundCheck();

    }


    // =====================================================
    // IS INSIDE PLAYER
    // =====================================================

    private bool IsInsidePlayer(
        UnityEngine.Vector2Int blockPosition
    )
    {

        if (
            playerCollision == null
        )
        {
            return false;
        }


        // =================================================
        // PLAYER
        // =================================================

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


        // =================================================
        // BLOCK
        // =================================================

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


        // =================================================
        // INTERSECTION
        // =================================================

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
