using UnityEngine;
using Game.World.Collision;


public class PlayerAnimationController :
    MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private PlayerController playerController;


    [SerializeField]
    private Animator animator;


    [Tooltip(
        "Объект, который содержит весь визуальный скелет игрока. " +
        "Именно он переворачивается влево/вправо."
    )]
    [SerializeField]
    private Transform facingRoot;


    [Header("Settings")]

    [SerializeField]
    private float horizontalDeadZone =
        0.01f;


    [SerializeField]
    private float verticalDeadZone =
        0.05f;


    private PlayerCollision playerCollision;


    private bool initialized;


    private bool facingRight =
        true;


    private Vector3 originalFacingScale;


    // Weapon-facing override.
    // Sword can lock the current facing for the whole swing.
    // Bow can force facing toward the cursor while drawing.
    private bool externalFacingOverride;


    private bool externalFacingRight =
        true;


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


    private void Awake()
    {
        if (playerController == null)
        {
            playerController =
                GetComponent<PlayerController>();
        }


        if (animator == null)
        {
            animator =
                GetComponentInChildren<Animator>(
                    true
                );
        }
    }


    private void Update()
    {
        if (!initialized)
        {
            TryInitialize();


            if (!initialized)
            {
                return;
            }
        }


        UpdateAnimator();

        UpdateFacing();
    }


    private void TryInitialize()
    {
        if (playerController == null)
        {
            Debug.LogError(
                "PLAYER ANIMATION: PlayerController not found."
            );


            return;
        }


        if (animator == null)
        {
            Debug.LogError(
                "PLAYER ANIMATION: Animator not found."
            );


            return;
        }


        if (facingRoot == null)
        {
            Debug.LogError(
                "PLAYER ANIMATION: FacingRoot not assigned."
            );


            return;
        }


        playerCollision =
            playerController.GetPlayerCollision();


        if (playerCollision == null)
        {
            return;
        }


        originalFacingScale =
            facingRoot.localScale;


        facingRight =
            originalFacingScale.x >=
            0f;


        externalFacingRight =
            facingRight;


        initialized =
            true;
    }


    private void UpdateAnimator()
    {
        float horizontalInput =
            playerController.GetHorizontalInput();


        float speed =
            Mathf.Abs(
                horizontalInput
            );


        if (speed < horizontalDeadZone)
        {
            speed = 0f;
        }


        float verticalVelocity =
            playerController.GetVerticalVelocity();


        if (
            Mathf.Abs(
                verticalVelocity
            )
            <
            verticalDeadZone
        )
        {
            verticalVelocity = 0f;
        }


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
            playerCollision.IsGrounded
        );
    }


    private void UpdateFacing()
    {
        if (externalFacingOverride)
        {
            SetFacing(
                externalFacingRight
            );


            return;
        }


        float horizontalInput =
            playerController.GetHorizontalInput();


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


    private void SetFacing(
        bool right
    )
    {
        if (
            facingRight == right
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
    // EXTERNAL FACING CONTROL
    // =====================================================

    public void SetExternalFacingOverride(
        bool enabled,
        bool right
    )
    {
        externalFacingOverride =
            enabled;


        externalFacingRight =
            right;


        if (
            enabled &&
            initialized
        )
        {
            // Apply immediately in the same frame.
            SetFacing(
                right
            );
        }
    }


    public void ClearExternalFacingOverride()
    {
        externalFacingOverride =
            false;
    }


    public bool IsFacingRight()
    {
        return facingRight;
    }


    public Animator GetAnimator()
    {
        return animator;
    }
}
