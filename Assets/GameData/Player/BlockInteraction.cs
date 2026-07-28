using UnityEngine;

using Game.World;


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
        // INITIALIZED
        // =================================================

        initialized =
            true;


        nextPlaceTime =
            0f;


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

        if (
            Input.GetMouseButtonDown(
                0
            )
        )
        {

            BreakBlock();

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

                if (
                    PlaceBlock()
                )
                {

                    nextPlaceTime =
                        Time.time +
                        placeInterval;

                }
                else
                {

                    // Если поставить блок не удалось,
                    // не блокируем следующую попытку.

                    nextPlaceTime =
                        Time.time +
                        placeInterval;

                }

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

    private void BreakBlock()
    {

        if (
            !TryGetMouseBlock(
                out UnityEngine.Vector2Int blockPosition
            )
        )
        {

            return;

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


                Debug.Log(
                    "BLOCK INTERACTION: FOREGROUND BLOCK BROKEN " +
                    x +
                    ", " +
                    y
                );

            }


            return;

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
            backgroundID != 0
        )
        {

            bool changed =
                worldManager.SetBackground(
                    x,
                    y,
                    0
                );


            if (
                changed
            )
            {

                Debug.Log(
                    "BLOCK INTERACTION: BACKGROUND BLOCK BROKEN " +
                    x +
                    ", " +
                    y
                );

            }


            return;

        }

    }


    // =====================================================
    // PLACE BLOCK
    // =====================================================

    private bool PlaceBlock()
    {

        if (
            !TryGetMouseBlock(
                out UnityEngine.Vector2Int targetPosition
            )
        )
        {

            return false;

        }


        Vector2 mouseWorld =
            GetMouseWorldPosition();


        UnityEngine.Vector2Int placePosition =
            GetAdjacentBlockPosition(
                targetPosition,
                mouseWorld
            );


        int x =
            placePosition.x;


        int y =
            placePosition.y;


        // =================================================
        // CHECK FOREGROUND
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
        // CHECK BACKGROUND
        // =================================================

        ushort backgroundID =
            world.GetBackground(
                x,
                y
            );


        // =================================================
        // PLACE IN FOREGROUND
        // =================================================
        //
        // Если место полностью пустое,
        // ставим обычный блок.
        //

        if (
            backgroundID == 0
        )
        {

            if (
                IsInsidePlayer(
                    placePosition
                )
            )
            {

                return false;

            }


            return
                worldManager.SetBlock(
                    x,
                    y,
                    placeBlockID
                );

        }


        // =================================================
        // BACKGROUND EXISTS
        // =================================================
        //
        // Если есть фон, но передний слой пуст,
        // обычный блок всё равно можно поставить
        // поверх заднего фона.
        //

        if (
            IsInsidePlayer(
                placePosition
            )
        )
        {

            return false;

        }


        return
            worldManager.SetBlock(
                x,
                y,
                placeBlockID
            );

    }


    // =====================================================
    // GET MOUSE BLOCK
    // =====================================================

    private bool TryGetMouseBlock(
        out UnityEngine.Vector2Int blockPosition
    )
    {

        blockPosition =
            UnityEngine.Vector2Int.zero;


        if (
            playerCamera == null
        )
        {

            return false;

        }


        Vector2 mouseWorld =
            GetMouseWorldPosition();


        // =================================================
        // DISTANCE
        // =================================================

        Vector2 playerPosition =
            new Vector2(
                transform.position.x,
                transform.position.y
            );


        float distance =
            Vector2.Distance(
                playerPosition,
                mouseWorld
            );


        if (
            distance >
            interactionDistance
        )
        {

            return false;

        }


        // =================================================
        // WORLD -> BLOCK
        // =================================================
        //
        // ВАЖНО:
        //
        // Мы НЕ ищем ближайший блок по лучу.
        //
        // Мы берём именно ту клетку,
        // в которую попала мышь.
        //

        blockPosition =
            new UnityEngine.Vector2Int(
                Mathf.FloorToInt(
                    mouseWorld.x
                ),

                Mathf.FloorToInt(
                    mouseWorld.y
                )
            );


        // =================================================
        // CHECK FOREGROUND
        // =================================================

        ushort foregroundID =
            world.GetBlock(
                blockPosition.x,
                blockPosition.y
            );


        if (
            foregroundID != 0
        )
        {

            return true;

        }


        // =================================================
        // CHECK BACKGROUND
        // =================================================

        ushort backgroundID =
            world.GetBackground(
                blockPosition.x,
                blockPosition.y
            );


        if (
            backgroundID != 0
        )
        {

            return true;

        }


        return false;

    }


    // =====================================================
    // MOUSE WORLD POSITION
    // =====================================================

    private Vector2 GetMouseWorldPosition()
    {

        Vector3 mouse =
            Input.mousePosition;


        mouse.z =
            Mathf.Abs(
                playerCamera.transform.position.z
            );


        Vector3 worldPoint =
            playerCamera.ScreenToWorldPoint(
                mouse
            );


        return new Vector2(
            worldPoint.x,
            worldPoint.y
        );

    }


    // =====================================================
    // GET ADJACENT BLOCK
    // =====================================================

    private UnityEngine.Vector2Int
        GetAdjacentBlockPosition(
            UnityEngine.Vector2Int target,
            Vector2 mouseWorld
        )
    {

        Vector2 center =
            new Vector2(
                target.x +
                0.5f,

                target.y +
                0.5f
            );


        Vector2 direction =
            mouseWorld -
            center;


        // =================================================
        // HORIZONTAL
        // =================================================

        if (
            Mathf.Abs(
                direction.x
            )
            >
            Mathf.Abs(
                direction.y
            )
        )
        {

            if (
                direction.x >
                0f
            )
            {

                return
                    new UnityEngine.Vector2Int(
                        target.x + 1,
                        target.y
                    );

            }


            return
                new UnityEngine.Vector2Int(
                    target.x - 1,
                    target.y
                );

        }


        // =================================================
        // VERTICAL
        // =================================================

        if (
            direction.y >
            0f
        )
        {

            return
                new UnityEngine.Vector2Int(
                    target.x,
                    target.y + 1
                );

        }


        return
            new UnityEngine.Vector2Int(
                target.x,
                target.y - 1
            );

    }


    // =====================================================
    // PLAYER COLLISION UPDATE
    // =====================================================

    private void ForcePlayerCollisionUpdate()
    {

        if (
            playerCollision == null
        )
        {
            return;
        }


        // =================================================
        // RESOLVE OVERLAPS
        // =================================================

        playerCollision.ResolveOverlaps();


        // =================================================
        // FORCE GROUND CHECK
        // =================================================

        playerCollision.ForceGroundCheck();

    }


    // =====================================================
    // PLAYER COLLISION
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


        Vector2 size =
            playerCollision.GetColliderSize();


        Vector2 position =
            transform.position;


        // =================================================
        // PLAYER BOUNDS
        // =================================================

        float playerLeft =
            position.x -
            size.x *
            0.5f;


        float playerRight =
            position.x +
            size.x *
            0.5f;


        float playerBottom =
            position.y -
            size.y *
            0.5f;


        float playerTop =
            position.y +
            size.y *
            0.5f;


        // =================================================
        // BLOCK BOUNDS
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