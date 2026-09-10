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
        8f;


    [SerializeField]
    private float gravity =
        25f;


    [SerializeField]
    private float maxFallSpeed =
        20f;


    // =====================================================
    // REFERENCES
    // =====================================================

    private PlayerCollision playerCollision;

    private WorldCollision worldCollision;


    // =====================================================
    // STATE
    // =====================================================

    private float verticalVelocity;

    private float horizontalInput;

    private bool initialized;


    // =====================================================
    // START
    // =====================================================

    private void Start()
    {

        // =================================================
        // PLAYER COLLISION
        // =================================================

        playerCollision =
            GetComponent<PlayerCollision>();


        if (
            playerCollision == null
        )
        {

            Debug.LogError(
                "PLAYER CONTROLLER: PlayerCollision not found."
            );

            return;

        }


        // =================================================
        // WORLD MANAGER
        // =================================================

        WorldManager worldManager =
            WorldManager.Instance;


        if (
            worldManager == null
        )
        {

            Debug.LogError(
                "PLAYER CONTROLLER: WorldManager is null."
            );

            return;

        }


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
                "PLAYER CONTROLLER: WorldCollision is null."
            );

            return;

        }


        // =================================================
        // INITIALIZE COLLISION
        // =================================================

        playerCollision.Initialize(
            worldCollision
        );


        // =================================================
        // INITIAL GROUND CHECK
        // =================================================

        playerCollision.ForceGroundCheck();


        initialized =
            true;


        Debug.Log(
            "PLAYER CONTROLLER: INITIALIZED."
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
        // HORIZONTAL INPUT
        // =================================================

        horizontalInput =
            Input.GetAxisRaw(
                "Horizontal"
            );


        // =================================================
        // JUMP
        // =================================================

        if (
            Input.GetKeyDown(
                KeyCode.Space
            )
        )
        {

            Jump();

        }

    }


    // =====================================================
    // FIXED UPDATE
    // =====================================================

    private void FixedUpdate()
    {

        if (
            !initialized
        )
        {
            return;
        }


        // =================================================
        // HORIZONTAL MOVEMENT
        // =================================================

        float horizontalMovement =
            horizontalInput *
            moveSpeed *
            Time.fixedDeltaTime;


        // =================================================
        // GRAVITY
        // =================================================

        ApplyGravity();


        // =================================================
        // TOTAL MOVEMENT
        // =================================================

        Vector2 movement =
            new Vector2(
                horizontalMovement,
                verticalVelocity *
                Time.fixedDeltaTime
            );


        // =================================================
        // MOVE
        // =================================================

        playerCollision.Move(
            movement
        );


        // =================================================
        // STOP VERTICAL VELOCITY
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


    // =====================================================
    // GRAVITY
    // =====================================================

    private void ApplyGravity()
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


            return;

        }


        verticalVelocity -=
            gravity *
            Time.fixedDeltaTime;


        if (
            verticalVelocity <
            -maxFallSpeed
        )
        {

            verticalVelocity =
                -maxFallSpeed;

        }

    }


    // =====================================================
    // JUMP
    // =====================================================

    private void Jump()
    {

        if (
            !playerCollision.IsGrounded
        )
        {
            return;
        }


        verticalVelocity =
            jumpForce;

    }


    // =====================================================
    // WORLD BLOCK CHANGED
    // =====================================================

    public void OnWorldBlockChanged()
    {

        if (
            !initialized ||
            playerCollision == null
        )
        {
            return;
        }


        // =================================================
        // ÎÁÍÎÂËßÅÌ ÑÎÑÒÎßÍÈÅ ÇÅÌËÈ
        // =================================================

        playerCollision.RefreshAfterWorldChange();


        // =================================================
        // ÅÑËÈ ÇÅÌËÈ ÍÅÒ
        // =================================================

        if (
            !playerCollision.IsGrounded
        )
        {

            // Íå îñòàâëÿåì èãðîêà
            // ñ îòðèöàòåëüíîé ñêîðîñòüþ,
            // êîòîðàÿ ìîãëà áûòü ñáðîøåíà
            // äî èçìåíåíèÿ áëîêà.

            if (
                verticalVelocity >=
                0f
            )
            {

                verticalVelocity =
                    -0.01f;

            }

        }

    }


    // =====================================================
    // GET PLAYER COLLISION
    // =====================================================

    public PlayerCollision GetPlayerCollision()
    {

        return
            playerCollision;

    }


    // =====================================================
    // GET VERTICAL VELOCITY
    // =====================================================

    public float GetVerticalVelocity()
    {

        return
            verticalVelocity;

    }
    // =====================================================
    // GET HORIZONTAL INPUT
    // =====================================================

    public float GetHorizontalInput()
    {
        return horizontalInput;
    }

}