using UnityEngine;

using Game.World;
using Game.World.Collision;


public class PlayerController :
    MonoBehaviour
{

    // =====================================================
    // MOVEMENT
    // =====================================================

    [Header("Movement")]

    [SerializeField]
    private float moveSpeed =
        5f;


    [SerializeField]
    private float jumpForce =
        12f;


    [SerializeField]
    private float gravity =
        45f;


    [SerializeField]
    private float maxFallSpeed =
        25f;


    // =====================================================
    // COLLISION
    // =====================================================

    [Header("Collision")]

    [SerializeField]
    private PlayerCollision playerCollision;


    // =====================================================
    // WORLD
    // =====================================================

    private WorldCollision worldCollision;


    // =====================================================
    // MOVEMENT STATE
    // =====================================================

    private float verticalVelocity;


    // =====================================================
    // AWAKE
    // =====================================================

    private void Awake()
    {

        if (
            playerCollision == null
        )
        {

            playerCollision =
                GetComponent<PlayerCollision>();

        }

    }


    // =====================================================
    // START
    // =====================================================

    private void Start()
    {

        WorldManager worldManager =
            WorldManager.Instance;


        if (
            worldManager == null
        )
        {

            Debug.LogError(
                "PLAYER: WorldManager is null."
            );

            enabled =
                false;

            return;

        }


        worldCollision =
            worldManager.GetWorldCollision();


        if (
            worldCollision == null
        )
        {

            Debug.LogError(
                "PLAYER: WorldCollision is null."
            );

            enabled =
                false;

            return;

        }


        if (
            playerCollision == null
        )
        {

            Debug.LogError(
                "PLAYER: PlayerCollision is null."
            );

            enabled =
                false;

            return;

        }


        playerCollision.Initialize(
            worldCollision
        );


        verticalVelocity =
            0f;


        // =================================================
        // FIX OVERLAPS
        // =================================================

        playerCollision.ResolveOverlaps();


        Debug.Log(
            "PLAYER: INITIALIZED."
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


        HandleJump();

    }


    // =====================================================
    // FIXED UPDATE
    // =====================================================

    private void FixedUpdate()
    {

        if (
            playerCollision == null
        )
        {
            return;
        }


        HandleHorizontalMovement();


        HandleGravity();

    }


    // =====================================================
    // HORIZONTAL MOVEMENT
    // =====================================================

    private void HandleHorizontalMovement()
    {

        float horizontal =
            Input.GetAxisRaw(
                "Horizontal"
            );


        if (
            Mathf.Abs(
                horizontal
            ) < 0.001f
        )
        {
            return;
        }


        float movement =
            horizontal *
            moveSpeed *
            Time.fixedDeltaTime;


        playerCollision.Move(
            new Vector2(
                movement,
                0f
            )
        );

    }


    // =====================================================
    // GRAVITY
    // =====================================================

    private void HandleGravity()
    {

        if (
            playerCollision.IsGrounded
        )
        {

            if (
                verticalVelocity < 0f
            )
            {

                verticalVelocity =
                    0f;

            }

        }
        else
        {

            verticalVelocity -=
                gravity *
                Time.fixedDeltaTime;


            verticalVelocity =
                Mathf.Max(
                    verticalVelocity,
                    -maxFallSpeed
                );

        }


        float movement =
            verticalVelocity *
            Time.fixedDeltaTime;


        if (
            Mathf.Abs(
                movement
            ) < 0.0001f
        )
        {
            return;
        }


        bool wasGrounded =
            playerCollision.IsGrounded;


        playerCollision.Move(
            new Vector2(
                0f,
                movement
            )
        );


        if (
            !wasGrounded &&
            playerCollision.IsGrounded &&
            verticalVelocity < 0f
        )
        {

            verticalVelocity =
                0f;

        }

    }


    // =====================================================
    // JUMP
    // =====================================================

    private void HandleJump()
    {

        if (
            !Input.GetKeyDown(
                KeyCode.Space
            )
        )
        {
            return;
        }


        if (
            !playerCollision.IsGrounded
        )
        {
            return;
        }


        verticalVelocity =
            jumpForce;

    }

}