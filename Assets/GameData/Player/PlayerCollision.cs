
using Game.World.Collision;
using Game.GameplaySystems.Interactions;

using UnityEngine;


public class PlayerCollision :
    MonoBehaviour
{

    // =====================================================
    // COLLISION SETTINGS
    // =====================================================

    [Header("Collision")]

    [SerializeField]
    private Vector2 colliderSize =
        new Vector2(
            0.8f,
            1.8f
        );


    [SerializeField]
    private float skin =
        0.02f;


    [Tooltip(
        "Maximum height that horizontal movement may automatically step up. " +
        "0.55 allows 1/2-block slabs/stair steps but not a full block."
    )]
    [SerializeField]
    private float maxAutoStepHeight =
        0.55f;


    [Header("World")]

    [SerializeField]
    private float blockSize =
        1f;

    private int ignoredPlatformY = int.MinValue;

    // =====================================================
    // WORLD
    // =====================================================

    private WorldCollision worldCollision;


    // =====================================================
    // STATE
    // =====================================================

    public bool IsGrounded
    {
        get;
        private set;
    }


    // =====================================================
    // INITIALIZE
    // =====================================================

    public void Initialize(
        WorldCollision worldCollision
    )
    {

        this.worldCollision =
            worldCollision;


        IsGrounded =
            false;

    }


    // =====================================================
    // MOVE
    // =====================================================

    public void Move(
        Vector2 movement
    )
    {
        float playerBottom = transform.position.y - colliderSize.y * 0.5f;

        if (ignoredPlatformY != int.MinValue &&
            playerBottom < ignoredPlatformY - 0.001f)
        {
            ignoredPlatformY = int.MinValue;
        }

        if (
            worldCollision ==
            null
        )
        {

            return;

        }


        bool groundedBeforeMove =
            CheckGroundAt(
                transform.position.x,
                transform.position.y
            );


        IsGrounded =
            false;


        // =================================================
        // HORIZONTAL
        // =================================================

        if (
            Mathf.Abs(
                movement.x
            )
            >
            0.0001f
        )
        {

            MoveHorizontal(
                movement.x,
                groundedBeforeMove
            );

        }


        // =================================================
        // VERTICAL
        // =================================================

        if (
            Mathf.Abs(
                movement.y
            )
            >
            0.0001f
        )
        {

            MoveVertical(
                movement.y
            );

        }


        // =================================================
        // FINAL GROUND CHECK
        // =================================================

        CheckGround();

    }


    // =====================================================
    // HORIZONTAL MOVEMENT
    // =====================================================

    private void MoveHorizontal(
        float movement,
        bool allowAutoStep
    )
    {

        float direction =
            Mathf.Sign(
                movement
            );


        float distance =
            Mathf.Abs(
                movement
            );


        const float step =
            0.02f;


        float moved =
            0f;


        bool canStep =
            allowAutoStep;


        while (
            moved <
            distance
        )
        {

            float currentStep =
                Mathf.Min(
                    step,
                    distance -
                    moved
                );


            float testX =
                transform.position.x +
                direction *
                currentStep;


            if (
                CheckCollisionAt(
                    testX,
                    transform.position.y
                )
            )
            {

                if (
                    canStep
                    &&
                    TryAutoStepUp(
                        testX,
                        out float steppedY
                    )
                )
                {

                    transform.position =
                        new Vector3(
                            testX,
                            steppedY,
                            transform.position.z
                        );


                    moved +=
                        currentStep;


                    canStep =
                        CheckGroundAt(
                            transform.position.x,
                            transform.position.y
                        );


                    continue;

                }


                break;

            }


            transform.position =
                new Vector3(
                    testX,
                    transform.position.y,
                    transform.position.z
                );


            moved +=
                currentStep;


            canStep =
                CheckGroundAt(
                    transform.position.x,
                    transform.position.y
                );

        }

    }


    private bool TryAutoStepUp(
        float testX,
        out float steppedY
    )
    {

        steppedY =
            transform.position.y;


        if (
            maxAutoStepHeight <=
            0f
        )
        {

            return false;

        }


        const float step =
            0.02f;


        float raised =
            step;


        while (
            raised <=
            maxAutoStepHeight +
            0.0001f
        )
        {

            float testY =
                transform.position.y +
                raised;


            if (
                !CheckCollisionAt(
                    testX,
                    testY
                )
            )
            {

                // Do not "climb" into empty air.
                // The new position must have a surface directly below it.
                if (
                    CheckGroundAt(
                        testX,
                        testY
                    )
                )
                {

                    steppedY =
                        testY;


                    return true;

                }

            }


            raised +=
                step;

        }


        return false;

    }


    // =====================================================
    // VERTICAL MOVEMENT
    // =====================================================

    private void MoveVertical(
        float movement
    )
    {

        float direction =
            Mathf.Sign(
                movement
            );


        float distance =
            Mathf.Abs(
                movement
            );


        const float step =
            0.02f;


        float moved =
            0f;


        while (
            moved <
            distance
        )
        {

            float currentStep =
                Mathf.Min(
                    step,
                    distance -
                    moved
                );


            float testY =
                transform.position.y +
                direction *
                currentStep;

            float halfHeight = colliderSize.y * 0.5f;

            if (direction < 0f)
            {
                float previousBottom =
                    transform.position.y - halfHeight;

                float nextBottom =
                    testY - halfHeight;

                Rect horizontalBounds = new Rect(
                    transform.position.x - colliderSize.x * 0.5f + skin,
                    0f,
                    Mathf.Max(0.001f, colliderSize.x - skin * 2f),
                    0.001f
                );

                if (worldCollision.TryGetPlatformTop(
                    horizontalBounds,
                    previousBottom,
                    nextBottom,
                    ignoredPlatformY,
                    out float platformTop))
                {
                    transform.position = new Vector3(
                        transform.position.x,
                        platformTop + halfHeight,
                        transform.position.z
                    );

                    IsGrounded = true;
                    return;
                }
            }

            if (
                CheckCollisionAt(
                    transform.position.x,
                    testY
                )
            )
            {

                if (
                    direction <
                    0f
                )
                {

                    IsGrounded =
                        true;

                }


                break;

            }


            transform.position =
                new Vector3(
                    transform.position.x,
                    testY,
                    transform.position.z
                );


            moved +=
                currentStep;

        }

    }
    private bool IsPlatformUnderFeet(
    float bottom,
    float minX,
    float maxX
)
    {
        if (worldCollision == null || maxX <= minX)
            return false;

        Rect horizontalBounds = new Rect(
            minX,
            0f,
            maxX - minX,
            0.001f
        );

        const float tolerance = 0.025f;

        if (!worldCollision.TryGetPlatformTop(
            horizontalBounds,
            bottom + tolerance,
            bottom - tolerance,
            ignoredPlatformY,
            out float platformTop
        ))
        {
            return false;
        }

        return Mathf.Abs(platformTop - bottom) <= tolerance;
    }

    // =====================================================
    // GROUND CHECK
    // =====================================================

    private void CheckGround()
    {
        if (worldCollision == null)
        {
            IsGrounded = false;
            return;
        }

        float halfWidth = colliderSize.x * 0.5f;
        float bottom = transform.position.y - colliderSize.y * 0.5f;
        float minX = transform.position.x - halfWidth + skin;
        float maxX = transform.position.x + halfWidth - skin;

        IsGrounded =
    IntersectsAny(
        minX,
        bottom - skin - 0.03f,
        maxX,
        bottom + 0.01f
    )
    ||
    IsPlatformUnderFeet(bottom, minX, maxX);
    }


    private bool CheckGroundAt(
        float centerX,
        float centerY
    )
    {

        if (
            worldCollision ==
            null
        )
        {

            return false;

        }


        float halfWidth =
            colliderSize.x *
            0.5f;


        float halfHeight =
            colliderSize.y *
            0.5f;


        float minX =
            centerX -
            halfWidth +
            skin;


        float maxX =
            centerX +
            halfWidth -
            skin;


        float bottom =
            centerY -
            halfHeight;


        // A thin AABB directly below the player's feet.
        // It detects the real top of slabs and stair rectangles.
        Rect groundProbe =
            new Rect(
                minX,
                bottom -
                skin *
                2f,

                Mathf.Max(
                    0.001f,
                    maxX -
                    minX
                ),

                skin *
                2f +
                0.002f
            );


        return
    worldCollision.OverlapsAny(groundProbe)
    ||
    InteractiveBlockRuntime.IntersectsClosedFurniture(groundProbe)
    ||
    IsPlatformUnderFeet(bottom, minX, maxX);

    }


    // =====================================================
    // COLLISION CHECK
    // =====================================================

    // [BT-AUTO-PLAYER-COLLISION]
    private bool CheckCollisionAt(
        float centerX,
        float centerY)
    {
        if (worldCollision == null)
            return false;

        float halfWidth = colliderSize.x * 0.5f;
        float halfHeight = colliderSize.y * 0.5f;

        float minX = centerX - halfWidth + skin;
        float maxX = centerX + halfWidth - skin;
        float minY = centerY - halfHeight + skin;
        float maxY = centerY + halfHeight - skin;

        return
            IntersectsAny(
                minX,
                minY,
                maxX,
                maxY
            );
    }


    private bool IntersectsAny(
        float minX,
        float minY,
        float maxX,
        float maxY
    )
    {
        if (
            Game.BlockTransforms.BlockShapeCollision.Intersects(
                worldCollision,
                minX,
                minY,
                maxX,
                maxY
            )
        )
        {
            return true;
        }

        return
            InteractiveBlockRuntime.IntersectsClosedFurniture(
                new Rect(
                    minX,
                    minY,
                    Mathf.Max(0f, maxX - minX),
                    Mathf.Max(0f, maxY - minY)
                )
            );
    }


    // =====================================================
    // REFRESH AFTER WORLD CHANGE
    // =====================================================

    public void RefreshAfterWorldChange()
    {

        if (
            worldCollision ==
            null
        )
        {

            return;

        }


        CheckGround();

    }


    // =====================================================
    // RESOLVE OVERLAPS
    // =====================================================

    public void ResolveOverlaps()
    {

        if (
            worldCollision ==
            null
        )
        {

            return;

        }


        const int maxIterations =
            200;


        const float resolveStep =
            0.01f;


        for (
            int i = 0;
            i < maxIterations;
            i++
        )
        {

            if (
                !CheckCollisionAt(
                    transform.position.x,
                    transform.position.y
                )
            )
            {

                break;

            }


            transform.position +=
                Vector3.up *
                resolveStep;

        }


        CheckGround();

    }


    // =====================================================
    // FORCE GROUND CHECK
    // =====================================================

    public void ForceGroundCheck()
    {

        CheckGround();

    }


    // =====================================================
    // GET COLLIDER SIZE
    // =====================================================

    public Vector2 GetColliderSize()
    {

        return
            colliderSize;

    }


    // =====================================================
    // GET WORLD COLLISION
    // =====================================================

    public WorldCollision GetWorldCollision()
    {

        return
            worldCollision;

    }

    public void DropThroughPlatforms()
    {
        if (worldCollision == null)
            return;

        ignoredPlatformY = int.MinValue;

        float bottom =
            transform.position.y - colliderSize.y * 0.5f;

        float halfWidth = colliderSize.x * 0.5f;

        Rect horizontalBounds = new Rect(
            transform.position.x - halfWidth + skin,
            0f,
            Mathf.Max(0.001f, colliderSize.x - skin * 2f),
            0.001f
        );

        const float tolerance = 0.06f;

        if (worldCollision.TryGetPlatformTop(
            horizontalBounds,
            bottom + tolerance,
            bottom - tolerance,
            int.MinValue,
            out float platformTop
        ))
        {
            if (Mathf.Abs(platformTop - bottom) <= tolerance)
            {
                ignoredPlatformY =
                    Mathf.FloorToInt(platformTop - 0.0001f);
            }
        }

        IsGrounded = false;
    }

    // =====================================================
    // DEBUG
    // =====================================================

    private void OnDrawGizmosSelected()
    {

        Gizmos.DrawWireCube(
            transform.position,
            colliderSize
        );

    }

}
