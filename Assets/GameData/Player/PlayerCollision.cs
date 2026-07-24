
using Game.World.Collision;
using UnityEngine;


public class PlayerCollision :
    MonoBehaviour
{

    // =====================================================
    // COLLISION
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


    [Header("World")]

    [SerializeField]
    private float blockSize =
        1f;


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

        if (
            worldCollision == null
        )
        {

            return;

        }


        IsGrounded =
            false;


        // =================================================
        // HORIZONTAL
        // =================================================

        if (
            movement.x != 0f
        )
        {

            MoveHorizontal(
                movement.x
            );

        }


        // =================================================
        // VERTICAL
        // =================================================

        if (
            movement.y != 0f
        )
        {

            MoveVertical(
                movement.y
            );

        }


        // =================================================
        // GROUND CHECK
        // =================================================

        CheckGround();

    }


    // =====================================================
    // HORIZONTAL MOVEMENT
    // =====================================================

    private void MoveHorizontal(
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


        float step =
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
                    distance - moved
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

        }

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


        float step =
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
                    distance - moved
                );


            float testY =
                transform.position.y +
                direction *
                currentStep;


            if (
                CheckCollisionAt(
                    transform.position.x,
                    testY
                )
            )
            {

                if (
                    direction < 0f
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


    // =====================================================
    // GROUND CHECK
    // =====================================================

    private void CheckGround()
    {

        if (
            worldCollision == null
        )
        {

            IsGrounded =
                false;

            return;

        }


        float halfWidth =
            colliderSize.x *
            0.5f;


        float bottom =
            transform.position.y -
            colliderSize.y *
            0.5f;


        float checkY =
            bottom -
            skin;


        float minX =
            transform.position.x -
            halfWidth +
            skin;


        float maxX =
            transform.position.x +
            halfWidth -
            skin;


        int minBlockX =
            Mathf.FloorToInt(
                minX /
                blockSize
            );


        int maxBlockX =
            Mathf.FloorToInt(
                maxX /
                blockSize
            );


        int blockY =
            Mathf.FloorToInt(
                checkY /
                blockSize
            );


        IsGrounded =
            false;


        for (
            int x = minBlockX;
            x <= maxBlockX;
            x++
        )
        {

            if (
                worldCollision.IsSolid(
                    x,
                    blockY
                )
            )
            {

                IsGrounded =
                    true;

                return;

            }

        }

    }




    // =====================================================
    // COLLISION CHECK
    // =====================================================

    private bool CheckCollisionAt(
        float centerX,
        float centerY
    )
    {

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


        float minY =
            centerY -
            halfHeight +
            skin;


        float maxY =
            centerY +
            halfHeight -
            skin;


        int minBlockX =
            Mathf.FloorToInt(
                minX /
                blockSize
            );


        int maxBlockX =
            Mathf.FloorToInt(
                maxX /
                blockSize
            );


        int minBlockY =
            Mathf.FloorToInt(
                minY /
                blockSize
            );


        int maxBlockY =
            Mathf.FloorToInt(
                maxY /
                blockSize
            );


        for (
            int x = minBlockX;
            x <= maxBlockX;
            x++
        )
        {

            for (
                int y = minBlockY;
                y <= maxBlockY;
                y++
            )
            {

                if (
                    worldCollision.IsSolid(
                        x,
                        y
                    )
                )
                {

                    return true;

                }

            }

        }


        return false;

    }


    // =====================================================
    // RESOLVE OVERLAPS
    // =====================================================

    public void ResolveOverlaps()
    {

        if (
            worldCollision == null
        )
        {

            return;

        }


        const int maxIterations =
            100;


        for (
            int i = 0;
            i < maxIterations;
            i++
        )
        {

            bool isOverlapping =
                CheckCollisionAt(
                    transform.position.x,
                    transform.position.y
                );


            if (
                !isOverlapping
            )
            {

                break;

            }


            // Выталкиваем игрока вверх
            // маленькими шагами.

            transform.position +=
                Vector3.up *
                0.01f;

        }


        // После исправления позиции
        // заново проверяем землю.

        CheckGround();

    }


    // =====================================================
    // GET COLLIDER SIZE
    // =====================================================

    public Vector2 GetColliderSize()
    {

        return colliderSize;

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

