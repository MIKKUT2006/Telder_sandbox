using System.Collections;

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
    // MOVEMENT
    // =====================================================

    private float verticalVelocity;


    // =====================================================
    // READY
    // =====================================================

    private bool ready;


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

    private IEnumerator Start()
    {

        // =================================================
        // ∆ƒ®Ã WORLD MANAGER
        // =================================================

        while (
            WorldManager.Instance == null
        )
        {
            yield return null;
        }


        // =================================================
        // ∆ƒ®Ã √Œ“Œ¬ÕŒ—“‹ Ã»–¿
        // =================================================

        while (
            !WorldManager.Instance.IsReady
        )
        {
            yield return null;
        }


        WorldManager worldManager =
            WorldManager.Instance;


        // =================================================
        // WORLD COLLISION
        // =================================================

        worldCollision =
            worldManager.GetWorldCollision();


        if (
            worldCollision == null
        )
        {

            Debug.LogError(
                "PLAYER: WorldCollision is null."
            );

            yield break;

        }


        // =================================================
        // PLAYER COLLISION
        // =================================================

        if (
            playerCollision == null
        )
        {

            Debug.LogError(
                "PLAYER: PlayerCollision is null."
            );

            yield break;

        }


        playerCollision.Initialize(
            worldCollision
        );


        // =================================================
        // RESET
        // =================================================

        verticalVelocity =
            0f;


        // =================================================
        // READY
        // =================================================

        ready =
            true;


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
            !ready
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
            !ready
        )
        {
            return;
        }


        HandleHorizontalMovement();


        HandleGravity();

    }


    // =====================================================
    // HORIZONTAL
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
            ) <
            0.001f
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
                verticalVelocity <
                0f
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
            ) <
            0.0001f
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
            verticalVelocity <
            0f
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