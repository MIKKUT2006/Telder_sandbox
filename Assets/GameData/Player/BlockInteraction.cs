
using UnityEngine;
using Game.World;

//  ÓÓ‰ËÌ‡Ú˚ ·ÎÓÍÓ‚ ÏË‡.
// ›ÚÓ Õ≈ UnityEngine.Vector2Int.
using WorldVector2Int = Game.World.Vector2Int;


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


    // =====================================================
    // STATE
    // =====================================================

    private bool initialized;


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
            Input.GetMouseButtonDown(
                1
            )
        )
        {

            PlaceBlock();

        }

    }


    // =====================================================
    // BREAK BLOCK
    // =====================================================

    private void BreakBlock()
    {

        WorldVector2Int blockPosition;


        // =================================================
        // œŒÀ”◊¿≈Ã ¡ÀŒ  »Ã≈ÕÕŒ œŒƒ Ã€ÿ Œ…
        // =================================================

        if (
            !TryGetMouseBlock(
                out blockPosition
            )
        )
        {

            Debug.Log(
                "BLOCK INTERACTION: NO TARGET BLOCK."
            );

            return;

        }


        // =================================================
        // œŒÀ”◊¿≈Ã ID ¡ÀŒ ¿
        // =================================================

        ushort blockID =
            world.GetBlock(
                blockPosition.x,
                blockPosition.y
            );


        if (
            blockID == 0
        )
        {

            Debug.Log(
                "BLOCK INTERACTION: TARGET IS AIR."
            );

            return;

        }


        Debug.Log(
            "BLOCK INTERACTION: BREAK " +
            blockPosition.x +
            ", " +
            blockPosition.y
        );


        // =================================================
        // ”ƒ¿Àﬂ≈Ã ¡ÀŒ  ◊≈–≈« WORLDMANAGER
        //
        // WorldManager.SetBlock:
        //
        // 1. ÏÂÌˇÂÚ ‰‡ÌÌ˚Â Chunk
        // 2. Ó·ÌÓ‚ÎˇÂÚ ChunkRenderer
        // 3. Ó·ÌÓ‚ÎˇÂÚ ChunkCollision
        // =================================================

        bool changed =
            worldManager.SetBlock(
                blockPosition.x,
                blockPosition.y,
                0
            );


        if (
            !changed
        )
        {

            Debug.LogWarning(
                "BLOCK INTERACTION: FAILED TO BREAK."
            );

            return;

        }


        // =================================================
        // Œ¡ÕŒ¬Àﬂ≈Ã COLLISION PLAYER
        //
        // ›ÚÓ ÌÛÊÌÓ, ˜ÚÓ·˚ ÂÒÎË Ë„ÓÍ ÒÚÓˇÎ Ì‡
        // ‡ÁÛ¯ÂÌÌÓÏ ·ÎÓÍÂ, Â„Ó ÒÓÒÚÓˇÌËÂ ÁÂÏÎË
        // Ó·ÌÓ‚ËÎÓÒ¸ Ò‡ÁÛ.
        // =================================================

        PlayerCollision playerCollision =
            GetComponent<PlayerCollision>();


        if (
            playerCollision != null
        )
        {

            playerCollision.ForceGroundCheck();

        }


        Debug.Log(
            "BLOCK INTERACTION: BLOCK BROKEN."
        );

    }


    // =====================================================
    // PLACE BLOCK
    // =====================================================

    private void PlaceBlock()
    {

        WorldVector2Int targetPosition;


        // =================================================
        // œŒÀ”◊¿≈Ã »Ã≈ÕÕŒ ¡ÀŒ  œŒƒ Ã€ÿ Œ…
        // =================================================

        if (
            !TryGetMouseBlock(
                out targetPosition
            )
        )
        {

            Debug.Log(
                "BLOCK INTERACTION: NO TARGET."
            );

            return;

        }


        // =================================================
        // œŒ«»÷»ﬂ Ã€ÿ»
        // =================================================

        Vector2 mouseWorld =
            GetMouseWorldPosition();


        // =================================================
        // »Ÿ≈Ã —Œ—≈ƒÕﬁﬁ  À≈“ ”
        // =================================================

        WorldVector2Int placePosition =
            GetAdjacentBlockPosition(
                targetPosition,
                mouseWorld
            );


        // =================================================
        // œ–Œ¬≈–ﬂ≈Ã, ◊“Œ “¿Ã ¬Œ«ƒ”’
        // =================================================

        if (
            world.GetBlock(
                placePosition.x,
                placePosition.y
            ) != 0
        )
        {

            Debug.Log(
                "BLOCK INTERACTION: PLACE POSITION BLOCKED."
            );

            return;

        }


        // =================================================
        // Õ≈ —“¿¬»Ã ¡ÀŒ  ¬Õ”“–» »√–Œ ¿
        // =================================================

        if (
            IsInsidePlayer(
                placePosition
            )
        )
        {

            Debug.Log(
                "BLOCK INTERACTION: PLAYER BLOCKS PLACEMENT."
            );

            return;

        }


        // =================================================
        // ”—“¿Õ¿¬À»¬¿≈Ã ¡ÀŒ 
        // =================================================

        bool changed =
            worldManager.SetBlock(
                placePosition.x,
                placePosition.y,
                placeBlockID
            );


        if (
            !changed
        )
        {

            Debug.LogWarning(
                "BLOCK INTERACTION: FAILED TO PLACE."
            );

            return;

        }


        Debug.Log(
            "BLOCK INTERACTION: BLOCK PLACED AT " +
            placePosition.x +
            ", " +
            placePosition.y
        );

    }


    // =====================================================
    // GET MOUSE BLOCK
    // =====================================================

    private bool TryGetMouseBlock(
        out WorldVector2Int blockPosition
    )
    {

        blockPosition =
            new WorldVector2Int(
                0,
                0
            );


        // =================================================
        // œŒÀ”◊¿≈Ã œŒ«»÷»ﬁ Ã€ÿ» ¬ Ã»–≈
        // =================================================

        Vector2 mouseWorld =
            GetMouseWorldPosition();


        // =================================================
        // œ–Œ¬≈–ﬂ≈Ã ƒ»—“¿Õ÷»ﬁ Œ“ »√–Œ ¿
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

            Debug.Log(
                "BLOCK INTERACTION: TOO FAR."
            );

            return false;

        }


        // =================================================
        // œ–≈Œ¡–¿«”≈Ã œŒ«»÷»ﬁ Ã€ÿ» ¬  ŒŒ–ƒ»Õ¿“€ ¡ÀŒ ¿
        //
        // ¬¿∆ÕŒ:
        //
        // Ã˚ Õ≈ ‰ÂÎ‡ÂÏ Raycast ÓÚ Ë„ÓÍ‡.
        //
        // Ã˚ Õ≈ Ë˘ÂÏ ÔÂ‚˚È ·ÎÓÍ Ì‡ ÔÛÚË.
        //
        // Ã˚ ·Â∏Ï –Œ¬ÕŒ “”  À≈“ ”,
        // Ì‡ ÍÓÚÓÛ˛ ÛÍ‡Á˚‚‡ÂÚ Ï˚¯¸.
        // =================================================

        int blockX =
            Mathf.FloorToInt(
                mouseWorld.x
            );


        int blockY =
            Mathf.FloorToInt(
                mouseWorld.y
            );


        blockPosition =
            new WorldVector2Int(
                blockX,
                blockY
            );


        // =================================================
        // œ–Œ¬≈–ﬂ≈Ã, ≈—“‹ À» ¬ ›“Œ…  À≈“ ≈ ¡ÀŒ 
        // =================================================

        ushort blockID =
            world.GetBlock(
                blockPosition.x,
                blockPosition.y
            );


        if (
            blockID == 0
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

        if (
            playerCamera == null
        )
        {

            return Vector2.zero;

        }


        Vector3 mouseScreenPosition =
            Input.mousePosition;


        // ƒÎˇ ÓÚÓ„‡ÙË˜ÂÒÍÓÈ 2D-Í‡ÏÂ˚
        // ËÒÔÓÎ¸ÁÛÂÏ ‡ÒÒÚÓˇÌËÂ ‰Ó ÔÎÓÒÍÓÒÚË Z = 0.

        float distanceFromCamera =
            Mathf.Abs(
                playerCamera.transform.position.z
            );


        mouseScreenPosition.z =
            distanceFromCamera;


        Vector3 worldPoint =
            playerCamera.ScreenToWorldPoint(
                mouseScreenPosition
            );


        return new Vector2(
            worldPoint.x,
            worldPoint.y
        );

    }


    // =====================================================
    // GET ADJACENT BLOCK
    // =====================================================

    private WorldVector2Int GetAdjacentBlockPosition(
        WorldVector2Int target,
        Vector2 mouseWorld
    )
    {

        // =================================================
        // ÷≈Õ“– ÷≈À≈¬Œ√Œ ¡ÀŒ ¿
        // =================================================

        Vector2 center =
            new Vector2(
                target.x +
                0.5f,

                target.y +
                0.5f
            );


        // =================================================
        // Õ¿œ–¿¬À≈Õ»≈ Œ“ ÷≈Õ“–¿ ¡ÀŒ ¿   Ã€ÿ»
        // =================================================

        Vector2 direction =
            mouseWorld -
            center;


        // =================================================
        // ≈—À» Ã€ÿ‹ ¡À»∆≈   À≈¬Œ…/œ–¿¬Œ… —“Œ–ŒÕ≈
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
                    new WorldVector2Int(
                        target.x + 1,
                        target.y
                    );

            }


            return
                new WorldVector2Int(
                    target.x - 1,
                    target.y
                );

        }


        // =================================================
        // ¬≈–’
        // =================================================

        if (
            direction.y >
            0f
        )
        {

            return
                new WorldVector2Int(
                    target.x,
                    target.y + 1
                );

        }


        // =================================================
        // Õ»«
        // =================================================

        return
            new WorldVector2Int(
                target.x,
                target.y - 1
            );

    }


    // =====================================================
    // PLAYER COLLISION
    // =====================================================

    private bool IsInsidePlayer(
        WorldVector2Int blockPosition
    )
    {

        PlayerCollision collision =
            GetComponent<PlayerCollision>();


        if (
            collision == null
        )
        {

            return false;

        }


        Vector2 size =
            collision.GetColliderSize();


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
        // AABB OVERLAP
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

