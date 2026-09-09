using Game.World;
using System;
using UnityEngine;
using UnityEngine.LightTransport;
using UnityEngine.UIElements;


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


    [SerializeField]
    private ushort placeBlockID =
        1;


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
        // BREAK
        // =================================================
        //
        // Теперь можно держать ЛКМ.
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
        // Теперь можно держать ПКМ.
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
        // Сначала всегда ломаем foreground.
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

            bool changed =
                worldManager.SetBlock(
                    x,
                    y,
                    0
                );


            if (
                changed
            )
            {
                ForcePlayerCollisionUpdate();
            }


            return changed;

        }


        // =================================================
        // BACKGROUND
        // =================================================
        //
        // Если foreground уже пуст,
        // ломаем background.
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


        bool backgroundChanged =
            worldManager.SetBackground(
                x,
                y,
                0
            );


        return backgroundChanged;

    }


    // =====================================================
    // PLACE BLOCK
    // =====================================================

    private bool PlaceBlock()
    {

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
        // BACKGROUND
        // =================================================
        //
        // Если background существует,
        // foreground можно поставить прямо поверх него.
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

            return
                worldManager.SetBlock(
                    x,
                    y,
                    placeBlockID
                );

        }


        // =================================================
        // NO BACKGROUND
        // =================================================
        //
        // В полностью пустой клетке можно строить,
        // если есть хотя бы один соседний foreground.
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


        // =================================================
        // PLACE
        // =================================================

        return
            worldManager.SetBlock(
                x,
                y,
                placeBlockID
            );

    }


    // =====================================================
    // GET MOUSE CELL
    // =====================================================
    //
    // Получает именно клетку под курсором.
    //
    // Никакого поиска ближайшего блока.
    // Никаких границ.
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


        // =================================================
        // MOUSE -> WORLD
        // =================================================

        Vector2 mouseWorld =
            GetMouseWorldPosition();


        // =================================================
        // WORLD -> CELL
        // =================================================

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
        // DISTANCE
        // =================================================
        //
        // Проверяем расстояние до центра клетки.
        //

        Vector2 cellCenter =
            new Vector2(
                x + 0.5f,
                y + 0.5f
            );


        Vector2 playerPosition =
            new Vector2(
                transform.position.x,
                transform.position.y
            );


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

        // =================================================
        // LEFT
        // =================================================

        if (
            world.GetBlock(
                x - 1,
                y
            ) != 0
        )
        {
            return true;
        }


        // =================================================
        // RIGHT
        // =================================================

        if (
            world.GetBlock(
                x + 1,
                y
            ) != 0
        )
        {
            return true;
        }


        // =================================================
        // BELOW
        // =================================================

        if (
            world.GetBlock(
                x,
                y - 1
            ) != 0
        )
        {
            return true;
        }


        // =================================================
        // ABOVE
        // =================================================

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
