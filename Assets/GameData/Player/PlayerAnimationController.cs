using UnityEngine;
using Game.World.Collision;


public class PlayerAnimationController :
    MonoBehaviour
{
    // =====================================================
    // REFERENCES
    // =====================================================

    [Header("References")]

    [SerializeField]
    private PlayerController playerController;


    [SerializeField]
    private Animator animator;


    [Tooltip(
        "Объект, который содержит весь визуальный скелет игрока. " +
        "Именно он будет переворачиваться влево/вправо."
    )]
    [SerializeField]
    private Transform facingRoot;


    // =====================================================
    // SETTINGS
    // =====================================================

    [Header("Settings")]

    [SerializeField]
    private float horizontalDeadZone =
        0.01f;


    [SerializeField]
    private float verticalDeadZone =
        0.05f;


    // =====================================================
    // STATE
    // =====================================================

    private PlayerCollision playerCollision;


    private bool initialized;


    private bool facingRight =
        true;


    private Vector3 originalFacingScale;


    // =====================================================
    // ANIMATOR HASHES
    // =====================================================

    private static readonly int SpeedHash =
        Animator.StringToHash(
            "Speed"
        );


    private static readonly int VerticalSpeedHash =
        Animator.StringToHash(
            "VerticalSpeed"
        );


    private static readonly int GroundedHash =
        Animator.StringToHash(
            "Grounded"
        );


    // =====================================================
    // AWAKE
    // =====================================================

    private void Awake()
    {
        // =================================================
        // PLAYER CONTROLLER
        // =================================================

        if (
            playerController ==
            null
        )
        {
            playerController =
                GetComponent<PlayerController>();
        }


        // =================================================
        // ANIMATOR
        // =================================================

        if (
            animator ==
            null
        )
        {
            animator =
                GetComponentInChildren<Animator>(
                    true
                );
        }
    }


    // =====================================================
    // UPDATE
    // =====================================================

    private void Update()
    {
        // PlayerController и PlayerCollision
        // инициализируются в Start().
        //
        // Поэтому здесь используем отложенную
        // инициализацию, чтобы не зависеть от
        // Script Execution Order.

        if (
            !initialized
        )
        {
            TryInitialize();


            if (
                !initialized
            )
            {
                return;
            }
        }


        UpdateAnimator();

        UpdateFacing();
    }


    // =====================================================
    // INITIALIZE
    // =====================================================

    private void TryInitialize()
    {
        if (
            playerController ==
            null
        )
        {
            Debug.LogError(
                "PLAYER ANIMATION: PlayerController not found."
            );

            return;
        }


        if (
            animator ==
            null
        )
        {
            Debug.LogError(
                "PLAYER ANIMATION: Animator not found."
            );

            return;
        }


        if (
            facingRoot ==
            null
        )
        {
            Debug.LogError(
                "PLAYER ANIMATION: FacingRoot not assigned."
            );

            return;
        }


        // =================================================
        // COLLISION
        // =================================================

        playerCollision =
            playerController
                .GetPlayerCollision();


        // PlayerController.Start() мог ещё не выполниться.
        if (
            playerCollision ==
            null
        )
        {
            return;
        }


        // =================================================
        // ORIGINAL SCALE
        // =================================================

        originalFacingScale =
            facingRoot.localScale;


        // Определяем первоначальное направление
        // из текущего scale.

        facingRight =
            originalFacingScale.x >= 0f;


        initialized =
            true;


        Debug.Log(
            "PLAYER ANIMATION: INITIALIZED."
        );
    }


    // =====================================================
    // UPDATE ANIMATOR
    // =====================================================

    private void UpdateAnimator()
    {
        // =================================================
        // HORIZONTAL
        // =================================================

        float horizontalInput =
            playerController
                .GetHorizontalInput();


        float speed =
            Mathf.Abs(
                horizontalInput
            );


        if (
            speed <
            horizontalDeadZone
        )
        {
            speed =
                0f;
        }


        // =================================================
        // VERTICAL
        // =================================================

        float verticalVelocity =
            playerController
                .GetVerticalVelocity();


        if (
            Mathf.Abs(
                verticalVelocity
            )
            <
            verticalDeadZone
        )
        {
            verticalVelocity =
                0f;
        }


        // =================================================
        // GROUNDED
        // =================================================

        bool grounded =
            playerCollision.IsGrounded;


        // =================================================
        // ANIMATOR PARAMETERS
        // =================================================

        animator.SetFloat(
            SpeedHash,
            speed
        );


        animator.SetFloat(
            VerticalSpeedHash,
            verticalVelocity
        );


        animator.SetBool(
            GroundedHash,
            grounded
        );
    }


    // =====================================================
    // UPDATE FACING
    // =====================================================

    private void UpdateFacing()
    {
        float horizontalInput =
            playerController
                .GetHorizontalInput();


        // =================================================
        // RIGHT
        // =================================================

        if (
            horizontalInput >
            horizontalDeadZone
        )
        {
            SetFacing(
                true
            );

            return;
        }


        // =================================================
        // LEFT
        // =================================================

        if (
            horizontalInput <
            -horizontalDeadZone
        )
        {
            SetFacing(
                false
            );
        }
    }


    // =====================================================
    // SET FACING
    // =====================================================

    private void SetFacing(
        bool right
    )
    {
        if (
            facingRight ==
            right
        )
        {
            return;
        }


        facingRight =
            right;


        Vector3 scale =
            originalFacingScale;


        scale.x =
            Mathf.Abs(
                originalFacingScale.x
            )
            *
            (
                right
                    ? 1f
                    : -1f
            );


        facingRoot.localScale =
            scale;
    }


    // =====================================================
    // PUBLIC
    // =====================================================

    public bool IsFacingRight()
    {
        return facingRight;
    }


    public Animator GetAnimator()
    {
        return animator;
    }
}