using Game.World;
using Game.World.Collision;

using UnityEngine;


public class PlayerController :
    MonoBehaviour
{

    [Header("Movement")]

    [SerializeField]
    private float moveSpeed =
        5f;


    [SerializeField]
    private float jumpForce =
        8f;


    [Header("Gravity")]

    [SerializeField]
    private float gravity =
        25f;


    [SerializeField]
    private float maxFallSpeed =
        20f;


    [Header("Collision")]

    [SerializeField]
    private PlayerCollision playerCollision;


    private float verticalVelocity;


    private WorldManager worldManager;


    // =====================================================
    // AWAKE
    // =====================================================

    private void Awake()
    {

        // Сначала пытаемся найти
        // PlayerCollision на этом же объекте.

        if (
            playerCollision == null
        )
        {

            playerCollision =
                GetComponent<
                    PlayerCollision
                >();

        }


        // Если не нашли —
        // ищем среди дочерних объектов.

        if (
            playerCollision == null
        )
        {

            playerCollision =
                GetComponentInChildren<
                    PlayerCollision
                >();

        }


        // Последняя проверка.

        if (
            playerCollision == null
        )
        {

            Debug.LogError(
                "PLAYER: PlayerCollision component was not found. " +
                "Add PlayerCollision to Player or one of its children."
            );

        }

    }


    // =====================================================
    // START
    // =====================================================

    private void Start()
    {

        worldManager =
            WorldManager.Instance;


        if (
            worldManager == null
        )
        {

            Debug.LogError(
                "PLAYER: WorldManager is null."
            );


            return;

        }


        if (
            playerCollision == null
        )
        {

            Debug.LogError(
                "PLAYER: PlayerCollision is null."
            );


            return;

        }


        WorldCollision worldCollision =
            worldManager.GetWorldCollision();


        if (
            worldCollision == null
        )
        {

            Debug.LogError(
                "PLAYER: WorldCollision is null."
            );


            return;

        }


        playerCollision.Initialize(
            worldCollision
        );


        Debug.Log(
            "PLAYER: PlayerCollision initialized successfully."
        );

    }


    // =====================================================
    // UPDATE
    // =====================================================

    private void Update()
    {

        if (
            playerCollision == null
        )
        {
            return;
        }


        // =================================================
        // HORIZONTAL
        // =================================================

        float horizontal =
            Input.GetAxisRaw(
                "Horizontal"
            );


        float horizontalMovement =
            horizontal *
            moveSpeed *
            Time.deltaTime;


        // =================================================
        // JUMP
        // =================================================

        if (
            Input.GetKeyDown(
                KeyCode.Space
            )
        )
        {

            if (
                playerCollision.IsGrounded
            )
            {

                verticalVelocity =
                    jumpForce;

            }

        }


        // =================================================
        // GRAVITY
        // =================================================

        verticalVelocity -=
            gravity *
            Time.deltaTime;


        if (
            verticalVelocity <
            -maxFallSpeed
        )
        {

            verticalVelocity =
                -maxFallSpeed;

        }


        // =================================================
        // MOVEMENT
        // =================================================

        Vector2 movement =
            new Vector2(
                horizontalMovement,
                verticalVelocity *
                Time.deltaTime
            );


        playerCollision.Move(
            movement
        );


        // =================================================
        // RESET FALL SPEED
        // =================================================

        if (
            playerCollision.IsGrounded &&
            verticalVelocity < 0f
        )
        {

            verticalVelocity =
                0f;

        }

    }

}

